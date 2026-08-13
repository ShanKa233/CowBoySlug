namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 绳子技能按键组合的抽象基类。
    /// 每个方法对应一个动作的按键判定;新建组合继承本类或 RopeControlsV1 只重写想改的动作。
    /// 切换组合:修改 RopeConfig.Controls 的赋值。
    ///
    /// 添加新组合示例:
    /// <code>
    /// public class RopeControlsV2 : RopeControlsV1
    /// {
    ///     public override bool FastCallBack(Player player) => player.input[0].pckp;
    /// }
    /// // RopeConfig.cs 里: RopeConfig.Controls = new RopeControlsV2();
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
        /// 召回入口按键(原 CanNotCall 中的按键条件,不含吃东西/空手等状态条件)
        /// </summary>
        public abstract bool CallBackTrigger(Player player);

        /// <summary>快速唤回(矛飞回来)</summary>
        public abstract bool FastCallBack(Player player);

        /// <summary>攻击甩矛按键(不含连打检查,连打检查单独拆出以保持 return 语义)</summary>
        public abstract bool AttackTrigger(Player player);

        /// <summary>连打取消:连打拾取键时本帧整个召回流程什么都不做</summary>
        public abstract bool MashCancel(Player player);

        /// <summary>慢速拉绳</summary>
        public abstract bool SlowPull(Player player);

        /// <summary>弄断绳子</summary>
        public abstract bool BreakRope(Player player);

        /// <summary>爬墙跳跃把玩家拽向绳子(带 10 帧输入缓冲)</summary>
        public abstract bool WallJumpPull(Player player);

        /// <summary>矛插在生物上时跳跃把玩家拽过去(当帧判定)</summary>
        public abstract bool CreatureJumpPull(Player player);

        /// <summary>拖动被矛插住的生物</summary>
        public abstract bool DragCreature(Player player);
    }

    /// <summary>
    /// 组合1(当前默认):行为与重构前完全一致。
    /// 召回=按住拾取; 快唤=按住上; 攻击=另一只手按拾取; 慢速=按住拾取;
    /// 断绳=下+特殊; 跳跃拉=按跳跃; 拖动=想拾取
    /// </summary>
    public class RopeControlsV1 : RopeControls
    {
        public override bool CallBackTrigger(Player player) =>
            (player.input[0].pckp || player.input[1].pckp) && player.input[0].y >= 0;

        public override bool FastCallBack(Player player) => player.input[0].y > 0;

        public override bool AttackTrigger(Player player) =>
            player.input[1].pckp && !player.input[0].pckp;

        public override bool MashCancel(Player player) => PickupMashCount(player) > 5;

        public override bool SlowPull(Player player) => player.input[0].pckp;

        public override bool BreakRope(Player player) =>
            player.input[0].y < 0 && player.input[0].spec;

        public override bool WallJumpPull(Player player)
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

        public override bool CreatureJumpPull(Player player) => player.input[0].jmp;

        public override bool DragCreature(Player player) => player.wantToPickUp > 0;
    }
}
