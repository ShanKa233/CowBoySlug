using CowBoySLug;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace CowBoySlug.ExAbility
{
    public static class ExRock
    {
        public static ConditionalWeakTable<Rock, SuperShootModule> module = new ConditionalWeakTable<Rock, SuperShootModule>();
        public static SuperShootModule SuperRock(this Rock rock) => module.GetValue(rock, (Rock _) => new SuperShootModule(rock));

        public static bool IsSuperRock(this Weapon weapon, out SuperShootModule superRock)
        {
            superRock = null;
            var rock = (weapon as Rock);
            if (rock == null) return false;

            superRock = rock.SuperRock();
            return superRock.powerCount > 0;
        }

    }
    public class SuperShootModule
    {
        public static int MaxReboundTime = 12;
        Rock rock;
        public int powerCount = 0;

        public Color origColor;
        public Color endColor = Color.yellow;

        public Color nowColor => Color.Lerp(origColor, endColor, Mathf.InverseLerp(0, MaxReboundTime, powerCount));
        public void Rebound(Vector2? shootDir = null)
        {
            powerCount--;
            rock.room.AddObject(new Explosion.ExplosionLight(rock.firstChunk.pos, 50f, 0.3f, 3, new Color(1f, 1f, 1f)));
            rock.room.AddObject(new Explosion.ExplosionLight(rock.firstChunk.pos, 50f, 0.3f, 2, rock.SuperRock().nowColor));


            var dir = shootDir ?? -rock.firstChunk.contactPoint.ToVector2() + Custom.RNV() * 0.2f;

            //if (rock.firstChunk.contactPoint.x != 0) dir.x *= -1;
            //if (rock.firstChunk.contactPoint.y != 0) dir.y *= -1;

            //IntVector2 throwDir = new IntVector2(0, 0);

            //if (dir.x > 0)
            //    throwDir.x = 1;
            //else if (dir.x < 0)
            //    throwDir.x = -1;

            //if (dir.y > 0)
            //    throwDir.y = 1;
            //else if (dir.y < 0)
            //    throwDir.y = -1;

            rock.room.ScreenMovement(new Vector2?(rock.firstChunk.pos), rock.throwDir.ToVector2() * 1.5f, 0f);
            rock.room.PlaySound(SoundID.Rock_Hit_Wall, rock.firstChunk);


            rock.throwDir = new IntVector2(Math.Sign(-dir.x), Math.Sign(-dir.y));
            rock.changeDirCounter = 3;
            rock.ChangeOverlap(true);
            //rock.firstChunk.MoveFromOutsideMyUpdate(false, rock.firstChunk.pos);
            rock.firstChunk.vel = dir * 40f * Custom.LerpMap(powerCount, 0, MaxReboundTime, 0.5f, 10f);
            rock.ChangeMode(Rock.Mode.Thrown);
            rock.setRotation = dir;
            rock.rotationSpeed = 10f;


            if (rock.room.BeingViewed)
            {
                for (int i = 0; i < 7; i++)
                {
                    rock.room.AddObject(new Spark(rock.firstChunk.pos + rock.throwDir.ToVector2() * (rock.firstChunk.rad - 1f), Custom.DegToVec(Random.value * 360f) * 10f * Random.value + -dir * 3f, nowColor, null, 2, 4));
                }
            }

        }

        public void SetColor(Color oldColor, Color powerColor)
        {
            if (powerCount == 0)
            {
                origColor = oldColor;
            }
            endColor = powerColor;
        }
        public void SetColor(Color powerColor)
        {
            if (powerCount == 0)
            {
                origColor = rock.color;
            }
            endColor = powerColor;
        }


        public SuperShootModule(Rock rock)
        {
            this.rock = rock;
        }
        public static void OnHook()
        {
            On.Player.ThrowObject += Player_ThrowObject;
            On.Weapon.ChangeMode += WhenRockCantAttackStop;

            On.Weapon.Update += Weapon_Update;





            On.Weapon.HitWall += Weapon_HitWall;
            On.Rock.HitSomething += Rock_HitSomething;
            On.Weapon.WeaponDeflect += Weapon_WeaponDeflect;

            On.Rock.DrawSprites += ChangeRockColor;
        }

        private static bool Rock_HitSomething(On.Rock.orig_HitSomething orig, Rock self, SharedPhysics.CollisionResult result, bool eu)
        {
            bool origFlag = orig.Invoke(self, result, eu);
            if (origFlag&&self.IsSuperRock(out SuperShootModule superRock))
            {
                if (result.obj is Creature)
                {
                    (result.obj as Creature).Violence(self.firstChunk, new Vector2?(self.firstChunk.vel * self.firstChunk.mass), result.chunk, result.onAppendagePos, Creature.DamageType.Blunt, 5, 80);
                }
            }


            return origFlag;
        }

        private static void Weapon_Update(On.Weapon.orig_Update orig, Weapon self, bool eu)
        {
            if (self.IsSuperRock(out var superRock))
            {
                if (self.firstChunk.ContactPoint.x != 0 || self.firstChunk.ContactPoint.y != 0)
                {
                    superRock.Rebound();
                }
            }
            orig.Invoke(self, eu);
        }

        private static void WhenRockCantAttackStop(On.Weapon.orig_ChangeMode orig, Weapon self, Weapon.Mode newMode)
        {
            orig.Invoke(self, newMode);
            if (self.IsSuperRock(out var superRock) && newMode == Weapon.Mode.Free && superRock.powerCount > 0)
            {
                superRock.Rebound();
            }
        }

        private static void Weapon_HitWall(On.Weapon.orig_HitWall orig, Weapon self)
        {
            var rock = (self as Rock);
            if (rock != null && rock.SuperRock().powerCount > 0)
            {
                rock.SuperRock().Rebound();
                return;
            }

            orig.Invoke(self);
        }
        private static void Weapon_WeaponDeflect(On.Weapon.orig_WeaponDeflect orig, Weapon self, Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
        {
            if (self.IsSuperRock(out var superRock))
            {
                //player.firstChunk.pos = Vector2.Lerp(player.firstChunk.pos, inbetweenPos, 0.5f);
                //player.firstChunk.vel = deflectDir * bounceSpeed * 0.5f;
                self.vibrate = 20;
                superRock.Rebound(deflectDir);
                return;
            }
            orig.Invoke(self, inbetweenPos, deflectDir, bounceSpeed);
        }

        private static void ChangeRockColor(On.Rock.orig_DrawSprites orig, Rock self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (self.SuperRock().powerCount > 0)
            {
                var color = self.SuperRock().nowColor;
                if (sLeaser.sprites[0].color != color)
                    sLeaser.sprites[0].color = color;

                if (sLeaser.sprites[1].color != color)
                    sLeaser.sprites[1].color = color;
            }

        }

        private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            //获取扔的俗头
            Rock rock = self.grasps[grasp].grabbed as Rock;
            orig.Invoke(self, grasp, eu);

            //测有没有扔出超级石头的资格
            bool canSuperShoot = rock != null && Plugin.RockShot.TryGet(self, out bool flag) && flag;
            if (!canSuperShoot) return;

            if (self.switchHandsProcess > 0f)
            {
                if (self.switchHandsProcess > 0.8f) return;

                self.mushroomCounter = 2;
                self.mushroomEffect = 0.5f;

                rock.SuperRock().powerCount = MaxReboundTime;
                rock.SuperRock().SetColor(Color.red);
                rock.SuperRock().Rebound(new Vector2(self.ThrowDirection, self.input[0].y));
                
            }


        }

        //public void RockMake(Rock rock)
        //{
        //    //检查玩家有没有做出正确的操作
        //    bool triga1 = player.input[0].x != 0 || player.input[0].y != 0;
        //    bool triga2 = stopTime > 15 && player.switchHandsCounter > 0;
        //    if (triga1 && triga2 && rock != null)
        //    {
        //        if (PlayerHook.rockModule.TryGetValue(rock, out var value))
        //        {
        //            PlayerHook.rockModule.Remove(rock);
        //        }
        //        PlayerHook.rockModule.Add(rock, new SuperRockModule(rock));
        //    }
        //}//确认是否属于超级投掷出去的石头并打上标记

    }
    //public class SuperRockModule
    //{
    //    //public bool isSuperRock = true;
    //    //public Rock player;
    //    //public Color rockColor = new Color(103 / 255f, 5 / 255f, 4 / 255f);
    //    //public bool canMoreFast = true;


    //    public void RockPowerUp()
    //    {
    //        var rock = player;
    //        if (rock != null && PlayerHook.rockModule.TryGetValue(rock, out var superRock) && superRock.isSuperRock)
    //        {

    //            var player = rock.thrownBy as Player;
    //            if (player != null)
    //            {
    //                rock.firstChunk.vel = new Vector2(player.input[0].x, player.input[0].y) * 250;
    //                player.mushroomCounter = 2;
    //                player.mushroomEffect = 0.5f;
    //            }
    //            canMoreFast = false;
    //        }

    //    private static void SuperRock_CreatureViolence(On.Creature.orig_Violence orig, Creature player, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    //    {
    //        if (source != null)
    //        {
    //            var superRock = source.owner as Rock;
    //            if (superRock != null && rockModule.TryGetValue(superRock, out var flag) && flag.isSuperRock)
    //            {
    //                damage = 5;
    //                stunBonus = 20;
    //            }
    //        }

    //        orig.Invoke(player, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    //    }


}
