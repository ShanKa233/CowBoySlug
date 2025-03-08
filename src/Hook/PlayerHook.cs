using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CowBoySlug;
using CowBoySLug;
using CowBoySlug.CowBoy.Ability.RopeUse;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using RWCustom;
using UnityEngine;

namespace CowBoySLug
{
    internal class PlayerHook
    {
        //public static ConditionalWeakTable<Player, CowBoyModule> cowboyModules = new ConditionalWeakTable<Player, CowBoyModule>();
        public static void Hook()
        {
            try
            {
                On.Player.ctor += CowBoy_ctor;
                On.Player.Update += Player_Update;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CowBoySlug] Exception in PlayerHook.Hook: {ex.Message}\n{ex.StackTrace}");
            }
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
        private static void CowBoy_ctor(
            On.Player.orig_ctor orig,
            Player self,
            AbstractCreature abstractCreature,
            World world
        )
        {
            try
            {
                orig.Invoke(self, abstractCreature, world);

                // 检查必要的对象
                if (self == null || 
                    Plugin.menu == null || 
                    Plugin.menu.foodMod == null || 
                    self.room == null || 
                    self.room.world == null || 
                    self.room.world.game == null || 
                    self.room.world.game.session == null || 
                    self.room.world.game.session.characterStats == null)
                {
                    return;
                }

                //特殊饱腹度系统相关
                if (
                    Plugin.menu.foodMod.Value
                    && self.room.world.game.session.characterStats.name == CowBoyModule.Name
                )
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
                else if (self.room.world.game.session.characterStats.name == CowBoyModule.Name)
                {
                    self.slugcatStats.foodToHibernate = self.slugcatStats.maxFood;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CowBoySlug] Exception in CowBoy_ctor: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            try
            {
                orig.Invoke(self, eu);
                bool isCowBoy = self.IsCowBoys(out var cowBoyModule);
                if (!isCowBoy)
                {
                    // 不是牛仔角色，直接返回
                    return;
                }
                cowBoyModule.Update();

                // 检查Plugin.menu
                if (Plugin.menu == null || Plugin.menu.foodMod == null || !Plugin.menu.foodMod.Value)
                {
                    // foodMod未启用或配置有问题，直接返回
                    return;
                }

                // 检查self.room
                if (self.room == null)
                {
                    return;
                }

                // 检查self.room.world及其相关对象
                if (self.room.world == null || 
                    self.room.world.game == null || 
                    self.room.world.game.session == null || 
                    self.room.world.game.session.characterStats == null)
                {
                    return;
                }

                if (self.room.world.game.session.characterStats.name == CowBoyModule.Name)
                {
                    cowBoyModule.UseFood();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CowBoySlug] Exception in PlayerHook.Player_Update: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
