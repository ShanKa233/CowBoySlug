using CowBoySlug;
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

namespace CowBoySlug.Mechanics.ShootSkill
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
            return superRock.remainingBounces > 0;
        }

    }
    public class SuperShootModule
    {
        public static int MaxBounceCount = 12;
        Rock rock;
        public Rock Rock => rock;
        public int remainingBounces = 0;

        // 添加玩家引用
        private Player throwerPlayer;
        public Player ThrowerPlayer => throwerPlayer;

        // 移除反弹冷却时间
        // private int reboundCooldown = 0;
        // private const int ReboundCooldownMax = 5; // 5帧的冷却时间

        public Color normalColor;
        public Color superShotColor = Color.yellow;

        public Color currentColor => Color.Lerp(normalColor, superShotColor, Mathf.InverseLerp(0, MaxBounceCount, remainingBounces));

        // 添加玩家参数，如果为null则使用已保存的throwerPlayer
        public void Bounce(Vector2? bounceDirection = null, Player player = null)
        {
            // 如果提供了玩家参数，更新throwerPlayer
            if (player != null)
            {
                throwerPlayer = player;
            }

            // 移除冷却检查
            // if (reboundCooldown > 0)
            // {
            //     return;
            // }

            // 每次反弹时减少剩余反弹次数
            if (remainingBounces > 0)
            {
                remainingBounces--;
                rock.room.AddObject(new Explosion.ExplosionLight(rock.firstChunk.pos, 50f, 0.3f, 3, new Color(1f, 1f, 1f)));
                rock.room.AddObject(new Explosion.ExplosionLight(rock.firstChunk.pos, 50f, 0.3f, 2, rock.SuperRock().currentColor));


                var direction = bounceDirection ?? -rock.firstChunk.contactPoint.ToVector2() + Custom.RNV() * 0.2f;

                rock.room.ScreenMovement(new Vector2?(rock.firstChunk.pos), rock.throwDir.ToVector2() * 1.5f, 0f);
                rock.room.PlaySound(SoundID.Rock_Hit_Wall, rock.firstChunk);


                rock.throwDir = new IntVector2(Math.Sign(-direction.x), Math.Sign(-direction.y));
                rock.changeDirCounter = 3;
                rock.ChangeOverlap(true);

                // 根据剩余的反弹次数调整速度
                float speedMultiplier = Custom.LerpMap(remainingBounces, 0, MaxBounceCount, 0.5f, 10f);
                rock.firstChunk.vel = direction * 40f * speedMultiplier;

                rock.ChangeMode(Rock.Mode.Thrown);
                rock.setRotation = direction;
                rock.rotationSpeed = 10f;


                if (rock.room.BeingViewed)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        rock.room.AddObject(new Spark(rock.firstChunk.pos + rock.throwDir.ToVector2() * (rock.firstChunk.rad - 1f), Custom.DegToVec(Random.value * 360f) * 10f * Random.value + -direction * 3f, currentColor, null, 2, 4));
                    }
                }
            }
            else
            {
                // 如果没有剩余反弹次数，则不再反弹，恢复正常行为
                rock.ChangeMode(Rock.Mode.Free);
            }
        }

        public void SetColor(Color oldColor, Color powerColor)
        {
            if (remainingBounces == 0)
            {
                normalColor = oldColor;
            }
            superShotColor = powerColor;
        }
        public void SetColor(Color powerColor)
        {
            if (remainingBounces == 0)
            {
                normalColor = rock.color;
            }
            superShotColor = powerColor;
        }


        public SuperShootModule(Rock rock)
        {
            this.rock = rock;
            this.throwerPlayer = null;
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
            if (origFlag && self.IsSuperRock(out SuperShootModule superRock))
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
                // 移除冷却时间更新
                // if (superRock.reboundCooldown > 0)
                // {
                //     superRock.reboundCooldown--;
                // }

                // 只有当还有剩余反弹次数时才检查接触点
                if (superRock.remainingBounces > 0 && (self.firstChunk.ContactPoint.x != 0 || self.firstChunk.ContactPoint.y != 0))
                {
                    // 传递已保存的玩家引用
                    superRock.Bounce();
                }
            }
            orig.Invoke(self, eu);
        }

        private static void WhenRockCantAttackStop(On.Weapon.orig_ChangeMode orig, Weapon self, Weapon.Mode newMode)
        {
            orig.Invoke(self, newMode);
            if (self.IsSuperRock(out var superRock) && newMode == Weapon.Mode.Free && superRock.remainingBounces > 0)
            {
                // 传递已保存的玩家引用
                superRock.Bounce();
            }
        }

        private static void Weapon_HitWall(On.Weapon.orig_HitWall orig, Weapon self)
        {
            var rock = (self as Rock);
            if (rock != null && rock.SuperRock().remainingBounces > 0)
            {
                // 传递已保存的玩家引用
                rock.SuperRock().Bounce();
                return;
            }

            orig.Invoke(self);
        }

        private static void Weapon_WeaponDeflect(On.Weapon.orig_WeaponDeflect orig, Weapon self, Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
        {
            if (self.IsSuperRock(out var superRock) && superRock.remainingBounces > 0)
            {
                self.vibrate = 20;
                // 传递已保存的玩家引用
                superRock.Bounce(deflectDir);
                return;
            }
            orig.Invoke(self, inbetweenPos, deflectDir, bounceSpeed);
        }

        private static void ChangeRockColor(On.Rock.orig_DrawSprites orig, Rock self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (self.SuperRock().remainingBounces > 0)
            {
                var color = self.SuperRock().currentColor;
                if (sLeaser.sprites[0].color != color)
                    sLeaser.sprites[0].color = color;

                if (sLeaser.sprites[1].color != color)
                    sLeaser.sprites[1].color = color;
            }

        }

        private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            //获取扔的石头
            Rock rock = self.grasps[grasp].grabbed as Rock;
            orig.Invoke(self, grasp, eu);

            //检查是否有资格使用超级射击
            bool canSuperShoot = rock != null && CowBoySLug.Plugin.RockShot.TryGet(self, out bool flag) && flag;
            if (!canSuperShoot) return;

            if (self.switchHandsProcess > 0f)
            {
                if (self.switchHandsProcess > 0.8f) return;

                self.mushroomCounter = 2;
                self.mushroomEffect = 0.5f;
                SuperShoot(self, rock);
            }
        }
        public static void SuperShoot( Player player,Rock rock)
        {
            // 调用本地方法
            SuperShoot_Local(player, rock);
            
            // 如果在线模式，调用兼容方法
            if (Compatibility.ModCompat_Helpers.RainMeadow_IsOnline)
            {
                Compatibility.Meadow.MeadowCompat.SuperShoot(player, rock);
            }
        }

        /// <summary>
        /// 处理本地超级射击的方法
        /// </summary>
        /// <param name="rock">被射击的石头</param>
        /// <param name="player">射击的玩家</param>
        public static void SuperShoot_Local(Player player,Rock rock)
        {
            rock.SuperRock().remainingBounces = MaxBounceCount;
            rock.SuperRock().SetColor(Color.red);
            rock.SuperRock().Bounce(new Vector2(player.ThrowDirection, player.input[0].y), player);
        }
    }

}
