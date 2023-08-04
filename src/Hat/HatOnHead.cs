using CowBoySLug;
using IL.Menu;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CowBoySlug
{
    public   class HatOnHead
    {
        public  void Hook()
        {
            //加载材质
            On.RainWorld.OnModsInit += Karma_OnModsInit;

            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }
        public  FAtlas atlas;
        public string atlasPath;
        public string atlasTopName;
        public  int index;
        public PlayerFeature<bool> playerFeature;
        public Color color;



        public   HatOnHead(string atlasPath,string atlasTopName,Color color,PlayerFeature<bool> playerFeature)
        {
            this.atlasPath = atlasPath;
            this.atlasTopName = atlasTopName;
            this.color = color;
            this.playerFeature = playerFeature;

        }


        public  void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(self, sLeaser, rCam);
            bool flag = atlas._elementsByName.TryGetValue(atlasTopName+"HeadA0", out var element);
            bool flag2 = playerFeature.TryGet(self.player, out var flag3) && flag3;
            if (flag2&&flag) 
            {
                index = sLeaser.sprites.Length;
                Array.Resize<FSprite>(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
                //给扩容的身体新建一个精灵,并使用材质
                sLeaser.sprites[index] = new FSprite(atlasTopName+ sLeaser.sprites[3].element.name, true);
                self.AddToContainer(sLeaser,rCam,null);
            }
        }

      

        public  void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);

            bool flag = playerFeature.TryGet(self.player, out var flag0) && flag0;
            bool flag2 = index>0&&index<sLeaser.sprites.Length;

            if (flag2&&flag)
            {
                //如果玩家有这个特征,和模型而且数组空位已经增加就执行材质添加
                //将材质精灵添加到背景图层
                FContainer fContainer2 = rCam.ReturnFContainer("Midground");
                fContainer2.AddChild(sLeaser.sprites[index]);
                //让外套材质覆盖其他身体部件
                for (int i = 0; i < 4; i++)
                {
                    sLeaser.sprites[index].MoveInFrontOfOtherNode(sLeaser.sprites[i]);
                }
            }


        }

        public  void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            bool flag = playerFeature.TryGet(self.player, out bool flag1) && flag1;//玩家是否可以获取仙女特征,并开启仙女特征
            bool flag2 = atlas._elementsByName.TryGetValue(atlasTopName+"HeadA0", out var element);//玩家是否加载仙女材质

            //如果玩家有此特征就让外套同步到身体上,跟随脖子旋转
            if (flag && flag2)
            {
                Color color = this.color;
                //外套颜色设定
                sLeaser.sprites[index].color = color;
                //给外套材质样式赋值
                sLeaser.sprites[index].element = new FSprite(atlasTopName + sLeaser.sprites[3].element.name, true).element;


                sLeaser.sprites[index].scaleX = sLeaser.sprites[3].scaleX;
                sLeaser.sprites[index].scaleY = sLeaser.sprites[3].scaleY;
                //同步外套与脖子一同旋转
                sLeaser.sprites[index].rotation = sLeaser.sprites[3].rotation;
                //同步外套坐标至身体处
                sLeaser.sprites[index].x = sLeaser.sprites[3].x;
                sLeaser.sprites[index].y = sLeaser.sprites[3].y;
            }
        }
        
        public  void Karma_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            atlas=Futile.atlasManager.LoadAtlas(atlasPath);
        }








    }
}
