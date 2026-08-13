using System;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace CowBoySlug.Mechanics.RopeSkill
{
    /// <summary>
    /// 钩索惯性期间,用 ILHook 让原版的地面/表面摩擦从源头削弱
    /// 判定条件:UserData.OwnerHasRopeMomentum(owner)
    /// </summary>
    public static class FrictionIL
    {
        /// <summary>
        /// 惯性模式下保留的原版摩擦比例(1=摩擦不变,0=完全无摩擦)
        /// </summary>
        public const float FrictionKeepRatio = 0.3f;

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

            // 6. 惯性时 TerrainImpact 不撞晕不撞死
            //    原版位置:Player.TerrainImpact,Player.cs 6462-6584 行
            //    speed > num → Die(),speed > num2 → Stun()
            //    改法:惯性时把 speed 参数归零,所有伤害/眩晕/死亡分支都不触发,只剩轻碰声
            IL.Player.TerrainImpact += frictionhook6;
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
                    // 有钩索惯性时按比例保留刹车,否则按原值
                    c.EmitDelegate<Func<float, PhysicalObject, float>>(
                        (friction, owner) =>
                            UserData.OwnerHasRopeMomentum(owner)
                                ? 1f - (1f - friction) * FrictionKeepRatio
                                : friction
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
                // 有钩索惯性时按比例保留刹车,否则按原值
                c.EmitDelegate<Func<float, PhysicalObject, float>>(
                    (friction, owner) =>
                        UserData.OwnerHasRopeMomentum(owner)
                            ? friction * FrictionKeepRatio
                            : friction
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
                // 有钩索惯性时按比例保留逼近,否则按原值
                c.EmitDelegate<Func<float, Player, float>>(
                    (x, player) =>
                        UserData.OwnerHasRopeMomentum(player) ? x * FrictionKeepRatio : x
                );
            }
            else
            {
                UnityEngine.Debug.Log("FrictionIL.frictionhook5: 未能定位到贴地移动逼近");
            }

            // base.bodyChunks[1].pos = feetStuckPos.Value;
            // 站立时脚被每帧硬钉回地面,向上的拉力被抹掉
            // 惯性时跳过整个赋值序列(脚的位置保持不变=身体的位置),否则原样执行
            // 用独立的 c2 游标从头定位,不受上面定位的游标位置影响
            // 完整 7 条序列在 DLL 中唯一(已验证命中偏移 719)
            var c2 = new ILCursor(il);
            var feetStuckPosField = typeof(Player).GetField(
                "feetStuckPos",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (c2.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchCall<PhysicalObject>("get_bodyChunks"),
                i => i.MatchLdcI4(1),
                i => i.MatchLdelemRef(),
                i => i.MatchLdarg(0),
                i => i.MatchLdflda(feetStuckPosField),
                i => i.Match(OpCodes.Call)
                ))
            {
                // cursor 在序列第一条之前,栈: [](序列压 2 弹 2,净空)
                var keep = c2.DefineLabel();
                var skip = c2.DefineLabel();
                c2.Emit(OpCodes.Ldarg, 0);
                c2.Emit(OpCodes.Call, typeof(UserData).GetMethod(nameof(UserData.OwnerHasRopeMomentum)));
                c2.Emit(OpCodes.Brfalse, keep);
                // 钩索惯性:跳过整个赋值序列,pos 保持不变(=身体的位置)
                c2.Emit(OpCodes.Br, skip);
                c2.MarkLabel(keep);
                // 非惯性:原序列照常执行(pos 字段声明在 BodyChunk 上,类型是 Vector2)
                c2.GotoNext(MoveType.After, i => i.MatchStfld<BodyChunk>("pos"));
                c2.MarkLabel(skip);
            }
            else
            {
                UnityEngine.Debug.Log("FrictionIL.frictionhook5: 未能定位到脚钉复位");
            }

            //比较意义不明的部分

            // // 惯性时跳过 WallClimb 接管(Player.cs 12147),防止贴墙爬墙模式干扰被拉越障
            // // 注意:赋值序列是 ldarg.0 → ldsfld → stfld 三条,ldarg.0 也在序列内
            // if (c2.TryGotoNext(MoveType.Before,
            //     i => i.MatchLdarg(0),
            //     i => i.MatchLdsfld<Player.BodyModeIndex>("WallClimb"),
            //     i => i.MatchStfld<Player>("bodyMode")
            //     ))
            // {
            //     // 栈: [](ldsfld 压1 stfld 弹1,净空)
            //     var skipWallClimb = c2.DefineLabel();
            //     c2.Emit(OpCodes.Ldarg, 0);
            //     c2.Emit(OpCodes.Call, typeof(UserData).GetMethod(nameof(UserData.OwnerHasRopeMomentum)));
            //     c2.Emit(OpCodes.Brtrue, skipWallClimb);
            //     c2.GotoNext(MoveType.After, i => i.MatchStfld<Player>("bodyMode"));
            //     c2.MarkLabel(skipWallClimb);
            // }
            // else
            // {
            //     UnityEngine.Debug.Log("FrictionIL.frictionhook5: 未能定位到 WallClimb 接管");
            // }

            // // 惯性时跳过 LedgeGrab 吸边触发(Player.cs 12363-12366),防止被吸在台阶边缘
            // // 注意:赋值序列是 ldarg.0 → ldsfld → stfld 三条,ldarg.0 也在序列内
            // if (c2.TryGotoNext(MoveType.Before,
            //     i => i.MatchLdarg(0),
            //     i => i.MatchLdsfld<Player.AnimationIndex>("LedgeGrab"),
            //     i => i.MatchStfld<Player>("animation")
            //     ))
            // {
            //     // 栈: [](净空)
            //     var skipLedgeGrab = c2.DefineLabel();
            //     c2.Emit(OpCodes.Ldarg, 0);
            //     c2.Emit(OpCodes.Call, typeof(UserData).GetMethod(nameof(UserData.OwnerHasRopeMomentum)));
            //     c2.Emit(OpCodes.Brtrue, skipLedgeGrab);
            //     c2.GotoNext(MoveType.After, i => i.MatchStfld<Player>("animation"));
            //     c2.MarkLabel(skipLedgeGrab);
            // }
            // else
            // {
            //     UnityEngine.Debug.Log("FrictionIL.frictionhook5: 未能定位到 LedgeGrab 触发");
            // }
        }

        private static void frictionhook6(ILContext il)
        {
            var c = new ILCursor(il);
            // 方法开头:惯性时把 speed 参数归零,避免 TerrainImpact 撞晕/撞死
            // 参数索引(实例方法):1=chunk, 2=direction, 3=speed, 4=firstContact
            var keep = c.DefineLabel();
            c.Emit(OpCodes.Ldarg, 0);
            c.Emit(OpCodes.Call, typeof(UserData).GetMethod(nameof(UserData.OwnerHasRopeMomentum)));
            c.Emit(OpCodes.Brfalse, keep);
            c.Emit(OpCodes.Ldc_R4, 0f);
            c.Emit(OpCodes.Starg, 3); // speed = 0
            c.MarkLabel(keep);
        }
    }
}
