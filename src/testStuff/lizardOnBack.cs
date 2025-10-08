using UnityEngine;
using System;
using RWCustom;
using System.Runtime.CompilerServices;
using MoreSlugcats;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace src.testStuff
{
    public static class LizardOnBackHook
    {
        public static Hook On_CanPutSlugOnBack;
        public static Hook On_CanPutSpearToBack;
        public static void Hook()
        {
            // Hook到Player.CanPutSlugToBack属性的getter方法//防止背了蜥蜴再背其他东西
            On_CanPutSlugOnBack = new Hook(
                typeof(Player).GetProperty("CanPutSlugToBack",
                    BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(LizardOnBackHook).GetMethod("CanPutSlugOnBack_Prefix",
                    BindingFlags.Static | BindingFlags.Public)
            );

            // Hook到Player.CanPutSpearToBack属性的getter方法//防止背了蜥蜴再背其他东西
            On_CanPutSpearToBack = new Hook(
                typeof(Player).GetProperty("CanPutSpearToBack",
                    BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(LizardOnBackHook).GetMethod("CanPutSpearToBack_Prefix",
                    BindingFlags.Static | BindingFlags.Public)
            );

            On.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated;//视觉上的修改

            On.Player.GrabUpdate += Player_GrabUpdate;
            IL.Player.GrabUpdate += IL_Player_GrabUpdate;
            On.Player.MaulingUpdate += PlayerOnMaulingUpdate;//防止撕咬的时候拿蜥蜴
            On.Player.EatMeatUpdate += PlayerOnEatMeatUpdate;//防止吃东西的时候拿蜥蜴
            On.Player.SwallowObject += PlayerOnSwallowObject;//防止吐东西的时候拿蜥蜴


            On.Player.Grabability += PlayerOnGrabability;//获取抓取能力
            On.Player.ObjectEaten += PlayerOnObjectEaten;//吃东西防止背蜥蜴
            On.Creature.Grab += CreatureOnGrab;//抓取生物
            On.Player.IsCreatureLegalToHoldWithoutStun += PlayerOnIsCreatureLegalToHoldWithoutStun;//拿手上不会晕


            On.Player.Stun += PlayerOnStun;//击晕放下蜥蜴
            On.Player.Die += PlayerOnDie;//死亡放下蜥蜴
        }

        // 处理Player.CanPutSlugToBack属性的Hook前缀

        public static bool CanPutSlugOnBack_Prefix(Func<Player, bool> orig, Player self)
        {
            // 检查玩家背上是否已有蜥蜴
            var lizardData = LizardOnBack.GetLizardOnBackData(self);
            if (lizardData.HasALizard)
            {
                // 如果背上有蜥蜴，禁止放置蛞蝓
                return false;
            }

            // 否则，继续执行原始方法
            return orig(self);
        }

        // 处理Player.CanPutSpearToBack属性的Hook前缀
        public static bool CanPutSpearToBack_Prefix(Func<Player, bool> orig, Player self)
        {
            // 检查玩家背上是否已有蜥蜴
            var lizardData = LizardOnBack.GetLizardOnBackData(self);
            if (lizardData.HasALizard)
            {
                // 如果背上有蜥蜴，禁止放置长矛
                return false;
            }

            // 否则，继续执行原始方法
            return orig(self);
        }

        private static void PlayerOnSwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
        {
            orig(self, grasp);
            LizardOnBack.GetLizardOnBackData(self).interactionLocked = true;
        }

        private static void PlayerOnEatMeatUpdate(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
        {
            orig(self, graspIndex);
            LizardOnBack.GetLizardOnBackData(self).increment = false;
            LizardOnBack.GetLizardOnBackData(self).interactionLocked = true;
        }


        private static void PlayerOnMaulingUpdate(On.Player.orig_MaulingUpdate orig, Player self, int graspIndex)
        {
            orig(self, graspIndex);
            LizardOnBack.GetLizardOnBackData(self).increment = false;
            LizardOnBack.GetLizardOnBackData(self).interactionLocked = true;

        }


        private static void PlayerOnObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            orig(self, edible);
            if (ModManager.MSC && SlugcatStats.NourishmentOfObjectEaten(self.SlugCatClass, edible) == -1)
            {
                return;
            }
            else
            {
                LizardOnBack.GetLizardOnBackData(self).interactionLocked = true;
            }
        }


        public static bool CanLizardToBack(Player self)
        {
            var lizardData = LizardOnBack.GetLizardOnBackData(self);
            if (lizardData.CanPutLizardToBack
            && self.pickUpCandidate is Lizard
            && ((self.grasps[0] != null && (self.Grabability(self.grasps[0].grabbed) > Player.ObjectGrabability.BigOneHand || self.grasps[0].grabbed is Lizard))
            || (self.grasps[1] != null && (self.Grabability(self.grasps[1].grabbed) > Player.ObjectGrabability.BigOneHand || self.grasps[1].grabbed is Lizard))
            || (self.grasps[0] != null && self.grasps[1] != null) || self.bodyMode == Player.BodyModeIndex.Crawl))
            {
                self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                lizardData.LizardToBack(self.pickUpCandidate as Lizard);
                return true;
            }
            return false;
        }
        private static void IL_Player_GrabUpdate(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel label = null;
            // 防止吃东西的时候拿上拿下
            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchStloc(1),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("spearOnBack")))
            {
                // UnityEngine.Debug.Log("定到吃东西");
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Action<Player>>(player =>
                {
                    // UnityEngine.Debug.Log("因为吃东西阻止背蜥蜴");
                    LizardOnBack.GetLizardOnBackData(player).increment = false;
                });
            }

            //如果能拿重新允许拿
            if (c.TryGotoNext(MoveType.Before,
                x => x.Match(OpCodes.Ldc_I4_M1),
                x => x.Match(OpCodes.Bgt_S),
                x => x.MatchLdarg(0)))
            {
                // UnityEngine.Debug.Log("定到CanRetrieveSlugFromBack");
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Action<Player>>(player =>
                {
                    var lizardData = LizardOnBack.GetLizardOnBackData(player);
                    // 手中是否有蜥蜴
                    var lizardGrasp = -1;
                    if (lizardData.CanPutLizardToBack)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            if (player.grasps[i] != null && lizardData.CanPutLizardToBack && player.grasps[i].grabbed is Lizard)
                            {
                                lizardGrasp = i;
                                break;
                            }
                        }
                    }
                    // 有蜥蜴可以背
                    if (player.input[0].pckp && (lizardGrasp > -1 || lizardData.CanRetrieveLizardFromBack))
                    {
                        // UnityEngine.Debug.Log("因为第二次测验通过所以increment = true");
                        lizardData.increment = true;
                    }
                });
            }

            //吐东西防止吐东西的时候拿蜥蜴
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("slugOnBack"),
                x => x.MatchLdcI4(1),
                x => x.MatchStfld<Player.SlugOnBack>("interactionLocked"),
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0),
                x => x.MatchStfld<Player>("swallowAndRegurgitateCounter")))
            {
                // UnityEngine.Debug.Log("定到吐东西");
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Action<Player>>(player =>
                {
                    var lizardData = LizardOnBack.GetLizardOnBackData(player);
                    lizardData.interactionLocked = true;
                });
            }

            // 定位到wantToThrow > 0的判断后
            if (c.TryGotoNext(MoveType.Before,
                x => x.Match(OpCodes.Brtrue_S),
                x => x.MatchLdsfld<ModManager>("CoopAvailable"),
                x => x.Match(OpCodes.Brfalse),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>("wantToThrow"),
                x => x.MatchLdcI4(0)))
            {
                // UnityEngine.Debug.Log("定到丢蜥蜴");
                c.Emit(OpCodes.Ldarg, 0);
                c.Emit(OpCodes.Ldarg, 1);
                c.EmitDelegate<Action<Player, bool>>((player, eu) =>
                {
                    var lizardData = LizardOnBack.GetLizardOnBackData(player);
                    if (player.wantToThrow > 0 && lizardData.HasALizard)
                    {
                        lizardData.ThrowLizard(eu);
                    }
                });
            }



            var c2 = c.Clone();

            if (c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(54),
                x => x.MatchLdloc(54),
                x => x.MatchLdcI4(2),
                x => x.Match(OpCodes.Blt))
                &&
                c2.TryGotoNext(MoveType.After,
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(50)))
            {
                // UnityEngine.Debug.Log("获取成功illabel");
                label = c.MarkLabel();

                c2.Emit(OpCodes.Ldarg, 0);
                c2.EmitDelegate<Func<Player, bool>>(CanLizardToBack);
                c2.Emit(OpCodes.Brtrue, label);
            }
        }


        private static void PlayerOnDie(On.Player.orig_Die orig, Player self)
        {
            if (LizardOnBack.GetLizardOnBackData(self).HasALizard)
            {
                LizardOnBack.GetLizardOnBackData(self).DropLizard();
            }
            orig(self);
        }


        private static void PlayerOnStun(On.Player.orig_Stun orig, Player self, int st)
        {
            var lizardData = LizardOnBack.GetLizardOnBackData(self);
            int origSt = st;
            if (self.room != null)
            {
                if (self.Malnourished)
                {
                    origSt = Mathf.RoundToInt((float)st * (self.exhausted ? 2f : 1.5f));
                }
                if (origSt > UnityEngine.Random.Range(40, 80) && lizardData.HasALizard && self.stunDamageType != Creature.DamageType.Blunt)
                {
                    lizardData.DropLizard();
                }
            }
            orig(self, st);
        }


        public static bool LikesPlayer(Lizard liz, Player player)
        {
            return liz.AI.LikeOfPlayer(liz.AI.tracker.RepresentationForCreature(player.abstractCreature, addIfMissing: false)) > 0.5f;
        }
        public static bool PlayerOnIsCreatureLegalToHoldWithoutStun(On.Player.orig_IsCreatureLegalToHoldWithoutStun orig, Player self, Creature grabcheck)
        {
            if (grabcheck is Lizard liz && LikesPlayer(liz, self))
            {
                return true;
            }
            return orig.Invoke(self, grabcheck);
        }
        public static bool CreatureOnGrab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspused, int chunkgrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideequallydominant, bool pacifying)
        {
            if (self is Player player && obj is Lizard liz && LikesPlayer(liz, player))
            {
                shareability = Creature.Grasp.Shareability.NonExclusive;
                pacifying = false;
            }
            return orig.Invoke(self, obj, graspused, chunkgrabbed, shareability, dominance, overrideequallydominant, pacifying);
        }
        public static Player.ObjectGrabability PlayerOnGrabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            var lizardData = LizardOnBack.GetLizardOnBackData(self);
            if (obj is Lizard liz && LikesPlayer(liz, self) && LizardOnBack.GetLizardOnBackData(self).lizard != obj)
            {
                return Player.ObjectGrabability.TwoHands;
            }
            //防止拿到蜥蜴嘴巴里的东西
            if (lizardData.HasALizard)
            {
                foreach (var grasp in lizardData.lizard.grasps)
                {
                    if (grasp?.grabbed == obj)
                    {
                        return Player.ObjectGrabability.CantGrab;
                    }
                }
            }
            return orig.Invoke(self, obj);
        }



        private static void Player_GraphicsModuleUpdated(On.Player.orig_GraphicsModuleUpdated orig, Player self, bool actuallyViewed, bool eu)
        {
            LizardOnBack.GetLizardOnBackData(self).GraphicsModuleUpdated(actuallyViewed, eu);
            orig(self, actuallyViewed, eu);
        }


        private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            var lizardData = LizardOnBack.GetLizardOnBackData(self);
            lizardData.Update(eu);

            bool num = (
                (self.input[0].x == 0 && self.input[0].y == 0 && !self.input[0].jmp && !self.input[0].thrw)
                || (ModManager.MMF && self.input[0].x == 0 && self.input[0].y == 1 && !self.input[0].jmp && !self.input[0].thrw
                && (self.bodyMode != Player.BodyModeIndex.ClimbingOnBeam || self.animation == Player.AnimationIndex.BeamTip || self.animation == Player.AnimationIndex.StandOnBeam)))
                && (self.mainBodyChunk.submersion < 0.5f || self.isRivulet);
            int num5 = -1;
            int num6 = -1;
            int lizardIndex = -1;
            if (num)
            {
                int num8 = -1;
                if (ModManager.MSC)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (self.grasps[i] != null)
                        {
                            if (self.grasps[i].grabbed is JokeRifle)
                            {
                                num5 = i;
                            }
                            else if (JokeRifle.IsValidAmmo(self.grasps[i].grabbed))
                            {
                                num6 = i;
                            }
                        }
                    }
                }
                int num9 = 0;
                while (num6 < 0 && num9 < 2 && (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
                {
                    if (self.grasps[num9] != null && self.grasps[num9].grabbed is IPlayerEdible && (self.grasps[num9].grabbed as IPlayerEdible).Edible)
                    {
                        num6 = num9;
                    }
                    num9++;
                }
                if ((num6 == -1
                || (self.FoodInStomach >= self.MaxFoodInStomach
                && !(self.grasps[num6].grabbed is KarmaFlower)
                && !(self.grasps[num6].grabbed is Mushroom)))
                && (self.objectInStomach == null || lizardData.CanPutLizardToBack))
                {
                    int num10 = 0;
                    while (num8 < 0 && num5 < 0 && lizardIndex < 0 && num10 < 2)
                    {
                        if (self.grasps[num10] != null)
                        {
                            if ((lizardData.CanPutLizardToBack
                            && self.grasps[num10].grabbed is Lizard
                            && !(self.grasps[num10].grabbed as Lizard).dead)
                            )
                            {
                                lizardIndex = num10;
                            }
                            // else if (CanPutSpearToBack && base.grasps[num10].grabbed is Spear)
                            // {
                            //     num5 = num10;
                            // }
                            else if (self.CanBeSwallowed(self.grasps[num10].grabbed))
                            {
                                num8 = num10;
                            }
                        }
                        num10++;
                    }
                }
                if (num6 > -1 && self.noPickUpOnRelease < 1)
                {
                    if (!self.input[0].pckp)
                    {
                        int j;
                        for (j = 1; j < 10 && self.input[j].pckp; j++)
                        {
                        }
                        if (j > 1 && j < 10)
                        {
                            // self.PickupPressed();
                        }
                    }
                }
                else if (self.input[0].pckp && !self.input[1].pckp)
                {
                    // self.PickupPressed();
                }
                if (self.input[0].pckp)
                {
                    if (lizardIndex > -1 || lizardData.CanRetrieveLizardFromBack)
                    {
                        // UnityEngine.Debug.Log("因为第一次测验通过所以increment = true");
                        lizardData.increment = true;
                    }
                }
            }

            // var lizardIndex = -1;
            // for (int i = 0; i < 2; i++)
            // {
            //     if (self.grasps[i]?.grabbed is Lizard)
            //     {
            //         lizardIndex = i;
            //     }
            // }
            // if (self.input[0].pckp && ((lizardIndex > -1 && lizardData.CanPutLizardToBack) || lizardData.CanRetrieveLizardFromBack))
            // {
            //     lizardData.increment = true;
            // }


            orig(self, eu);

            if (self.wantToPickUp <= 0)
            {
                return;
            }
            bool flag6 = true;
            if (self.animation == Player.AnimationIndex.DeepSwim)
            {
                if (self.grasps[0] == null && self.grasps[1] == null)
                {
                    flag6 = false;
                }
                else
                {
                    for (int num19 = 0; num19 < 10; num19++)
                    {
                        if (self.input[num19].y > -1 || self.input[num19].x != 0)
                        {
                            flag6 = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int num20 = 0; num20 < 5; num20++)
                {
                    if (self.input[num20].y > -1)
                    {
                        flag6 = false;
                        break;
                    }
                }
            }
            if (ModManager.MSC)
            {
                if (self.grasps[0] != null && self.grasps[0].grabbed is EnergyCell && self.mainBodyChunk.submersion > 0f)
                {
                    flag6 = false;
                }
            }
            if (!ModManager.MMF && self.grasps[0] != null && self.HeavyCarry(self.grasps[0].grabbed))
            {
                flag6 = true;
            }
            if (flag6)
            {
                int num21 = -1;
                for (int num22 = 0; num22 < 2; num22++)
                {
                    if (self.grasps[num22] != null)
                    {
                        num21 = num22;
                        break;
                    }
                }
                if (num21 > -1)
                {
                }
                else if (lizardData.HasALizard && self.mainBodyChunk.ContactPoint.y < 0)
                {
                    self.room.socialEventRecognizer.CreaturePutItemOnGround(lizardData.lizard, self);
                    lizardData.DropLizard();
                    self.wantToPickUp = 0;
                }
            }
        }

        public class LizardOnBack
        {
            public static ConditionalWeakTable<Player, LizardOnBack> ExPlayerLizardOnBackData = new ConditionalWeakTable<Player, LizardOnBack>();
            public static LizardOnBack GetLizardOnBackData(Player player) => ExPlayerLizardOnBackData.GetValue(player, _ => new LizardOnBack(player));
            public Player owner;

            public Lizard lizard;

            public bool increment;

            public int counter;

            public bool interactionLocked;

            public Player.AbstractOnBackStick abstractStick;

            public bool HasALizard => lizard != null;

            public bool CanPutLizardToBack
            {
                get
                {
                    if (!interactionLocked && lizard == null)
                    {
                        bool flag = true;
                        if (owner.spearOnBack != null)
                        {
                            flag = flag && !owner.spearOnBack.HasASpear;
                        }
                        if (owner.slugOnBack != null)
                        {
                            flag = flag && !owner.slugOnBack.HasASlug;
                        }
                        return flag;
                    }
                    return false;
                }
            }

            public bool CanRetrieveLizardFromBack
            {
                get
                {
                    // if (!ModManager.MSC && !ModManager.CoopAvailable)
                    // {
                    //     return false;
                    // }

                    if (owner.CanRetrieveSpearFromBack || owner.CanRetrieveSlugFromBack || lizard == null || interactionLocked || (owner.grasps[0] != null && owner.grasps[1] != null))
                    {
                        return false;
                    }

                    for (int i = 0; i < owner.grasps.Length; i++)
                    {
                        // 如果玩家手中抓着的物体是大型物体(比如长矛)或者是蜥蜴,则不能从背上取下蜥蜴
                        if (owner.grasps[i] != null && (owner.Grabability(owner.grasps[i].grabbed) > Player.ObjectGrabability.BigOneHand || owner.grasps[i].grabbed is Player))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }


            public LizardOnBack(Player owner)
            {
                this.owner = owner;
            }

            public void Update(bool eu)
            {
                // // UnityEngine.Debug.Log($"counter: {counter}");
                // // UnityEngine.Debug.Log($"increment: {increment}");
                // if (lizard != null)
                // {
                //     // UnityEngine.Debug.Log($"lizard on back: {lizard.Template.name}");
                // }
                // for (int i = 0; i < owner.grasps.Length; i++)
                // {
                //     if (owner.grasps[i]?.grabbed is Lizard)
                //     {
                //         // UnityEngine.Debug.Log($"lizard in hand {i}: {(owner.grasps[i].grabbed as Lizard).Template.name}");
                //     }
                // }
                // 如果背上有蜥蜴，确保蜥蜴可以穿过地板
                if (lizard != null)
                {
                    lizard.GoThroughFloors = true;

                    // 防止蜥蜴抓取其他蜥蜴
                    // for (int i = 0; i < lizard.grasps?.Length; i++)
                    // {
                    //     if (lizard.grasps[i]?.grabbed is Player)
                    //     {
                    //         lizard.ReleaseGrasp(i);
                    //     }
                    // }
                }

                // 处理increment标志，用于蜥蜴放到手上或背上的计时器
                if (increment)
                {
                    counter++;

                    // 有蜥蜴时，长按20帧将蜥蜴拿下来
                    if (lizard != null && counter > 20)
                    {
                        LizardToHand(eu);
                        counter = 0;
                    }
                    // 没有蜥蜴时，长按20帧将手上的蜥蜴放到背上
                    else if (lizard == null && counter > 20)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            if (owner.grasps[j] != null && owner.grasps[j].grabbed is Lizard)
                            {
                                owner.bodyChunks[0].pos += Custom.DirVec(owner.grasps[j].grabbed.firstChunk.pos, owner.bodyChunks[0].pos) * 2f;
                                LizardToBack(owner.grasps[j].grabbed as Lizard);
                                counter = 0;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    counter = 0;
                }

                // 松开拾取键时解除交互锁定
                if (!owner.input[0].pckp)
                {
                    interactionLocked = false;
                }

                // 重置increment标志，在Player_GrabUpdate中设置
                increment = false;
            }

            public void GraphicsModuleUpdated(bool actuallyViewed, bool eu)
            {
                // 如果没有蜥蜴，不需要更新
                if (lizard == null)
                {
                    return;
                }

                // 检查是否需要释放蜥蜴
                if (owner.slatedForDeletetion || lizard.slatedForDeletetion || lizard.grabbedBy.Count > 0)
                {
                    if (abstractStick != null)
                    {
                        abstractStick.Deactivate();
                    }
                    ChangeOverlap(newOverlap: true);
                    lizard = null;
                    return;
                }

                // 设置蜥蜴的碰撞状态
                ChangeOverlap(newOverlap: false);
                // if (!ModManager.CoopAvailable || lizard.isSlugpup)
                // {
                // 	lizard.bodyChunks[1].MoveFromOutsideMyUpdate(eu, (owner.graphicsModule != null) ? (owner.graphicsModule as PlayerGraphics).head.pos : owner.mainBodyChunk.pos);
                // 	lizard.bodyChunks[1].vel = owner.mainBodyChunk.vel;
                // 	lizard.bodyChunks[0].vel = Vector2.Lerp(lizard.bodyChunks[0].vel, Vector2.Lerp(owner.mainBodyChunk.vel, new Vector2(0f, 5f), 0.5f), 0.3f);
                // 	return;
                // }
                Vector2 moveTo = Vector2.Lerp(lizard.bodyChunks[0].pos, owner.bodyChunks[0].pos + Custom.DirVec(owner.bodyChunks[1].pos, owner.bodyChunks[0].pos) * 14f, 0.75f);
                Vector2 moveTo2 = Vector2.Lerp(lizard.bodyChunks[1].pos, owner.bodyChunks[1].pos + Custom.DirVec(owner.bodyChunks[1].pos, owner.bodyChunks[0].pos) * 14f, 0.75f);
                lizard.bodyChunks[0].MoveFromOutsideMyUpdate(eu, moveTo);
                lizard.bodyChunks[1].MoveFromOutsideMyUpdate(eu, moveTo2);
                lizard.bodyChunks[1].vel = owner.mainBodyChunk.vel;
                lizard.bodyChunks[0].vel = Vector2.Lerp(lizard.bodyChunks[0].vel, Vector2.Lerp(owner.mainBodyChunk.vel, new Vector2(0f, 5f), 0.5f), 0.9f);
            }

            public void LizardToHand(bool eu)
            {
                // 如果没有蜥蜴在背上，无法执行
                if (lizard == null)
                {
                    return;
                }

                // 检查玩家手中是否有大型物体，如果有则不能将蜥蜴拿到手上
                for (int i = 0; i < 2; i++)
                {
                    if (owner.grasps[i] != null && owner.Grabability(owner.grasps[i].grabbed) > Player.ObjectGrabability.BigOneHand)
                    {
                        return;
                    }
                }

                // 寻找空闲的手
                int emptyHand = -1;
                for (int j = 0; j < 2; j++)
                {
                    if (owner.grasps[j] == null)
                    {
                        emptyHand = j;
                        break;
                    }
                }

                // 如果有空闲的手，将蜥蜴从背上拿到手上
                if (emptyHand != -1)
                {
                    // 蜥蜴移动到手的位置
                    if (owner.graphicsModule != null)
                    {
                        lizard.firstChunk.MoveFromOutsideMyUpdate(eu, (owner.graphicsModule as PlayerGraphics).hands[emptyHand].pos);
                    }

                    // 改变碰撞状态
                    ChangeOverlap(newOverlap: true);

                    // 抓取蜥蜴
                    owner.SlugcatGrab(lizard, emptyHand);
                    lizard = null;

                    // 锁定交互和设置拾取冷却
                    interactionLocked = true;
                    owner.noPickUpOnRelease = 20;
                    owner.room.PlaySound(SoundID.Slugcat_Pick_Up_Creature, owner.mainBodyChunk);

                    // 处理抽象连接
                    if (abstractStick != null)
                    {
                        abstractStick.Deactivate();
                        abstractStick = null;
                    }
                }
            }

            // public void CheckCircularGrabbing(Player playerToGrab, Player reference, bool slugOnBack)
            // {
            // 	if (!slugOnBack)
            // 	{
            // 		for (int i = 0; i < playerToGrab.grasps.Length; i++)
            // 		{
            // 			if (playerToGrab.grasps[i]?.grabbed is Player player)
            // 			{
            // 				if (player != reference)
            // 				{
            // 					CheckCircularGrabbing(player, reference, slugOnBack);
            // 					continue;
            // 				}
            // 				// JollyCustom.Log($"Player to back {playerToGrab} had another player, releasing...");
            // 				playerToGrab.ReleaseGrasp(i);
            // 			}
            // 		}
            // 		return;
            // 	}
            // 	LizardOnBack slugOnBack2 = playerToGrab.slugOnBack;
            // 	if (slugOnBack2 != null && slugOnBack2.HasALizard)
            // 	{
            // 		if (playerToGrab.slugOnBack.slugcat != reference)
            // 		{
            // 			CheckCircularGrabbing(playerToGrab.slugOnBack.slugcat, reference, slugOnBack);
            // 		}
            // 		else
            // 		{
            // 			playerToGrab.slugOnBack.DropSlug();
            // 		}
            // 	}
            // }

            public void LizardToBack(Lizard lizardToBack)
            {
                // 如果已经背着蜥蜴，不能再背另一只
                if (lizard != null)
                {
                    return;
                }

                // 释放手中的蜥蜴
                for (int i = 0; i < 2; i++)
                {
                    if (owner.grasps[i] != null && owner.grasps[i].grabbed == lizardToBack)
                    {
                        owner.ReleaseGrasp(i);
                        break;
                    }
                }
                //让蜥蜴放下嘴里的东西
                lizardToBack.LoseAllGrasps();

                // 处理蜥蜴可能抓着的玩家
                for (int j = 0; j < lizardToBack.grasps.Length; j++)
                {
                    if (lizardToBack.grasps[j]?.grabbed is Player)
                    {
                        lizardToBack.ReleaseGrasp(j);
                    }
                }

                // 处理其他可能抓着这只蜥蜴的玩家
                if (lizardToBack.grabbedBy != null)
                {
                    for (int k = 0; k < lizardToBack.grabbedBy.Count; k++)
                    {
                        if (!(lizardToBack.grabbedBy[k].grabber is Player player) || player == owner)
                        {
                            continue;
                        }
                        for (int l = 0; l < 2; l++)
                        {
                            if (player.grasps[l]?.grabbed == lizardToBack)
                            {
                                player.ReleaseGrasp(l);
                                break;
                            }
                        }
                    }
                }

                // 将蜥蜴放到背上
                lizard = lizardToBack;
                ChangeOverlap(newOverlap: false);
                interactionLocked = true;
                owner.noPickUpOnRelease = 20;
                owner.room.PlaySound(SoundID.Slugcat_Pick_Up_Creature, owner.mainBodyChunk);

                // 处理抽象连接
                if (abstractStick != null)
                {
                    abstractStick.Deactivate();
                }
                abstractStick = new Player.AbstractOnBackStick(owner.abstractPhysicalObject, lizardToBack.abstractPhysicalObject);
            }

            public void DropLizard()
            {
                if (lizard != null)
                {
                    // 允许蜥蜴碰撞和被武器击中
                    ChangeOverlap(newOverlap: true);

                    // 给蜥蜴一个随机的初始速度并略微抬高
                    lizard.firstChunk.vel = owner.mainBodyChunk.vel + Custom.RNV() * 3f * UnityEngine.Random.value;
                    lizard.bodyChunks[1].pos += new Vector2(0f, 10f);

                    // 清除蜥蜴引用
                    lizard = null;

                    // 移除抽象连接
                    if (abstractStick != null)
                    {
                        abstractStick.Deactivate();
                        abstractStick = null;
                    }
                }
            }
            public void ThrowLizard(bool eu)
            {
                if (lizard != null)
                {
                    var lizard = this.lizard;
                    LizardToHand(eu);
                    owner.ThrowObject(0, eu);
                    float num17 =
                    (owner.ThrowDirection >= 0) ? Mathf.Max(owner.bodyChunks[0].pos.x, owner.bodyChunks[1].pos.x)
                    : Mathf.Min(owner.bodyChunks[0].pos.x, owner.bodyChunks[1].pos.x);
                    for (int num18 = 0; num18 < lizard.bodyChunks.Length; num18++)
                    {
                        lizard.bodyChunks[num18].pos.y = lizard.firstChunk.pos.y + 20f;
                        if (owner.ThrowDirection < 0)
                        {
                            if (lizard.bodyChunks[num18].pos.x > num17 - 8f)
                            {
                                lizard.bodyChunks[num18].pos.x = num17 - 8f;
                            }
                            if (lizard.bodyChunks[num18].vel.x > 0f)
                            {
                                lizard.bodyChunks[num18].vel.x = 0f;
                            }
                        }
                        else if (owner.ThrowDirection > 0)
                        {
                            if (lizard.bodyChunks[num18].pos.x < num17 + 8f)
                            {
                                lizard.bodyChunks[num18].pos.x = num17 + 8f;
                            }
                            if (lizard.bodyChunks[num18].vel.x < 0f)
                            {
                                lizard.bodyChunks[num18].vel.x = 0f;
                            }
                        }
                    }
                }
            }

            public void ChangeOverlap(bool newOverlap)
            {
                lizard.CollideWithObjects = newOverlap;
                lizard.canBeHitByWeapons = newOverlap;
                // lizard.onBack = (newOverlap ? null : owner);
                if (lizard.graphicsModule != null && owner.room != null)
                {
                    for (int i = 0; i < owner.room.game.cameras.Length; i++)
                    {
                        owner.room.game.cameras[i].MoveObjectToContainer(lizard.graphicsModule, owner.room.game.cameras[i].ReturnFContainer((!newOverlap) ? "Background" : "Midground"));
                    }
                }
            }
        }
    }
}