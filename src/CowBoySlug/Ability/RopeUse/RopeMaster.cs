using System;
using System.Runtime.CompilerServices;
using CowBoySLug;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase.DataTypes;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;

namespace CowBoySlug.CowBoy.Ability.RopeUse
{
    public class RopeMaster
    {
        // 用于记录每个玩家的 RopeMaster 实例
        public static ConditionalWeakTable<Player, RopeMaster> modules =
            new ConditionalWeakTable<Player, RopeMaster>();

        /// <summary>
        /// 获取玩家的RopeMaster数据
        /// </summary>
        public static RopeMaster GetRopeMasterData(Player player)
        {
            if (modules.TryGetValue(player, out var ropeMaster))
            {
                return ropeMaster;
            }
            return null;
        }

        public static void Hook()
        {
            // 注册玩家构造函数的钩子
            On.Player.ctor += Player_ctor;

            // 注册扔矛事件的钩子
            On.Player.ThrownSpear += Player_ThrownSpear;

            // 注册抓取更新事件的钩子
            On.Player.GrabUpdate += BreakRopeUpdate;

            // 注册更新 MSC 事件的钩子
            On.Player.UpdateMSC += Player_UpdateMSC;
        }

        private static void Player_ThrownSpear(
            On.Player.orig_ThrownSpear orig,
            Player self,
            Spear spear
        )
        {
            // 调用原始的扔矛方法
            orig.Invoke(self, spear);

            // 检查玩家是否有 RopeMaster 模块
            if (!modules.TryGetValue(self, out var mod))
                return;

            // 增加回收的冷却时间
            spear.vibrate += 2;

            // 如果矛有绳子，销毁它
            if (spear.rope().rope != null)
            {
                spear.rope().rope.Destroy();
            }

            // 使用静态方法生成新的绳子
            CowRope.SpawnRope(
                self,
                spear,
                Color.Lerp(self.ShortCutColor(), mod.ropeColor, 0.5f),
                mod.ropeColor
            );

            // 更新当前绳索 - 需要在生成后重新获取
            mod.currentRope = spear.rope().rope;
        }

        private static void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
        {
            // 调用原始的更新 MSC 方法
            orig.Invoke(self);

            // 检查玩家是否有 RopeMaster 模块
            if (!modules.TryGetValue(self, out var module))
                return;

            // 召回矛
            CallBackSpear(self);
        }

        private static void Player_ctor(
            On.Player.orig_ctor orig,
            Player self,
            AbstractCreature abstractCreature,
            World world
        )
        {
            // 调用原始的构造函数
            orig.Invoke(self, abstractCreature, world);

            try
            {
                // 检查玩家是否有 RopeMaster 特性
                bool shouldHaveRopeMaster = false;

                // 检查是否是牛仔猫
                if (self.slugcatStats.name == CowBoyModule.Name)
                {
                    shouldHaveRopeMaster = true;
                }
                // 检查是否有RopeMaster特性
                else if (Plugin.RopeMasterFeature.TryGet(self, out var flag) && flag)
                {
                    shouldHaveRopeMaster = true;
                }

                if (shouldHaveRopeMaster)
                {
                    // 为玩家添加 RopeMaster 模块
                    modules.Add(self, new RopeMaster(self));
                    UnityEngine.Debug.Log($"[CowBoySlug] 为玩家{self.abstractCreature.ID}添加RopeMaster模块");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CowBoySlug] 添加RopeMaster模块时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // 玩家实例
        public Player player;

        // 绳子颜色
        public Color ropeColor = new Color(247 / 255f, 213 / 255f, 131 / 255f);

        // 当前的绳索
        private CowRope currentRope;

        /// <summary>
        /// 是否有绳索
        /// </summary>
        public bool HaveRope => currentRope != null && !currentRope.slatedForDeletetion;

        /// <summary>
        /// 部署绳索
        /// </summary>
        public void DeployRope()
        {
            if (HaveRope) return;

            // 查找玩家持有的矛
            Spear spear = null;
            for (int i = 0; i < player.grasps.Length; i++)
            {
                if (player.grasps[i]?.grabbed is Spear s)
                {
                    spear = s;
                    break;
                }
            }

            // 如果没有找到矛，尝试在房间中查找
            if (spear == null && player.room != null)
            {
                foreach (var obj in player.room.physicalObjects)
                {
                    foreach (var item in obj)
                    {
                        if (item is Spear s && Vector2.Distance(player.firstChunk.pos, s.firstChunk.pos) < 100f)
                        {
                            spear = s;
                            break;
                        }
                    }
                    if (spear != null) break;
                }
            }

            // 如果找到了矛，创建绳索
            if (spear != null)
            {
                currentRope = new CowRope(player, spear, ropeColor, Color.white);
                player.room.AddObject(currentRope);
            }
        }

        /// <summary>
        /// 收回绳索
        /// </summary>
        public void RetractRope()
        {
            if (!HaveRope) return;

            currentRope.Destroy();
            currentRope = null;
        }

        public RopeMaster(Player player)
        {
            this.player = player;
        }

        // 检查玩家是否不能召回矛
        public static bool CanNotCall(Player player)
        {
            bool flag = (player.input[0].pckp || player.input[1].pckp); // 如果玩家在两帧内按下过拿取键
            bool flag2 = player.eatMeat <= 1 && player.eatExternalFoodSourceCounter <= 1; // 玩家没有在吃东西
            bool flag3 = player.input[0].y >= 0 && player.FreeHand() != -1; // 玩家没有按下而且有一只空手
            return !(flag && flag2 && flag3);
        }

        // 获取与玩家连接的绳子
        public static CowRope NiceRope(Player player)
        {
            if (!RopeMaster.modules.TryGetValue(player, out var mod))
                return null; // 如果没有找到 RopeMaster 模块

            CowRope umbilical = null;

            // 搜索房间里面的所有矛找一根合适的出来
            foreach (var obj in player.room.updateList)
            {
                CowRope testUmbilical = null;
                var spear = obj as Spear;

                // 检查矛是否有绳子
                if (spear != null && spear.rope().IsRopeSpear)
                {
                    testUmbilical = spear.rope().rope;
                }

                // 检查绳子是否符合条件
                if (
                    !(
                        testUmbilical != null
                        && testUmbilical.spear != null
                        && testUmbilical.player == player
                    )
                )
                    continue;

                // 检查绳子是否被抓住
                if (
                    testUmbilical.spear.grabbedBy.Count > 0
                    && testUmbilical.spear.grabbedBy[0].grabber == player
                )
                    continue;

                // 检查绳子是否有限制
                if (testUmbilical.limited)
                    continue;

                umbilical = testUmbilical;
                break;
            }

            return umbilical;
        }

        // 召回矛的方法
        private static void CallBackSpear(Player player)
        {
            if (CanNotCall(player))
                return;

            var umbilical = NiceRope(player);
            if (umbilical == null)
                return;

            // 如果玩家按下了拿取键，召回矛
            if (player.input[0].pckp && !player.input[1].pckp)
            {
                umbilical.used = true;
            }
        }

        // 更新绳子断裂的方法
        private static void BreakRopeUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            if (!modules.TryGetValue(self, out var module))
                return;

            // 如果玩家按下了跳跃键，断开绳子
            if (self.input[0].jmp && !self.input[1].jmp)
            {
                var umbilical = NiceRope(self);
                if (umbilical != null)
                {
                    umbilical.limited = true;
                }
            }
        }
    }
}
