using System;
using System.Runtime.CompilerServices;
using RWCustom;
using SlugBase.DataTypes;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;

namespace CowBoySlug.Mechanics.RopeSkill
{
    public class UserData
    {
        // 用于记录每个玩家的 RopeMaster 实例
        public static ConditionalWeakTable<Player, UserData> modules =
            new ConditionalWeakTable<Player, UserData>();

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
           
           Handler.ThrowSpearWithRope(self, spear, mod.ropeColor);
        }

        private static void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
        {
            // 调用原始的更新 MSC 方法
            orig.Invoke(self);

            // 检查玩家是否有 RopeMaster 模块
            if (!modules.TryGetValue(self, out var module))
                return;

            // 召回矛
            Handler.CallBackSpear(self);
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
                modules.Add(self, new UserData(self));
            }
        }

        // 玩家实例
        public Player player;

        // 绳子颜色
        public Color ropeColor = new Color(247 / 255f, 213 / 255f, 131 / 255f);

        public UserData(Player player)
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
        public static Simulator NiceRope(Player player)
        {
            if (!UserData.modules.TryGetValue(player, out var mod))
                return null; // 如果没有找到 RopeMaster 模块

            Simulator umbilical = null;

            // 搜索房间里面的所有矛找一根合适的出来
            foreach (var obj in player.room.updateList)
            {
                Simulator testUmbilical = null;
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
            Simulator umbilical
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

    }
}
