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
            
            On.Player.ThrownSpear += Player_ThrownSpear;//扔矛的时候生成线
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            CallBackSpear(self);//召回矛
            if (!PlayerHook.cowboyModules.TryGetValue(self, out var mod)) return;//如果是牛仔猫
            GPSpawnRope(self);//用于在其他gp玩家的画面中生成矛
        }

        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);
            if (!PlayerHook.cowboyModules.TryGetValue(self, out var mod)) return;//如果扔矛的是牛仔猫
            
            
            spear.vibrate += 2;//增加回收的cd

            //提示其他gp的玩家我扔矛制造线了要!
            if (Plugin.enableGhostPlayer && !GhostPlayerImports.IsNetworkPlayer(self))
            {
                //Debug.Log("有发信号");
                GhostPlayerImports.TrySendImportantValue(new CowBoyData() { id = GhostPlayerImports.GetPlayerNetID(self),type=0 }, false);
            }


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

        public static bool CanNotCall(Player player)
        {
            bool flag = (player.input[0].pckp || player.input[1].pckp);//如果玩家在两帧内按下过拿取键
            bool flag2 = player.eatMeat <= 1 && player.eatExternalFoodSourceCounter <= 1;//而且玩家没有在吃东西
            bool flag3 = player.input[0].y >= 0 && player.FreeHand() != -1;//玩家没有按下而且有一只空手
            return !(flag && flag2 && flag3);
        }
        public static NewRope NiceRope(Player player)
        {
            NewRope umbilical = null;
            //搜索房间里面的所有矛找一根合适的出来
            foreach (var obj in UseCowBoyRope.RopeList)
            {
                //如果这个东西是 丝线 而且 丝线的发射者是玩家 而且丝线上绑着矛
                var testUmbilical = obj;
                if (!(testUmbilical != null && testUmbilical.spear != null && testUmbilical.player == player)) continue;//测丝线上的矛是不是玩家的,不是就跳过这个循环
                if (testUmbilical.spear.grabbedBy.Count > 0 && testUmbilical.spear.grabbedBy[0].grabber == player) continue;//防止玩家对自己手上的矛继续操作

                #region 因为在线的update里面如果先断了就会从列表排除
                //循环看看有没有哪一节的宽度小于0
                for (int i = 0; i < testUmbilical.points.GetLength(0); i++)
                {
                    if (testUmbilical.points[i, 3].x <= 0f)//如果如果小于零说明没连接
                    {
                        continue;
                    }
                }

                #endregion

                //如果矛的丝连接着而且还没找到过其他符合条件的丝,把要拉动的丝设定为这个丝
                if (umbilical == null)
                {
                    umbilical = testUmbilical;
                }
                else//如果已经找到过其他丝就比较两个丝哪个更近
                {
                    //老矛离玩家的距离是否比新矛距离大
                    bool b = Math.Abs(umbilical.spear.firstChunk.pos.x - player.mainBodyChunk.pos.x) > Math.Abs(testUmbilical.spear.firstChunk.pos.x - player.mainBodyChunk.pos.x);
                    umbilical = b ? testUmbilical : umbilical;
                }
            }

            return umbilical;
        }

        public static bool WhenSpearOnSomeThing(Spear spear,Player player,float range, Vector2 whereIsPlayer, Vector2 whenRopeClose,NewRope umbilical)
        {
            //如果插到墙上就拔下来然后变成自由状态,或爬墙
            if (spear.hasHorizontalBeamState && spear.mode == Weapon.Mode.StuckInWall)//如果线在墙上就清理一下,防止残留
            {
                //爬墙
                int canGrab = 0;
                for (int i = 0; i < 10; i++)
                {
                    if (player.input[i].jmp || player.input[0].jmp)
                    {
                        canGrab++;
                    }
                }
                if (range > 1 && spear.firstChunk.pos.y + 3 > player.mainBodyChunk.pos.y && player.gravity > 0 && canGrab > 2)
                {
                    if (player.wantToPickUp > 0)
                    {
                        player.mainBodyChunk.vel -= whereIsPlayer * 1.5f;
                        spear.vibrate += 5;
                    }
                    else
                    {
                        player.mainBodyChunk.vel -= whereIsPlayer * 2f;
                        player.jumpBoost = 1.5f;
                    }


                    //手部动画
                    if (Hands.module.TryGetValue(player, out var handModules))
                    {
                        Vector2 start = umbilical.points[0, 0];
                        Vector2 end = new Vector2(umbilical.points[umbilical.points.GetLength(0) - 1, 0].x, player.mainBodyChunk.pos.y);
                        float[] t = { 1f, 0.03f, 0.78f, 0.57f };
                        handModules.move(start, end, 5, 8f, umbilical, t, true);
                    }

                    return true;
                }

                //取下矛
                spear.resetHorizontalBeamState();
                spear.stuckInWall = new Vector2?(default(Vector2));
                spear.vibrate = 10;
                spear.firstChunk.collideWithTerrain = true;
                spear.abstractSpear.stuckInWallCycles = 0;
                spear.ChangeMode(Spear.Mode.Free);
            }
            //如果插到了生物就拖动他-待改
            else if (spear.mode == Spear.Mode.StuckInCreature)
            {
                if (player.wantToPickUp > 0)
                {
                    //拉绳子手部动作
                    if (Hands.module.TryGetValue(player, out var handModules))
                    {
                        Vector2 start = player.mainBodyChunk.pos;
                        Vector2 end = spear.firstChunk.pos;
                        float[] t = { 1f, 0.03f, 0.78f, 0.57f };
                        handModules.move(start, end, 5, 2f, umbilical, t, true);
                    }

                    //玩家受到拉力
                    float massPower = spear.stuckInObject.TotalMass > 20 ? 20 : spear.stuckInObject.TotalMass;
                    player.mainBodyChunk.vel -= whereIsPlayer * (massPower / player.TotalMass) / 1.5f;
                    spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel += whereIsPlayer * (player.TotalMass / spear.stuckInObject.TotalMass) / 1.5f * 2;
                }
                else if (!Custom.DistLess(player.mainBodyChunk.pos, spear.stuckInChunk.pos, 60))
                {
                    if (player.input[0].jmp)
                    {
                        player.mainBodyChunk.vel -= whereIsPlayer * 3;
                    }
                    spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel += whereIsPlayer * 3;
                }

            }
            //对拿着这个矛的生物操作
            else if (spear.mode == Weapon.Mode.Carried && spear.grabbedBy[0].grabber != player && spear.grabbedBy[0].grabber != null)
            {
                spear.grabbedBy[0].grabber.Stun(40);
            }
            return false;
        }

        public static void CallBackSpear(Player player)
        {
            if (CanNotCall(player))return;
            var umbilical = NiceRope(player);//找到一个好线
            if (umbilical == null) return;
            if (!(umbilical.spear != null && player.room == umbilical.spear.room && umbilical.spear.vibrate <= 0)) return;//测测矛可不可以用
            //如果找到了符合条件的矛,而且矛没有震动

            var spear = umbilical.spear;

            //是否做出快速唤回动作
            bool flagFastBackAction = player.input[0].y > 0;
            //检查能不能直视到
            bool flagSee = player.room.VisualContact(spear.firstChunk.pos, player.firstChunk.pos);
            //检查距离
            var range = Vector2.Distance(spear.firstChunk.pos, player.bodyChunks[1].pos);
            //求出玩家相对于矛的方位
            Vector2 whereIsPlayer = (player.bodyChunks[1].pos - spear.firstChunk.pos).normalized;
            //离矛最近的丝的方向
            Vector2 whenRopeClose = (umbilical.points[umbilical.points.GetLength(0) - 4, 0] - spear.firstChunk.pos).normalized;

            if (WhenSpearOnSomeThing(spear,player,range,whereIsPlayer,whenRopeClose,umbilical)) return;
            //稍微加固丝线
            for (int i = 0; i < umbilical.points.GetLength(0); i++)
            {
                umbilical.points[i, 3].x = 30f;
            }

            //防止吃东西 吐东西
            if (spear.mode != Weapon.Mode.Carried)
            {
                player.swallowAndRegurgitateCounter = 0;
                player.slugOnBack.counter = 0;
            }

            //在无重力情况下给玩家施加移动力
            if (spear.mode != Weapon.Mode.Carried && player.gravity <= 0)
            {
                player.mainBodyChunk.vel -= whereIsPlayer / 2;
            }



            //如果玩家离矛很近而且可以直视矛而且按了拿取按键就拿起矛
            if (range < 50 && flagSee && spear.mode != Weapon.Mode.Carried)
            {
                if (player.FreeHand() != -1)
                {
                    player.SlugcatGrab(spear, player.FreeHand());
                    player.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, spear.firstChunk);
                    spear.vibrate += 10;
                    spear.canBeHitByWeapons = true;//让矛可以挡下攻击
                }
            }
            //回收矛模式
            else if (flagFastBackAction && range > 50)
            {

                //拉绳子手部动作
                if (Hands.module.TryGetValue(player, out var handModules))
                {
                    float[] t = { 0.35f, 0.89f, 0.72f, 0.72f };
                    handModules.move(spear.firstChunk.pos, umbilical.points[4, 0], 5, 15f, umbilical, t, false);
                }


                spear.ChangeMode(Weapon.Mode.Free);

                spear.firstChunk.vel = whereIsPlayer * 27;
                spear.SetRandomSpin();
                if (spear.gravity > 0)
                {
                    spear.firstChunk.vel.y += 10;
                }

                //动画相关
                if (flagSee)
                {
                    //if (handTochRopeCont > 0 && umbilical != null && umbilical.room == player.room && umbilical.spear != null && umbilical.spear.room == player.room && dragPointSeted && startAndEndSeted)
                    //{
                    //    for (int i = 2; i < umbilical.points.GetLength(0) - 1; i++)
                    //    {
                    //        if (Vector2.Distance(umbilical.points[i, 0], player.mainBodyChunk.pos) < 40)
                    //        {
                    //            Vector2 trueoPos = Vector2.Lerp(playerFreeHandPos, spear.firstChunk.pos, i / umbilical.points.GetLength(0));
                    //            umbilical.points[i, 0] = playerFreeHandPos;
                    //        }
                    //        else
                    //        {
                    //            Vector2 trueoPos = Vector2.Lerp(playerFreeHandPos, spear.firstChunk.pos, i / umbilical.points.GetLength(0));
                    //            umbilical.points[i, 0] = Vector2.Lerp(umbilical.points[i, 0], trueoPos, 0.4f);
                    //        }
                    //    }
                    //}
                }
            }
            //攻击模式
            else if (player.input[1].pckp && !player.input[0].pckp && range > 35)
            {
                int pckpTime = 0;
                for (int i = 0; i < 7; i++)
                {
                    if (player.input[i].pckp)
                    {
                        pckpTime++;
                    }
                }
                if (pckpTime > 5)
                {
                    return;
                }


                //控制手和绳子
                if (Hands.module.TryGetValue(player, out var handModules))
                {
                    float[] t = { .9f, -0.04f, .78f, 1.31f };
                    handModules.move(umbilical.points[0, 0], player.mainBodyChunk.pos + whereIsPlayer * 25, 20, 15f, umbilical, t, true);
                }
                for (int i = 0; i < umbilical.points.GetLength(0) / 3; i++)
                {
                    umbilical.points[i, 2] += whereIsPlayer * 30;
                }



                spear.ChangeMode(Weapon.Mode.Thrown);
                spear.spearDamageBonus *= 0.9f;
                spear.thrownBy = player;
                spear.throwDir = new IntVector2(Convert.ToInt32(whereIsPlayer.x), Convert.ToInt32(whereIsPlayer.y)); ;

                spear.rotation = spear.throwDir.ToVector2();
                spear.firstChunk.pos -= whereIsPlayer;
                spear.firstChunk.vel.x = spear.throwDir.x * 60 * spear.spearDamageBonus;

                if (spear.gravity > 0)
                {
                    spear.firstChunk.vel.y = 2.9f;
                }

            }
            //慢速模式
            else if (player.input[0].pckp)
            {
                //控制手和绳子
                if (Hands.module.TryGetValue(player, out var handModules))
                {
                    float[] t = { .9f, -0.04f, .72f, .72f };
                    handModules.move(umbilical.points[0, 0], spear.firstChunk.pos, 5, 10f, umbilical, t, true);
                }


                spear.firstChunk.vel += whereIsPlayer * 1.3f + whenRopeClose * 0.2f; ;
                spear.rotation = -(player.bodyChunks[1].pos - spear.firstChunk.pos).normalized;
                if (spear.gravity > 0)
                {
                    spear.firstChunk.vel.y += 0.4f;
                }
            }
            else if (spear.mode == Weapon.Mode.StuckInCreature)
            {
                spear.ChangeMode(Weapon.Mode.Free);
            }

        }//唤回矛



        public static void GPSpawnRope(Player player)
        {
            //让其他客户端的猫矛上有线
            if (Plugin.enableGhostPlayer && GhostPlayerImports.IsNetworkPlayer(player))
            {//如果有接收到信息
                if (GhostPlayerImports.TryGetImportantValue(typeof(CowBoyData), out var obj) &&
                    ((CowBoyData)obj).id == GhostPlayerImports.GetPlayerNetID(player)&& ((CowBoyData)obj).type==0)
                {
                    if (!PlayerHook.cowboyModules.TryGetValue(player, out var mod)) return;//如果是牛仔猫


                    //在房间里面找个玩家扔出去的矛
                    foreach (var item in player.room.updateList)
                    {
                        var spear = item as Spear;
                        //给一个被判断为服务器扔出的矛,而且在飞行的矛加上丝线,或加固丝线
                        if (spear!= null && spear.abstractSpear.ID.spawner==-2&&Vector2.Distance(spear.firstChunk.lastLastPos,spear.firstChunk.pos)>10)
                        {

                            //如果有线就跳过
                            foreach (var item2 in UseCowBoyRope.RopeList)
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
                            UseCowBoyRope.SpawnRope(item as Spear, player, Color.Lerp(player.ShortCutColor(), mod.ropeColor, 0.5f), mod.ropeColor);
                            return;
                        }
                    }
                }
                return;
            }


        }


    }
}
