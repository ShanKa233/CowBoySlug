using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using RainMeadow;
using UnityEngine;

namespace Compatibility.Meadow
{
    /// <summary>
    /// Rain-Meadow 兼容层。
    /// 弱引用纪律:Meadow 只是加分项,未启用时必须能正常运行。
    /// 因此本类的【字段与公开方法签名】不得引用 RainMeadow 类型(类加载/调用方 JIT 时解析),
    /// RainMeadow 类型只允许出现在方法体里(方法体 JIT 惰性,联机路径才会触发,届时程序集已加载)。
    /// </summary>
    public static class MeadowCompat
    {

        public static bool IsHost => !IsOnline || OnlineManager.lobby.isOwner;

        public static bool IsOnline => OnlineManager.lobby is not null;

        public static bool IsOnlineFriendlyFire => RainMeadow.RainMeadow.isStoryMode(out var story) && story.friendlyFire;

        /// <summary>
        /// 单机恒 true;联机时表示该实体由本机模拟(签名安全,可被单机路径直接调用)。
        /// </summary>
        public static bool IsMine(AbstractPhysicalObject apo)
        {
            return !IsOnline || (apo.GetOnlineObject()?.isMine ?? true);
        }

        /// <summary>
        /// 把帽子的佩戴/视觉状态同步挂到 Meadow 的同步流(签名安全,内部有联机检查)。
        /// </summary>
        public static void TryAttachHat(CowBoySlug.CowBoyHat hat)
        {
            if (!IsOnline)
                return;
            HatSyncData.TryAttach(hat);
        }

        /// <summary>
        /// 把绳矛状态同步挂到 Meadow 的同步流(签名安全,内部有联机检查)。
        /// </summary>
        public static void TryAttachRope(Spear spear)
        {
            if (!IsOnline)
                return;
            RopeSyncData.TryAttach(spear);
        }

        // RPC 委托缓存:字段用 Delegate 基类(签名不含 RainMeadow 类型,类初始化时不需要解析程序集),
        // 方法体内做一次强类型转换并缓存,无反射;方法体 JIT 惰性,联机时才触发
        private static Delegate dCreateRopeSpear;
        private static Delegate dThrowSpearWithRope;
        private static Delegate dCallBackSpear;
        private static Delegate dHandleRopeBreaking;
        private static Delegate dSuperShoot;
        private static Delegate dPullSpearFromWall;

        // FromStick 的原方法委托(帽子的自定义 stick 静默跳过,其余走原逻辑)
        private delegate AbstractObjStickRepr orig_FromStick(AbstractPhysicalObject.AbstractObjectStick stick);
        private static Delegate origFromStick;
        // Hook 对象必须持有强引用,否则被 GC 回收时自动 detach
        private static Hook fromStickHook;

        internal static void InitCompat()
        {
            // 注册在线资源可用时的回调和SlugcatStats构造函数的钩子
            //暂时没什么需要初始化的
            OnlineResource.OnAvailable += OnlineResourceOnOnAvailable;

            // 帽子的 AbstractHatWearStick 是自定义 stick,Meadow 不认识它:
            // 每 tick 状态构造时都会打 "stick not implemented" 错误日志并返回 null。
            // 佩戴关系已由 HatSyncData 状态同步管理,stick 是纯本地结构,静默跳过。
            // (Rain-Meadow 没有 MMHOOK 程序集,不能用 On.RainMeadow 委托,用 RuntimeDetour 手动 hook)
            var fromStickMethod = typeof(RainMeadow.AbstractObjStickRepr).GetMethod(
                "FromStick",
                BindingFlags.Public | BindingFlags.Static);
            fromStickHook = new Hook(
                fromStickMethod,
                typeof(MeadowCompat).GetMethod(
                    nameof(FromStick_Hook),
                    BindingFlags.NonPublic | BindingFlags.Static));
            origFromStick = fromStickHook.GenerateTrampoline<orig_FromStick>();
        }
        private static void OnlineResourceOnOnAvailable(OnlineResource resource)
        {
        }

        private static AbstractObjStickRepr FromStick_Hook(AbstractPhysicalObject.AbstractObjectStick stick)
        {
            if (stick is CowBoySlug.AbstractHatWearStick)
            {
                return null;
            }
            return ((orig_FromStick)origFromStick)(stick);
        }

        internal static void CreateRopeSpear(Player player, Spear spear, Color start, Color end)
        {

            var playerOpo = player.abstractPhysicalObject.GetOnlineObject();
            var spearOpo = spear.abstractPhysicalObject.GetOnlineObject();
            if (playerOpo is null || spearOpo is null)
            {
                return;
            }

            dCreateRopeSpear ??= (Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject, Color, Color>)MeadowRPCs.CreateRopeSpear;

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe)
                {
                    continue;
                }

                onlinePlayer.InvokeRPC(dCreateRopeSpear, playerOpo, spearOpo, start, end);
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

            dThrowSpearWithRope ??= (Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject, Color>)MeadowRPCs.ThrowSpearWithRope;

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe)
                {
                    continue;
                }

                onlinePlayer.InvokeRPC(dThrowSpearWithRope, playerOpo, spearOpo, ropeColor);
            }
        }

        /// <summary>
        /// 处理召回矛的网络同步(按住召回键时每帧触发,用 InvokeOnceRPC 去重)
        /// </summary>
        /// <param name="player">召回矛的玩家</param>
        internal static void CallBackSpear(Player player)
        {
            var playerOpo = player.abstractPhysicalObject.GetOnlineObject();
            if (playerOpo is null)
            {
                return;
            }

            dCallBackSpear ??= (Action<RPCEvent, OnlinePhysicalObject>)MeadowRPCs.CallBackSpear;

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (!onlinePlayer.isMe)
                {
                    onlinePlayer.InvokeOnceRPC(dCallBackSpear, playerOpo);
                }
            }
        }

        /// <summary>
        /// 处理绳子断裂的网络同步方法
        /// </summary>
        /// <param name="player">玩家</param>
        /// <param name="spear">连接的矛</param>
        internal static void HandleRopeBreaking(Player player, Spear spear)
        {
            var playerOpo = player.abstractPhysicalObject.GetOnlineObject();
            var spearOpo = spear.abstractPhysicalObject.GetOnlineObject();
            if (playerOpo is null || spearOpo is null)
            {
                return;
            }

            dHandleRopeBreaking ??= (Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject>)MeadowRPCs.HandleRopeBreaking;

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (!onlinePlayer.isMe)
                {
                    onlinePlayer.InvokeRPC(dHandleRopeBreaking, playerOpo, spearOpo);
                }
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

            dSuperShoot ??= (Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject>)MeadowRPCs.SuperShoot;

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe)
                {
                    continue;
                }

                onlinePlayer.InvokeRPC(dSuperShoot, playerOpo, rockOpo);
            }
        }

        /// <summary>
        /// 处理从墙上拔出矛的网络同步
        /// </summary>
        /// <param name="spear">需要拔出的矛</param>
        internal static void PullSpearFromWall(Spear spear)
        {
            var spearOpo = spear.abstractPhysicalObject.GetOnlineObject();
            if (spearOpo is null)
            {
                return;
            }

            dPullSpearFromWall ??= (Action<RPCEvent, OnlinePhysicalObject>)MeadowRPCs.PullSpearFromWall;

            foreach (var onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe)
                {
                    continue;
                }

                onlinePlayer.InvokeRPC(dPullSpearFromWall, spearOpo);
            }
        }
    }
}
