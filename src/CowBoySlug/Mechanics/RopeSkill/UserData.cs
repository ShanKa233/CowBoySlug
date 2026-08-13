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

            // 注册玩家更新事件的钩子,维护钩索惯性计数器
            On.Player.Update += Player_Update;

            // 钩索惯性期间让原版地面摩擦不生效的 ILHook
            FrictionIL.Hook();
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            // 更新钩索惯性计数器
            UpdateRopeMomentum(self);
            orig.Invoke(self, eu);
            // 惯性期间贴墙时抬升,越过一格高的障碍
            StepOverObstacle(self);
        }

        /// <summary>
        /// 钩索惯性期间,被绳子拖着撞上一格台阶时,朝绳子的目标点方向注入动量帮助越过障碍
        /// 模仿原版滑铲(bellySlide)每帧正弦冲量的构造(Player.cs 8350)
        /// </summary>
        private static void StepOverObstacle(Player player)
        {
            if (!modules.TryGetValue(player, out var module) || module.ropeMomentum <= 0)
                return;
            var rope = player.HandData().pullinggRope;
            if (rope == null || rope.player != player)
                return;

            // 绳子下一段的方向(拉绳子位移的目标点,与 WhenSpearOnSomeThing 的 playerToRopeDir 一致)
            if (Custom.DistLess(player.mainBodyChunk.pos, rope.RopeShowPos(1), 0.5f))
                return;
            Vector2 pullDir = Custom.DirVec(player.mainBodyChunk.pos, rope.RopeShowPos(1));
            // 玩家沿目标方向的速度投影(趋近0说明被障碍挡住了)
            float speedAlongPull = Vector2.Dot(player.mainBodyChunk.vel, pullDir);

            for (int i = 0; i < 2; i++)
            {
                var chunk = player.bodyChunks[i];
                if (chunk.ContactPoint.x == 0)
                    continue;
                // 只有绳子的目标点在墙后(被拖着撞墙,而不是自己走向墙)才越障
                if (pullDir.x * chunk.ContactPoint.x < 0.2f)
                    continue;
                // 贴着的墙那一格:实心且上方一格是空气(一格高的台阶)
                var wallTile =
                    player.room.GetTilePosition(chunk.pos) + new IntVector2(chunk.ContactPoint.x, 0);
                if (
                    !player.room.GetTile(wallTile).Solid
                    || player.room.GetTile(wallTile + new IntVector2(0, 1)).Solid
                )
                    continue;
                // 身体块还没越过墙顶且移动受阻时,朝目标点方向注入动量+向上抬升
                float wallTop = (wallTile.y + 1) * 20f;
                if (chunk.pos.y < wallTop + chunk.rad && speedAlongPull < 2f)
                {
                    chunk.vel += pullDir * 2f + new Vector2(0f, 3f);
                }
            }
        }

        private static void BreakRopeUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            var player = self;
            // 检查玩家是否是牛仔猫并且按下了断绳组合
            if (player.IsCowBoys() && Controls.BreakRope(player))
            {
                var rope = NiceRope(player); // 获取与玩家连接的绳子

                if (rope != null)
                {
                    Handler.HandleRopeBreaking(player, rope.spear);
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

        // 钩索惯性:拉绳子时充满,不拉时慢慢消散
        public int ropeMomentum = 0;
        // 钩索惯性的最大值
        public const int RopeMomentumMax = 5;

        /// <summary>
        /// 当前生效的按键组合(调试时改这里切换,如 new RopeControlsV2())
        /// </summary>
        public static RopeControls Controls = new RopeControlsV1();

        public UserData(Player player)
        {
            this.player = player;
        }

        /// <summary>
        /// 更新钩索惯性计数器:不拉时递减
        /// </summary>
        public static void UpdateRopeMomentum(Player player)
        {
            if (!modules.TryGetValue(player, out var module))
                return;
            if (module.ropeMomentum > 0)
            {
                module.ropeMomentum--;
            }
        }

        /// <summary>
        /// 玩家被绳子拉动时充满钩索惯性
        /// </summary>
        public static void FillRopeMomentum(Player player)
        {
            if (modules.TryGetValue(player, out var module))
            {
                module.ropeMomentum = RopeMomentumMax;
            }
        }

        // 收矛距离(与 Handler.CallBackSpear_Local 的收矛判定共用)
        public const float PickUpRange = 80f;

        /// <summary>
        /// 钩索拉升力系数:距离越近越弱,收矛距离的一半处为0,收矛距离处为满值
        /// </summary>
        public static float PullForceFactor(float range)
        {
            return Mathf.InverseLerp(PickUpRange / 2f, PickUpRange, range);
        }

        /// <summary>
        /// 供 ILHook 调用:判断物理对象的主人是否处于钩索惯性状态
        /// 有惯性时原版的地面/表面摩擦不应该生效
        /// </summary>
        public static bool OwnerHasRopeMomentum(PhysicalObject owner)
        {
            return owner is Player player
                && modules.TryGetValue(player, out var module)
                && module.ropeMomentum > 0;
        }

        // 检查玩家是否不能召回矛
        public static bool CanNotCall(Player player)
        {
            bool canCall = Controls.CallBackTrigger(player); // 按键组合条件
            bool notEating = player.eatMeat <= 1 && player.eatExternalFoodSourceCounter <= 1; // 玩家没有在吃东西
            bool handFree = player.FreeHand() != -1; // 有一只空手
            return !(canCall && notEating && handFree);
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

            // 滑铲加速投出的矛在飞行中可以作为钩索锚点位移
            // 检测速度:飞行速度高于阈值才是锚点;钩索消耗矛的飞行速度,速度耗尽后矛静止,失去锚点能力
            if (
                spear.mode == Weapon.Mode.Thrown
                && spear.firstChunk.vel.magnitude > RopeData.HookEnergySpeedThreshold
            )
            {
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                if (range > 10 && player.gravity > 0 && Controls.WallJumpPull(player))
                {
                    // 拉绳方向与矛的飞行方向至少差90度(反向拉扯)才提供位移
                    Vector2 spearFlyDir = spear.firstChunk.vel.normalized;
                    if (Vector2.Dot(playerToRopeDir, spearFlyDir) < 0f)
                    {
                        // 像普通拉取模式一样给玩家向量速度,矛按原版自然减速,
                        // 速度降到阈值以下后矛静止,自然失去锚点能力
                        player.circuitSwimResistance *= Mathf.InverseLerp(
                            player.mainBodyChunk.vel.magnitude + player.bodyChunks[1].vel.magnitude,
                            15f,
                            9f
                        );
                        player.bodyChunks[1].vel += playerToRopeDir * 3f * PullForceFactor(range);
                        FillRopeMomentum(player);
                        return true;
                    }
                }
                return false;
            }

            // 如果插到墙上就拔下来然后变成自由状态
            if (
                (spear.hasHorizontalBeamState && spear.mode == Weapon.Mode.StuckInWall)
                || (!spear.spinning && spear.mode == Weapon.Mode.Free)
            )
            {
                // 爬墙
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                if (range > 10 && player.gravity > 0 && Controls.WallJumpPull(player))
                {
                    player.circuitSwimResistance *= Mathf.InverseLerp(
                        player.mainBodyChunk.vel.magnitude + player.bodyChunks[1].vel.magnitude,
                        15f,
                        9f
                    );
                    // 距离越近拉升越弱,收矛距离一半处为0
                    player.bodyChunks[1].vel += playerToRopeDir * 3f * PullForceFactor(range);
                    FillRopeMomentum(player);
                    return true;
                }

                if (spear.mode == Weapon.Mode.StuckInWall)
                {
                    // 取下矛
                    Handler.PullSpearFromWall(spear);
                }
            }
            // 如果插到了生物就拖动他
            else if (spear.mode == Spear.Mode.StuckInCreature)
            {
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                if (Controls.DragCreature(player))
                {
                    // 玩家受到拉力(生物越重拉力越小)
                    float pullForce = Mathf.InverseLerp(
                        1,
                        10,
                        spear.stuckInObject.TotalMass / player.TotalMass
                    );
                    if (pullForce > 0)
                    {
                        FillRopeMomentum(player);
                    }
                    // 距离越近拉升越弱,收矛距离一半处为0
                    player.bodyChunks[1].vel +=
                        playerToRopeDir * pullForce * 20 * PullForceFactor(range);
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
                    if (Controls.CreatureJumpPull(player))
                    {
                        // 距离越近拉升越弱,收矛距离一半处为0
                        player.bodyChunks[1].vel += playerToRopeDir * 3f * PullForceFactor(range);
                        FillRopeMomentum(player);
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
