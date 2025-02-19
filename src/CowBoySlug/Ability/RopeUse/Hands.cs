using CowBoySLug;
using RWCustom;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CowBoySlug.CowBoy.Ability.RopeUse
{
    public static class Hands
    {
        public static void Hook()
        {
            //On.Player.ctor += Player_ctor;//给牛仔猫加入使用手的字典里
            //On.Player.Update += Player_Update;


            On.PlayerGraphics.Update += PlayerGraphics_Update;
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig.Invoke(self);

            self.player.HandData().Update();

            if (!RopeMaster.modules.TryGetValue(self.player, out var module)) return;
            //if (self.player.FreeHand() == -1) return;


            if (self.player.Consious && self.player.sleepCurlUp <= 0)
            {
                //var rope = RopeMaster.NiceRope(self.player);
                var handData = self.player.HandData();
                var rope = handData.pullinggRope;


                if (handData.pullCount > 0 && handData.pullinggRope != null)
                {
                    var pullHand = handData.handEngagedInPull;
                    var playerToRopeDir = Custom.DirVec(rope.points[0, 0], rope.RopeShowPos(1));

                    self.hands[pullHand].reachingForObject = true;
                    self.hands[pullHand].absoluteHuntPos = Custom.ClosestPointOnLine(rope.points[0, 0], rope.RopeShowPos(1), rope.points[0,0] + playerToRopeDir * handData.pullCount*4);


                    int min = 0;
                    for (int i = 1; i < handData.pullinggRope.points.GetLength(0); i++)
                    {
                        var minDIs = Vector2.Distance(self.hands[pullHand].pos, handData.pullinggRope.points[min, 0]);
                        var thisDis = Vector2.Distance(self.hands[pullHand].pos, handData.pullinggRope.points[i, 0]);
                        min = minDIs < thisDis ? min : i; 
                    }
                    handData.pullinggRope.points[min,0]= self.hands[pullHand].pos;


                    //self.hands[pullHand].pos = Custom.ClosestPointOnLine(rope.points[0, 0], rope.RopeShowPos(1), rope.points[0, 0] + playerToRopeDir * handData.pullCount * 2);

                    //if (Custom.DistLess(self.hands[pullHand].pos+playerToRopeDir*40, , 40f))
                    //{
                    //    self.hands[pullHand].pos = self.thrownObject.firstChunk.pos;
                    //}
                    //else
                    //{
                    //    self.hands[pullHand].vel += Custom.DirVec(self.hands[pullHand].pos, self.thrownObject.firstChunk.pos) * 6f;
                    //}

                    //self.hands[1 - self.handEngagedInThrowing].vel -= Custom.DirVec(self.hands[pullHand].pos, self.thrownObject.firstChunk.pos) * 3f;
                }

            }
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

        //private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        //{
        //    orig.Invoke(self, eu);
        //    if (!module.TryGetValue(self, out var handModules)) return;//测有没有在手的字典里

        //    if (!(handModules.moveCount > 0)) { handModules.time = 0; return; }//测手能不能动,不能就把动画帧归0
        //    if (handModules.rope == null || handModules.hand() == null || handModules.rope.room != self.room) return;//测动的条件满足不满足

        //    if (handModules.moveCount > 0) handModules.moveCount--;//减少动的时间
        //    var umbilical = handModules.rope;

        //    Vector2 posStart = handModules.posStart;
        //    Vector2 posEnd = handModules.posEnd;

        //    if (handModules.look) (self.graphicsModule as PlayerGraphics).LookAtPoint(umbilical.spear.firstChunk.pos, 3f);//拉矛的时候看着矛
        //    handModules.hand().reachingForObject = true;

        //    handModules.hand().absoluteHuntPos = Vector2.Lerp(posStart, posEnd, handModules.getT());

        //    ////让绳子往手上贴
        //    //for (int i = 2; i < umbilical.points.GetLength(0) - 1; i++)
        //    //{
        //    //    if (Vector2.Distance(umbilical.points[i, 0], player.mainBodyChunk.pos) < 40)
        //    //    {
        //    //        umbilical.points[i, 0] = handModules.handPos();
        //    //    }
        //    //    else
        //    //    {
        //    //        Vector2 trueoPos = Vector2.Lerp(handModules.handPos(), umbilical.spear.firstChunk.pos, i / umbilical.points.GetLength(0));
        //    //        umbilical.points[i, 0] = Vector2.Lerp(umbilical.points[i, 0], trueoPos, 0.3f);
        //    //    }
        //    //}

        //    handModules.time = handModules.time > handModules.cycleTime ? 0 : handModules.time + 1;//超过了一轮动作的时间就从动作最开始重新开始
        //}


        //public static ConditionalWeakTable<Player, HandModules> module = new ConditionalWeakTable<Player, HandModules>();
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            //module.Add(self, new HandModules(self));
        }
    }

    public static class HandAnimation
    {
        public static ConditionalWeakTable<Player, HandData> modules = new ConditionalWeakTable<Player, HandData>();
        public static HandData HandData(this Player player) => modules.GetValue(player, (_) => new HandData());


    }
    public class HandData
    {
        public CowRope pullinggRope;
        public int pullCount = 0;
        public int handEngagedInPull;


        public void Update()
        {
            if (pullCount > 0) pullCount--;


        }
        public void Pulling(int count, CowRope rope, int useHand)
        {
            if (pullCount < 2)
            {
                pullCount += count;
            }

            pullinggRope = rope;

            handEngagedInPull = useHand;

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

        public float[] abxy = { 0.88f, -0.01f, 0.59f, 0.99f };//用于记录曲线类型

        public float getT()
        {
            float t = time / cycleTime;
            return Hands.Cubicbezier(abxy[0], abxy[1], abxy[2], abxy[3], t);
        }

        public void move(Vector2 start, Vector2 end, int moveCount, float cycleTime, CowRope rope, float[] abxy, bool look)
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
