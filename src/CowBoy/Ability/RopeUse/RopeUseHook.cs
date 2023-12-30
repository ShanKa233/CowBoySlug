using CowBoySLug;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CowBoySlug.CowBoy.Ability.RopeUse;
using UnityEngine;
using RWCustom;
using static CowBoySLug.Plugin;
using Smoke;

namespace CowBoySlug.CowBoy.Ability.RopeUse
{
    public static class RopeUseHook
    {
        public static void Hook()
        {
            
            //On.Player.ThrownSpear += Player_ThrownSpear;//扔矛的时候生成线
            //On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (!PlayerHook.cowboyModules.TryGetValue(self, out var mod)) return;//如果是牛仔猫
            GPSpawnRope(self);//用于在其他gp玩家的画面中生成矛
        }

        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);
            //提示其他gp的玩家我扔矛制造线了要!
            if (Plugin.enableGhostPlayer && !GhostPlayerImports.IsNetworkPlayer(self))
            {
                //Debug.Log("有发信号");
                GhostPlayerImports.TrySendImportantValue(new CowBoyData() { id = GhostPlayerImports.GetPlayerNetID(self),type=0 }, false);
            }
        }

        public static void GPHandMove(Player player)
        {
            if (Hands.module.TryGetValue(player, out var handModules))
            {
                if (Plugin.enableGhostPlayer && GhostPlayerImports.IsNetworkPlayer(player))
                {//如果有接收到信息
                    if (GhostPlayerImports.TryGetImportantValue(typeof(CowBoyData), out var obj) &&
                        ((CowBoyData)obj).id == GhostPlayerImports.GetPlayerNetID(player) && ((CowBoyData)obj).type == 1)
                    {
                        var umbilical = RopeMaster.NiceRope(player);
                        if (umbilical == null) return;
                        var spear = umbilical.spear;
                        float[] t = { .9f, -0.04f, .72f, .72f };
                        handModules.move(umbilical.points[0, 0], spear.firstChunk.pos, 5, 10f, umbilical, t, true);
                    }
                    return;
                }
            }
        }
        public static void GPSpawnRope(Player player)
        {
            //让其他客户端的猫矛上有线
            if (Plugin.enableGhostPlayer && GhostPlayerImports.IsNetworkPlayer(player))
            {//如果有接收到信息
                if (GhostPlayerImports.TryGetImportantValue(typeof(CowBoyData), out var obj) &&
                    ((CowBoyData)obj).id == GhostPlayerImports.GetPlayerNetID(player)&& ((CowBoyData)obj).type==0)
                {
                    if (!RopeMaster.modules.TryGetValue(player, out var mod)) return;//如果是牛仔猫


                    //在房间里面找个玩家扔出去的矛
                    foreach (var item in player.room.updateList)
                    {
                        var spear = item as Spear;
                        //给一个被判断为服务器扔出的矛,而且在飞行的矛加上丝线,或加固丝线
                        if (spear!= null && spear.abstractSpear.ID.spawner==-2&&Vector2.Distance(spear.firstChunk.lastLastPos,spear.firstChunk.pos)>10)
                        {

                            //如果有线就跳过
                            foreach (var item2 in mod.ropes)
                            {
                                //寻找矛上有没有其他的线
                                if (item2 != null && item2.spear != null && item2.spear == spear)
                                {//如果有线就加固线然后return
                                    var umbilical = item2;
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

                            //弄个线上去
                            CowRope.SpawnRope(player, item as Spear, Color.Lerp(player.ShortCutColor(), mod.ropeColor, 0.5f), mod.ropeColor);
                            return;
                        }
                    }
                }
                return;
            }


        }


    }
}
