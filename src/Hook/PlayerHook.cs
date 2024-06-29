using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CowBoySlug;
using CowBoySLug;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using RWCustom;
using CowBoySlug.CowBoy.Ability.RopeUse;
using CowBoySlug.CowBoySlugMod;

namespace CowBoySLug
{
    internal class PlayerHook
    {

        //public static ConditionalWeakTable<Player, CowBoyModule> cowboyModules = new ConditionalWeakTable<Player, CowBoyModule>();
        public static void Hook()
        {
            On.Player.ctor += CowBoy_ctor;

            On.Player.Update += Player_Update;//

            
            //On.Player.GrabUpdate += Player_GrabUpdate;
            
        }

        //private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        //{
        //    orig.Invoke(self, eu);
        //    if (Hat.modules.TryGetValue(self,out var hatModule))
        //    {
        //        bool flag = hatModule.haveHat;
        //        bool flag2 = self.wantToPickUp>0&&self.input[0].y < 0&&self.grasps[0]==null&&self.grasps[1]==null;
        //        if (flag&&flag2) 
        //        {
        //            Hat.PlacePlayerHat(self, hatModule);


        //        }
        //    }
        //}

        //初始化牛仔角色 
        private static void CowBoy_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
           
            orig.Invoke(self, abstractCreature, world);

            //特殊饱腹度系统相关
            if (Plugin.menu.foodMod.Value && self.room.world.game.session.characterStats.name == CowBoy.Name)
            {
                if (self.PlaceKarmaFlower)
                {
                    self.slugcatStats.maxFood += 4;
                }
                if (self.playerState.foodInStomach < self.slugcatStats.maxFood)
                {
                    self.playerState.foodInStomach = self.slugcatStats.maxFood;
                }
            }
            else if (self.room.world.game.session.characterStats.name == CowBoy.Name)
            {
                self.slugcatStats.foodToHibernate = self.slugcatStats.maxFood;
            }

        }
        static private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            if (self.IsCowBoys(out var cowBoyModule))
            {
                cowBoyModule.Update();

                if (Plugin.menu.foodMod.Value&& self.room.world.game.session.characterStats.name==CowBoy.Name)cowBoyModule.UseFood();

            }

        }





    }
}
