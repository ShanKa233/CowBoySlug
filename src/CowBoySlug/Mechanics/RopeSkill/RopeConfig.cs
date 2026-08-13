using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 钩索技能的全局调参常量与按键组合入口。
    /// 调试数值时优先来这里找。
    /// </summary>
    public static class RopeConfig
    {
        /// <summary>
        /// 当前生效的按键组合(调试时改这里切换,如 new RopeControlsV2())
        /// </summary>
        public static RopeControls Controls = new RopeControlsV1();

        /// <summary>
        /// 收矛距离(与 Handler.CallBackSpear_Local 的收矛判定共用)
        /// </summary>
        public const float PickUpRange = 80f;

        /// <summary>
        /// 钩索拉升力系数:距离越近越弱,收矛距离的一半处为0,收矛距离处为满值
        /// </summary>
        public static float PullForceFactor(float range)
        {
            return Mathf.InverseLerp(PickUpRange / 2f, PickUpRange, range);
        }

        /// <summary>
        /// 飞行锚点速度阈值:滑铲加速投出的矛(+15)飞行速度显著高于普通投掷,
        /// 飞行中速度高于此值的矛可以作为钩索锚点位移,钩索会消耗矛的飞行速度
        /// </summary>
        public const float HookEnergySpeedThreshold = 28f;
    }
}
