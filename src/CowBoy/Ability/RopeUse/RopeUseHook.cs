using CowBoySLug;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CowBoySlug.CowBoy.Ability.RopeUse;
using UnityEngine;

namespace CowBoySlug.CowBoy.Ability.RopeUse
{
    public static class RopeUseHook
    {
        public static void Hook()
        {
            On.Player.ThrownSpear += Player_ThrownSpear;
        }

        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);
            if (!PlayerHook.cowboyModules.TryGetValue(self, out var mod)) return;//如果扔矛的是牛仔猫
            spear.vibrate += 2;//增加回收的cd
            //如果有线就跳过
            foreach (var item in UseCowBoyRope.RopeList)
            {
                //寻找矛上有没有其他的线
                if (item != null && item.spear != null && item.spear == spear)
                {//如果有线就加固线然后return
                    var umbilical = item;
                    if (umbilical.points.GetLength(0) > 10)
                    {
                        for (int i = 0; i < umbilical.points.GetLength(0); i++)
                        {
                            umbilical.points[i, 3].x = 25f;
                        }
                    }
                    return;
                }
            }
            //如果没线就弄个出来
            UseCowBoyRope.SpawnRope(spear, self, Color.Lerp(self.ShortCutColor(), mod.ropeColor, 0.5f), mod.ropeColor);
        }





    }
}
