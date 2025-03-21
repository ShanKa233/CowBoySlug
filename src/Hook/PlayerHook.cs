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
using UnityEngine;

namespace CowBoySLug
{
    internal class PlayerHook
    {
        //public static ConditionalWeakTable<Player, CowBoyModule> cowboyModules = new ConditionalWeakTable<Player, CowBoyModule>();
        public static void Hook()
        {
            On.Player.ctor += CowBoy_ctor;
            On.Player.Update += Player_Update;
        }
        //初始化牛仔角色
        private static void CowBoy_ctor(
            On.Player.orig_ctor orig,
            Player self,
            AbstractCreature abstractCreature,
            World world
        )
        {
            orig.Invoke(self, abstractCreature, world);
            if (!self.IsCowBoys(out var cowBoyModule))
            {
                return;
            }
            
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
                && self.room.world.game.session.characterStats.name.value == CowBoyModule.CowboySlugID
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
            else if (self.room.world.game.session.characterStats.name.value == CowBoyModule.CowboySlugID)
            {
                self.slugcatStats.foodToHibernate = self.slugcatStats.maxFood;
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (!self.IsCowBoys(out var cowBoyModule))
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

            if (self.room.world.game.session.characterStats.name.value == CowBoyModule.CowboySlugID)
            {
                cowBoyModule.UseFood();
            }
        }
    }
}
