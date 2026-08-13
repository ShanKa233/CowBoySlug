namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 绳子技能按键组合的抽象基类。
    /// 按键按"操作模式"分组,每个方法对应一个动作的按键判定:
    ///   回收模式(Retrieve):让矛回来  —— FastRetrieve 快速唤回 / SlowRetrieve 慢速收线
    ///   钩爪模式(Grapple)  :让玩家移动 —— GrapplePull 墙或飞行锚点 / GrappleCreaturePull 插在生物上拽人
    ///   钓竿模式(Fishing)  :让附着物移动 —— FishingPull 拖拽被矛插住的生物
    ///   攻击模式(Attack)   :甩矛攻击  —— AttackTrigger / MashCancel
    ///   其他:BreakRope 断绳
    /// 新建组合继承本类或 RopeControlsV1 只重写想改的动作。
    /// 切换组合:修改 RopeConfig.Controls 的赋值。
    ///
    /// 添加新组合示例:
    /// <code>
    /// public class RopeControlsV3 : RopeControlsV1
    /// {
    ///     public override bool FastRetrieve(Player player) => player.input[0].pckp;
    /// }
    /// // RopeConfig.cs 里: RopeConfig.Controls = new RopeControlsV3();
    /// </code>
    /// </summary>
    public abstract class RopeControls
    {
        // 工具:最近 7 帧按过拾取键的帧数(连打检测)
        protected static int PickupMashCount(Player player)
        {
            int pckpTime = 0;
            for (int i = 0; i < 7; i++)
            {
                if (player.input[i].pckp)
                {
                    pckpTime++;
                }
            }
            return pckpTime;
        }

        /// <summary>
        /// 召回入口按键(所有模式的公共入口;吃东西/空手等状态条件在 Handler.CanNotCall 里检查)
        /// </summary>
        public abstract bool CallBackTrigger(Player player);

        #region 回收模式(让矛回来)

        /// <summary>快速唤回(矛飞回来)</summary>
        public abstract bool FastRetrieve(Player player);

        /// <summary>慢速收线(矛慢慢靠近)</summary>
        public abstract bool SlowRetrieve(Player player);

        /// <summary>
        /// 慢速收线时矛插在生物上是否先拔下来再收线。
        /// 组合2为 true:拾取长按是回收意图,解除插生物状态后正常慢速回收;
        /// 组合1为 false:拾取长按是钓竿意图,不拔矛,慢速收线动作带着生物走。
        /// </summary>
        public virtual bool SlowRetrievePullsSpearOut => false;

        #endregion

        #region 钩爪模式(让玩家移动)

        /// <summary>爬墙跳跃把玩家拽向绳子(带 10 帧输入缓冲);飞行锚点同样使用</summary>
        public abstract bool GrapplePull(Player player);

        /// <summary>矛插在生物上时跳跃把玩家拽过去(当帧判定)</summary>
        public abstract bool GrappleCreaturePull(Player player);

        #endregion

        #region 钓竿模式(让附着物移动)

        /// <summary>拖拽被矛插住的生物(钓竿模式总开关,重拉和轻拉都算)</summary>
        public abstract bool FishingPull(Player player);

        /// <summary>
        /// 钓竿重拉:钓竿键的点按(当帧按下,松开或持续按住为 false)。
        /// FishingPull 为真且本方法为 false 时是轻拉(长按慢慢持续拉动)
        /// </summary>
        public abstract bool FishingHeavy(Player player);

        /// <summary>
        /// 钓竿模式是否独立入口:true 时不用按住拾取,单独按钓竿键即可拖拽生物
        /// (由 UserData.Player_UpdateMSC 调用 Handler.FishSpear);
        /// false 时钓竿键只在召回流程内作为分支按键生效
        /// </summary>
        public virtual bool FishingStandalone => false;

        /// <summary>
        /// 钓竿键未按下时是否仍轻拉被插住的生物(组合1的历史行为:召回流程中自动轻拽;
        /// 组合2为 false,轻拉只由长按钓竿键触发)
        /// </summary>
        public virtual bool FishingLightWhenIdle => false;

        #endregion

        #region 攻击模式(甩矛)

        /// <summary>攻击甩矛按键(不含连打检查,连打检查单独拆出以保持 return 语义)</summary>
        public abstract bool AttackTrigger(Player player);

        /// <summary>连打取消:连打拾取键时本帧整个召回流程什么都不做</summary>
        public abstract bool MashCancel(Player player);

        #endregion

        #region 其他

        /// <summary>弄断绳子</summary>
        public abstract bool BreakRope(Player player);

        #endregion
    }

    /// <summary>
    /// 组合1(当前默认):行为与重构前完全一致。
    /// 召回=按住拾取; 快唤=按住上; 慢速收线=按住拾取;
    /// 钩爪(墙/飞行锚点)=按跳跃(带缓冲); 钩爪(生物拽人)=当帧跳跃;
    /// 钓竿(拖生物)=想拾取; 攻击=另一只手按拾取; 断绳=下+特殊;
    /// </summary>
    public class RopeControlsV1 : RopeControls
    {
        public override bool CallBackTrigger(Player player) =>
            (player.input[0].pckp || player.input[1].pckp) && player.input[0].y >= 0;

        public override bool FastRetrieve(Player player) => player.input[0].y > 0;

        public override bool SlowRetrieve(Player player) => player.input[0].pckp;

        public override bool GrapplePull(Player player)
        {
            int canGrab = 0;
            for (int i = 0; i < 10; i++)
            {
                if (player.input[i].jmp || player.input[0].jmp)
                {
                    canGrab++;
                }
            }
            return canGrab > 2;
        }

        public override bool GrappleCreaturePull(Player player) => player.input[0].jmp;

        public override bool FishingPull(Player player) => player.wantToPickUp > 0;

        // 组合1没有点按/长按概念,想要拾取时全是重拉(行为与重构后一致)
        public override bool FishingHeavy(Player player) => player.wantToPickUp > 0;

        // 组合1历史行为:不想要拾取时(距离够远)也自动轻拽生物
        public override bool FishingLightWhenIdle => true;

        public override bool AttackTrigger(Player player) =>
            player.input[1].pckp && !player.input[0].pckp;

        public override bool MashCancel(Player player) => PickupMashCount(player) > 5;

        public override bool BreakRope(Player player) =>
            player.input[0].y < 0 && player.input[0].spec;
    }

    /// <summary>
    /// 组合2:与组合1的差异只在钓竿模式——按住特殊键拖拽被矛插住的生物,
    /// 且钓竿是独立入口(不用按住拾取,单独按特殊键即可,由 Handler.FishSpear 处理)。
    /// 其余按键与组合1一致(断绳仍是下+特殊)。
    /// 切换:修改 RopeConfig.Controls 的赋值,如 RopeConfig.Controls = new RopeControlsV2();
    /// </summary>
    public class RopeControlsV2 : RopeControlsV1
    {
        public override bool FishingPull(Player player) => player.input[0].spec;

        // 点按(当帧按下)重拉;持续按住是轻拉慢慢持续拉动
        public override bool FishingHeavy(Player player) =>
            player.input[0].spec && !player.input[1].spec;

        public override bool FishingStandalone => true;

        // 组合2:拾取长按是回收意图,先把矛从生物身上拔下来,再正常慢速回收
        public override bool SlowRetrievePullsSpearOut => true;
    }
}
