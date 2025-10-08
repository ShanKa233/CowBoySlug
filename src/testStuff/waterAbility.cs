using System;
using CowBoySlug;

namespace src.testStuff
{
    public class waterAbility
    {
        public static void Hook()
        {
            //用于让玩家不会在水里自动上浮的代码
            On.Player.JollyUpdate += Player_JollyUpdate;
        }

        private static void Player_JollyUpdate(On.Player.orig_JollyUpdate orig, Player self, bool eu)
        {
            if (self.slugcatStats.name == CowBoyModule.CowboySlugName)
            {
                self.buoyancy=0.9f;
            }
            orig(self, eu);
        }
    }
}