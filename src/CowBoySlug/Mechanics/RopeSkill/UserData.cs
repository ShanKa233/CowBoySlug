using System.Runtime.CompilerServices;
using Compatibility;
using RWCustom;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 每玩家的钩索模块:角色数据(绳子颜色/钩索惯性)+ 玩家侧钩子入口。
    /// 按键判定在 RopeControls,动作与查询在 Handler,调参常量在 RopeConfig。
    /// </summary>
    public class UserData
    {
        // 用于记录每个玩家的 RopeMaster 实例
        public static ConditionalWeakTable<Player, UserData> modules =
            new ConditionalWeakTable<Player, UserData>();

        // 能使用这个能力的词条
        public static readonly PlayerFeature<bool> RopeMasterFeature = PlayerBool(
            "cowboyslug/rope_master"
        );

        public static void Hook()
        {
            // 注册玩家构造函数的钩子
            On.Player.ctor += Player_ctor;

            // 注册扔矛事件的钩子
            On.Player.ThrownSpear += Player_ThrownSpear;

            // 注册抓取更新事件的钩子
            On.Player.GrabUpdate += BreakRopeUpdate;

            // 注册进食判定的钩子,拉矛期间不吃东西
            On.Player.CanEatMeat += Player_CanEatMeat;

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
            // 远端玩家对象由 Meadow 状态驱动,惯性越障只在本机模拟的玩家上执行
            if (!self.IsLocal())
                return;
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

            // 绳子下一段的方向(拉绳子位移的目标点,与 Handler.WhenSpearOnSomeThing 的 playerToRopeDir 一致)
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

        private static bool Player_CanEatMeat(
            On.Player.orig_CanEatMeat orig,
            Player self,
            Creature crit
        )
        {
            // 拉矛期间不吃东西(原版吃肉靠按住拾取,与召回入口按键冲突,直接拦判定)
            if (Handler.IsPullingRope(self))
                return false;
            return orig.Invoke(self, crit);
        }

        private static void BreakRopeUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            var player = self;
            // 检查玩家是否是牛仔猫并且按下了断绳组合(远端玩家对象输入是同步的,只有本机模拟的玩家才执行)
            if (player.IsCowBoys() && player.IsLocal() && RopeConfig.Controls.BreakRope(player))
            {
                var rope = Handler.NiceRope(player); // 获取与玩家连接的绳子

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

            // 远端玩家对象也会触发扔矛(输入同步),但绳矛只能由本机模拟的玩家创建
            if (!self.IsLocal())
                return;

            // 检查玩家是否有 RopeMaster 模块
            if (!modules.TryGetValue(self, out var mod))
                return;

            Handler.ThrowSpearWithRope(self, spear, mod.ropeColor);
        }

        private static void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
        {
            // 调用原始的更新 MSC 方法
            orig.Invoke(self);

            // 远端玩家对象的输入是同步的,召回/钓竿只能由本机模拟的玩家执行,否则双端重复操作矛
            if (!self.IsLocal())
                return;

            // 检查玩家是否有 RopeMaster 模块
            if (!modules.TryGetValue(self, out var module))
                return;

            // 钓竿模式独立入口:组合开启时,不用按住拾取也能拖拽生物
            if (RopeConfig.Controls.FishingStandalone)
            {
                Handler.FishSpear(self);
            }

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
    }
}
