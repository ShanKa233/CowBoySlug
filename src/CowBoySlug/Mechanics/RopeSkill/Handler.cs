using System;
using Compatibility;
using Compatibility.Meadow;
using RWCustom;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    public class Handler
    {

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

        /// <summary>
        /// 处理召回矛的方法，支持网络同步
        /// </summary>
        /// <param name="player">召回矛的玩家</param>
        public static void CallBackSpear(Player player)
        {
            // 获取与玩家连接的绳子
            var umbilical = UserData.NiceRope(player);
            if (umbilical == null || umbilical.spear == null)
                return;
                
            // 调用本地方法
            CallBackSpear_Local(player);
            
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
            var umbilical = UserData.NiceRope(player);
            if (umbilical == null || umbilical.spear == null)
                return;
                
            var spear = umbilical.spear;
            
            // 检查矛是否可以用
            if (!(player.room == spear.room && spear.vibrate <= 0))
                return;

            // 是否做出快速唤回动作
            bool flagFastBackAction = player.input[0].y > 0;
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
            if (UserData.WhenSpearOnSomeThing(spear, player, range, umbilical))
                return;

            // 防止吃东西 吐东西
            if (spear.mode != Weapon.Mode.Carried)
            {
                player.swallowAndRegurgitateCounter = 0;
                player.slugOnBack.counter = 0;
            }

            // 在无重力情况下给玩家施加移动力
            if (spear.mode != Weapon.Mode.Carried && player.gravity <= 0)
            {
                player.mainBodyChunk.vel -= spearToEndPointDir / 2;
            }

            // 如果玩家离矛很近而且可以直视矛而且按了拿取按键就拿起矛
            if (range < 80 && flagSee && spear.mode != Weapon.Mode.Carried)
            {
                if (player.FreeHand() != -1)
                {
                    player.SlugcatGrab(spear, player.FreeHand());
                    player.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, spear.firstChunk);
                    spear.canBeHitByWeapons = true; // 让矛可以挡下攻击
                }
            }
            // 回收矛模式
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
            // 慢速模式
            else if (player.input[0].pckp)
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
    }
}