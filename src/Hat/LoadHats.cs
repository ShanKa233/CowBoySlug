using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowBoySlug
{
    public class LoadHats
    {
        public static bool loaded = false;
        public static void Hook()
        {
            if (loaded)
            {
                Load();
                loaded= true;
            }
        }
        public static void Load()
        {

        }


    }
}
