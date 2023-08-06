
using CowBoySLug;
using RWCustom;
using SlugBase.DataTypes;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using CowBoySlug.CowBoy.Ability.RopeUse;

namespace CowBoySlug
{
    public class CowBoyModule
    {
        int timeToRemoveFood = 900;//减少食物的时间
        public int stopTime = 0;//静止不动的时间
        public int changeHand = 0;//换手了没
        public int spearPower = 0;//拉矛力
        public int callBackCD = 0;


        public int scarfIndex;//围巾的下标

        public int handTochRopeCont = 0;

        public bool playerGraphicsHandChanged=false;
        public Vector2 playerHandWantTochPos;
        public Vector2 playerFreeHandPos;
        public NewRope playerUsingUmbilical;

        public Vector2 handStart;
        public Vector2 handEnd;

        public bool startAndEndSeted = false;
        public bool dragPointSeted = false;

        public Color ropeColor = new Color(247 / 255f, 213 / 255f, 131 / 255f);
        public Color scarfColor = Color.yellow;
        public readonly SlugcatStats.Name name;
        public readonly SlugBaseCharacter character;

        public Player self;
        public CowBoyModule(Player player)
        {
            this.self = player;
            stopTime = 0;
            changeHand = 0;
            timeToRemoveFood = 1200;


        }


        public void UseFood()
        {
            if (self.playerState.permanentDamageTracking > 1000)
            {
                self.Die();
            }
            if (self.FoodInStomach <= 0) { self.playerState.permanentDamageTracking++; }
            if (timeToRemoveFood <= 0 && self.playerState.foodInStomach > 0)
            {
                self.SubtractFood(1);
                timeToRemoveFood = 1800;
            }
            if (timeToRemoveFood == 1800 && self.playerState.foodInStomach <= 0)
            {
                self.Stun(180);
            }
        }
        public void BackToNormal()
        {
            playerGraphicsHandChanged = false;
            if (self.playerState.foodInStomach > 0)
            {
                self.playerState.permanentDamageTracking = 0;
            }
            if (timeToRemoveFood > 0)
            {
                timeToRemoveFood--;
            }
            if (stopTime > 0 && !(self.input[0].x == 0 && self.input[0].y == 0))
            {
                stopTime -= 3;
            }
            if (changeHand > 0)
            {
                changeHand--;
            }
            if (callBackCD > 0)
            {
                callBackCD--;
            }
            if (spearPower > 0)
            {
                spearPower--;
            }
            if (handTochRopeCont > 0)
            {
                handTochRopeCont--;
            }
            else
            {
                playerFreeHandPos = new Vector2();
            }
        }
        public void NotMove()
        {
            var self = this.self;
            bool flag = self.input[0].x == 0 && self.input[0].y == 0;
            bool range = stopTime < 30;
            if (flag && range)
            {
                stopTime++;
            }
        }
        public void ChangeHand()
        {
            var self = this.self;
            bool range = changeHand < 5;
            if (range)
            {
                changeHand += 5;
            }
        }


        public void RockMake(Rock rock)
        {
            //检查玩家有没有做出正确的操作
            bool triga1 = self.input[0].x != 0 || self.input[0].y != 0;
            bool triga2 = stopTime > 15 && self.switchHandsCounter > 0;
            if (triga1 && triga2 && rock != null)
            {
                if (PlayerHook.rockModule.TryGetValue(rock, out var value))
                {
                    PlayerHook.rockModule.Remove(rock);
                }
                PlayerHook.rockModule.Add(rock, new SuperRockModule(rock));
            }



        }//确认是否属于超级投掷出去的石头并打上标记



