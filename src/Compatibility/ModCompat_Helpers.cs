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