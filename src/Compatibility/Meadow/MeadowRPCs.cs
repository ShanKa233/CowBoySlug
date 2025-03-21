using RainMeadow;
using UnityEngine;
using CowBoySlug.Mechanics.RopeSkill;

namespace Compatibility.Meadow
{
    public static class MeadowRPCs
    {
        /// <summary>
        /// 处理绳矛创造时的RPC方法
        /// 当玩家创建绳矛时，通过网络同步绳矛的创建
        /// </summary>
        /// <param name="event">RPC事件参数</param>
        /// <param name="playerOpo">绳矛的在线物理对象</param>
        /// <param name="spearOpo">绳子的在线物理对象</param>
        [RPCMethod]
        public static void CreateRopeSpear(RPCEvent @event, OnlinePhysicalObject playerOpo, OnlinePhysicalObject spearOpo,Color start,Color end)
        {
            if (playerOpo?.apo?.realizedObject is not Player player)
            {
                return;
            }

            if (spearOpo?.apo?.realizedObject is not Spear spear)
            {
                return;
            }
            Handler.SpawnRope_Local(player, spear, start, end);
        }

        /// <summary>
        /// 处理扔矛时生成绳子的RPC方法
        /// 当玩家扔矛并生成绳子时，通过网络同步绳子的创建
        /// </summary>
        /// <param name="event">RPC事件参数</param>
        /// <param name="playerOpo">扔矛的玩家的在线物理对象</param>
        /// <param name="spearOpo">被扔出的矛的在线物理对象</param>
        /// <param name="ropeColor">绳子的颜色</param>
        [RPCMethod]
        public static void ThrowSpearWithRope(RPCEvent @event, OnlinePhysicalObject playerOpo, OnlinePhysicalObject spearOpo, Color ropeColor)
        {
            if (playerOpo?.apo?.realizedObject is not Player player)
            {
                return;
            }

            if (spearOpo?.apo?.realizedObject is not Spear spear)
            {
                return;
            }

            // 调用本地方法处理扔矛生成绳子的逻辑
            Handler.ThrowSpearWithRope_Local(player, spear, ropeColor);
        }

        /// <summary>
        /// 处理召回矛的RPC方法
        /// 当玩家召回矛时，通过网络同步矛的行为
        /// </summary>
        /// <param name="event">RPC事件参数</param>
        /// <param name="playerOpo">玩家的在线物理对象</param>
        [RPCMethod]
        public static void CallBackSpear(RPCEvent @event, OnlinePhysicalObject playerOpo)
        {
            if (playerOpo?.apo?.realizedObject is not Player player)
            {
                return;
            }

            // 调用本地方法处理召回矛的逻辑
            Handler.CallBackSpear_Local(player);
        }

        /// <summary>
        /// 处理绳子断裂的RPC方法
        /// 当玩家的绳子断裂时，通过网络同步断裂效果
        /// </summary>
        /// <param name="event">RPC事件参数</param>
        /// <param name="playerOpo">玩家的在线物理对象</param>
        /// <param name="spearOpo">矛的在线物理对象</param>
        [RPCMethod]
        public static void HandleRopeBreaking(RPCEvent @event, OnlinePhysicalObject playerOpo, OnlinePhysicalObject spearOpo)
        {
            if (playerOpo?.apo?.realizedObject is not Player player)
            {
                return;
            }

            if (spearOpo?.apo?.realizedObject is not Spear spear)
            {
                return;
            }

            // 调用本地方法处理绳子断裂的逻辑
            Handler.HandleRopeBreaking_Local(player, spear);
        }

        /// <summary>
        /// 处理超级射击的RPC方法
        /// 当玩家使用超级射击时，通过网络同步石头的行为
        /// </summary>
        /// <param name="event">RPC事件参数</param>
        /// <param name="playerOpo">玩家的在线物理对象</param>
        /// <param name="rockOpo">石头的在线物理对象</param>
        [RPCMethod]
        public static void SuperShoot(RPCEvent @event, OnlinePhysicalObject playerOpo, OnlinePhysicalObject rockOpo)
        {
            if (playerOpo?.apo?.realizedObject is not Player player)
            {
                return;
            }

            if (rockOpo?.apo?.realizedObject is not Rock rock)
            {
                return;
            }

            // 调用本地方法处理超级射击的逻辑
            CowBoySlug.Mechanics.ShootSkill.SuperShootModule.SuperShoot_Local(player, rock);
        }

        /// <summary>
        /// 处理从墙上拔出矛的RPC方法
        /// 当玩家从墙上拔出矛时，通过网络同步矛的行为
        /// </summary>
        /// <param name="event">RPC事件参数</param>
        /// <param name="spearOpo">矛的在线物理对象</param>
        [RPCMethod]
        public static void PullSpearFromWall(RPCEvent @event, OnlinePhysicalObject spearOpo)
        {
            if (spearOpo?.apo?.realizedObject is not Spear spear)
            {
                return;
            }

            // 调用本地方法处理从墙上拔出矛的逻辑
            Handler.PullSpearFromWall_Local(spear);
        }
    }
}