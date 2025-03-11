using System;
using CowBoySlug.CowBoy.Ability.RopeUse;
using RainMeadow;
using src.Compatibility.Meadow;
using UnityEngine;

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
            // 注册在线资源可用时的回调和SlugcatStats构造函数的钩子
            OnlineResource.OnAvailable += OnlineResourceOnOnAvailable;
        }
        private static void OnlineResourceOnOnAvailable(OnlineResource resource)
        {
        }

        internal static void CreateRopeSpear(Player player,Spear spear,Color start,Color end)
        {

            var playerOpo = player.abstractPhysicalObject.GetOnlineObject();
            var spearOpo = spear.abstractPhysicalObject.GetOnlineObject();
            if (playerOpo is null||spearOpo is null)
            {
                return;
            }

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe)
                {
                    continue;
                }



                // 修复反射调用，确保正确传递参数和类型
                // 使用正确的委托类型，并添加颜色参数
                onlinePlayer.InvokeRPC(
                    typeof(src.Compatibility.Meadow.MeadowRPCs).GetMethod(nameof(src.Compatibility.Meadow.MeadowRPCs.CreateRopeSpear))!
                    .CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject, Color, Color>)), 
                    playerOpo, 
                    spearOpo,
                    start,
                    end);
            }

        }
    }
}