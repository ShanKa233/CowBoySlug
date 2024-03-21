using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CowBoySlug.CowBoySlugMod
{
    public static class CowBoy
    {
        public static ConditionalWeakTable<Player,ExPlayer> modules = new ConditionalWeakTable<Player, ExPlayer>();
        
        public static ExPlayer GetCowBoys(this Player player) => modules.GetValue(player,(Player _)=>new ExPlayer(player));

    }
    public class ExPlayer
    {
        Player player;

        public ExPlayer(Player player)
        {
            this.player = player;
        }
    }
}
