using CowBoySLug;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CowBoySlug
{
    public static class Hat
    {
        public static ConditionalWeakTable<Player, AbstractHatWearStick> modules = new ConditionalWeakTable<Player, AbstractHatWearStick>();

        public static void Hook()
        {
            On.RainWorld.OnModsInit += LoadHatTextrue;//读取帽子形状贴图
            On.Player.CanBeSwallowed += Hat_CanBeSwallowed;//让帽子物品可以吞下

            On.Player.Grabability += Player_Grabability;//让帽子戴着的时候不会被抓到

            On.Player.ThrowObject += Player_ThrowObject;//扔帽子的时候运行的方法

            //On.Creature.PlaceInRoom += Creature_PlaceInRoom;

            //On.Player.ctor += PlayerHat_ctor;//用老的增加玩家贴图的方式来初始化绘制帽子

            //On.PlayerGraphics.InitiateSprites += Hat_InitiateSprites;
            //On.PlayerGraphics.AddToContainer += Hat_AddToContainer;
            //On.PlayerGraphics.DrawSprites += Hat_DrawSprites;

        }

        //private static void Creature_PlaceInRoom(On.Creature.orig_PlaceInRoom orig, Creature self, Room placeRoom)
        //{
        //    orig.Invoke(self,placeRoom);
        //    if (self is Player&&AbstractHatWearStick.GetHatModule(self as Player).HaveHat)
        //    {
        //        var hatList = AbstractHatWearStick.GetHatModule(self as Player).Hatlist;
        //        foreach (var hat in hatList)
        //        {
        //            if (hat.wearers == self)
        //            {
        //                //hat.PlaceInRoom(placeRoom);
        //                UnityEngine.Debug.Log("帽子放下");
        //                placeRoom.AddObject(hat);
        //                for (int j = 0; j < hat.bodyChunks.Length; j++)
        //                {
        //                    hat.bodyChunks[j].pos = self.mainBodyChunk.pos;
        //                    hat.bodyChunks[j].lastPos = self.mainBodyChunk.pos;
        //                    hat.bodyChunks[j].lastLastPos = self.mainBodyChunk.pos;
        //                    hat.bodyChunks[j].setPos = default(Vector2?);
        //                    hat.bodyChunks[j].vel *= 0f;
        //                }
        //            }
        //        }

        //    }
        //}

        private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            //如果扔的是帽子就改变一下帽子的飞行方向
            CowBoyHat hat = self.grasps[grasp].grabbed as CowBoyHat;
            if (hat != null)
            {
                if (self.input[0].x == 0 && self.input[0].y > 0)
                {
                    hat.rotation = new Vector2(self.input[0].x, 0.3f * self.input[0].y).normalized;

                }
                else
                {
                    hat.rotation = new Vector2(self.ThrowDirection, 0.3f * self.input[0].y).normalized;

                }
            }

            orig.Invoke(self, grasp, eu);
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            CowBoyHat hat = obj as CowBoyHat;
            if (hat != null)
            {
                //如果帽子被戴着而且被自己戴着,就不能拿自己的帽子
                if (hat.wearers != null && hat.wearers == self) return Player.ObjectGrabability.CantGrab;

                //在这个位置直接修改帽子的拿取来让他不会戴着的时候被抓
            }
            return orig.Invoke(self, obj);
        }
        private static bool Hat_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            if (testObj is CowBoyHat)
            {
                return true;
            }
            else
            {
                return orig.Invoke(self, testObj);
            }

        }

        private static void LoadHatTextrue(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            hatAtlas = Futile.atlasManager.LoadAtlas("illustrations/hatSharp");
        }


        public static FAtlas hatAtlas;




        public static float FixHatRotation(PlayerGraphics self)
        {
            var player = self.player;
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.mainBodyChunk.pos.x > player.bodyChunks[1].pos.x)
                {
                    return -70;
                }
                else
                {
                    return 70;
                }

            }
            else if (self.player.bodyMode == Player.BodyModeIndex.Stand && self.player.input[0].x > 0)
            {
                return -20;
            }
            else if (self.player.bodyMode == Player.BodyModeIndex.Stand && self.player.input[0].x < 0)
            {
                return 20;
            }
            else
            {
                return 0;
            }
        }

        public static void DrawHatDecoratePice(HatType shape, TriangleMesh sprite, Vector2 vector, Vector2 per, Vector2 dir, PlayerGraphics player)
        {
            switch (shape)
            {
                case HatType.Strap:
                    //绑带b版本
                    sprite.MoveVertice(0, vector + (per * -7) + (dir * -2));
                    sprite.MoveVertice(1, vector + (per * -6) + (dir * 0));
                    sprite.MoveVertice(2, vector + (per * 7) + (dir * -2));
                    sprite.MoveVertice(3, vector + (per * 6) + (dir * 0));
                    break;
                case HatType.Feather:
                    //羽毛版本
                    if (WhenHatInRight(player))
                    {
                        sprite.alpha = 0;
                        return;
                    }
                    sprite.MoveVertice(0, vector - (per * 3) + (dir * -2));
                    sprite.MoveVertice(1, vector - (per * 9) + (dir * 2));
                    sprite.MoveVertice(2, vector - (per * 7) + (dir * 4));
                    sprite.MoveVertice(3, vector - (per * 13) + (dir * 5));
                    break;
                default:
                    string type = ("hatSharp-" + shape);
                    if (hatAtlas._elementsByName.TryGetValue(type, out var element))
                    {
                        if (WhenHatInRight(player))
                        {
                            sprite.alpha = 0;
                            return;
                        }

                        sprite.SetElementByName(type);
                        sprite.UVvertices[0] = hatAtlas._elementsByName[type].uvBottomLeft;
                        sprite.UVvertices[1] = hatAtlas._elementsByName[type].uvTopLeft;
                        sprite.UVvertices[2] = hatAtlas._elementsByName[type].uvBottomRight;
                        sprite.UVvertices[3] = hatAtlas._elementsByName[type].uvTopRight;


                        sprite.MoveVertice(0, vector - (per * 0) + (dir * -5));
                        sprite.MoveVertice(1, vector - (per * 3) + (dir * 4));
                        sprite.MoveVertice(2, vector - (per * 11) + (dir * -6));
                        sprite.MoveVertice(3, vector - (per * 13) + (dir * 4));

                    }
                    else
                    {
                        sprite.SetElementByName("Futile_White");
                        sprite.MoveVertice(0, vector + (per * -7) + (dir * -2));
                        sprite.MoveVertice(1, vector + (per * -6) + (dir * 0));
                        sprite.MoveVertice(2, vector + (per * 7) + (dir * -2));
                        sprite.MoveVertice(3, vector + (per * 6) + (dir * 0));
                    }

                    break;
            }
        }
        public static bool WhenHatInRight(PlayerGraphics self)
        {
            if (self == null)
            {
                return false;
            }
            var player = self.player;

            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.mainBodyChunk.pos.x > player.bodyChunks[1].pos.x)
                {
                    return true;
                }
            }
            else if (self.player.bodyMode == Player.BodyModeIndex.Stand && self.player.input[0].x > 0)
            {
                return true;
            }
            return false;
        }
    }

    public class AbstractHatWearStick : AbstractPhysicalObject.AbstractObjectStick
    {
        public AbstractPhysicalObject AbsHat => this.A;
        public AbstractPhysicalObject Wearer => this.B;

        AbstractCreature wearer;
        AbstractPhysicalObject hat;

        public List<CowBoyHat> Hatlist = new List<CowBoyHat>();

        public AbstractHatWearStick(AbstractPhysicalObject hat, AbstractCreature wearer) : base(hat, wearer)
        {
            this.hat = hat;
            this.wearer = wearer;

        }

        public bool HaveHat => Hatlist.Count > 0;

        //public static AbstractHatWearStick GetHatModule(Player player) => Hat.modules.GetValue(player, (p) => new AbstractHatWearStick());

        //public static AbstractHatWearStick GetHatModule(Player player) => Hat.modules.GetValue(player, (p) => new AbstractHatWearStick());

    }

    public enum HatType
    {
        None,
        Strap,
        Feather,
        Bone,
        Star,
        Grass,
        Bone2,
        Spider,
        Love,
        Eye,
        Moon, Bug,

    }
}
