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
            On.PlayerGraphics.Update += PlayerGraphics_Update;
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            // 原版手部动画先照常跑,下面只在"拉绳中"时覆盖手的动作
            orig.Invoke(self);

            // 先推进手动画状态:pullCount 每帧递减,拉绳的"力"会随时间消退
            self.player.HandData().Update();

            if (!UserData.modules.TryGetValue(self.player, out var module)) return;
            if (!self.player.Consious || self.player.sleepCurlUp > 0) return; // 昏迷/蜷缩时不演拉绳动画

            var handData = self.player.HandData();
            if (handData.pullCount > 0 && handData.pullinggRope != null)
            {
                UpdatePullHand(self, handData); // 拉绳中:演"甩手-抓绳-回收"的循环
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

        /// <summary>
        /// 拉绳中的手部动画:目标点沿绳子由远及近地回缩,手被甩过去、贴住绳子、带着绳子收回来,
        /// 一轮 pullCount 衰减完就形成"伸手拉一把"的动作
        /// </summary>
        private static void UpdatePullHand(PlayerGraphics self, HandData handData)
        {
            var pullHand = handData.handEngagedInPull; // Pulling() 时记录下来的拉绳手(一般是 FreeHand,哪只空着用哪只)
            if (pullHand < 0 || pullHand > 1) return; // 两只手都占着(FreeHand()==-1)时演不了拉绳动画,直接跳过

            var rope = handData.pullinggRope;
            var playerToRopeDir = Custom.DirVec(rope.points[0, 0], rope.RopeShowPos(1)); // 从绳子贴玩家那一端指向绳子前段,即"顺着绳子往远处"的方向

            // 手要够到的目标点:从贴身处出发、沿绳子方向走 pullCount × PullReachPerCount × reachMul 的距离,再投影回绳子上。
            // pullCount 在 Pulling() 时被加到 10~20,之后逐帧递减 → 目标点由远及近
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

            // 遍历所有绳子段,找出离手最近的那一段,把它直接钉在手的位置上 → 绳子像被手捏着,手往哪走绳子就跟到哪
            int min = 0;
            for (int i = 1; i < rope.points.GetLength(0); i++)
            {
                var minDIs = Vector2.Distance(self.hands[pullHand].pos, rope.points[min, 0]);
                var thisDis = Vector2.Distance(self.hands[pullHand].pos, rope.points[i, 0]);
                min = minDIs < thisDis ? min : i;
            }
            rope.points[min,0]= self.hands[pullHand].pos;

            // 标记"这一帧在拉绳":拉绳一停,主循环会在停止的那一帧把手收回
            handData.wasPulling = true;
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
}
