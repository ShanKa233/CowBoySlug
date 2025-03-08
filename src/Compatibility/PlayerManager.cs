using System.Collections.Generic;
using UnityEngine;

namespace CowBoySlug.Compatibility
{
    /// <summary>
    /// 玩家管理器，用于管理玩家
    /// </summary>
    public static class PlayerManager
    {
        /// <summary>
        /// 获取所有玩家
        /// </summary>
        public static List<Player> GetPlayers()
        {
            List<Player> players = new List<Player>();
            
            if (UnityEngine.Object.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game)
            {
                foreach (var abstractPlayer in game.Players)
                {
                    if (abstractPlayer.realizedCreature is Player player)
                    {
                        players.Add(player);
                    }
                }
            }
            
            return players;
        }
        
        /// <summary>
        /// 获取本地玩家
        /// </summary>
        public static Player GetLocalPlayer()
        {
            if (UnityEngine.Object.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game)
            {
                if (game.Players.Count > 0 && game.Players[0].realizedCreature is Player player)
                {
                    return player;
                }
            }
            
            return null;
        }
    }
} 