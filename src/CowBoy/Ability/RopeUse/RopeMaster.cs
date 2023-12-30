using RWCustom;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;

namespace CowBoySlug.CowBoy.Ability.RopeUse
{
    public class RopeMaster
    {
        public static ConditionalWeakTable<Player,RopeMaster> modules = new ConditionalWeakTable<Player, RopeMaster>();//用来记录谁有这个字典

        public static readonly PlayerFeature<bool> RopeMasterFeature = PlayerBool("cowboyslug/rope_master");//能使用这个能力的词条
        public static readonly PlayerColor RopeColor = new PlayerColor("Rope");//绳子颜色

        public static void Hook()
        {
            On.Player.ctor += Player_ctor;
            On.Player.ThrownSpear += Player_ThrownSpear;
            On.Player.UpdateMSC += Player_UpdateMSC;
            On.Player.MovementUpdate += Player_MovementUpdate;
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            //if (!modules.TryGetValue(self, out var module)) return;
            //CallBackSpear(self);//召回矛
        }

        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);

            if (!modules.TryGetValue(self, out var mod)) return;//如果扔矛的是牛仔猫

            spear.vibrate += 2;//增加回收的cd

            //如果有线就跳过
            foreach (var item in mod.ropes)
            {
                //寻找矛上有没有其他的线
                if (item != null && item.spear != null && item.spear == spear)
                {//如果有线就加固线然后return
                    var umbilical = item;
                    if (umbilical.points.GetLength(0) > 10)
                    {
                        for (int i = 0; i < umbilical.points.GetLength(0); i++)
                        {
                            umbilical.points[i, 3].x = 200f;
                        }
                    }
                    return;
                }
            }
            //如果没线就弄个出来
            CowRope.SpawnRope(self, spear, Color.Lerp(self.ShortCutColor(), mod.ropeColor, 0.5f), mod.ropeColor);
        }

        private static void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
        {
            orig.Invoke(self);
            if (!modules.TryGetValue(self, out var module)) return;
            CallBackSpear(self);//召回矛
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature,world);
            if (RopeMasterFeature.TryGet(self,out var flag)&&flag)
            {
                modules.Add(self, new RopeMaster(self));
            }
        }


        public List<CowRope> ropes= new List<CowRope>();//用来记录所有绳子的list

        public Player player;//玩家

        public Vector2 handStart;
        public Vector2 handEnd;


        public bool startAndEndSeted = false;
        public bool dragPointSeted = false;

        public Color ropeColor = new Color(247 / 255f, 213 / 255f, 131 / 255f);
        

        public RopeMaster(Player player)
        {
            this.player = player;
        }


        public static bool CanNotCall(Player player)
        {
            bool flag = (player.input[0].pckp || player.input[1].pckp);//如果玩家在两帧内按下过拿取键
            bool flag2 = player.eatMeat <= 1 && player.eatExternalFoodSourceCounter <= 1;//而且玩家没有在吃东西
            bool flag3 = player.input[0].y >= 0 && player.FreeHand() != -1;//玩家没有按下而且有一只空手
            return !(flag && flag2 && flag3);
        }
        public static CowRope NiceRope(Player player)
        {
            if (!RopeMaster.modules.TryGetValue(player, out var mod)) return null;//如果扔矛的是牛仔猫
            CowRope umbilical = null;
            //搜索房间里面的所有矛找一根合适的出来
            foreach (var obj in mod.ropes)
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

        public static bool WhenSpearOnSomeThing(Spear spear, Player player, float range, Vector2 whereIsPlayer, Vector2 whenRopeClose, CowRope umbilical)
        {
            var playerToRopeDir= Custom.DirVec(player.mainBodyChunk.pos, umbilical.RopePos(1));
            //如果插到墙上就拔下来然后变成自由状态,或爬墙
            if ((spear.hasHorizontalBeamState && spear.mode == Weapon.Mode.StuckInWall))//如果线在墙上就清理一下,防止残留
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
                if (range > 10&& player.gravity > 0 && canGrab > 2)
                {
                    //player.standing = false;
                    if (player.wantToPickUp > 0)
                    {
                        player.mainBodyChunk.vel += playerToRopeDir* 1.5f;
                        //spear.vibrate += 5;
                    }
                    else
                    {
                        player.mainBodyChunk.vel += playerToRopeDir * 2f;
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
            if (CanNotCall(player)) return;
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
            Vector2 fristPointForSpear = Custom.DirVec(spear.firstChunk.pos, umbilical.RopePos(umbilical.rope.TotalPositions - 2));
            //离矛最近的丝的方向
            Vector2 fristPointForPlayer = Custom.DirVec(umbilical.playerPos, umbilical.RopePos(1));

            if (WhenSpearOnSomeThing(spear, player, range, fristPointForSpear, fristPointForPlayer, umbilical)) return;
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
                player.mainBodyChunk.vel -= fristPointForSpear / 2;
            }



            //如果玩家离矛很近而且可以直视矛而且按了拿取按键就拿起矛
            if (range < 50 && flagSee && spear.mode != Weapon.Mode.Carried)
            {
                if (player.FreeHand() != -1)
                {
                    player.SlugcatGrab(spear, player.FreeHand());
                    player.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, spear.firstChunk);
                    //spear.vibrate += 10;
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

                spear.firstChunk.vel = fristPointForSpear * 27+ Custom.RNV();
                spear.setRotation = -fristPointForSpear.normalized;
                //if (flagSee)
                //{
                //    spear.SetRandomSpin();
                //}
                if (spear.gravity > 0)
                {
                    spear.firstChunk.vel.y += 10;
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
                    handModules.move(umbilical.points[0, 0], player.mainBodyChunk.pos + fristPointForSpear * 25, 20, 15f, umbilical, t, true);
                }
                for (int i = 0; i < umbilical.points.GetLength(0) / 3; i++)
                {
                    umbilical.points[i, 2] += fristPointForSpear * 30;
                }



                spear.ChangeMode(Weapon.Mode.Thrown);
                spear.spearDamageBonus *= 0.9f;
                spear.thrownBy = player;
                spear.throwDir = new IntVector2(Convert.ToInt32(fristPointForSpear.x), Convert.ToInt32(fristPointForSpear.y)); ;

                spear.rotation = spear.throwDir.ToVector2();
                spear.firstChunk.pos -= fristPointForSpear;
                //spear.firstChunk.vel.x = spear.throwDir.x * 60 * spear.spearDamageBonus;
                spear.firstChunk.vel = spear.throwDir.ToVector2() * 60 * spear.spearDamageBonus;

                if (spear.gravity > 0)
                {
                    //spear.firstChunk.vel.y = 2.9f;
                }

            }
            //慢速模式
            else if (player.input[0].pckp)
            {
                //控制手和绳子
                //if (Hands.module.TryGetValue(player, out var handModules))
                //{
                //    float[] t = { .9f, -0.04f, .72f, .72f };
                //    handModules.move(umbilical.points[0, 0], spear.firstChunk.pos, 5, 10f, umbilical, t, true);
                //}

                spear.firstChunk.vel += fristPointForSpear * 2f+Custom.RNV()*0.2f;
                spear.setRotation= -fristPointForSpear.normalized;

                if (spear.gravity > 0)
                {
                    //spear.firstChunk.vel.y += 0.4f;
                }
            }
            else if (spear.mode == Weapon.Mode.StuckInCreature)
            {
                spear.ChangeMode(Weapon.Mode.Free);
            }

        }//唤回矛


    }
}
