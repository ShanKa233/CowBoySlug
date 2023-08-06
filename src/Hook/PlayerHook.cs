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

namespace CowBoySLug
{
    internal class PlayerHook
    {

        public static ConditionalWeakTable<Player, CowBoyModule> cowboyModules = new ConditionalWeakTable<Player, CowBoyModule>();
        public static ConditionalWeakTable<Rock,SuperRockModule> rockModule = new ConditionalWeakTable<Rock, SuperRockModule>();
        public static void Hook()
        {
            On.Player.ctor += CowBoy_ctor;

            On.Player.Update += Player_Update;//

            On.Player.CanBeSwallowed += Hat_CanBeSwallowed;
            On.Player.GrabUpdate += Player_GrabUpdate;
            On.Player.SleepUpdate += Player_SleepUpdate;


            On.Player.ThrowObject += CowBoy_Throw;//超级石头射击和加速
            On.Creature.SwitchGrasps += Player_SwitchGrasps;//换手时增加一个计数来确认是否换手

            On.Rock.Update += SuperRock_Stop;//超级石头撞到东西停下
            On.Creature.Violence += SuperRock_CreatureViolence;//超级石头对生物造成伤害

        }

        private static bool Hat_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            if (testObj is CowBoyHat)
            {
                return true;
            }
            else
            {
                return orig.Invoke(self, testObj);
            }
            
        }

        private static void Player_SleepUpdate(On.Player.orig_SleepUpdate orig, Player self)
        {
            orig.Invoke(self);
            if (Hat.modules.TryGetValue(self, out var hatModule))
            {
                if ((hatModule.haveHat&&self.readyForWin&& self.touchedNoInputCounter > (ModManager.MMF ? 40 : 20))||(self.forceSleepCounter > 260))
                {
                    var newHat = new CowBoyHat(new CowBoyHatAbstract(self.room.world, (self).abstractCreature.pos, self.room.game.GetNewID()));
                    newHat.mainColor = hatModule.mainColor;
                    newHat.decorateColor = hatModule.decorateColor;
                    newHat.shape = hatModule.shape;
                    newHat.setMainColor = true;
                    hatModule.haveHat = false;

                    newHat.PlaceInRoom(self.room);
                    newHat.room.abstractRoom.AddEntity(newHat.abstractPhysicalObject);
                }
            }
        }

        private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (Hat.modules.TryGetValue(self,out var hatModule))
            {
                bool flag = hatModule.haveHat;
                bool flag2 = self.wantToPickUp>0&&self.input[0].y < 0&&self.grasps[0]==null&&self.grasps[1]==null;
                if (flag&&flag2) 
                {
                    Hat.PlacePlayerHat(self, hatModule);


                }
            }
        }

        //初始化牛仔角色 
        private static void CowBoy_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
           
            orig.Invoke(self, abstractCreature, world);
            if ((Plugin.RockShot.TryGet(self, out var value) && value))
            {
                cowboyModules.Add(self, new CowBoyModule(self));

                //特殊饱腹度系统相关
                if (Plugin.menu.foodMod.Value&& self.room.world.game.session.characterStats.name.value == "CowBoySLug")
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
                else if(self.room.world.game.session.characterStats.name.value == "CowBoySLug")
                {
                    self.slugcatStats.foodToHibernate = self.slugcatStats.maxFood;
                }
                  
            }
        }
        static private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            //j检查玩家是否有这个rock设计的标签而且标签是true


            bool flag = Plugin.RockShot.TryGet(self, out var flag2) && flag2;


            if (flag && cowboyModules.TryGetValue(self, out var cowBoyModule))
            {
                

                cowBoyModule.BackToNormal();
                cowBoyModule.NotMove();

                cowBoyModule.CallBackSpear();
                cowBoyModule.RopeAndHandKeepMove();

                if (Plugin.menu.foodMod.Value&& self.room.world.game.session.characterStats.name.value == "CowBoySLug")
                {
                    cowBoyModule.UseFood();
                }
            }
        }





        //检查是否换手
        private static void Player_SwitchGrasps(On.Creature.orig_SwitchGrasps orig, Creature self, int fromGrasp, int toGrasp)
        {orig.Invoke(self, fromGrasp, toGrasp);
            var player = self as Player;
            if (player != null)
            {
                bool flag = Plugin.RockShot.TryGet(player, out var flag2) && flag2;
                if (flag && cowboyModules.TryGetValue(player, out var cowBoyModule))
                {
                    cowBoyModule.ChangeHand();
                }
            }
        }
        private static void CowBoy_Throw(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            Rock rook = self.grasps[grasp].grabbed as Rock;
            bool flag = Plugin.RockShot.TryGet(self, out var flag2) && flag2;
            bool flag3 = cowboyModules.TryGetValue(self, out var cowBoyModule) && rook != null;

            if (flag && flag3)
            {
                cowBoyModule.RockMake(rook);
            }
            orig.Invoke(self, grasp, eu);

        }







       
        private static void SuperRock_CreatureViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (source!=null)
            {
                var superRock = source.owner as Rock;
                if (superRock != null && rockModule.TryGetValue(superRock, out var flag) && flag.isSuperRock)
                {
                    damage = 5;
                    stunBonus = 20;
                }
            }
            
            orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static void SuperRock_Stop(On.Rock.orig_Update orig, Rock self, bool eu)
        {
            orig.Invoke(self, eu);
            if (rockModule.TryGetValue(self,out var superRockModule)&&superRockModule.isSuperRock)
            {
                if (superRockModule.canMoreFast)
                {
                    superRockModule.RockPowerUp();
                }
                if (self.firstChunk.vel.x == 0)
                {
                    superRockModule.isSuperRock = false;
                }
            }
        }
        //停止石头
        
    }
}
