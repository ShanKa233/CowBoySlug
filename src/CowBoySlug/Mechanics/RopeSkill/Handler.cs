using System;
using System.Reflection;
using Compatibility;
using Compatibility.Meadow;
using RWCustom;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 钩索技能的动作层(partial,按模式拆文件):
    ///   本文件  :生成与投掷 / 断裂 / 召回分发器 / 拔墙矛 / 查询
    ///   Handler.Retrieve.cs :回收模式(让矛回来)——拿取/快速唤回/慢速收线
    ///   Handler.Grapple.cs  :钩爪模式(让玩家移动)——飞行锚点/墙锚点/生物锚点
    ///   Handler.Fishing.cs  :钓竿模式(让附着物移动)——独立入口/重拉/轻拉/空闲轻拽
    ///   Handler.Attack.cs   :攻击模式(甩矛)
    /// 每玩家数据在 UserData,按键判定在 RopeControls,调参常量在 RopeConfig。
    /// </summary>
    public partial class Handler
    {
        #region 生成与投掷

        public static void SpawnRope(Player player, Spear spear, Color start, Color end)
        {
            SpawnRope_Local(player, spear, start, end);
            if (ModCompat_Helpers.RainMeadow_IsOnline)
            {
                // 绳矛状态进入 Meadow 同步流(新加入玩家自动补建)
                MeadowCompat.TryAttachRope(spear);
                MeadowCompat.CreateRopeSpear(player, spear, start, end);
            }
        }

        public static void SpawnRope_Local(Player player, Spear spear, Color start, Color end)
        {
            var rope = new Simulator(player, spear, start, end); //新建一个在矛上的丝线
            player.room.AddObject(rope); //召唤这个线
        }

        /// <summary>
        /// 处理扔矛时生成绳子的方法
        /// </summary>
        /// <param name="player">扔矛的玩家</param>
        /// <param name="spear">被扔出的矛</param>
        /// <param name="ropeColor">绳子的颜色</param>
        public static void ThrowSpearWithRope(Player player, Spear spear, Color ropeColor)
        {
            // 调用本地方法
            ThrowSpearWithRope_Local(player, spear, ropeColor);

            // 如果在线模式，调用兼容方法
            if (ModCompat_Helpers.RainMeadow_IsOnline)
            {
                // 绳矛状态进入 Meadow 同步流(存在性/归属/收紧状态,新加入玩家自动补建)
                MeadowCompat.TryAttachRope(spear);
                MeadowCompat.ThrowSpearWithRope(player, spear, ropeColor);
            }
        }

        /// <summary>
        /// 处理本地扔矛时生成绳子的方法
        /// </summary>
        /// <param name="player">扔矛的玩家</param>
        /// <param name="spear">被扔出的矛</param>
        /// <param name="ropeColor">绳子的颜色</param>
        public static void ThrowSpearWithRope_Local(Player player, Spear spear, Color ropeColor)
        {
            // 增加回收的冷却时间
            spear.vibrate += 2;

            // 如果矛已经有绳子，销毁它
            if (spear.rope().rope != null)
            {
                spear.rope().rope.Destroy();
            }

            // 生成新的绳子（仅本地）
            SpawnRope_Local(
                player,
                spear,
                Color.Lerp(player.ShortCutColor(), ropeColor, 0.5f),
                ropeColor
            );
        }

        #endregion

        #region 断裂

        /// <summary>
        /// 处理绳子断裂的方法，支持网络同步
        /// </summary>
        /// <param name="player">玩家</param>
        /// <param name="spear">连接的矛</param>
        public static void HandleRopeBreaking(Player player, Spear spear)
        {
            // 调用本地方法
            HandleRopeBreaking_Local(player, spear);

            // 如果在线模式，调用兼容方法
            if (ModCompat_Helpers.RainMeadow_IsOnline)
            {
                MeadowCompat.HandleRopeBreaking(player, spear);
            }
        }

        /// <summary>
        /// 处理本地绳子断裂的方法
        /// </summary>
        /// <param name="player">玩家</param>
        /// <param name="spear">连接的矛</param>
        public static void HandleRopeBreaking_Local(Player player, Spear spear)
        {
            // 增加绳子的断裂计数
            spear.rope().brokenCount += 10;

            // 播放声音和生成火花效果
            if (spear.rope().brokenCount > 60)
            {
                player.room.PlaySound(
                    SoundID.Miros_Beak_Snap_Hit_Other,
                    player.firstChunk,
                    false,
                    0.5f,
                    0.2f
                );

                for (int n = 2; n > 0; n--)
                {
                    player.room.AddObject(
                        new Spark(
                            player.firstChunk.pos,
                            Custom.RNV(),
                            Color.white,
                            null,
                            10,
                            20
                        )
                    );
                }
            }
        }

        #endregion

        #region 召回分发器

        /// <summary>
        /// 处理召回矛的方法，支持网络同步
        /// </summary>
        /// <param name="player">召回矛的玩家</param>
        public static void CallBackSpear(Player player)
        {
            // 获取与玩家连接的绳子
            var umbilical = NiceRope(player);
            if (umbilical == null || umbilical.spear == null)
                return;

            if (CanNotCall(player))
                return;
            // 调用本地方法
            CallBackSpear_Local(player);

            // 如果在线模式，调用兼容方法
            // 注:原先"矛插着回收时其他端残余隐形矛"的故障,根源是远端玩家对象(输入同步)
            // 也执行了本地召回逻辑;现在钩子已按 IsLocal 门控,RPC 远端也只做视觉反馈
            if (ModCompat_Helpers.RainMeadow_IsOnline)
            {
                MeadowCompat.CallBackSpear(player);
            }
        }

        /// <summary>
        /// 处理本地召回矛的方法。
        /// 召回的总流程:先按矛的状态分发给钩爪/钓竿模式,再按按键分发给回收/攻击模式。
        /// 各模式的具体动作在对应 partial 文件里。
        /// </summary>
        /// <param name="player">召回矛的玩家</param>
        public static void CallBackSpear_Local(Player player)
        {
            // 获取与玩家连接的绳子
            var umbilical = NiceRope(player);
            if (umbilical == null || umbilical.spear == null)
                return;

            var spear = umbilical.spear;

            // 检查矛是否可以用
            if (!(player.room == spear.room && spear.vibrate <= 0))
                return;

            // 拉矛期间防止吃东西/吐东西:清空原版进食相关计数器。
            // 放在模式分发之前,钩爪/钓竿全程都不进食;
            // 吃肉的入口判定另由 UserData 的 CanEatMeat 钩子拦截。
            if (spear.mode != Weapon.Mode.Carried)
            {
                player.eatMeat = 0;
                player.eatExternalFoodSourceCounter = 0;
                player.swallowAndRegurgitateCounter = 0;
                // 吃水果/素食走 eatCounter 路径:≤15 时松开拾取也会自动继续吃,
                // 所以不能清零(清零反而触发),顶高到 30 让它自然冷却
                if (player.eatCounter <= 15)
                {
                    player.eatCounter = 30;
                }
                if (player.slugOnBack != null)
                {
                    player.slugOnBack.counter = 0;
                }
            }

            // 是否做出快速唤回动作
            bool flagFastBackAction = RopeConfig.Controls.FastRetrieve(player);
            // 检查能不能直视到
            bool flagSee = player.room.VisualContact(spear.firstChunk.pos, player.firstChunk.pos);
            // 检查距离
            var range = Vector2.Distance(umbilical.spearEndPos, player.bodyChunks[1].pos);

            Vector2 spearToEndPointDir = Custom.DirVec(
                spear.firstChunk.pos,
                umbilical.RopePos(umbilical.rope.TotalPositions - 2)
            );

            umbilical.used = true;
            // 快速唤回(上+召回)只在收矛范围内优先于拉力模式;
            // 超出收矛范围时拉力模式优先,进行中途不会被快速唤回打断
            if (
                !(flagFastBackAction && range <= RopeConfig.PickUpRange)
                && WhenSpearOnSomeThing(spear, player, range, umbilical)
            )
                return;

            // 矛还插在墙上时统一先走拔矛流程(清横梁/卡墙数据),
            // 保证后面的捡起/快唤/攻击/慢速各模式分支面对的都是已拔出的矛,
            // 避免原版把插墙矛钉回墙或横梁残留
            if (spear.mode == Weapon.Mode.StuckInWall)
            {
                PullSpearFromWall(spear);
            }

            // 在无重力情况下给玩家施加移动力
            if (spear.mode != Weapon.Mode.Carried && player.gravity <= 0)
            {
                player.mainBodyChunk.vel -= spearToEndPointDir / 2;
                UserData.FillRopeMomentum(player);
            }

            // 回收模式-拿取:玩家离矛很近而且可以直视矛就拿起矛
            if (range < RopeConfig.PickUpRange && flagSee && spear.mode != Weapon.Mode.Carried)
            {
                PickUpSpear(player, spear);
            }
            // 回收模式-快速唤回(矛飞回来)
            else if (flagFastBackAction && range > 50)
            {
                FastRetrieve(player, spear, umbilical, spearToEndPointDir);
            }
            // 攻击模式-甩矛(连打取消保持整帧 return 语义,在这里检查)
            else if (RopeConfig.Controls.AttackTrigger(player) && range > 35)
            {
                if (RopeConfig.Controls.MashCancel(player))
                {
                    return;
                }

                AttackSpear(player, spear, umbilical, spearToEndPointDir);
            }
            // 回收模式-慢速收线(矛慢慢靠近)
            else if (RopeConfig.Controls.SlowRetrieve(player))
            {
                // 组合2:拾取长按是回收意图,矛插在生物上时先拔下来再正常慢速回收;
                // 但钩爪模式(按跳跃)需要矛继续插在生物上当锚点,不拔
                if (RopeConfig.Controls.SlowRetrievePullsSpearOut
                    && spear.mode == Weapon.Mode.StuckInCreature
                    && !RopeConfig.Controls.GrappleCreaturePull(player))
                {
                    spear.ChangeMode(Weapon.Mode.Free);
                }
                SlowRetrieve(player, spear, umbilical, spearToEndPointDir);
            }
            else if (spear.mode == Weapon.Mode.StuckInCreature)
            {
                spear.ChangeMode(Weapon.Mode.Free);
            }
        }

        #endregion

        #region 拔墙矛

        /// <summary>
        /// 从墙上拔出矛的方法，支持网络同步
        /// </summary>
        /// <param name="spear">需要拔出的矛</param>
        public static void PullSpearFromWall(Spear spear)
        {
            // 调用本地方法
            PullSpearFromWall_Local(spear);

            // 如果在线模式，调用兼容方法
            if (ModCompat_Helpers.RainMeadow_IsOnline)
            {
                MeadowCompat.PullSpearFromWall(spear);
            }
        }

        /// <summary>
        /// 本地从墙上拔出矛的方法
        /// </summary>
        /// <param name="spear">需要拔出的矛</param>
        public static void PullSpearFromWall_Local(Spear spear)
        {
            spear.resetHorizontalBeamState();
            spear.stuckInWall = new Vector2?(default(Vector2));
            spear.vibrate = 10;
            spear.firstChunk.collideWithTerrain = true;
            spear.abstractSpear.stuckInWallCycles = 0;
            // 刚插上的矛 addPoles 还没被原版 Update 消费(横梁标记延迟一帧生效),
            // ChangeMode 清掉 stuckInWall 后,残留的 addPoles 会在下一帧原版 Update 里触发空引用;
            // addPoles 是原版私有字段,用反射补清
            addPolesField?.SetValue(spear, false);
            spear.ChangeMode(Spear.Mode.Free);
        }

        // 原版 Spear 的私有字段:插墙后延迟一帧设置横梁的标记
        private static readonly FieldInfo addPolesField = typeof(Spear).GetField(
            "addPoles",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        #endregion

        #region 查询与判定

        // 玩家是否处于拉矛操作中(召回流程激活,或组合2的钓竿独立入口激活)。
        // 供 UserData 的 CanEatMeat 钩子使用:拉矛期间不吃东西。
        public static bool IsPullingRope(Player player)
        {
            // 召回流程激活:按了召回键且能召回(没在吃东西/有空手)
            if (RopeConfig.Controls.CallBackTrigger(player) && !CanNotCall(player))
                return true;

            // 钓竿独立入口激活:组合2单单按钓竿键(召回流程没激活时)
            if (
                RopeConfig.Controls.FishingStandalone
                && !RopeConfig.Controls.CallBackTrigger(player)
                && RopeConfig.Controls.FishingPull(player)
            )
                return true;

            return false;
        }

        // 检查玩家是否不能召回矛
        public static bool CanNotCall(Player player)
        {
            bool canCall = RopeConfig.Controls.CallBackTrigger(player); // 按键组合条件
            bool notEating = player.eatMeat <= 1 && player.eatExternalFoodSourceCounter <= 1; // 玩家没有在吃东西
            bool handFree = player.FreeHand() != -1; // 有一只空手
            return !(canCall && notEating && handFree);
        }

        // 获取与玩家连接的绳子
        public static Simulator NiceRope(Player player)
        {
            if (!UserData.modules.TryGetValue(player, out var mod))
                return null; // 如果没有找到 RopeMaster 模块

            Simulator umbilical = null;

            // 搜索房间里面的所有矛找一根合适的出来
            foreach (var obj in player.room.updateList)
            {
                Simulator testUmbilical = null;
                var spear = obj as Spear;

                // 检查矛是否有绳子
                if (spear != null && spear.rope().IsRopeSpear)
                {
                    testUmbilical = spear.rope().rope;
                }

                // 检查绳子是否符合条件
                if (
                    !(
                        testUmbilical != null
                        && testUmbilical.spear != null
                        && testUmbilical.player == player
                    )
                )
                    continue;

                // 检查绳子是否被抓住
                if (
                    testUmbilical.spear.grabbedBy.Count > 0
                    && testUmbilical.spear.grabbedBy[0].grabber == player
                )
                    continue;

                // 检查绳子是否有限制
                if (testUmbilical.limited)
                    continue;

                // 循环检查绳子的宽度
                for (int i = 0; i < testUmbilical.points.GetLength(0); i++)
                {
                    if (testUmbilical.points[i, 3].x <= 0f)
                    {
                        continue;
                    }
                }

                // 如果找到合适的绳子
                if (umbilical == null)
                {
                    umbilical = testUmbilical;
                }
                else
                {
                    // 比较两个绳子的距离
                    bool b =
                        Math.Abs(umbilical.spear.firstChunk.pos.x - player.mainBodyChunk.pos.x)
                        > Math.Abs(
                            testUmbilical.spear.firstChunk.pos.x - player.mainBodyChunk.pos.x
                        );
                    umbilical = b ? testUmbilical : umbilical;
                }
            }

            return umbilical;
        }

        // 检查矛是否在某个物体上(召回流程的模式分流器:按矛的状态分发到各模式)
        public static bool WhenSpearOnSomeThing(
            Spear spear,
            Player player,
            float range,
            Simulator umbilical
        )
        {
            var playerToRopeDir = Custom.DirVec(player.mainBodyChunk.pos, umbilical.RopeShowPos(1));

            // 飞行中的矛:钩爪模式-空中锚点(速度高于阈值才有锚点能力)
            if (
                spear.mode == Weapon.Mode.Thrown
                && spear.firstChunk.vel.magnitude > RopeConfig.HookEnergySpeedThreshold
            )
            {
                return TryAirGrapple(spear, player, range, umbilical, playerToRopeDir);
            }

            // 插墙上或落地的矛:钩爪模式-拉玩家,矛还插墙时顺手拔下
            if (
                (spear.hasHorizontalBeamState && spear.mode == Weapon.Mode.StuckInWall)
                || (!spear.spinning && spear.mode == Weapon.Mode.Free)
            )
            {
                return TryWallGrapple(spear, player, range, umbilical, playerToRopeDir);
            }

            // 插在生物上的矛:钓竿模式拖生物 / 钩爪模式拽玩家
            if (spear.mode == Spear.Mode.StuckInCreature)
            {
                TryFishingCreature(spear, player, range, umbilical, playerToRopeDir);
                return false;
            }

            // 对拿着这个矛的生物操作
            if (
                spear.grabbedBy.Count > 0
                && spear.grabbedBy[0] != null
                && spear.grabbedBy[0].grabber != player
                && spear.grabbedBy[0].grabber != null
            )
            {
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                spear.grabbedBy[0].Release();
            }
            return false;
        } // 当矛插在什么东西上或被什么东西带着

        #endregion
    }
}
