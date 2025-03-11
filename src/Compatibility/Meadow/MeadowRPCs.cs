using CowBoySlug.CowBoy.Ability.RopeUse;
using RainMeadow;
using UnityEngine;

namespace src.Compatibility.Meadow
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
            CowRope.SpawnRope_Local(player, spear, start, end);

        }

    }
}