
using CowBoySLug;
using RWCustom;
using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace CowBoySlug
{
    public class PlayerGraphicsHook
    {
        public static void Hook()
        {
            On.Rock.DrawSprites += Rock_DrawSprites; //画出红红的石头来表示石头飞的很快


            On.RainWorld.OnModsInit += CowBoySlug_LoadTexture;
            On.PlayerGraphics.InitiateSprites += CowBoy_InitiateSprites;
            On.PlayerGraphics.DrawSprites += CowBoy_DrawSprites;
            On.PlayerGraphics.AddToContainer += CowBoye_AddToContainer;
        }

        #region 和猫猫样子有关的部分
        
        
        

        //加载材质进入游戏

        private static void CowBoySlug_LoadTexture(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            //在读取mod的时候加载材质
            atlas = Futile.atlasManager.LoadAtlas("atlases/CowBoyHead");
        }


        //添加我的特殊材质部件到游戏显示的大舞台当中
        private static void CowBoye_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            if (Plugin.HaveScarf.TryGet(self.player, out var flag3) && flag3)
            {
                if (PlayerHook.cowboyModules.TryGetValue(self.player, out var cowBoyModule))
                {
                    //防止重复执行的flag
                    bool flag = cowBoyModule.scarfIndex > 0 && sLeaser.sprites.Length > cowBoyModule.scarfIndex;

                    if (flag)
                    {
                        AddScarfToContainer(sLeaser, rCam,cowBoyModule);
                    }
                }
            }
        }

        //扩容
        private static void CowBoy_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(self, sLeaser, rCam);
            if (!PlayerHook.cowboyModules.TryGetValue(self.player, out var cowBoy)){return;}

            bool flag = Plugin.HaveScarf.TryGet(self.player, out bool flag1) && flag1;//玩家是否可以获取特征,并开启特征
            bool flag2 = atlas._elementsByName.TryGetValue("CowBoy-HeadA0", out var element);//玩家是否加载材质
            if (flag && flag2)// 满足条件开始给玩家的精灵数组(灵魂容器)扩容并添加各类精灵要素
            {
                if (PlayerHook.cowboyModules.TryGetValue(self.player, out var cowBoyModule))
                {
                    cowBoyModule.ropeColor = Plugin.RopeColor.GetColor(self).Value;
                }
                GetScarfIndex(sLeaser, cowBoy);//帮围巾的贴图数组扩容并且获取围巾的index

                //将开启后的精灵添加进容器
                self.AddToContainer(sLeaser, rCam, null);
            }


        }

        //绘制蛞蝓猫
        private static void CowBoy_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (!PlayerHook.cowboyModules.TryGetValue(self.player, out var cowBoy)) { return; }
            bool flag = Plugin.HaveScarf.TryGet(self.player, out bool flag1) && flag1;//玩家是否可以获取仙女特征,并开启仙女特征
            bool flag2 = atlas._elementsByName.TryGetValue("CowBoy-HeadA0", out var element);//玩家是否加载仙女材质

            //如果玩家有此特征就让外套同步到身体上,跟随脖子旋转
            if (flag && flag2 )
            {
                DrawScarf(self, sLeaser, rCam, timeStacker,cowBoy);
                DrawUseRopeAnimetion(self, sLeaser, rCam, timeStacker, camPos);
            }
        }

        public static int HeadRotation(PlayerGraphics self, Vector2 vector, Vector2 per, Vector2 dir, out Vector2 vector2)
        {
            var player = self.player;
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.mainBodyChunk.pos.x > player.bodyChunks[1].pos.x)
                {
                    vector2 = vector - per * 2 + dir;
                    return -1;
                }
                else
                {
                    vector2 = vector - per + dir;
                    return 1;
                }

            }
            else
            {
                vector2 = vector;
                return 1;
            }
        }

        //精灵图位置
        public static FAtlas atlas;
        #endregion


        #region 用来弄显示的一堆方法

        public static void GetScarfIndex(RoomCamera.SpriteLeaser sLeaser,CowBoyModule cowBoyModule)
        {
            //值数等于原本扩容前的身体精灵数组长度
            var scarfIndex = sLeaser.sprites.Length;
            cowBoyModule.scarfIndex=scarfIndex;
            //给原本的身体精灵扩容
            Array.Resize<FSprite>(ref sLeaser.sprites, sLeaser.sprites.Length + 2);

            //给扩容的身体新建一个精灵,并使用材质
            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
       {
                new TriangleMesh.Triangle(0, 1, 2),
                new TriangleMesh.Triangle(1, 2, 3)
       };
            //给扩容的身体新建一个精灵,并使用材质
            sLeaser.sprites[scarfIndex] = new FSprite("CowBoy-" + sLeaser.sprites[3].element.name, true);
            sLeaser.sprites[scarfIndex + 1] = new TriangleMesh("Futile_White", tris, false, false);
        }

        public static void AddScarfToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam,CowBoyModule cowBoyModule)
        {
            var scarfIndex = cowBoyModule.scarfIndex;
            //如果玩家有这个特征,和模型而且数组空位已经增加就执行材质添加
            //将材质精灵添加到背景图层
            FContainer fContainer2 = rCam.ReturnFContainer("Midground");
            //FContainer fContainer2 = rCam.ReturnFContainer("Items");
            fContainer2.AddChild(sLeaser.sprites[scarfIndex]);
            fContainer2.AddChild(sLeaser.sprites[scarfIndex + 1]);


            //让材质覆盖其他身体部件
            for (int i = 0; i < scarfIndex; i++)
            {
                sLeaser.sprites[scarfIndex].MoveInFrontOfOtherNode(sLeaser.sprites[i]);
                sLeaser.sprites[scarfIndex + 1].MoveInFrontOfOtherNode(sLeaser.sprites[i]);
            }
        }
        public static void DrawScarf(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker,CowBoyModule cowBoyModule)
        {
            var scarfIndex = cowBoyModule.scarfIndex;
            //颜色设定
            Color scarfColor = Plugin.ScarfColor.GetColor(self).Value;
            sLeaser.sprites[scarfIndex].color = scarfColor;

            //材质赋值
            sLeaser.sprites[scarfIndex].element = new FSprite("CowBoy-" + sLeaser.sprites[3].element.name, true).element;

            //坐标的旋转角度设定
            sLeaser.sprites[scarfIndex].scaleX = sLeaser.sprites[3].scaleX;
            sLeaser.sprites[scarfIndex].scaleY = sLeaser.sprites[3].scaleY;
            //同步外套与脖子一同旋转
            sLeaser.sprites[scarfIndex].rotation = sLeaser.sprites[3].rotation;
            //同步外套坐标至身体处
            sLeaser.sprites[scarfIndex].x = sLeaser.sprites[3].x;
            sLeaser.sprites[scarfIndex].y = sLeaser.sprites[3].y;


            //后半条围巾的设定
            Vector2 dir = self.player.mainBodyChunk.Rotation;
            Vector2 per = Custom.PerpendicularVector(dir);
            Vector2 vector = Vector2.Lerp(self.player.mainBodyChunk.lastPos, self.player.mainBodyChunk.pos, timeStacker);
            vector = new Vector2(sLeaser.sprites[scarfIndex].x, sLeaser.sprites[scarfIndex].y);

            var tailMove = (self.tail[3].lastPos - Vector2.Lerp(self.tail[3].lastPos, self.tail[3].pos, timeStacker)).normalized;
            var scar = sLeaser.sprites[scarfIndex + 1] as TriangleMesh;
            scar.color = scarfColor;

            int changeDir = HeadRotation(self, vector, per, dir, out vector);
            float whenWalk = (self.player.bodyMode == Player.BodyModeIndex.Stand && self.player.input[0].x > 0) ? -0.75f : 1;
            float whenWalkLeft = (self.player.bodyMode == Player.BodyModeIndex.Stand && self.player.input[0].x < 0) ? 1.1f : 1;

            scar.MoveVertice(0, vector - dir * 6 - (per * 4) * changeDir * whenWalk / whenWalkLeft);
            scar.MoveVertice(1, vector - dir * 4 - (per * 11) * changeDir * whenWalk / whenWalkLeft + tailMove / 2);
            scar.MoveVertice(2, vector - dir * 8 - (per * 12) * changeDir * whenWalk / whenWalkLeft + tailMove / 2);
            scar.MoveVertice(3, vector - dir * 4 - (per * 18) * changeDir * whenWalk / whenWalkLeft + tailMove);

        }

        public static void DrawUseRopeAnimetion(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (self.player.Consious)
            {
                if (self.throwCounter > 0 && self.thrownObject != null)
                {
                    return;
                }
                else if (self.player.handOnExternalFoodSource != null)
                {
                    return;
                }
                else if ((self.player.grasps[0] != null && self.player.grasps[0].grabbed is TubeWorm) || (self.player.grasps[1] != null && self.player.grasps[1].grabbed is TubeWorm))
                {
                    return;
                }
                else if (self.player.spearOnBack != null && self.player.spearOnBack.counter > 5)
                {
                    return;
                }
            }
            else
            {
                return;
            }
            if (PlayerHook.cowboyModules.TryGetValue(self.player, out var cowBoyModule) && cowBoyModule.playerHandWantTochPos != null && cowBoyModule.handTochRopeCont > 0)
            {
                if (self.player.FreeHand() != -1)
                {

                    self.hands[self.player.FreeHand()].absoluteHuntPos = cowBoyModule.playerHandWantTochPos;
                    if (cowBoyModule.playerGraphicsHandChanged == false)
                    {
                        cowBoyModule.playerGraphicsHandChanged = true;
                        self.hands[self.player.FreeHand()].pos -= self.player.mainBodyChunk.lastPos - self.player.mainBodyChunk.pos;
                    }



                    self.hands[self.player.FreeHand()].mode = Limb.Mode.HuntAbsolutePosition;
                    self.hands[self.player.FreeHand()].reachingForObject = true;


                    cowBoyModule.playerFreeHandPos = self.hands[self.player.FreeHand()].pos;
                    cowBoyModule.dragPointSeted = true;


                }
                else
                {
                    cowBoyModule.dragPointSeted = false;
                    cowBoyModule.startAndEndSeted = false;
                }
            }
            else
            {
                cowBoyModule.dragPointSeted = false;
                cowBoyModule.startAndEndSeted = false;
            }
        }
        #endregion


        //画石头
        private static void Rock_DrawSprites(On.Rock.orig_DrawSprites orig, Rock self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (PlayerHook.rockModule.TryGetValue(self, out var superRock))
            {
                sLeaser.sprites[0].color = superRock.rockColor;
            }
        }



    }
}
