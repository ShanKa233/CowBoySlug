using System;
using RainMeadow;
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
            //暂时没什么需要初始化的
            OnlineResource.OnAvailable += OnlineResourceOnOnAvailable;
        }
        private static void OnlineResourceOnOnAvailable(OnlineResource resource)
        {
        }

        internal static void CreateRopeSpear(Player player, Spear spear, Color start, Color end)
        {

            var playerOpo = player.abstractPhysicalObject.GetOnlineObject();
            var spearOpo = spear.abstractPhysicalObject.GetOnlineObject();
            if (playerOpo is null || spearOpo is null)
            {
                return;
            }

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe)
                {
                    continue;
                }



                //用反射来调用RPC方法
                onlinePlayer.InvokeRPC(
                    typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.CreateRopeSpear))!
                    .CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject, Color, Color>)),
                    playerOpo,
                    spearOpo,
                    start,
                    end);
            }

        }

        /// <summary>
        /// 处理扔矛时生成绳子的网络同步
        /// </summary>
        /// <param name="player">扔矛的玩家</param>
        /// <param name="spear">被扔出的矛</param>
        /// <param name="ropeColor">绳子的颜色</param>
        internal static void ThrowSpearWithRope(Player player, Spear spear, Color ropeColor)
        {
            var playerOpo = player.abstractPhysicalObject.GetOnlineObject();
            var spearOpo = spear.abstractPhysicalObject.GetOnlineObject();
            if (playerOpo is null || spearOpo is null)
            {
                return;
            }

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe)
                {
                    continue;
                }

                // 用反射来调用RPC方法
                onlinePlayer.InvokeRPC(
                    typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.ThrowSpearWithRope))!
                    .CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject, Color>)),
                    playerOpo,
                    spearOpo,
                    ropeColor);
            }
        }

        /// <summary>
        /// 处理召回矛的网络同步
        /// </summary>
        /// <param name="player">召回矛的玩家</param>
        internal static void CallBackSpear(Player player)
        {
            var playerOpo = player.abstractPhysicalObject.GetOnlineObject();
            if (playerOpo is null)
            {
                return;
            }

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe)
                {
                    continue;
                }

                // 用反射来调用RPC方法
                onlinePlayer.InvokeRPC(
                    typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.CallBackSpear))!
                    .CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject>)),
                    playerOpo);
            }
        }

        /// <summary>
        /// 处理超级射击的网络同步
        /// </summary>
        /// <param name="player">射击的玩家</param>
        /// <param name="rock">被射击的石头</param>
        internal static void SuperShoot(Player player, Rock rock)
        {
            var playerOpo = player.abstractPhysicalObject.GetOnlineObject();
            var rockOpo = rock.abstractPhysicalObject.GetOnlineObject();
            if (playerOpo is null || rockOpo is null)
            {
                return;
            }

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe)
                {
                    continue;
                }

                // 用反射来调用RPC方法
                onlinePlayer.InvokeRPC(
                    typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.SuperShoot))!
                    .CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject>)),
                    playerOpo,
                    rockOpo);
            }
        }
    }
}