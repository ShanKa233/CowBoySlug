using System;
using System.Linq;
using Compatibility.Meadow;
using Compatibility.CatPunch;

namespace Compatibility
{
    public static class ModCompat_Helpers
    {
        // Rain Meadow
        public static bool IsModEnabled_RainMeadow => ModManager.ActiveMods.Any(x => x.id == "henpemaz_rainmeadow");

        public static bool RainMeadow_IsHost => !IsModEnabled_RainMeadow || Meadow.MeadowCompat.IsHost;
        public static bool RainMeadow_IsOnline => IsModEnabled_RainMeadow && Meadow.MeadowCompat.IsOnline;

        /// <summary>
        /// IsLocal 的签名安全包装:单机/未启用 Meadow 恒 true;联机时表示该实体由本机模拟。
        /// 不要直接用 RainMeadow 的 IsLocal 扩展——那个扩展所在类在 RainMeadow 程序集里,
        /// 调用方 JIT 时会触发程序集解析,未启用 Meadow 时直接崩。
        /// 本方法签名不含 RainMeadow 类型,方法体里的引用由执行期短路保护(惰性 JIT)。
        /// </summary>
        public static bool IsLocal(this PhysicalObject po)
        {
            return !IsModEnabled_RainMeadow || Meadow.MeadowCompat.IsMine(po.abstractPhysicalObject);
        }

        // CatPunchPunch
        public static bool IsModEnabled_CatPunchPunch => ModManager.ActiveMods.Any(x => x.id == "harvie.catpunchpunch");

        public static bool CatPunchPunch_IsEnabled => IsModEnabled_CatPunchPunch;


        public static void InitModCompat()
        {
            if (IsModEnabled_RainMeadow)
            {
                MeadowCompat.InitCompat();
            }

            if (IsModEnabled_CatPunchPunch)
            {
                CatPunchCompat.InitCompat();
            }
        }
    }
}