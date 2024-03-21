using CowBoySLug;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CowBoySlug.CowBoy.Ability.RopeUse
{
    public static class Hands
    {
        public static void Hook()
        {
            On.Player.ctor += Player_ctor;//给牛仔猫加入使用手的字典里
            On.Player.Update += Player_Update;
        }
        public static float Cubicbezier(float ax, float ay, float bx, float by, float t)
        {
            //see https://cubic-bezier.com/
            Vector2 a = Vector2.zero;
            Vector2 a1 = new Vector2(ax, ay);
            Vector2 b1 = new Vector2(bx, by);
            Vector2 b = Vector2.one;

            Vector2 c1 = Vector2.Lerp(a, a1, t);
            Vector2 c2 = Vector2.Lerp(b1, b, t);

            return Vector2.Lerp(c1, c2, t).y;

        }
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (!module.TryGetValue(self, out var handModules)) return;//测有没有在手的字典里

            if (!(handModules.moveCount > 0)) { handModules.time = 0; return; }//测手能不能动,不能就把动画帧归0
            if (handModules.rope == null || handModules.hand() == null || handModules.rope.room != self.room) return;//测动的条件满足不满足

            if (handModules.moveCount > 0) handModules.moveCount--;//减少动的时间
            var umbilical = handModules.rope;

            Vector2 posStart = handModules.posStart;
            Vector2 posEnd = handModules.posEnd;

            if (handModules.look) (self.graphicsModule as PlayerGraphics).LookAtPoint(umbilical.spear.firstChunk.pos, 3f);//拉矛的时候看着矛
            handModules.hand().reachingForObject = true;

            handModules.hand().absoluteHuntPos = Vector2.Lerp(posStart, posEnd, handModules.getT());

            ////让绳子往手上贴
            //for (int i = 2; i < umbilical.points.GetLength(0) - 1; i++)
            //{
            //    if (Vector2.Distance(umbilical.points[i, 0], self.mainBodyChunk.pos) < 40)
            //    {
            //        umbilical.points[i, 0] = handModules.handPos();
            //    }
            //    else
            //    {
            //        Vector2 trueoPos = Vector2.Lerp(handModules.handPos(), umbilical.spear.firstChunk.pos, i / umbilical.points.GetLength(0));
            //        umbilical.points[i, 0] = Vector2.Lerp(umbilical.points[i, 0], trueoPos, 0.3f);
            //    }
            //}

            handModules.time = handModules.time > handModules.cycleTime? 0 : handModules.time + 1;//超过了一轮动作的时间就从动作最开始重新开始
        }


        public static ConditionalWeakTable<Player, HandModules> module = new ConditionalWeakTable<Player, HandModules>();
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            module.Add(self, new HandModules(self));
        }
    }
    public class HandModules
    {
        public Player player;
        public CowRope rope;


        #region 用于控制动画方式的参数
        public int moveCount = 0;//动画持续时间
        public float cycleTime = 0;//用于确定一轮动作的时间
        public int time = 0;//用于控制动画频率
        public bool look = true;


        public Vector2 posStart;//手开始的位置
        public Vector2 posEnd;//手结束的位置


        #endregion


        public Vector2 ropePos;//手想要碰的点
        public Vector2 handPos()
        {
            if (player.FreeHand() == -1)
            {
                return player.mainBodyChunk.pos;
            }
            return (player.graphicsModule as PlayerGraphics).hands[player.FreeHand()].pos;
        }//手的位置
        public SlugcatHand hand()
        {
            if (player.FreeHand() == -1)
            {
                return null;
            }
            return (player.graphicsModule as PlayerGraphics).hands[player.FreeHand()];
        }//蛞蝓猫的可用手

        public float[] abxy = {0.88f, -0.01f, 0.59f, 0.99f};//用于记录曲线类型

        public float getT()
        {
            float t = time/cycleTime;
            return Hands.Cubicbezier(abxy[0], abxy[1], abxy[2], abxy[3],t);
        }

        public void move (Vector2 start,Vector2 end,int moveCount,float cycleTime, CowRope rope,float[] abxy,bool look)
        {
            this.rope = rope;
            this.posStart = start;
            this.posEnd = end;
            this.moveCount = moveCount;
            this.cycleTime = cycleTime;
            this.abxy = abxy;
            this.look = look;
            if (Plugin.enableGhostPlayer && !GhostPlayerImports.IsNetworkPlayer(player))
            {
                GhostPlayerImports.TrySendImportantValue(new CowBoyData() { id = GhostPlayerImports.GetPlayerNetID(player), type = 1 }, false);
            }
        }


        public HandModules(Player player)
        {
            this.player = player;
        }
    }
}
