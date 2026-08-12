using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 钩索惯性期间,用 ILHook 让原版的地面/表面摩擦从源头不生效
    /// 判定条件:UserData.OwnerHasRopeMomentum(owner)
    /// </summary>
    public static class FrictionIL
    {
        public static void Hook()
        {
            // 1. 斜坡横向摩擦
            //    原版位置:BodyChunk.cs 645 和 656 行,共两处
            //    vel.x *= 1f - owner.surfaceFriction;
            //    改法:惯性时乘数改为 1(不刹车)
            IL.BodyChunk.checkAgainstSlopesVertically += frictionhook1;

            // 2. 陡坡滑动摩擦
            //    原版位置:BodyChunk.cs 565 行
            //    vel -= Vector2.Dot(vel, vector) * Mathf.Clamp01(1f - owner.surfaceFriction * 2f) * vector;
            //    改法:惯性时 Clamp01 的参数传 1(系数为0,不刹车)
            IL.BodyChunk.CheckVerticalCollision += frictionhook2;

            // 3.(待用)打滚/滑铲地面摩擦
            //    原版位置:Player.cs 8355-8361 行
            //    if (base.bodyChunks[m].ContactPoint.y == 0)
            //        base.bodyChunks[m].vel.x *= surfaceFriction;
            //    改法:惯性时跳过整个乘法
            IL.Player.UpdateBodyMode += frictionhook3;

            // 4.(可选)贴墙下滑刹车
            //    原版位置:Player.cs 5531-5538 行
            //    base.bodyChunks[...].vel.y *= Mathf.Clamp(1f - surfaceFriction * (...), 0f, 1f);
            //    改法:惯性时跳过
            IL.Player.Update += frictionhook4;

            // 5. 贴地移动逼近(地面比空中慢的元凶)
            //    原版位置:Player.MovementUpdate 内,Player.cs 12563-12571 行
            //    贴地时每帧把横向速度向目标(没按方向键时是0)逼近 Pow(surfaceFriction, 1.5f) ≈ 0.35
            //    vel.x += (num16 - vel.x) * Mathf.Pow(surfaceFriction, 1.5f);
            //    改法:惯性时把逼近系数改为0(不逼近),绳子给的速度不被吃掉
            IL.Player.MovementUpdate += frictionhook5;
        }

        private static void frictionhook1(ILContext il)
        {
            var c = new ILCursor(il);
            // 两处 vel.x *= 1f - owner.surfaceFriction;
            for (int i = 0; i < 2; i++)
            {
                if (c.TryGotoNext(MoveType.After,
                    i => i.MatchLdflda<BodyChunk>("vel"),
                    i => i.MatchLdflda<Vector2>("x"),
                    i => i.Match(OpCodes.Dup),
                    i => i.Match(OpCodes.Ldind_R4),
                    i => i.MatchLdcR4(1f),
                    i => i.MatchLdarg(0),
                    i => i.MatchCall<BodyChunk>("get_owner"),
                    i => i.MatchLdfld<PhysicalObject>("surfaceFriction"),
                    i => i.MatchSub()
                    ))
                {
                    // 栈: [..., &vel, &vel.x, 乘数]
                    c.Emit(OpCodes.Ldarg, 0); // owner
                    // 有钩索惯性时乘数改为1(不刹车),否则按原值
                    c.EmitDelegate<Func<float, PhysicalObject, float>>(
                        (friction, owner) => UserData.OwnerHasRopeMomentum(owner) ? 1f : friction
                    );
                }
                else
                {
                    UnityEngine.Debug.Log("FrictionIL.frictionhook1: 未能定位到斜坡摩擦乘法(第" + (i + 1) + "处)");
                    break;
                }
            }
        }

        private static void frictionhook2(ILContext il)
        {
            var c = new ILCursor(il);
            // vel -= Vector2.Dot(vel, vector) * Mathf.Clamp01(1f - owner.surfaceFriction * 2f) * vector;
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<BodyChunk>("vel"),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<BodyChunk>("vel"),
                i => i.MatchLdloc(18),
                i => i.MatchCall<Vector2>("Dot"),
                i => i.MatchLdcR4(1f),
                i => i.MatchLdarg(0),
                i => i.MatchCall<BodyChunk>("get_owner"),
                i => i.MatchLdfld<PhysicalObject>("surfaceFriction"),
                i => i.MatchLdcR4(2f),
                i => i.MatchMul(),
                i => i.MatchSub()
                ))
            {
                // 栈: [..., dot, 1 - sf * 2]
                c.Emit(OpCodes.Ldarg, 0); // owner
                // 有钩索惯性时 Clamp01 的参数改为0(系数为0,不刹车),否则按原值
                c.EmitDelegate<Func<float, PhysicalObject, float>>(
                    (friction, owner) => UserData.OwnerHasRopeMomentum(owner) ? 0f : friction
                );
            }
            else
            {
                UnityEngine.Debug.Log("FrictionIL.frictionhook2: 未能定位到陡坡滑动摩擦");
            }
        }

        private static void frictionhook3(ILContext il)
        {
            var c = new ILCursor(il);
            // TODO: 打滚/滑铲摩擦(Player.cs 8355-8361),惯性时跳过 vel.x *= surfaceFriction
        }

        private static void frictionhook4(ILContext il)
        {
            // TODO: 惯性时跳过贴墙下滑的 vel.y 刹车
        }

        private static void frictionhook5(ILContext il)
        {
            var c = new ILCursor(il);
            // vel.x += (num16 - vel.x) * Mathf.Pow(surfaceFriction, 1.5f);
            // 贴地时每帧把横向速度向目标逼近,这是地面比空中慢的元凶
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdfld<PhysicalObject>("surfaceFriction"),
                i => i.MatchLdcR4(1.5f),
                i => i.MatchCall<Mathf>("Pow")
                ))
            {
                // 栈: [..., 逼近系数]
                c.Emit(OpCodes.Ldarg, 0); // this
                // 有钩索惯性时逼近系数改为0(不逼近),否则按原值
                c.EmitDelegate<Func<float, Player, float>>(
                    (x, player) => UserData.OwnerHasRopeMomentum(player) ? 0f : x
                );
            }
            else
            {
                UnityEngine.Debug.Log("FrictionIL.frictionhook5: 未能定位到贴地移动逼近");
            }
        }
    }
}
