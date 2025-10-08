using System;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using static Creature;
using static Player;
using Random = UnityEngine.Random;

namespace src.testStuff
{
    public class grabupdateTest
    {
        public static void Hook()
        {
            On.Player.GrabUpdate += Player_GrabUpdate;
        }

        private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            if (self.spearOnBack != null)
            {
                self.spearOnBack.Update(eu);
            }
            if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
            {
                self.slugOnBack.Update(eu);
            }
            bool flag = ((self.input[0].x == 0 && self.input[0].y == 0 && !self.input[0].jmp && !self.input[0].thrw) || (ModManager.MMF && self.input[0].x == 0 && self.input[0].y == 1 && !self.input[0].jmp && !self.input[0].thrw && (self.bodyMode != Player.BodyModeIndex.ClimbingOnBeam || self.animation == Player.AnimationIndex.BeamTip || self.animation == Player.AnimationIndex.StandOnBeam))) && (self.mainBodyChunk.submersion < 0.5f || self.isRivulet);
            bool flag2 = false;
            bool flag3 = false;
            self.craftingObject = false;
            int num = -1;
            int num2 = -1;
            bool flag4 = false;
            if (ModManager.MSC && !self.input[0].pckp && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                PlayerGraphics.TailSpeckles tailSpecks = (self.graphicsModule as PlayerGraphics).tailSpecks;
                if (tailSpecks.spearProg > 0f)
                {
                    tailSpecks.setSpearProgress(Mathf.Lerp(tailSpecks.spearProg, 0f, 0.05f));
                    if (tailSpecks.spearProg < 0.025f)
                    {
                        tailSpecks.setSpearProgress(0f);
                    }
                }
                else
                {
                    self.smSpearSoundReady = false;
                }
            }
            if (self.input[0].pckp && !self.input[1].pckp && self.switchHandsProcess == 0f && !self.isSlugpup)
            {
                bool flag5 = self.grasps[0] != null || self.grasps[1] != null;
                if (self.grasps[0] != null && (self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.TwoHands || self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.Drag))
                {
                    flag5 = false;
                }
                if (flag5)
                {
                    if (self.switchHandsCounter == 0)
                    {
                        self.switchHandsCounter = 15;
                    }
                    else
                    {
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.switchHandsProcess = 0.01f;
                        self.wantToPickUp = 0;
                        self.noPickUpOnRelease = 20;
                    }
                }
                else
                {
                    self.switchHandsProcess = 0f;
                }
            }
            if (self.switchHandsProcess > 0f)
            {
                float num3 = self.switchHandsProcess;
                self.switchHandsProcess += 0.083333336f;
                if (num3 < 0.5f && self.switchHandsProcess >= 0.5f)
                {
                    self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Complete, self.mainBodyChunk);
                    self.SwitchGrasps(0, 1);
                }
                if (self.switchHandsProcess >= 1f)
                {
                    self.switchHandsProcess = 0f;
                }
            }
            int num4 = -1;
            int num5 = -1;
            int num6 = -1;
            if (flag)
            {
                int num7 = -1;
                if (ModManager.MSC)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (self.grasps[i] != null)
                        {
                            if (self.grasps[i].grabbed is JokeRifle)
                            {
                                num2 = i;
                            }
                            else if (JokeRifle.IsValidAmmo(self.grasps[i].grabbed))
                            {
                                num = i;
                            }
                        }
                    }
                }
                int num8 = 0;
                while (num5 < 0 && num8 < 2 && (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
                {
                    if (self.grasps[num8] != null && self.grasps[num8].grabbed is IPlayerEdible && (self.grasps[num8].grabbed as IPlayerEdible).Edible)
                    {
                        num5 = num8;
                    }
                    num8++;
                }
                if ((num5 == -1 || (self.FoodInStomach >= self.MaxFoodInStomach && !(self.grasps[num5].grabbed is KarmaFlower) && !(self.grasps[num5].grabbed is Mushroom))) && (self.objectInStomach == null || self.CanPutSpearToBack || self.CanPutSlugToBack))
                {
                    int num9 = 0;
                    while (num7 < 0 && num4 < 0 && num6 < 0 && num9 < 2)
                    {
                        if (self.grasps[num9] != null)
                        {
                            if ((self.CanPutSlugToBack && self.grasps[num9].grabbed is Player && !(self.grasps[num9].grabbed as Player).dead) || self.CanIPutDeadSlugOnBack(self.grasps[num9].grabbed as Player))
                            {
                                num6 = num9;
                            }
                            else if (self.CanPutSpearToBack && self.grasps[num9].grabbed is Spear)
                            {
                                num4 = num9;
                            }
                            else if (self.CanBeSwallowed(self.grasps[num9].grabbed))
                            {
                                num7 = num9;
                            }
                        }
                        num9++;
                    }
                }
                if (num5 > -1 && self.noPickUpOnRelease < 1)
                {
                    if (!self.input[0].pckp)
                    {
                        int num10 = 1;
                        while (num10 < 10 && self.input[num10].pckp)
                        {
                            num10++;
                        }
                        if (num10 > 1 && num10 < 10)
                        {
                            self.PickupPressed();
                        }
                    }
                }
                else if (self.input[0].pckp && !self.input[1].pckp)
                {
                    self.PickupPressed();
                }
                if (self.input[0].pckp)
                {
                    if (ModManager.MSC && (self.FreeHand() == -1 || self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer) && self.GraspsCanBeCrafted())
                    {
                        self.craftingObject = true;
                        flag3 = true;
                        num5 = -1;
                    }
                    if (num6 > -1 || self.CanRetrieveSlugFromBack)
                    {
                        UnityEngine.Debug.Log("设置slugOnBack.increment为true - 原因：玩家正在尝试从背上取出或放置小鼻涕虫");
                        self.slugOnBack.increment = true;
                    }
                    else if (num4 > -1 || self.CanRetrieveSpearFromBack)
                    {
                        self.spearOnBack.increment = true;
                    }
                    else if ((num7 > -1 || self.objectInStomach != null || self.isGourmand) && (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
                    {
                        flag3 = true;
                    }
                    if (num > -1 && num2 > -1)
                    {
                        flag4 = true;
                    }
                    if (ModManager.MSC && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear && (self.grasps[0] == null || self.grasps[1] == null) && num5 == -1 && self.input[0].y == 0)
                    {
                        PlayerGraphics.TailSpeckles tailSpecks2 = (self.graphicsModule as PlayerGraphics).tailSpecks;
                        if (tailSpecks2.spearProg == 0f)
                        {
                            tailSpecks2.newSpearSlot();
                        }
                        if (tailSpecks2.spearProg < 0.1f)
                        {
                            tailSpecks2.setSpearProgress(Mathf.Lerp(tailSpecks2.spearProg, 0.11f, 0.1f));
                        }
                        else
                        {
                            if (!self.smSpearSoundReady)
                            {
                                self.smSpearSoundReady = true;
                                self.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Pull, 0f, 1f, 1f + UnityEngine.Random.value * 0.5f);
                            }
                            tailSpecks2.setSpearProgress(Mathf.Lerp(tailSpecks2.spearProg, 1f, 0.05f));
                        }
                        if (tailSpecks2.spearProg > 0.6f)
                        {
                            (self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * ((tailSpecks2.spearProg - 0.6f) / 0.4f) * 2f;
                        }
                        if (tailSpecks2.spearProg > 0.95f)
                        {
                            tailSpecks2.setSpearProgress(1f);
                        }
                        if (tailSpecks2.spearProg == 1f)
                        {
                            self.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Grab, 0f, 1f, 0.5f + UnityEngine.Random.value * 1.5f);
                            self.smSpearSoundReady = false;
                            Vector2 pos = (self.graphicsModule as PlayerGraphics).tail[(int)((float)(self.graphicsModule as PlayerGraphics).tail.Length / 2f)].pos;
                            for (int j = 0; j < 4; j++)
                            {
                                Vector2 a = Custom.DirVec(pos, self.bodyChunks[1].pos);
                                self.room.AddObject(new WaterDrip(pos + Custom.RNV() * UnityEngine.Random.value * 1.5f, Custom.RNV() * 3f * UnityEngine.Random.value + a * Mathf.Lerp(2f, 6f, UnityEngine.Random.value), false));
                            }
                            for (int k = 0; k < 5; k++)
                            {
                                Vector2 a2 = Custom.RNV();
                                self.room.AddObject(new Spark(pos + a2 * UnityEngine.Random.value * 40f, a2 * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 4, 18));
                            }
                            int spearType = tailSpecks2.spearType;
                            tailSpecks2.setSpearProgress(0f);
                            AbstractSpear abstractSpear = new AbstractSpear(self.room.world, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID(), false);
                            self.room.abstractRoom.AddEntity(abstractSpear);
                            abstractSpear.pos = self.abstractCreature.pos;
                            abstractSpear.RealizeInRoom();
                            Vector2 vector = self.bodyChunks[0].pos;
                            Vector2 a3 = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
                            if (Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y) > Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y)
                            {
                                vector += Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 5f;
                                a3 *= -1f;
                                a3.x += 0.4f * (float)self.flipDirection;
                                a3.Normalize();
                            }
                            abstractSpear.realizedObject.firstChunk.HardSetPosition(vector);
                            abstractSpear.realizedObject.firstChunk.vel = Vector2.ClampMagnitude((a3 * 2f + Custom.RNV() * UnityEngine.Random.value) / abstractSpear.realizedObject.firstChunk.mass, 6f);
                            if (self.FreeHand() > -1)
                            {
                                self.SlugcatGrab(abstractSpear.realizedObject, self.FreeHand());
                            }
                            if (abstractSpear.type == AbstractPhysicalObject.AbstractObjectType.Spear)
                            {
                                (abstractSpear.realizedObject as Spear).Spear_makeNeedle(spearType, true);
                                if ((self.graphicsModule as PlayerGraphics).useJollyColor)
                                {
                                    (abstractSpear.realizedObject as Spear).jollyCustomColor = new Color?(PlayerGraphics.JollyColor(self.playerState.playerNumber, 2));
                                }
                            }
                            self.wantToThrow = 0;
                        }
                    }
                }
                if (num5 > -1 && self.wantToPickUp < 1 && (self.input[0].pckp || self.eatCounter <= 15) && self.Consious && Custom.DistLess(self.mainBodyChunk.pos, self.mainBodyChunk.lastPos, 3.6f))
                {
                    if (self.graphicsModule != null)
                    {
                        (self.graphicsModule as PlayerGraphics).LookAtObject(self.grasps[num5].grabbed);
                    }
                    if (ModManager.MSC && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && (self.KarmaCap == 9 || (self.room.game.IsArenaSession && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType != DLCSharedEnums.GameTypeID.Challenge) || (self.room.game.session is ArenaGameSession && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType == DLCSharedEnums.GameTypeID.Challenge && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeMeta.ascended)) && self.grasps[num5].grabbed is Fly && self.eatCounter < 1)
                    {
                        self.room.PlaySound(SoundID.Snail_Pop, self.mainBodyChunk, false, 1f, 1.5f + UnityEngine.Random.value);
                        self.eatCounter = 30;
                        self.room.AddObject(new ShockWave(self.grasps[num5].grabbed.firstChunk.pos, 25f, 0.8f, 4, false));
                        for (int l = 0; l < 5; l++)
                        {
                            self.room.AddObject(new Spark(self.grasps[num5].grabbed.firstChunk.pos, Custom.RNV() * 3f, Color.yellow, null, 25, 90));
                        }
                        self.grasps[num5].grabbed.Destroy();
                        self.grasps[num5].grabbed.abstractPhysicalObject.Destroy();
                        if (self.room.game.IsArenaSession)
                        {
                            self.AddFood(1);
                        }
                    }
                    flag2 = true;
                    if (self.FoodInStomach < self.MaxFoodInStomach || self.grasps[num5].grabbed is KarmaFlower || self.grasps[num5].grabbed is Mushroom)
                    {
                        flag3 = false;
                        if (self.spearOnBack != null)
                        {
                            self.spearOnBack.increment = false;
                        }
                        if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                        {
                            UnityEngine.Debug.Log("设置slugOnBack.increment为false - 原因：玩家正在进食肉类，暂停与背上小鼻涕虫的交互");
                            self.slugOnBack.increment = false;
                        }
                        if (self.eatCounter < 1)
                        {
                            self.eatCounter = 15;
                            self.BiteEdibleObject(eu);
                        }
                    }
                    else if (self.eatCounter < 20 && self.room.game.cameras[0].hud != null)
                    {
                        self.room.game.cameras[0].hud.foodMeter.RefuseFood();
                    }
                }
            }
            else if (self.input[0].pckp && !self.input[1].pckp)
            {
                self.PickupPressed();
            }
            else
            {
                if (self.CanPutSpearToBack)
                {
                    for (int m = 0; m < 2; m++)
                    {
                        if (self.grasps[m] != null && self.grasps[m].grabbed is Spear)
                        {
                            num4 = m;
                            break;
                        }
                    }
                }
                if (self.CanPutSlugToBack)
                {
                    for (int n = 0; n < 2; n++)
                    {
                        if (self.grasps[n] != null && self.grasps[n].grabbed is Player && !(self.grasps[n].grabbed as Player).dead)
                        {
                            num6 = n;
                            break;
                        }
                    }
                }
                if (self.input[0].pckp && (num6 > -1 || self.CanRetrieveSlugFromBack))
                {
                    UnityEngine.Debug.Log("设置slugOnBack.increment为true - 原因：玩家按下拾取键并且可以从背上取出或放置小鼻涕虫");
                    self.slugOnBack.increment = true;
                }
                if (self.input[0].pckp && (num4 > -1 || self.CanRetrieveSpearFromBack))
                {
                    self.spearOnBack.increment = true;
                }
            }
            int num11 = 0;
            if (ModManager.MMF && (self.grasps[0] == null || !(self.grasps[0].grabbed is Creature)) && self.grasps[1] != null && self.grasps[1].grabbed is Creature)
            {
                num11 = 1;
            }
            if (ModManager.MSC && SlugcatStats.SlugcatCanMaul(self.SlugCatClass))
            {
                if (self.input[0].pckp && self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && (self.CanMaulCreature(self.grasps[num11].grabbed as Creature) || self.maulTimer > 0))
                {
                    self.maulTimer++;
                    (self.grasps[num11].grabbed as Creature).Stun(60);
                    self.MaulingUpdate(num11);
                    if (self.spearOnBack != null)
                    {
                        self.spearOnBack.increment = false;
                        self.spearOnBack.interactionLocked = true;
                    }
                    if (self.slugOnBack != null)
                    {
                        UnityEngine.Debug.Log("设置slugOnBack.increment为false - 原因：玩家正在进食肉类，暂停与背上小鼻涕虫的交互");
                        self.slugOnBack.increment = false;
                        self.slugOnBack.interactionLocked = true;
                    }
                    if (self.grasps[num11] != null && self.maulTimer % 40 == 0)
                    {
                        self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                        self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                        Custom.Log(new string[]
                        {
                    "Mauled target"
                        });
                        if (!(self.grasps[num11].grabbed as Creature).dead)
                        {
                            for (int num12 = UnityEngine.Random.Range(8, 14); num12 >= 0; num12--)
                            {
                                self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[num11].grabbedChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + self.grasps[num11].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(self.grasps[num11].grabbed.firstChunk.pos, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
                            }
                            Creature creature = self.grasps[num11].grabbed as Creature;
                            creature.SetKillTag(self.abstractCreature);
                            creature.Violence(self.bodyChunks[0], new Vector2?(new Vector2(0f, 0f)), self.grasps[num11].grabbedChunk, null, Creature.DamageType.Bite, 1f, 15f);
                            creature.stun = 5;
                            if (creature.abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.Inspector)
                            {
                                creature.Die();
                            }
                        }
                        self.maulTimer = 0;
                        self.wantToPickUp = 0;
                        if (self.grasps[num11] != null)
                        {
                            self.TossObject(num11, eu);
                            self.ReleaseGrasp(num11);
                        }
                        self.standing = true;
                    }
                    return;
                }
                if (self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && (self.grasps[num11].grabbed as Creature).Consious && !self.IsCreatureLegalToHoldWithoutStun(self.grasps[num11].grabbed as Creature))
                {
                    Custom.Log(new string[]
                    {
                "Lost hold of live mauling target"
                    });
                    self.maulTimer = 0;
                    self.wantToPickUp = 0;
                    self.ReleaseGrasp(num11);
                    return;
                }
            }
            if (self.input[0].pckp && self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && self.CanEatMeat(self.grasps[num11].grabbed as Creature) && (self.grasps[num11].grabbed as Creature).Template.meatPoints > 0)
            {
                self.eatMeat++;
                self.EatMeatUpdate(num11);
                if (!ModManager.MMF)
                {
                }
                if (self.spearOnBack != null)
                {
                    self.spearOnBack.increment = false;
                    self.spearOnBack.interactionLocked = true;
                }
                if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                {
                    UnityEngine.Debug.Log("设置slugOnBack.increment和interactionLocked - 原因：玩家正在吃肉");
                    self.slugOnBack.increment = false;
                    self.slugOnBack.interactionLocked = true;
                }
                if (self.grasps[num11] != null && self.eatMeat % 80 == 0 && ((self.grasps[num11].grabbed as Creature).State.meatLeft <= 0 || self.FoodInStomach >= self.MaxFoodInStomach))
                {
                    self.eatMeat = 0;
                    self.wantToPickUp = 0;
                    self.TossObject(num11, eu);
                    self.ReleaseGrasp(num11);
                    self.standing = true;
                }
                return;
            }
            if (!self.input[0].pckp && self.grasps[num11] != null && self.eatMeat > 60)
            {
                self.eatMeat = 0;
                self.wantToPickUp = 0;
                self.TossObject(num11, eu);
                self.ReleaseGrasp(num11);
                self.standing = true;
                return;
            }
            self.eatMeat = Custom.IntClamp(self.eatMeat - 1, 0, 50);
            self.maulTimer = Custom.IntClamp(self.maulTimer - 1, 0, 20);
            if (!ModManager.MMF || self.input[0].y == 0)
            {
                if (flag2 && self.eatCounter > 0)
                {
                    if (ModManager.MSC)
                    {
                        if (num5 <= -1 || self.grasps[num5] == null || !(self.grasps[num5].grabbed is GooieDuck) || (self.grasps[num5].grabbed as GooieDuck).bites != 6 || self.timeSinceSpawned % 2 == 0)
                        {
                            self.eatCounter--;
                        }
                        if (num5 > -1 && self.grasps[num5] != null && self.grasps[num5].grabbed is GooieDuck && (self.grasps[num5].grabbed as GooieDuck).bites == 6 && self.FoodInStomach < self.MaxFoodInStomach)
                        {
                            (self.graphicsModule as PlayerGraphics).BiteStruggle(num5);
                        }
                    }
                    else
                    {
                        self.eatCounter--;
                    }
                }
                else if (!flag2 && self.eatCounter < 40)
                {
                    self.eatCounter++;
                }
            }
            if (flag4 && self.input[0].y == 0)
            {
                self.reloadCounter++;
                if (self.reloadCounter > 40)
                {
                    (self.grasps[num2].grabbed as JokeRifle).ReloadRifle(self.grasps[num].grabbed);
                    BodyChunk mainBodyChunk = self.mainBodyChunk;
                    mainBodyChunk.vel.y = mainBodyChunk.vel.y + 4f;
                    self.room.PlaySound(SoundID.Gate_Clamp_Lock, self.mainBodyChunk, false, 0.5f, 3f + UnityEngine.Random.value);
                    AbstractPhysicalObject abstractPhysicalObject = self.grasps[num].grabbed.abstractPhysicalObject;
                    self.ReleaseGrasp(num);
                    abstractPhysicalObject.realizedObject.RemoveFromRoom();
                    abstractPhysicalObject.Room.RemoveEntity(abstractPhysicalObject);
                    self.reloadCounter = 0;
                }
            }
            else
            {
                self.reloadCounter = 0;
            }
            if (ModManager.MMF && self.mainBodyChunk.submersion >= 0.5f)
            {
                flag3 = false;
            }
            if (flag3)
            {
                if (self.craftingObject)
                {
                    self.swallowAndRegurgitateCounter++;
                    if (self.swallowAndRegurgitateCounter > 105)
                    {
                        self.SpitUpCraftedObject();
                        self.swallowAndRegurgitateCounter = 0;
                    }
                }
                else if (!ModManager.MMF || self.input[0].y == 0)
                {
                    self.swallowAndRegurgitateCounter++;
                    if ((self.objectInStomach != null || self.isGourmand || (ModManager.MSC && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)) && self.swallowAndRegurgitateCounter > 110)
                    {
                        bool flag6 = false;
                        if (self.isGourmand && self.objectInStomach == null)
                        {
                            flag6 = true;
                        }
                        if (!flag6 || (flag6 && self.FoodInStomach >= 1))
                        {
                            if (flag6)
                            {
                                self.SubtractFood(1);
                            }
                            self.Regurgitate();
                        }
                        else
                        {
                            self.firstChunk.vel += new Vector2(UnityEngine.Random.Range(-1f, 1f), 0f);
                            self.Stun(30);
                        }
                        if (self.spearOnBack != null)
                        {
                            self.spearOnBack.interactionLocked = true;
                        }
                        if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                        {
                            self.slugOnBack.interactionLocked = true;
                        }
                        self.swallowAndRegurgitateCounter = 0;
                    }
                    else if (self.objectInStomach == null && self.swallowAndRegurgitateCounter > 90)
                    {
                        for (int num13 = 0; num13 < 2; num13++)
                        {
                            if (self.grasps[num13] != null && self.CanBeSwallowed(self.grasps[num13].grabbed))
                            {
                                self.bodyChunks[0].pos += Custom.DirVec(self.grasps[num13].grabbed.firstChunk.pos, self.bodyChunks[0].pos) * 2f;
                                self.SwallowObject(num13);
                                if (self.spearOnBack != null)
                                {
                                    self.spearOnBack.interactionLocked = true;
                                }
                                if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
                                {
                                    self.slugOnBack.interactionLocked = true;
                                }
                                self.swallowAndRegurgitateCounter = 0;
                                (self.graphicsModule as PlayerGraphics).swallowing = 20;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (self.swallowAndRegurgitateCounter > 0)
                    {
                        self.swallowAndRegurgitateCounter--;
                    }
                    if (self.eatCounter > 0)
                    {
                        self.eatCounter--;
                    }
                }
            }
            else
            {
                self.swallowAndRegurgitateCounter = 0;
            }
            for (int num14 = 0; num14 < self.grasps.Length; num14++)
            {
                if (self.grasps[num14] != null && self.grasps[num14].grabbed.slatedForDeletetion)
                {
                    self.ReleaseGrasp(num14);
                }
            }
            if (self.grasps[0] != null && self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.TwoHands)
            {
                self.pickUpCandidate = null;
            }
            else
            {
                PhysicalObject physicalObject = (self.dontGrabStuff < 1) ? self.PickupCandidate(20f) : null;
                if (self.pickUpCandidate != physicalObject && physicalObject != null && physicalObject is PlayerCarryableItem)
                {
                    (physicalObject as PlayerCarryableItem).Blink();
                }
                self.pickUpCandidate = physicalObject;
            }
            if (self.switchHandsCounter > 0)
            {
                self.switchHandsCounter--;
            }
            if (self.wantToPickUp > 0)
            {
                self.wantToPickUp--;
            }
            if (self.wantToThrow > 0)
            {
                self.wantToThrow--;
            }
            if (self.noPickUpOnRelease > 0)
            {
                self.noPickUpOnRelease--;
            }
            if (self.input[0].thrw && !self.input[1].thrw && (!ModManager.MSC || !self.monkAscension))
            {
                self.wantToThrow = 5;
            }
            if (self.wantToThrow > 0)
            {
                if (ModManager.MSC && MMF.cfgOldTongue.Value && self.grasps[0] == null && self.grasps[1] == null && self.SaintTongueCheck())
                {
                    Vector2 vector2 = new Vector2((float)self.flipDirection, 0.7f);
                    Vector2 normalized = vector2.normalized;
                    if (self.input[0].y > 0)
                    {
                        normalized = new Vector2(0f, 1f);
                    }
                    normalized = (normalized + self.mainBodyChunk.vel.normalized * 0.2f).normalized;
                    self.tongue.Shoot(normalized);
                    self.wantToThrow = 0;
                }
                else
                {
                    for (int num15 = 0; num15 < 2; num15++)
                    {
                        if (self.grasps[num15] != null && self.IsObjectThrowable(self.grasps[num15].grabbed))
                        {
                            self.ThrowObject(num15, eu);
                            self.wantToThrow = 0;
                            break;
                        }
                    }
                }
                if ((ModManager.MSC || ModManager.CoopAvailable) && self.wantToThrow > 0 && self.slugOnBack != null && self.slugOnBack.HasASlug)
                {
                    Player slugcat = self.slugOnBack.slugcat;
                    self.slugOnBack.SlugToHand(eu);
                    self.ThrowObject(0, eu);
                    float num16 = (self.ThrowDirection >= 0) ? Mathf.Max(self.bodyChunks[0].pos.x, self.bodyChunks[1].pos.x) : Mathf.Min(self.bodyChunks[0].pos.x, self.bodyChunks[1].pos.x);
                    for (int num17 = 0; num17 < slugcat.bodyChunks.Length; num17++)
                    {
                        slugcat.bodyChunks[num17].pos.y = self.firstChunk.pos.y + 20f;
                        if (self.ThrowDirection < 0)
                        {
                            if (slugcat.bodyChunks[num17].pos.x > num16 - 8f)
                            {
                                slugcat.bodyChunks[num17].pos.x = num16 - 8f;
                            }
                            if (slugcat.bodyChunks[num17].vel.x > 0f)
                            {
                                slugcat.bodyChunks[num17].vel.x = 0f;
                            }
                        }
                        else if (self.ThrowDirection > 0)
                        {
                            if (slugcat.bodyChunks[num17].pos.x < num16 + 8f)
                            {
                                slugcat.bodyChunks[num17].pos.x = num16 + 8f;
                            }
                            if (slugcat.bodyChunks[num17].vel.x < 0f)
                            {
                                slugcat.bodyChunks[num17].vel.x = 0f;
                            }
                        }
                    }
                }
            }
            if (self.wantToPickUp > 0)
            {
                bool flag7 = true;
                if (self.animation == Player.AnimationIndex.DeepSwim)
                {
                    if (self.grasps[0] == null && self.grasps[1] == null)
                    {
                        flag7 = false;
                    }
                    else
                    {
                        for (int num18 = 0; num18 < 10; num18++)
                        {
                            if (self.input[num18].y > -1 || self.input[num18].x != 0)
                            {
                                flag7 = false;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (int num19 = 0; num19 < 5; num19++)
                    {
                        if (self.input[num19].y > -1)
                        {
                            flag7 = false;
                            break;
                        }
                    }
                }
                if (ModManager.MSC)
                {
                    if (self.grasps[0] != null && self.grasps[0].grabbed is EnergyCell && self.mainBodyChunk.submersion > 0f)
                    {
                        flag7 = false;
                    }
                    else if (self.grasps[0] != null && self.grasps[0].grabbed is EnergyCell && self.canJump <= 0 && self.bodyMode != Player.BodyModeIndex.Crawl && self.bodyMode != Player.BodyModeIndex.CorridorClimb && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && self.animation != Player.AnimationIndex.HangFromBeam && self.animation != Player.AnimationIndex.ClimbOnBeam && self.animation != Player.AnimationIndex.AntlerClimb && self.animation != Player.AnimationIndex.VineGrab && self.animation != Player.AnimationIndex.ZeroGPoleGrab)
                    {
                        (self.grasps[0].grabbed as EnergyCell).Use(false);
                    }
                }
                if (!ModManager.MMF && self.grasps[0] != null && self.HeavyCarry(self.grasps[0].grabbed))
                {
                    flag7 = true;
                }
                if (flag7)
                {
                    int num20 = -1;
                    for (int num21 = 0; num21 < 2; num21++)
                    {
                        if (self.grasps[num21] != null)
                        {
                            num20 = num21;
                            break;
                        }
                    }
                    if (num20 > -1)
                    {
                        self.wantToPickUp = 0;
                        if (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer || !(self.grasps[num20].grabbed is Scavenger))
                        {
                            self.pyroJumpDropLock = 0;
                        }
                        if (self.pyroJumpDropLock == 0 && (!ModManager.MSC || self.wantToJump == 0))
                        {
                            self.ReleaseObject(num20, eu);
                            return;
                        }
                    }
                    else
                    {
                        if (self.spearOnBack != null && self.spearOnBack.spear != null && self.mainBodyChunk.ContactPoint.y < 0)
                        {
                            self.room.socialEventRecognizer.CreaturePutItemOnGround(self.spearOnBack.spear, self);
                            self.spearOnBack.DropSpear();
                            return;
                        }
                        if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null && self.slugOnBack.slugcat != null && self.mainBodyChunk.ContactPoint.y < 0)
                        {
                            self.room.socialEventRecognizer.CreaturePutItemOnGround(self.slugOnBack.slugcat, self);
                            self.slugOnBack.DropSlug();
                            self.wantToPickUp = 0;
                            return;
                        }
                        if (ModManager.MSC && self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveState.wearingCloak && self.AI == null)
                        {
                            self.room.game.GetStorySession.saveState.wearingCloak = false;
                            AbstractConsumable abstractConsumable = new AbstractConsumable(self.room.game.world, MoreSlugcatsEnums.AbstractObjectType.MoonCloak, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID(), -1, -1, null);
                            self.room.abstractRoom.AddEntity(abstractConsumable);
                            abstractConsumable.pos = self.abstractCreature.pos;
                            abstractConsumable.RealizeInRoom();
                            (abstractConsumable.realizedObject as MoonCloak).free = true;
                            for (int num22 = 0; num22 < abstractConsumable.realizedObject.bodyChunks.Length; num22++)
                            {
                                abstractConsumable.realizedObject.bodyChunks[num22].HardSetPosition(self.mainBodyChunk.pos);
                            }
                            self.dontGrabStuff = 15;
                            self.wantToPickUp = 0;
                            self.noPickUpOnRelease = 20;
                            return;
                        }
                    }
                }
                else if (self.pickUpCandidate != null)
                {
                    if (self.pickUpCandidate is Spear && self.CanPutSpearToBack && ((self.grasps[0] != null && self.Grabability(self.grasps[0].grabbed) >= Player.ObjectGrabability.BigOneHand) || (self.grasps[1] != null && self.Grabability(self.grasps[1].grabbed) >= Player.ObjectGrabability.BigOneHand) || (self.grasps[0] != null && self.grasps[1] != null)))
                    {
                        Custom.Log(new string[]
                        {
                    "spear straight to back"
                        });
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.spearOnBack.SpearToBack(self.pickUpCandidate as Spear);
                    }
                    else if (self.CanPutSlugToBack && self.pickUpCandidate is Player && (!(self.pickUpCandidate as Player).dead || self.CanIPutDeadSlugOnBack(self.pickUpCandidate as Player)) && ((self.grasps[0] != null && (self.Grabability(self.grasps[0].grabbed) > Player.ObjectGrabability.BigOneHand || self.grasps[0].grabbed is Player)) || (self.grasps[1] != null && (self.Grabability(self.grasps[1].grabbed) > Player.ObjectGrabability.BigOneHand || self.grasps[1].grabbed is Player)) || (self.grasps[0] != null && self.grasps[1] != null) || self.bodyMode == Player.BodyModeIndex.Crawl))
                    {
                        Custom.Log(new string[]
                        {
                    "slugpup/player straight to back"
                        });
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.slugOnBack.SlugToBack(self.pickUpCandidate as Player);
                    }
                    else
                    {
                        int num23 = 0;
                        for (int num24 = 0; num24 < 2; num24++)
                        {
                            if (self.grasps[num24] == null)
                            {
                                num23++;
                            }
                        }
                        if (self.Grabability(self.pickUpCandidate) == Player.ObjectGrabability.TwoHands && num23 < 4)
                        {
                            for (int num25 = 0; num25 < 2; num25++)
                            {
                                if (self.grasps[num25] != null)
                                {
                                    self.ReleaseGrasp(num25);
                                }
                            }
                        }
                        else if (num23 == 0)
                        {
                            for (int num26 = 0; num26 < 2; num26++)
                            {
                                if (self.grasps[num26] != null && self.grasps[num26].grabbed is Fly)
                                {
                                    self.ReleaseGrasp(num26);
                                    break;
                                }
                            }
                        }
                        int num27 = 0;
                        while (num27 < 2)
                        {
                            if (self.grasps[num27] == null)
                            {
                                if (self.pickUpCandidate is Creature)
                                {
                                    self.room.PlaySound(SoundID.Slugcat_Pick_Up_Creature, self.pickUpCandidate.firstChunk, false, 1f, 1f);
                                }
                                else if (self.pickUpCandidate is PlayerCarryableItem)
                                {
                                    for (int num28 = 0; num28 < self.pickUpCandidate.grabbedBy.Count; num28++)
                                    {
                                        if (self.pickUpCandidate.grabbedBy[num28].grabber.room == self.pickUpCandidate.grabbedBy[num28].grabbed.room)
                                        {
                                            self.pickUpCandidate.grabbedBy[num28].grabber.GrabbedObjectSnatched(self.pickUpCandidate.grabbedBy[num28].grabbed, self);
                                        }
                                        else
                                        {
                                            Custom.LogWarning(new string[]
                                            {
                                        string.Format("Item theft room mismatch? {0}", self.pickUpCandidate.grabbedBy[num28].grabbed.abstractPhysicalObject)
                                            });
                                        }
                                        self.pickUpCandidate.grabbedBy[num28].grabber.ReleaseGrasp(self.pickUpCandidate.grabbedBy[num28].graspUsed);
                                    }
                                    (self.pickUpCandidate as PlayerCarryableItem).PickedUp(self);
                                }
                                else
                                {
                                    self.room.PlaySound(SoundID.Slugcat_Pick_Up_Misc_Inanimate, self.pickUpCandidate.firstChunk, false, 1f, 1f);
                                }
                                self.SlugcatGrab(self.pickUpCandidate, num27);
                                if (self.pickUpCandidate.graphicsModule != null && self.Grabability(self.pickUpCandidate) < (Player.ObjectGrabability)5)
                                {
                                    self.pickUpCandidate.graphicsModule.BringSpritesToFront();
                                    break;
                                }
                                break;
                            }
                            else
                            {
                                num27++;
                            }
                        }
                    }
                    self.wantToPickUp = 0;
                }
            }
        }
    }
}