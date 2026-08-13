using RWCustom;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 钓竿模式(让附着物移动):拖拽被矛插住的生物。
    /// 两个入口:召回流程内(矛插生物时的分支 TryFishingCreature)和独立入口 FishSpear
    /// (组合开启 FishingStandalone 时不用按住拾取,由 UserData.Player_UpdateMSC 调用)。
    /// 变招:点按重拉(大力拽一下) / 长按轻拉(慢慢持续拉动);组合1还会自动轻拽(钓竿键没按也轻拉)。
    /// </summary>
    public partial class Handler
    {
        #region 钓竿模式(让附着物移动)

        /// <summary>
        /// 钓竿模式独立入口:单独按钓竿键(不用按住拾取)即可拖拽被矛插住的生物,不经过召回流程。
        /// 玩家已按住拾取(召回流程激活)时跳过,由 TryFishingCreature 处理,避免双重执行。
        /// </summary>
        public static void FishSpear(Player player)
        {
            // 召回流程激活时跳过,避免和 TryFishingCreature 重复执行
            if (RopeConfig.Controls.CallBackTrigger(player))
                return;

            // 吃东西/吐东西或没有空手时不拖,避免和原版抓取行为冲突
            if (player.eatMeat > 1 || player.eatExternalFoodSourceCounter > 1 || player.FreeHand() == -1)
                return;

            if (!RopeConfig.Controls.FishingPull(player))
                return;

            var umbilical = NiceRope(player);
            if (umbilical == null || umbilical.spear == null)
                return;

            var spear = umbilical.spear;
            if (spear.mode != Spear.Mode.StuckInCreature)
                return;

            player.HandData().Pulling(10, umbilical, player.FreeHand());
            if (RopeConfig.Controls.FishingHeavy(player))
            {
                DragCreatureOnSpear(player, spear, umbilical);
            }
            else
            {
                LightDragCreature(player, spear, umbilical);
            }
        }

        /// <summary>
        /// 召回流程内矛插生物时的钓竿分支:点按重拉/长按轻拉;
        /// 没按钓竿键时钩爪模式拽玩家(跳跃),组合1还会自动轻拽。
        /// </summary>
        private static void TryFishingCreature(
            Spear spear,
            Player player,
            float range,
            Simulator umbilical,
            Vector2 playerToRopeDir
        )
        {
            player.HandData().Pulling(10, umbilical, player.FreeHand());
            if (RopeConfig.Controls.FishingPull(player))
            {
                if (RopeConfig.Controls.FishingHeavy(player))
                {
                    DragCreatureOnSpear(player, spear, umbilical);
                }
                else
                {
                    LightDragCreature(player, spear, umbilical);
                }
                return;
            }

            // 钩爪模式:跳跃把玩家拽向生物(距离检查在方法内)
            TryCreatureGrapple(spear, player, range, umbilical, playerToRopeDir);

            // 组合1历史行为:钓竿键未按下时也自动轻拽(距离检查在方法内)
            if (RopeConfig.Controls.FishingLightWhenIdle)
            {
                LightDragCreature(player, spear, umbilical);
            }
        }

        /// <summary>
        /// 钓竿轻拉:长按钓竿键时慢慢持续拉动生物,力度小但每帧生效。
        /// 复用慢速收线(TrySlowRetrieve)的整套矛侧效果:防乱转+手部拉绳+矛加速朝向玩家,
        /// 矛插在生物上时这套动作就是"矛带着生物一起被拉向玩家";
        /// 另外让矛持续指向玩家,生物直接再受一点轻拉。
        /// 距离太近时不拉(生物已经在玩家脸上)。
        /// 调用前需保证 spear.mode == StuckInCreature。
        /// </summary>
        private static void LightDragCreature(Player player, Spear spear, Simulator umbilical)
        {
            if (Custom.DistLess(player.mainBodyChunk.pos, spear.stuckInChunk.pos, 60))
                return;

            Vector2 spearToEndPointDir = Custom.DirVec(
                spear.firstChunk.pos,
                umbilical.RopePos(umbilical.rope.TotalPositions - 2)
            );
            // 慢速收线同款效果:矛带着生物一起被拉向玩家
            TrySlowRetrieve(player, spear, umbilical, spearToEndPointDir);
            // 插生物矛的世界朝向由 stuckRotation(相对生物的角度)决定,持续指向玩家
            spear.stuckRotation = Custom.Angle(spearToEndPointDir, spear.stuckInChunk.Rotation);
            // 生物直接受一点轻拉
            spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel +=
                spearToEndPointDir * 3f;
        }

        /// <summary>
        /// 钓竿重拉:点按钓竿键时大力拽一下,生物被拉向矛的方向,玩家受到反向拉力。
        /// 调用前需保证 spear.mode == StuckInCreature。
        /// </summary>
        private static void DragCreatureOnSpear(Player player, Spear spear, Simulator umbilical)
        {
            var playerToRopeDir = Custom.DirVec(player.mainBodyChunk.pos, umbilical.RopeShowPos(1));
            Vector2 spearToEndPointDir = Custom.DirVec(
                spear.firstChunk.pos,
                umbilical.RopePos(umbilical.rope.TotalPositions - 2)
            );
            float range = Vector2.Distance(umbilical.spearEndPos, player.bodyChunks[1].pos);

            // 玩家受到反向拉力(生物越重拉力越大)
            float pullForce = Mathf.InverseLerp(1, 10, spear.stuckInObject.TotalMass / player.TotalMass);
            if (pullForce > 0)
            {
                UserData.FillRopeMomentum(player);
            }
            // 距离越近拉升越弱,收矛距离一半处为0
            player.bodyChunks[1].vel +=
                playerToRopeDir * pullForce * 20 * RopeConfig.PullForceFactor(range);
            // 矛持续朝向绳子方向(和轻拉一致,鱼钩指向钓鱼人)
            spear.stuckRotation = Custom.Angle(spearToEndPointDir, spear.stuckInChunk.Rotation);
            // 生物受拉:与玩家的反向拉力互补,生物越重越难拉动
            spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel +=
                spearToEndPointDir * (1f - pullForce) * 20;
        }

        #endregion
    }
}
