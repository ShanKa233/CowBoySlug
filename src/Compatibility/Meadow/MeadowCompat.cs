using System;
using RainMeadow;

namespace Compatibility.Meadow
{
    public static class MeadowCompat
    {

        public static bool IsHost => !IsOnline || OnlineManager.lobby.isOwner;

        public static bool IsOnline => OnlineManager.lobby is not null;

        public static bool IsOnlineFriendlyFire => RainMeadow.RainMeadow.isStoryMode(out var story) && story.friendlyFire;
        //   public static bool RainMeadow_IsMine(AbstractPhysicalObject obj)
        //     {
        //         return !RainMeadow_IsOnline || MeadowCompat.IsMine(obj);
        //     }

        //     public static bool RainMeadow_IsPosSynced(AbstractPhysicalObject obj)
        //     {
        //         return RainMeadow_IsOnline && MeadowCompat.IsPosSynced(obj);
        //     }

        //     public static int? RainMeadow_GetOwnerIdOrNull(AbstractPhysicalObject obj)
        //     {
        //         return RainMeadow_IsOnline ? MeadowCompat.GetOwnerId(obj) : null;
        //     }

        internal static void InitCompat()
        {
        }
    }
}