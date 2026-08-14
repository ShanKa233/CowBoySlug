using RWCustom;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 钩爪模式(让玩家移动):玩家被绳子拽向锚点,跳跃键触发。
    /// 三种锚点:飞行中的矛(高速甩出的矛反向拉扯) / 插墙或落地的矛 / 插在生物上的矛(距离较远时)。
    /// 由 Handler.WhenSpearOnSomeThing 按矛状态分发调用。
    /// </summary>
    public partial class Handler
    {
        #region 钩爪模式(让玩家移动)

        /// <summary>
        /// 飞行锚点:滑铲加速投出的矛在飞行中可以作为钩索锚点位移。
        /// 检测速度:飞行速度高于阈值才是锚点;钩索消耗矛的飞行速度,速度耗尽后矛静止,失去锚点能力。
        /// 返回 true 表示本帧已处理(拉人),打断召回流程。
        /// </summary>
        private static bool TryAirGrapple(
            Spear spear,
            Player player,
            float range,
            Simulator umbilical,
            Vector2 playerToRopeDir
        )
        {
            player.HandData().Pulling(10, umbilical, player.FreeHand(), 2f); // 钩爪伸手更远(距离倍率2)
            if (range > 10 && player.gravity > 0 && RopeConfig.Controls.GrapplePull(player))
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
                    player.bodyChunks[1].vel += playerToRopeDir * 3f * RopeConfig.PullForceFactor(range);
                    UserData.FillRopeMomentum(player);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 墙/落地锚点:按跳跃把玩家拽向绳子;矛还插在墙上时顺手走完拔矛流程。
        /// 返回 true 表示本帧已处理(拉人),打断召回流程。
        /// </summary>
        private static bool TryWallGrapple(
            Spear spear,
            Player player,
            float range,
            Simulator umbilical,
            Vector2 playerToRopeDir
        )
        {
            player.HandData().Pulling(10, umbilical, player.FreeHand(), 2f); // 钩爪伸手更远(距离倍率2)
            if (range > 10 && player.gravity > 0 && RopeConfig.Controls.GrapplePull(player))
            {
                player.circuitSwimResistance *= Mathf.InverseLerp(
                    player.mainBodyChunk.vel.magnitude + player.bodyChunks[1].vel.magnitude,
                    15f,
                    9f
                );
                // 距离越近拉升越弱,收矛距离一半处为0
                player.bodyChunks[1].vel += playerToRopeDir * 3f * RopeConfig.PullForceFactor(range);
                UserData.FillRopeMomentum(player);
                return true;
            }

            if (spear.mode == Weapon.Mode.StuckInWall)
            {
                // 取下矛
                PullSpearFromWall(spear);
            }
            return false;
        }

        /// <summary>
        /// 生物锚点:矛插在生物上且距离较远时,按跳跃把玩家拽向生物。
        /// 只施加力,不打断召回流程(组合1的空闲轻拽与钩爪同时生效)。
        /// </summary>
        private static void TryCreatureGrapple(
            Spear spear,
            Player player,
            float range,
            Simulator umbilical,
            Vector2 playerToRopeDir
        )
        {
            if (Custom.DistLess(player.mainBodyChunk.pos, spear.stuckInChunk.pos, 60))
                return;
            if (!RopeConfig.Controls.GrappleCreaturePull(player))
                return;

            // 钩爪模式:距离越近拉升越弱,收矛距离一半处为0
            player.bodyChunks[1].vel += playerToRopeDir * 3f * RopeConfig.PullForceFactor(range);
            UserData.FillRopeMomentum(player);
        }

        #endregion
    }
}
