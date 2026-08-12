using System;
using CatPunchPunchDP;

namespace Compatibility.CatPunch
{
    /// <summary>
    /// CatPunchPunch 兼容性类
    /// 提供与 CatPunchPunch 模组的联动功能
    /// 当 CatPunchPunch 启用时，注册自定义的可击打物品类型
    /// </summary>
    public static class CatPunchCompat
    {
        /// <summary>
        /// 初始化兼容性功能
        /// 向 CatPunchPunch 注册自定义 PunchExtend（HatPunch）
        /// </summary>
        internal static void InitCompat()
        {
            try
            {
                // 通过 CatPunchPunch 的 RegisterPunch API 注册帽子击打
                PunchExtender.RegisterPunch(new HatPunch());
            }
            catch (Exception e)
            {
                // UnityEngine.Debug.LogError($"[CowBoySlug] CatPunchPunch 兼容初始化失败: {e}");
            }
        }
    }
}
