
using CowBoySlug.CowBoy;
using CowBoySlug.CowBoy.Ability.RopeUse;
using CowBoySLug;
using RWCustom;
using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace CowBoySlug
{
    public class PlayerGraphicsHook
    {
        public static void Hook()
        {
            Scarf.Hook();

            On.RainWorld.OnModsInit += CowBoySlug_LoadTexture;
            On.PlayerGraphics.InitiateSprites += CowBoy_InitiateSprites;
            On.PlayerGraphics.DrawSprites += CowBoy_DrawSprites;
            

            
        }

        #region 和猫猫样子有关的部分
        
        
        

        //加载材质进入游戏

        private static void CowBoySlug_LoadTexture(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            //在读取mod的时候加载材质
            atlas = Futile.atlasManager.LoadAtlas("atlases/CowBoyHead");
            Futile.atlasManager.LoadAtlas("fisobs/icon_CowBoyHat");
        }
        //扩容
        private static void CowBoy_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(self, sLeaser, rCam);
            if (!RopeMaster.modules.TryGetValue(self.player, out var cowBoy)){return;}
            cowBoy.ropeColor = RopeMaster.RopeColor.GetColor(self).Value;
        }

        //绘制蛞蝓猫
        private static void CowBoy_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (!PlayerHook.cowboyModules.TryGetValue(self.player, out var cowBoy)) { return; }
            DrawUseRopeAnimetion(self, sLeaser, rCam, timeStacker, camPos);
        }


        //精灵图位置
        public static FAtlas atlas;
        public static FAtlas hatAtlas;
        #endregion


        #region 用来弄显示的一堆方法
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
        }
        #endregion





    }
}
