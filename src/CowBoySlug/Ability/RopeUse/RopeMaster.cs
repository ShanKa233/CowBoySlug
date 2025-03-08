using System;
using System.Runtime.CompilerServices;
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

        // 能使用这个能力的词条
        public static readonly PlayerFeature<bool> RopeMasterFeature = PlayerBool(
            "cowboyslug/rope_master"
        );

        // 绳子颜色
        public static readonly PlayerColor RopeColor = new PlayerColor("Rope");

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

        private static void BreakRopeUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            var player = self;
            // 检查玩家是否是牛仔猫并且按下了抓取键
            if (
                player.IsCowBoys()
                && player.input[0].y < 0 // 长按下和拾取
                && player.input[0].pckp
            )
            {
                var rope = NiceRope(player); // 获取与玩家连接的绳子

                if (rope != null)
                {
                    // 增加绳子的断裂计数
                    rope.spear.rope().brokenCount += 10;

                    // 播放声音和生成火花效果
                    if (rope.spear.rope().brokenCount > 30)
                    {
                        player.room.PlaySound(
                            SoundID.Miros_Beak_Snap_Hit_Other,
                            player.firstChunk,
                            false,
                            0.5f,
                            0.2f
                        );

                        for (int n = 2; n > 0; n--)
                        {
                            player.room.AddObject(
                                new Spark(
                                    player.firstChunk.pos,
                                    Custom.RNV(),
                                    Color.white,
                                    null,
                                    10,
                                    20
                                )
                            );
                        }
                    }
                }
            }
            // 调用原始的抓取更新方法
            orig.Invoke(self, eu);
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

            // 生成新的绳子
            CowRope rope = new CowRope(
                self,
                spear,
                Color.Lerp(self.ShortCutColor(), mod.ropeColor, 0.5f),
                mod.ropeColor
            );
            self.room.AddObject(rope);
            
            // 更新当前绳索
            mod.currentRope = rope;
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

            // 检查玩家是否有 RopeMaster 特性
            if (RopeMasterFeature.TryGet(self, out var flag) && flag)
            {
                // 为玩家添加 RopeMaster 模块
                modules.Add(self, new RopeMaster(self));
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

                // 循环检查绳子的宽度
                for (int i = 0; i < testUmbilical.points.GetLength(0); i++)
                {
                    if (testUmbilical.points[i, 3].x <= 0f)
                    {
                        continue;
                    }
                }

                // 如果找到合适的绳子
                if (umbilical == null)
                {
                    umbilical = testUmbilical;
                }
                else
                {
                    // 比较两个绳子的距离
                    bool b =
                        Math.Abs(umbilical.spear.firstChunk.pos.x - player.mainBodyChunk.pos.x)
                        > Math.Abs(
                            testUmbilical.spear.firstChunk.pos.x - player.mainBodyChunk.pos.x
                        );
                    umbilical = b ? testUmbilical : umbilical;
                }
            }

            return umbilical;
        }

        // 检查矛是否在某个物体上
        public static bool WhenSpearOnSomeThing(
            Spear spear,
            Player player,
            float range,
            CowRope umbilical
        )
        {
            var playerToRopeDir = Custom.DirVec(player.mainBodyChunk.pos, umbilical.RopeShowPos(1));
            Vector2 spearToEndPointDir = Custom.DirVec(
                spear.firstChunk.pos,
                umbilical.RopePos(umbilical.rope.TotalPositions - 2)
            );

            // 如果插到墙上就拔下来然后变成自由状态
            if (
                (spear.hasHorizontalBeamState && spear.mode == Weapon.Mode.StuckInWall)
                || (!spear.spinning && spear.mode == Weapon.Mode.Free)
            )
            {
                // 爬墙
                int canGrab = 0;
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                for (int i = 0; i < 10; i++)
                {
                    if (player.input[i].jmp || player.input[0].jmp)
                    {
                        canGrab++;
                    }
                }
                if (range > 10 && player.gravity > 0 && canGrab > 2)
                {
                    player.circuitSwimResistance *= Mathf.InverseLerp(
                        player.mainBodyChunk.vel.magnitude + player.bodyChunks[1].vel.magnitude,
                        15f,
                        9f
                    );
                    player.bodyChunks[1].vel += playerToRopeDir * 3f;
                    return true;
                }

                if (spear.mode == Weapon.Mode.StuckInWall)
                {
                    // 取下矛
                    spear.resetHorizontalBeamState();
                    spear.stuckInWall = new Vector2?(default(Vector2));
                    spear.vibrate = 10;
                    spear.firstChunk.collideWithTerrain = true;
                    spear.abstractSpear.stuckInWallCycles = 0;
                    spear.ChangeMode(Spear.Mode.Free);
                }
            }
            // 如果插到了生物就拖动他
            else if (spear.mode == Spear.Mode.StuckInCreature)
            {
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                if (player.wantToPickUp > 0)
                {
                    // 玩家受到拉力
                    player.bodyChunks[1].vel +=
                        playerToRopeDir
                        * Mathf.InverseLerp(
                            1,
                            10,
                            (spear.stuckInObject.TotalMass / player.TotalMass)
                        )
                        * 20;
                    spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel +=
                        spearToEndPointDir
                        * Mathf.InverseLerp(
                            1,
                            10,
                            (player.TotalMass / spear.stuckInObject.TotalMass)
                        )
                        * 20;
                }
                else if (!Custom.DistLess(player.mainBodyChunk.pos, spear.stuckInChunk.pos, 60))
                {
                    if (player.input[0].jmp)
                    {
                        player.bodyChunks[1].vel += playerToRopeDir * 3f;
                    }
                    spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel +=
                        spearToEndPointDir * 3f;
                }
            }
            // 对拿着这个矛的生物操作
            else if (
                spear.grabbedBy.Count > 0
                && spear.grabbedBy[0] != null
                && spear.grabbedBy[0].grabber != player
                && spear.grabbedBy[0].grabber != null
            )
            {
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                spear.grabbedBy[0].Release();
            }
            return false;
        } // 当矛插在什么东西上或被什么东西带着

        // 召回矛
        public static void CallBackSpear(Player player)
        {
            if (CanNotCall(player))
                return;

            var umbilical = NiceRope(player); // 找到一个好线
            if (umbilical == null)
                return;

            // 检查矛是否可以用
            if (
                !(
                    umbilical.spear != null
                    && player.room == umbilical.spear.room
                    && umbilical.spear.vibrate <= 0
                )
            )
                return;

            var spear = umbilical.spear;

            // 是否做出快速唤回动作
            bool flagFastBackAction = player.input[0].y > 0;
            // 检查能不能直视到
            bool flagSee = player.room.VisualContact(spear.firstChunk.pos, player.firstChunk.pos);
            // 检查距离
            var range = Vector2.Distance(umbilical.spearEndPos, player.bodyChunks[1].pos);

            Vector2 spearToEndPointDir = Custom.DirVec(
                spear.firstChunk.pos,
                umbilical.RopePos(umbilical.rope.TotalPositions - 2)
            );

            // 离矛最近的丝的方向
            Vector2 playerToFristPoint = Custom.DirVec(umbilical.playerPos, umbilical.RopePos(1));

            umbilical.used = true;
            if (WhenSpearOnSomeThing(spear, player, range, umbilical))
                return;

            // 防止吃东西 吐东西
            if (spear.mode != Weapon.Mode.Carried)
            {
                player.swallowAndRegurgitateCounter = 0;
                player.slugOnBack.counter = 0;
            }

            // 在无重力情况下给玩家施加移动力
            if (spear.mode != Weapon.Mode.Carried && player.gravity <= 0)
            {
                player.mainBodyChunk.vel -= spearToEndPointDir / 2;
            }

            // 如果玩家离矛很近而且可以直视矛而且按了拿取按键就拿起矛
            if (range < 80 && flagSee && spear.mode != Weapon.Mode.Carried)
            {
                if (player.FreeHand() != -1)
                {
                    player.SlugcatGrab(spear, player.FreeHand());
                    player.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, spear.firstChunk);
                    spear.canBeHitByWeapons = true; // 让矛可以挡下攻击
                }
            }
            // 回收矛模式
            else if (flagFastBackAction && range > 50)
            {
                // 拉绳子手部动作
                player.HandData().Pulling(15, umbilical, player.FreeHand());

                umbilical.loose = 1;

                spear.ChangeMode(Weapon.Mode.Free);

                spear.firstChunk.vel = spearToEndPointDir * 27 + Custom.RNV();
                spear.setRotation = -spearToEndPointDir.normalized;

                if (spear.gravity > 0)
                {
                    spear.firstChunk.vel.y += 10;
                }
            }
            // 攻击模式
            else if (player.input[1].pckp && !player.input[0].pckp && range > 35)
            {
                int pckpTime = 0;
                for (int i = 0; i < 7; i++)
                {
                    if (player.input[i].pckp)
                    {
                        pckpTime++;
                    }
                }
                if (pckpTime > 5)
                {
                    return;
                }

                // 控制手和绳子
                player.HandData().Pulling(20, umbilical, player.FreeHand());
                spear.ChangeMode(Weapon.Mode.Thrown);
                spear.spearDamageBonus *= 0.9f;
                spear.thrownBy = player;
                spear.throwDir = new IntVector2(
                    Convert.ToInt32(spearToEndPointDir.x),
                    Convert.ToInt32(spearToEndPointDir.y)
                );

                spear.rotation = spear.throwDir.ToVector2();
                spear.firstChunk.pos -= spearToEndPointDir;
                spear.firstChunk.vel += spear.throwDir.ToVector2() * 50 * spear.spearDamageBonus;
            }
            // 慢速模式
            else if (player.input[0].pckp)
            {
                spear.rope().cantRotationCount += 3;
                // 控制手和绳子
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                spear.firstChunk.vel += spearToEndPointDir * 2f + Custom.RNV() * 0.2f;

                spear.setRotation = -spearToEndPointDir.normalized;
            }
            else if (spear.mode == Weapon.Mode.StuckInCreature)
            {
                spear.ChangeMode(Weapon.Mode.Free);
            }
        } // 唤回矛
    }
}
