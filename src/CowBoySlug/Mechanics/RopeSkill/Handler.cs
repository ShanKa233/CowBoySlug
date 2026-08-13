using System;
using System.Reflection;
using Compatibility;
using Compatibility.Meadow;
using RWCustom;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 钩索技能的动作层:所有玩家可触发的动作(本地实现+网络同步包装)+ 查询判定。
    /// 每玩家数据在 UserData,按键判定在 RopeControls,调参常量在 RopeConfig。
    /// </summary>
    public class Handler
    {
        #region 生成与投掷

        public static void SpawnRope(Player player, Spear spear, Color start, Color end)
        {
            SpawnRope_Local(player, spear, start, end);
            if (ModCompat_Helpers.RainMeadow_IsOnline)
            {
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

        #region 召回

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

            // TODO: 修复矛收回时网络不同步导致的小故障
            // 问题描述：矛插着的时候回收,其他端口嗯用户会残余一个看不见的矛在地上

            // 如果在线模式，调用兼容方法
            if (ModCompat_Helpers.RainMeadow_IsOnline)
            {
                MeadowCompat.CallBackSpear(player);
            }
        }

        /// <summary>
        /// 处理本地召回矛的方法
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

            // 离矛最近的丝的方向
            Vector2 playerToFristPoint = Custom.DirVec(umbilical.playerPos, umbilical.RopePos(1));

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

            // 防止吃东西 吐东西
            if (spear.mode != Weapon.Mode.Carried)
            {
                player.swallowAndRegurgitateCounter = 0;
                if (player.slugOnBack != null)
                {
                    player.slugOnBack.counter = 0;
                }
            }

            // 在无重力情况下给玩家施加移动力
            if (spear.mode != Weapon.Mode.Carried && player.gravity <= 0)
            {
                player.mainBodyChunk.vel -= spearToEndPointDir / 2;
                UserData.FillRopeMomentum(player);
            }

            // 如果玩家离矛很近而且可以直视矛而且按了拿取按键就拿起矛
            if (range < RopeConfig.PickUpRange && flagSee && spear.mode != Weapon.Mode.Carried)
            {
                if (player.FreeHand() != -1)
                {
                    player.SlugcatGrab(spear, player.FreeHand());
                    player.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, spear.firstChunk);
                    spear.canBeHitByWeapons = true; // 让矛可以挡下攻击
                }
            }
            // 回收模式-快速唤回(矛飞回来)
            else if (flagFastBackAction && range > 50)
            {
                // 拉绳子手部动作
                player.HandData().Pulling(15, umbilical, player.FreeHand());

                umbilical.loose = 1;

                spear.ChangeMode(Weapon.Mode.Free);

                spear.firstChunk.vel = spearToEndPointDir * 27 + Custom.RNV();
                spear.setRotation = -spearToEndPointDir.normalized;

                if (spear.gravity > 0)
                {
                    spear.firstChunk.vel.y += 10;
                }
            }
            // 攻击模式
            else if (RopeConfig.Controls.AttackTrigger(player) && range > 35)
            {
                if (RopeConfig.Controls.MashCancel(player))
                {
                    return;
                }

                // 控制手和绳子
                player.HandData().Pulling(20, umbilical, player.FreeHand());
                spear.ChangeMode(Weapon.Mode.Thrown);
                spear.spearDamageBonus *= 0.9f;
                spear.thrownBy = player;
                spear.throwDir = new IntVector2(
                    Convert.ToInt32(spearToEndPointDir.x),
                    Convert.ToInt32(spearToEndPointDir.y)
                );

                spear.rotation = spear.throwDir.ToVector2();
                spear.firstChunk.pos -= spearToEndPointDir;
                spear.firstChunk.vel += spear.throwDir.ToVector2() * 50 * spear.spearDamageBonus;
            }
            // 回收模式-慢速收线(矛慢慢靠近)
            else if (RopeConfig.Controls.SlowRetrieve(player))
            {
                spear.rope().cantRotationCount += 3;
                // 控制手和绳子
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                spear.firstChunk.vel += spearToEndPointDir * 2f + Custom.RNV() * 0.2f;

                spear.setRotation = -spearToEndPointDir.normalized;
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

        /// <summary>
        /// 钓竿模式独立入口:组合开启 FishingStandalone 时,由 UserData.Player_UpdateMSC 调用。
        /// 单独按钓竿键(不用按住拾取)即可拖拽被矛插住的生物,不经过召回流程。
        /// 玩家已按住拾取(召回流程激活)时跳过,由 WhenSpearOnSomeThing 的钓竿分支处理,避免双重执行。
        /// </summary>
        public static void FishSpear(Player player)
        {
            // 召回流程激活时跳过,避免和 WhenSpearOnSomeThing 的钓竿分支重复执行
            if (RopeConfig.Controls.CallBackTrigger(player))
                return;

            // 吃东西/吐东西或没有空手时不拖,避免和原版抓取行为冲突
            if (player.eatMeat > 1 || player.eatExternalFoodSourceCounter > 1 || player.FreeHand() == -1)
                return;

            if (!RopeConfig.Controls.FishingPull(player))
                return;

            var umbilical = NiceRope(player);
            if (umbilical == null || umbilical.spear == null)
                return;

            var spear = umbilical.spear;
            if (spear.mode != Spear.Mode.StuckInCreature)
                return;

            player.HandData().Pulling(10, umbilical, player.FreeHand());
            if (RopeConfig.Controls.FishingHeavy(player))
            {
                DragCreatureOnSpear(player, spear, umbilical);
            }
            else
            {
                LightDragCreature(player, spear, umbilical);
            }
        }

        /// <summary>
        /// 钓竿轻拉:长按钓竿键时慢慢持续拉动生物,力度小但每帧生效。
        /// 距离太近时不拉(生物已经在玩家脸上)。
        /// 调用前需保证 spear.mode == StuckInCreature。
        /// </summary>
        private static void LightDragCreature(Player player, Spear spear, Simulator umbilical)
        {
            if (Custom.DistLess(player.mainBodyChunk.pos, spear.stuckInChunk.pos, 60))
                return;

            Vector2 spearToEndPointDir = Custom.DirVec(
                spear.firstChunk.pos,
                umbilical.RopePos(umbilical.rope.TotalPositions - 2)
            );
            spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel +=
                spearToEndPointDir * 3f;
        }

        /// <summary>
        /// 拖拽被矛插住的生物:生物被拉向矛的方向,玩家受到反向拉力。
        /// 调用前需保证 spear.mode == StuckInCreature。
        /// </summary>
        private static void DragCreatureOnSpear(Player player, Spear spear, Simulator umbilical)
        {
            var playerToRopeDir = Custom.DirVec(player.mainBodyChunk.pos, umbilical.RopeShowPos(1));
            Vector2 spearToEndPointDir = Custom.DirVec(
                spear.firstChunk.pos,
                umbilical.RopePos(umbilical.rope.TotalPositions - 2)
            );
            float range = Vector2.Distance(umbilical.spearEndPos, player.bodyChunks[1].pos);

            // 玩家受到反向拉力(生物越重拉力越大)
            float pullForce = Mathf.InverseLerp(1, 10, spear.stuckInObject.TotalMass / player.TotalMass);
            if (pullForce > 0)
            {
                UserData.FillRopeMomentum(player);
            }
            // 距离越近拉升越弱,收矛距离一半处为0
            player.bodyChunks[1].vel +=
                playerToRopeDir * pullForce * 20 * RopeConfig.PullForceFactor(range);
            // 生物受拉:与玩家的反向拉力互补,生物越重越难拉动
            spear.stuckInObject.bodyChunks[spear.stuckInChunkIndex].vel +=
                spearToEndPointDir * (1f - pullForce) * 20;
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

        // 检查矛是否在某个物体上
        public static bool WhenSpearOnSomeThing(
            Spear spear,
            Player player,
            float range,
            Simulator umbilical
        )
        {
            var playerToRopeDir = Custom.DirVec(player.mainBodyChunk.pos, umbilical.RopeShowPos(1));
            Vector2 spearToEndPointDir = Custom.DirVec(
                spear.firstChunk.pos,
                umbilical.RopePos(umbilical.rope.TotalPositions - 2)
            );

            // 滑铲加速投出的矛在飞行中可以作为钩索锚点位移
            // 检测速度:飞行速度高于阈值才是锚点;钩索消耗矛的飞行速度,速度耗尽后矛静止,失去锚点能力
            if (
                spear.mode == Weapon.Mode.Thrown
                && spear.firstChunk.vel.magnitude > RopeConfig.HookEnergySpeedThreshold
            )
            {
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                if (range > 10 && player.gravity > 0 && RopeConfig.Controls.GrapplePull(player))
                {
                    // 拉绳方向与矛的飞行方向至少差90度(反向拉扯)才提供位移
                    Vector2 spearFlyDir = spear.firstChunk.vel.normalized;
                    if (Vector2.Dot(playerToRopeDir, spearFlyDir) < 0f)
                    {
                        // 像普通拉取模式一样给玩家向量速度,矛按原版自然减速,
                        // 速度降到阈值以下后矛静止,自然失去锚点能力
                        player.circuitSwimResistance *= Mathf.InverseLerp(
                            player.mainBodyChunk.vel.magnitude + player.bodyChunks[1].vel.magnitude,
                            15f,
                            9f
                        );
                        player.bodyChunks[1].vel += playerToRopeDir * 3f * RopeConfig.PullForceFactor(range);
                        UserData.FillRopeMomentum(player);
                        return true;
                    }
                }
                return false;
            }

            // 如果插到墙上就拔下来然后变成自由状态
            if (
                (spear.hasHorizontalBeamState && spear.mode == Weapon.Mode.StuckInWall)
                || (!spear.spinning && spear.mode == Weapon.Mode.Free)
            )
            {
                // 钩爪模式:拉玩家(墙/飞行锚点)
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                if (range > 10 && player.gravity > 0 && RopeConfig.Controls.GrapplePull(player))
                {
                    player.circuitSwimResistance *= Mathf.InverseLerp(
                        player.mainBodyChunk.vel.magnitude + player.bodyChunks[1].vel.magnitude,
                        15f,
                        9f
                    );
                    // 距离越近拉升越弱,收矛距离一半处为0
                    player.bodyChunks[1].vel += playerToRopeDir * 3f * RopeConfig.PullForceFactor(range);
                    UserData.FillRopeMomentum(player);
                    return true;
                }

                if (spear.mode == Weapon.Mode.StuckInWall)
                {
                    // 取下矛
                    PullSpearFromWall(spear);
                }
            }
            // 如果插到了生物:钓竿模式拖生物(点按重拉/长按轻拉),钩爪模式拽玩家
            else if (spear.mode == Spear.Mode.StuckInCreature)
            {
                player.HandData().Pulling(10, umbilical, player.FreeHand());
                if (RopeConfig.Controls.FishingPull(player))
                {
                    if (RopeConfig.Controls.FishingHeavy(player))
                    {
                        DragCreatureOnSpear(player, spear, umbilical);
                    }
                    else
                    {
                        LightDragCreature(player, spear, umbilical);
                    }
                }
                else if (!Custom.DistLess(player.mainBodyChunk.pos, spear.stuckInChunk.pos, 60))
                {
                    if (RopeConfig.Controls.GrappleCreaturePull(player))
                    {
                        // 钩爪模式:距离越近拉升越弱,收矛距离一半处为0
                        player.bodyChunks[1].vel += playerToRopeDir * 3f * RopeConfig.PullForceFactor(range);
                        UserData.FillRopeMomentum(player);
                    }
                }
            }
            // 对拿着这个矛的生物操作
            else if (
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
