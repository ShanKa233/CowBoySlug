using System;
using System.Linq;
using Compatibility.Meadow;

namespace Compatibility
{
    public static class ModCompat_Helpers
    {
        // Rain Meadow
        public static bool IsModEnabled_RainMeadow => ModManager.ActiveMods.Any(x => x.id == "henpemaz_rainmeadow");

        public static bool RainMeadow_IsHost => !IsModEnabled_RainMeadow || Meadow.MeadowCompat.IsHost;
        public static bool RainMeadow_IsOnline => IsModEnabled_RainMeadow && Meadow.MeadowCompat.IsOnline;


        public static void InitModCompat()
        {
            if (IsModEnabled_RainMeadow)
            {
                try
                {
                    MeadowCompat.InitCompat();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to initialize Rain Meadow compatibility: {e.Message}");
                }
            }
        }
    }
}