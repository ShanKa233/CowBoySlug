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
        public static ConditionalWeakTable<Player, HatModule> modules = new ConditionalWeakTable<Player, HatModule>();

        public static void Hook()
        {
            On.RainWorld.OnModsInit += LoadHatTextrue;
            On.Player.ctor += PlayerHat_ctor;

            On.PlayerGraphics.InitiateSprites += Hat_InitiateSprites;
            On.PlayerGraphics.AddToContainer += Hat_AddToContainer;
            On.PlayerGraphics.DrawSprites += Hat_DrawSprites;

        }

        private static void LoadHatTextrue(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            hatAtlas = Futile.atlasManager.LoadAtlas("illustrations/hatSharp");
        }

        public static FAtlas hatAtlas;
        private static void Hat_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            if (!modules.TryGetValue(self.player, out var hatModule)) { return; }

            var index = hatModule.hatIndex;

            //防止重复执行的flag
            bool flag = index > 0 && sLeaser.sprites.Length > index;
            if (!flag) { return; }


            newContatiner = rCam.ReturnFContainer("Midground");
            for (int i = 0; i < 3; i++)
            {
                newContatiner.AddChild(sLeaser.sprites[index + i]);
            }

        }
        private static void Hat_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(self, sLeaser, rCam);
            if (!modules.TryGetValue(self.player, out var hatModule)) { return; }

            //值数等于原本扩容前的身体精灵数组长度
            var index = sLeaser.sprites.Length;
            hatModule.hatIndex = index;
            //给原本的身体精灵扩容
            Array.Resize<FSprite>(ref sLeaser.sprites, sLeaser.sprites.Length + 3);

            for (int i = index; i < index + 2; i++)
            {
                sLeaser.sprites[i] = new FSprite("Circle20");
            }



            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
                new TriangleMesh.Triangle(0, 1, 2),
                new TriangleMesh.Triangle(1, 2, 3),
            };
            sLeaser.sprites[index + 2] = new TriangleMesh("Futile_White", tris, false, false);
            var triangleMash = (sLeaser.sprites[index+2] as TriangleMesh);
            

            self.AddToContainer(sLeaser, rCam, null);
        }
        private static void Hat_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (!modules.TryGetValue(self.player, out var hatModule)) { return; }
            var index = hatModule.hatIndex;
            if (!hatModule.haveHat)
            {
                for (int i = index; i < index + 2; i++)
                {
                    sLeaser.sprites[i].alpha = 0;
                }
                (sLeaser.sprites[index + 2] as TriangleMesh).alpha=0;
                return;
            }

            for (int i = hatModule.hatIndex; i < hatModule.hatIndex + 2; i++)
            {
                sLeaser.sprites[i].alpha = 1;
            }
            (sLeaser.sprites[index + 2] as TriangleMesh).alpha = 1;

            var body = self.player.mainBodyChunk;
            Vector2 vector = sLeaser.sprites[3].GetPosition() + Custom.DegToVec(sLeaser.sprites[3].rotation+FixHatRotation(self)) * (7f);


            for (int i = index; i < index + 2; i++)
            {

                Vector2 showPos = vector;
                Vector2 showPos2 = showPos - Custom.DegToVec(sLeaser.sprites[3].rotation + FixHatRotation(self)) * (6 - 4);
                if (i != index)
                {
                    showPos = showPos2;
                }
                var spr = sLeaser.sprites[i];
                spr.SetPosition(showPos);
                spr.rotation = sLeaser.sprites[3].rotation+ FixHatRotation(self);
                spr.scale = 6 / 10f;
                if (i != index)
                {
                    spr.scaleX *= 2.5f;
                    spr.scaleY *= 0.6f;
                }

                spr.color = hatModule.mainColor;

            }
            sLeaser.sprites[index + 2].color = hatModule.decorateColor;


            //sLeaser.sprites[2].color = decorateColor;
            Vector2 dir = Custom.DegToVec(sLeaser.sprites[3].rotation+FixHatRotation(self));
            Vector2 per = Custom.PerpendicularVector(dir);

            Hat.DrawHatDecoratePice(hatModule.shape, sLeaser.sprites[index + 2] as TriangleMesh, vector, per, dir,self);

        }


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
        private static void PlayerHat_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            Hat.modules.Add(self, new HatModule());
        }

        public static void PlacePlayerHat(Player player,HatModule hatModule)
        {
            var newHat = new CowBoyHat(new CowBoyHatAbstract(player.room.world, player.abstractCreature.pos, player.room.game.GetNewID()));
            newHat.mainColor = hatModule.mainColor;
            newHat.decorateColor = hatModule.decorateColor;
            newHat.shape = hatModule.shape;
            newHat.setMainColor = true;

            hatModule.haveHat = false;

            newHat.PlaceInRoom(player.room);
            newHat.room.abstractRoom.AddEntity(newHat.abstractPhysicalObject);
        }
        public static void WearHat(CowBoyHat cowBoyHat,HatModule hatModule)
        {
            hatModule.haveHat = true;
            hatModule.shape = cowBoyHat.shape;
            hatModule.mainColor = cowBoyHat.mainColor;
            hatModule.decorateColor = cowBoyHat.decorateColor;
            cowBoyHat.room.PlaySound(SoundID.Big_Spider_Spit, cowBoyHat.firstChunk);

            cowBoyHat.room.abstractRoom.RemoveEntity(cowBoyHat.abstractPhysicalObject);
            cowBoyHat.RemoveFromRoom();
        }



        public static void DrawHatDecoratePice(HatType shape, TriangleMesh sprite,Vector2 vector,Vector2 per,Vector2 dir, PlayerGraphics player)
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

    public class HatModule
    {
        public int hatIndex = 0;

        public Color mainColor = Color.black;
        public Color decorateColor = Color.black;
        public HatType shape = HatType.Strap;

        public bool haveHat = false;
        public HatModule()
        {


        }
    }

    public enum HatType
    {
        None ,
        Strap ,
        Feather,
        Bone,
        Star,
        Grass,
        Bone2,
        Spider,
        Love,
        Eye,
        Moon,Bug,

    }
}
