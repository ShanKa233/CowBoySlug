using CowBoySlug.Mechanics.RopeSkill;
using RWCustom;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CowBoySlug.Mechanics
{
    public static class Hands
    {
        #region 拉绳动画手感参数(想调力度/速度就改这里)
        public const int BurstFrames = 6; // 发力窗口持续帧数:刚触发 Pulling 的这几帧,手会被猛地甩向绳子
        public const float BurstPush = 8f; // 发力窗口每帧把手沿绳子推出去的距离(px),越大越"猛"
        public const float HuntSpeed = 16f; // 手追绳子目标点的速度(px/帧),原版默认只有7
        public const float PullReachPerCount = 1.5f; // 拉绳目标点的伸出距离 = pullCount × 这个值(原6,已两次减半)
        public const float GrabDist = 40f; // 手离目标点多近就"啪"地直接贴住(抓住的顿挫感)
        public const float OtherHandBrace = 2f; // 空闲手向后撑的距离,身体显得在用力
        public const float MaxHandDist = 40f; // 手臂长度:手离锚点(身体)的最大距离,超过会被拽回,防止整条胳膊脱出去
        #endregion

        public static void Hook()
        {
            //On.Player.ctor += Player_ctor;//给牛仔猫加入使用手的字典里
            //On.Player.Update += Player_Update;


            On.PlayerGraphics.Update += PlayerGraphics_Update;
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            // 原版手部动画先照常跑,下面只在"拉绳中"时覆盖手的动作
            orig.Invoke(self);

            // 先推进手动画状态:pullCount 每帧递减,拉绳的"力"会随时间消退
            self.player.HandData().Update();

            if (!UserData.modules.TryGetValue(self.player, out var module)) return;
            //if (self.player.FreeHand() == -1) return;


            if (self.player.Consious && self.player.sleepCurlUp <= 0)
            {
                //var rope = RopeMaster.NiceRope(self.player);
                var handData = self.player.HandData();
                var rope = handData.pullinggRope;


                // pullCount > 0 说明刚触发过 Pulling():手会被"推"向绳子上离身体更远的点,
                // 之后这个点每帧朝身体缩回来,循环往复,看起来就是在一截一截地往回拽绳子
                if (handData.pullCount > 0 && handData.pullinggRope != null)
                {
                    var pullHand = handData.handEngagedInPull; // Pulling() 时记录下来的拉绳手(一般是 FreeHand,哪只空着用哪只)
                    if (pullHand < 0 || pullHand > 1) return; // 两只手都占着(FreeHand()==-1)时演不了拉绳动画,直接跳过

                    var playerToRopeDir = Custom.DirVec(rope.points[0, 0], rope.RopeShowPos(1)); // 从绳子贴玩家那一端指向绳子前段,即"顺着绳子往远处"的方向

                    // 手要够到的目标点:从贴身处出发、沿绳子方向走 pullCount × PullReachPerCount 的距离,再投影回绳子上。
                    // pullCount 在 Pulling() 时被加到 10~20,之后逐帧递减 → 目标点由远及近,手就做出"伸手拉一把"的循环
                    var targetPoint = Custom.ClosestPointOnLine(rope.points[0, 0], rope.RopeShowPos(1), rope.points[0,0] + playerToRopeDir * handData.pullCount * PullReachPerCount * handData.reachMul);

                    // 发力窗口(刚触发 Pulling 的几帧):直接把手的位置沿绳子方向猛推出去。
                    // 故意改 pos 而不是 vel —— vel 会被原版每帧的追踪逻辑覆盖,直接推位置才有"甩"出去的爆发感
                    if (handData.burst > 0)
                    {
                        self.hands[pullHand].pos += playerToRopeDir * BurstPush;
                    }

                    // 让手进入原版"伸手够东西"模式。注意这是一次性开关:原版每帧会把它清零,所以必须每帧重设
                    self.hands[pullHand].reachingForObject = true;

                    if (Custom.DistLess(self.hands[pullHand].pos, targetPoint, GrabDist))
                    {
                        // 离目标点够近:"啪"地直接抓住绳子,这一下顿挫是"大力"感的关键
                        self.hands[pullHand].pos = targetPoint;
                    }
                    else
                    {
                        // 离得远:追,但把追踪速度调高,手伸出去利落不磨蹭
                        self.hands[pullHand].absoluteHuntPos = targetPoint;
                        self.hands[pullHand].huntSpeed = HuntSpeed;
                    }

                    // 空闲手向后撑一小段,身体显得在使劲;撑完会被手臂锚点自动拉回身体
                    self.hands[1 - pullHand].pos -= playerToRopeDir * OtherHandBrace;

                    // 手臂长度钳制:原版的锚点约束是软性的(20px),爆发甩手和贴绳都可能把手甩到身体外面,
                    // 超过 MaxHandDist 就把手沿"身体→手"方向拉回可达范围(放在钉绳子之前,绳子也会跟着被拽回身体)
                    var handAnchor = self.hands[pullHand].connection.pos;
                    if (!Custom.DistLess(self.hands[pullHand].pos, handAnchor, MaxHandDist))
                    {
                        self.hands[pullHand].pos = handAnchor + Custom.DirVec(handAnchor, self.hands[pullHand].pos) * MaxHandDist;
                    }


                    // 遍历所有绳子段,找出离手最近的那一段:要让绳子"穿过"手的那个点
                    int min = 0;
                    for (int i = 1; i < handData.pullinggRope.points.GetLength(0); i++)
                    {
                        var minDIs = Vector2.Distance(self.hands[pullHand].pos, handData.pullinggRope.points[min, 0]);
                        var thisDis = Vector2.Distance(self.hands[pullHand].pos, handData.pullinggRope.points[i, 0]);
                        min = minDIs < thisDis ? min : i;
                    }
                    // 把最近的绳子点直接钉在手的位置上 → 绳子像被手捏着,手往哪走绳子就跟到哪
                    handData.pullinggRope.points[min,0]= self.hands[pullHand].pos;

                    // 标记"这一帧在拉绳":拉绳一停,下面的 else if 会在停止的那一帧把手收回
                    handData.wasPulling = true;


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
                else if (handData.wasPulling)
                {
                    // 拉绳刚停止的这一帧:把拉绳手"啪"地收进身体(Retracted:pos/vel 由身体接管,
                    // 残留目标点和速度全部作废,不会乱晃/乱指)。只收这一次,
                    // 下一帧原版就会自己把手放回默认位置,生硬但干净的转折
                    handData.wasPulling = false;
                    self.hands[handData.handEngagedInPull].mode = Limb.Mode.Retracted;
                }

            }
        }



        /// <summary>
        /// 把进度 t 按 bezier 缓动曲线重新映射:输入两个控制点 (ax,ay)(bx,by),输出缓动后的进度。
        /// 例如"前快后慢"的手部动作就可以用它,让拉绳的手伸出去利落、收回来有缓冲
        /// </summary>
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
    /// <summary>
    /// 拉绳动画的状态:记录"正在拉哪条绳子、还剩多少力度、用哪只手"
    /// </summary>
    public class HandData
    {
        public Simulator pullinggRope; // 正在被拉的绳子
        public int pullCount = 0; // 剩余"拉"的力度(帧数),每帧-1,归 0 后手恢复常态
        public int handEngagedInPull; // 这次拉绳用的是哪只手(0=左手 1=右手)
        public int burst = 0; // 发力窗口剩余帧数:>0 期间手会被猛地甩向绳子
        public float reachMul = 1f; // 本次拉绳的目标点距离倍率(钩爪模式传2,手伸得更远)
        public bool wasPulling = false; // 上一帧是否在拉绳:用于检测拉绳停止的瞬间


        public void Update()
        {
            if (pullCount > 0) pullCount--;
            if (burst > 0) burst--;


        }

        /// <summary>
        /// 各 Handler(钩爪/钓竿/回收等)在拉绳瞬间调用,触发一次拉绳动画
        /// </summary>
        /// <param name="count">这次动作的力度,pullCount 会累加到这个值(越大手伸得越远、动作持续越久)</param>
        /// <param name="rope">正在拉的绳子</param>
        /// <param name="useHand">用哪只手</param>
        /// <param name="reachMul">目标点距离倍率,钩爪等"伸手更远"的模式传 &gt;1</param>
        public void Pulling(int count, Simulator rope, int useHand, float reachMul = 1f)
        {
            // 上一次动画快结束(剩余不足 2 帧)时才允许叠加,避免动画被频繁重置、手一直伸着收不回来
            if (pullCount < 2)
            {
                pullCount += count;
                burst = Hands.BurstFrames; // 和新动作一起刷新发力窗口
            }

            pullinggRope = rope;

            handEngagedInPull = useHand;

            this.reachMul = reachMul; // 每次调用都更新倍率(不放进 pullCount<2 判断里,连续调用也能生效)

        }
    }

    public class HandModules
    {
        public Player player;
        public Simulator rope;


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

        public void move(Vector2 start, Vector2 end, int moveCount, float cycleTime, Simulator rope, float[] abxy, bool look)
        {
            this.rope = rope;
            this.posStart = start;
            this.posEnd = end;
            this.moveCount = moveCount;
            this.cycleTime = cycleTime;
            this.abxy = abxy;
            this.look = look;
        }


        public HandModules(Player player)
        {
            this.player = player;
        }
    }
}
