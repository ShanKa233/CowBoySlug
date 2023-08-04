using CowBoySLug;
using RWCustom;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CowBoySlug
{
    public static class Cloak
    {

        public static void Hook()
        {
            On.PlayerGraphics.AddToContainer += AddToContainer;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            bool flag2 = Plugin.RopeMaster.TryGet(self.player, out var flag3) && flag3;
            if (!flag3) { return; }
            sLeaser.sprites[index].color = Color.blue;
            sLeaser.sprites[index].scaleX = sLeaser.sprites[3].scaleX;
            sLeaser.sprites[index].scaleY = sLeaser.sprites[3].scaleY;
            //同步外套与脖子一同旋转
            sLeaser.sprites[index].rotation = sLeaser.sprites[3].rotation;
            //同步外套坐标至身体处
            sLeaser.sprites[index].x = sLeaser.sprites[3].x;
            sLeaser.sprites[index].y = sLeaser.sprites[3].y;
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(self, sLeaser, rCam);
            bool flag2 = Plugin.RopeMaster.TryGet(self.player, out var flag3) && flag3;
            if (!flag3) { return; }
            //仙女衣服值数等于原本扩容前的身体精灵数组长度
            index = sLeaser.sprites.Length;
            //给原本的身体精灵扩容
            Array.Resize<FSprite>(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
            //给扩容的身体新建一个精灵,并使用材质
            sLeaser.sprites[index] = TriangleMesh.MakeGridMesh("MoonCloakTex", 8);
            //将开启后的精灵添加进容器
            //sLeaser.sprites = new FSprite[1];
            //sLeaser.sprites[0] = TriangleMesh.MakeGridMesh("MoonCloakTex", 8);
            //for (int i = 0; i < size; i++)
            //{
            //    for (int j = 0; j < size; j++)
            //    {
            //        (sLeaser.sprites[0] as TriangleMesh).color = Color.blue;
            //    }
            //}
            self.AddToContainer(sLeaser, rCam, null);
        }

        private static void AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            bool flag2 = Plugin.RopeMaster.TryGet(self.player, out var flag3) && flag3;
            if (!flag3) { return; }
            if (index > 0 && index < sLeaser.sprites.Length)
            {
                FContainer fContainer2 = rCam.ReturnFContainer("Items");
                fContainer2.AddChild(sLeaser.sprites[index]);
                //newContatiner.AddChild(sLeaser.sprites[index]);
            }
            //if (newContatiner == null)
            //{
            //    newContatiner = rCam.ReturnFContainer("Items");
            //}
            //for (int i = 0; i < sLeaser.sprites.Length; i++)
            //{
            //    sLeaser.sprites[i].RemoveFromContainer();
            //    newContatiner.AddChild(sLeaser.sprites[i]);
            //}
        }

        public static int size = 9;
        public static int index;
    }
}
