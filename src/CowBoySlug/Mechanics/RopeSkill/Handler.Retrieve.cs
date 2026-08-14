using RWCustom;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 回收模式(让矛回来):拿取 / 快速唤回(矛飞回来) / 慢速收线(矛慢慢靠近)。
    /// 按键见 RopeControls;只做本地动作,由 Handler.CallBackSpear_Local 分发调用。
    /// </summary>
    public partial class Handler
    {
        #region 回收模式(让矛回来)

        /// <summary>
        /// 回收模式-拿取:玩家离矛很近而且可以直视矛时捡起矛。
        /// 调用处的距离/视线/模式判定已做完,这里只做捡起动作(空手是防御性检查)。
        /// </summary>
        private static void PickUpSpear(Player player, Spear spear)
        {
            if (player.FreeHand() == -1)
                return;

            player.SlugcatGrab(spear, player.FreeHand());
            player.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, spear.firstChunk);
            spear.canBeHitByWeapons = true; // 让矛可以挡下攻击
        }

        /// <summary>
        /// 回收模式-快速唤回:矛飞回玩家。拉绳子手部动作+绳子弹力拉满+矛朝玩家加速。
        /// 调用处的按键/距离判定已做完,这里只做动作。
        /// </summary>
        private static void FastRetrieve(
            Player player,
            Spear spear,
            Simulator umbilical,
            Vector2 spearToEndPointDir
        )
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

        /// <summary>
        /// 回收模式-慢速收线:矛慢慢靠近玩家。防乱转+矛朝向绳子+轻微加速。
        /// 调用处的按键判定已做完,这里只做动作。
        /// 矛插在生物上时,这套动作就是"矛带着生物一起移动"(钓竿效果的一部分)。
        /// </summary>
        private static void SlowRetrieve(
            Player player,
            Spear spear,
            Simulator umbilical,
            Vector2 spearToEndPointDir
        )
        {
            spear.rope().cantRotationCount += 3;
            // 控制手和绳子
            player.HandData().Pulling(10, umbilical, player.FreeHand());
            spear.firstChunk.vel += spearToEndPointDir * 2f + Custom.RNV() * 0.2f;

            spear.setRotation = -spearToEndPointDir.normalized;
        }

        #endregion
    }
}