        public void CallBackSpear()
        {
            if ((self.input[0].pckp || self.input[1].pckp) && self.eatMeat <= 1 && self.eatExternalFoodSourceCounter <= 1 && self.input[0].y>=0&&self.FreeHand()!=-1)
            {
                NewRope umbilical = null;
                //搜索房间里面的所有矛找一根合适的出来
                foreach (var obj in self.room.updateList)
                {
                    //如果这个东西是 丝线 而且 丝线的发射者是玩家 而且丝线上绑着矛
                    var testUmbilical = obj as NewRope;
                    bool connect = true;

                    if (testUmbilical != null && testUmbilical.spear != null && testUmbilical.player == self)
                    {
                        //防止玩家对自己手上的矛继续操作
                        if (testUmbilical.spear.grabbedBy.Count > 0 && testUmbilical.spear.grabbedBy[0].grabber == self)
                        {
                            continue;
                        }
                        //循环看看有没有哪一节的宽度小于0
                        for (int i = 0; i < testUmbilical.points.GetLength(0); i++)
                        {
                            if (testUmbilical.points[i, 3].x <= 0f)//如果如果小于零说明没连接
                            {
                                connect = false;
                            }
                        }
                        //如果矛的丝连接着而且还没找到过其他符合条件的丝,把要拉动的丝设定为这个丝
                        if (connect && umbilical == null)
                        {
                            umbilical = testUmbilical;
                        }
                        else if (connect)//如果已经找到过其他丝就比较两个丝哪个更近
                        {
                            //老矛离玩家的距离是否比新矛距离大
                            bool b = Math.Abs(umbilical.spear.firstChunk.pos.x - self.mainBodyChunk.pos.x) > Math.Abs(testUmbilical.spear.firstChunk.pos.x - self.mainBodyChunk.pos.x);
                            umbilical = b ? testUmbilical : umbilical;
                        }
                    }
                }

                //如果找到了符合条件的矛,而且矛没有震动
                if (umbilical != null && umbilical.spear != null && self.room == umbilical.spear.room && umbilical.spear.vibrate <= 0)
                {
                    var spear = umbilical.spear;

                    //是否做出快速唤回动作
                    bool flagFastBackAction = self.input[0].y > 0;
                    //检查能不能直视到
                    bool flagSee = self.room.VisualContact(spear.firstChunk.pos, self.firstChunk.pos);
                    //检查距离
                    var range = Vector2.Distance(spear.firstChunk.pos, self.bodyChunks[1].pos);
                    //求出玩家相对于矛的方位
                    Vector2 whereIsPlayer = (self.bodyChunks[1].pos - spear.firstChunk.pos).normalized;
                    //离矛最近的丝的方向
                    Vector2 whenRopeClose = (umbilical.points[umbilical.points.GetLength(0) - 4, 0] - spear.firstChunk.pos).normalized;


                    //稍微加固丝线
                    for (int i = 0; i < umbilical.points.GetLength(0); i++)
                    {
                        umbilical.points[i, 3].x = 30f;
                    }


                    //如果插到墙上就拔下来然后变成自由状态,或爬墙
                    if (spear.hasHorizontalBeamState && spear.mode == Weapon.Mode.StuckInWall)//如果线在墙上就清理一下,防止残留
                    {
                        //爬墙
                        int canGrab = 0;
                        for (int i = 0; i < 10; i++)
                        {
                            if (self.input[i].jmp|| self.input[0].jmp)
                            {
                                canGrab++;
                            }
                        }
                        if (range > 1 && spear.firstChunk.pos.y+3 > self.mainBodyChunk.pos.y && self.gravity > 0&&canGrab>2)
                        {
                            if (self.wantToPickUp>0)
                            {
                                self.mainBodyChunk.vel -= whereIsPlayer*1.5f;
                                spear.vibrate += 5;
                            }
                            else
                            {
                                self.mainBodyChunk.vel -= whereIsPlayer *2f;
                                self.jumpBoost = 1.5f;
                            }
                            
                            //爬墙的手部动作
                            if (handTochRopeCont <=0)
                            {
                                handTochRopeCont = 7;
                                playerUsingUmbilical = umbilical;
                            }
                            RopeAndHandKeepMove(umbilical.points[0, 0],new Vector2(umbilical.points[umbilical.points.GetLength(0)-1,0].x,self.mainBodyChunk.pos.y));
                            if ( handTochRopeCont > 0&& umbilical != null && umbilical.room == self.room && umbilical.spear != null && umbilical.spear.room == self.room && dragPointSeted && startAndEndSeted)
                            {
                                for (int i = 2; i < umbilical.points.GetLength(0) - 1; i++)
                                {
                                    if (Vector2.Distance(umbilical.points[i, 0], self.mainBodyChunk.pos) <40)
                                    {
                                        Vector2 trueoPos = Vector2.Lerp(playerFreeHandPos, spear.firstChunk.pos, i / umbilical.points.GetLength(0));
                                        umbilical.points[i, 0] = playerFreeHandPos;
                                    }
                                    else
                                    {
                                        Vector2 trueoPos = Vector2.Lerp(playerFreeHandPos, spear.firstChunk.pos, i / umbilical.points.GetLength(0));
                                        umbilical.points[i, 0] = Vector2.Lerp(umbilical.points[i, 0], trueoPos, 0.3f);
                                    }
                                }
                            }
                            return;
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
                        if (self.wantToPickUp > 0)
                        {
                            //拉绳子手部动作
                            if (handTochRopeCont <= 0)
                            {
                                handTochRopeCont = 3;
                                playerUsingUmbilical = umbilical;
                            }
                            RopeAndHandKeepMove(self.mainBodyChunk.pos, spear.firstChunk.pos);
                            if (handTochRopeCont > 0 && umbilical != null && umbilical.room == self.room && umbilical.spear != null && umbilical.spear.room == self.room && dragPointSeted && startAndEndSeted)
                            {
                                for (int i = 2; i < umbilical.points.GetLength(0) - 1; i++)
                                {
                                    Vector2 trueoPos = Vector2.Lerp(playerFreeHandPos, spear.firstChunk.pos, i / umbilical.points.GetLength(0));
                                    umbilical.points[i, 0] = Vector2.Lerp(umbilical.points[i, 0], trueoPos, 0.6f);
                                }
                            }

                            //玩家受到拉力
                            float massPower = spear.stuckInObject.TotalMass > 20 ? 20 : spear.stuckInObject.TotalMass;
                            self.mainBodyChunk.vel-= whereIsPlayer* (massPower/ self.TotalMass)/1.5f;
                            spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel += whereIsPlayer * (self.TotalMass/ spear.stuckInObject.TotalMass)/1.5f*2;
                        }
                        else if(!Custom.DistLess(self.mainBodyChunk.pos,spear.stuckInChunk.pos,60))
                        {
                            if (self.input[0].jmp)
                            {
                                self.mainBodyChunk.vel -= whereIsPlayer * 3;
                            }
                            spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel += whereIsPlayer * 3;
                        }
                        
                    }
                    //对拿着这个矛的生物操作
                    else if (spear.mode == Weapon.Mode.Carried && spear.grabbedBy[0].grabber != self && spear.grabbedBy[0].grabber != null)
                    {
                        spear.grabbedBy[0].grabber.Stun(40);
                    }



                    //防止吃东西 吐东西
                    if (spear.mode != Weapon.Mode.Carried)
                    {
                        self.swallowAndRegurgitateCounter = 0;
                        self.slugOnBack.counter= 0;
                    }

                    //在无重力情况下给玩家施加移动力
                    if (spear.mode != Weapon.Mode.Carried && self.gravity <= 0)
                    {
                        self.mainBodyChunk.vel -= whereIsPlayer / 2;
                    }



                    //如果玩家离矛很近而且可以直视矛而且按了拿取按键就拿起矛
                    if (range < 50 && flagSee && spear.mode != Weapon.Mode.Carried)
                    {
                        if (self.FreeHand() != -1)
                        {
                            handTochRopeCont = 0;
                            playerUsingUmbilical = null;
                            startAndEndSeted = false;
                            self.SlugcatGrab(spear, self.FreeHand());
                            self.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, spear.firstChunk);
                            spear.vibrate += 10;
                            spear.canBeHitByWeapons = true;//让矛可以挡下攻击
                        }
                    }
                    //回收矛模式
                    else if (flagFastBackAction && range > 50)
                    {
                        if (handTochRopeCont < 1)
                        {
                            handTochRopeCont = 20;
                        }
                        playerUsingUmbilical = umbilical;
                        RopeAndHandKeepMove(spear.firstChunk.pos, umbilical.points[4, 0]);

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
                            if (handTochRopeCont > 0 && umbilical != null && umbilical.room == self.room && umbilical.spear != null && umbilical.spear.room == self.room && dragPointSeted && startAndEndSeted)
                            {
                                for (int i = 2; i < umbilical.points.GetLength(0) - 1; i++)
                                {
                                    if (Vector2.Distance(umbilical.points[i, 0], self.mainBodyChunk.pos) < 40)
                                    {
                                        Vector2 trueoPos = Vector2.Lerp(playerFreeHandPos, spear.firstChunk.pos, i / umbilical.points.GetLength(0));
                                        umbilical.points[i, 0] = playerFreeHandPos;
                                    }
                                    else
                                    {
                                        Vector2 trueoPos = Vector2.Lerp(playerFreeHandPos, spear.firstChunk.pos, i / umbilical.points.GetLength(0));
                                        umbilical.points[i, 0] = Vector2.Lerp(umbilical.points[i, 0], trueoPos, 0.4f);
                                    }
                                }
                            }
                        }
                    }
                    //攻击模式
                    else if (self.input[1].pckp && !self.input[0].pckp && range > 35)
                    {
                        int pckpTime = 0;
                        for (int i = 0; i < 7; i++)
                        {
                            if (self.input[i].pckp)
                            {
                                pckpTime++;
                            }
                        }
                        if (pckpTime>5)
                        {
                            return;
                        }

                        handTochRopeCont = 3;
                        playerUsingUmbilical = umbilical;
                        RopeAndHandKeepMove(umbilical.points[0, 0], self.mainBodyChunk.pos + whereIsPlayer * 25);
                        for (int i = 0; i < umbilical.points.GetLength(0) / 3; i++)
                        {
                            umbilical.points[i, 2] += whereIsPlayer * 30;
                        }



                        spear.ChangeMode(Weapon.Mode.Thrown);
                        spear.spearDamageBonus *= 0.9f;
                        spear.thrownBy = self;
                        spear.throwDir = new IntVector2(Convert.ToInt32(whereIsPlayer.x), Convert.ToInt32(whereIsPlayer.y));;

                        spear.rotation = spear.throwDir.ToVector2();
                        spear.firstChunk.pos -= whereIsPlayer;
                        spear.firstChunk.vel.x = spear.throwDir.x *60*spear.spearDamageBonus;

                        if (spear.gravity > 0)
                        {
                            spear.firstChunk.vel.y = 2.9f;
                        }

                    }
                    //慢速模式
                    else if (self.input[0].pckp)
                    {

                        if (handTochRopeCont < 1)
                        {
                            handTochRopeCont = 10;
                            playerUsingUmbilical = umbilical;
                        }
                        RopeAndHandKeepMove(umbilical.points[0, 0], spear.firstChunk.pos);

                        if (handTochRopeCont > 0 && umbilical != null && umbilical.room == self.room && umbilical.spear != null && umbilical.spear.room == self.room && dragPointSeted && startAndEndSeted)
                        {
                            for (int i = 2; i < umbilical.points.GetLength(0) - 1; i++)
                            {
                                if (Vector2.Distance(umbilical.points[i, 0], self.mainBodyChunk.pos) < 40)
                                {
                                    Vector2 trueoPos = Vector2.Lerp(playerFreeHandPos, spear.firstChunk.pos, i / umbilical.points.GetLength(0));
                                    umbilical.points[i, 0] = playerFreeHandPos;
                                }
                                else
                                {
                                    Vector2 trueoPos = Vector2.Lerp(playerFreeHandPos, spear.firstChunk.pos, i / umbilical.points.GetLength(0));
                                    umbilical.points[i, 0] = Vector2.Lerp(umbilical.points[i, 0], trueoPos, 0.3f);
                                }
                            }
                        }

                        spear.firstChunk.vel += whereIsPlayer * 1.3f + whenRopeClose * 0.2f; ;
                        spear.rotation = -(self.bodyChunks[1].pos - spear.firstChunk.pos).normalized;
                        if (spear.gravity > 0)
                        {
                            spear.firstChunk.vel.y += 0.4f;
                        }
                    }
                    else if (spear.mode == Weapon.Mode.StuckInCreature)
                    {
                        spear.ChangeMode(Weapon.Mode.Free);
                    }
                }
            }
        }//唤回矛

        public void RopeAndHandKeepMove(Vector2 start, Vector2 end)
        {
            var umbilical = this.playerUsingUmbilical;
            this.handStart = start;
            this.handEnd = end;
            startAndEndSeted = true;
        }//设置手的移动点
        public void RopeAndHandKeepMove()
        {
            Vector2 start = this.handStart;
            Vector2 end = this.handEnd;
            var umbilical = this.playerUsingUmbilical;
            if (handTochRopeCont > 0 && umbilical != null && umbilical.room == self.room && umbilical.spear != null && umbilical.spear.room == self.room && dragPointSeted && startAndEndSeted)
            {
                //改线的位置让其跟手
                umbilical.points[1, 0] = playerFreeHandPos;
                //改手的位置从开始到结束
                playerHandWantTochPos = Vector2.Lerp(end, start, handTochRopeCont / 5);
            }
        }//让线配合手移动

    }



}
