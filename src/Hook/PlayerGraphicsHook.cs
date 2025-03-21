using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CowBoySlug.Graphics;
using CowBoySLug;
using RWCustom;
using SlugBase.DataTypes;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace CowBoySlug
{
    public class PlayerGraphicsHook
    {
        public static void Hook()
        {
            Scarf.Hook();
            On.PlayerGraphics.AddToContainer += CowBoy_AddToContainer;
            On.PlayerGraphics.InitiateSprites += CowBoy_InitiateSprites;
            On.PlayerGraphics.DrawSprites += CowBoy_DrawSprites;
        }

        private static void CowBoy_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            if (self.player.GetCowBoyData().scarf != null)
            {
                self.player.GetCowBoyData().scarf.AddToContainer(sLeaser, rCam, newContatiner);
            }
        }

        #region 和猫猫样子有关的部分

        //扩容
        private static void CowBoy_InitiateSprites(
            On.PlayerGraphics.orig_InitiateSprites orig,
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam
        )
        {
            orig.Invoke(self, sLeaser, rCam);
            if (self.player.GetCowBoyData().scarf != null)
            {
                self.player.GetCowBoyData().scarf.InitiateSprites(sLeaser, rCam);
            }
            if (Mechanics.RopeSkill.UserData.modules.TryGetValue(self.player, out var cowBoy))
            {
                cowBoy.ropeColor = Plugin.RopeColor.GetColor(self)?? PlayerGraphics.JollyColor(self.player.playerState.playerNumber,3);
            }


            // 将围巾添加到容器中
            self.AddToContainer(sLeaser, rCam, null);
        }

        //绘制蛞蝓猫
        private static void CowBoy_DrawSprites(
            On.PlayerGraphics.orig_DrawSprites orig,
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            Vector2 camPos
        )
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (self.player.GetCowBoyData().scarf != null)
            {
                self.player.GetCowBoyData().scarf.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }
            if (self.player.IsCowBoys(out var cowBoy))
            {
                DrawUseRopeAnimetion(self, sLeaser, rCam, timeStacker, camPos);
            }
        }

        //精灵图位置
        //public static FAtlas atlas;
        //public static FAtlas hatAtlas;
        #endregion


        #region 用来弄显示的一堆方法
        public static void DrawUseRopeAnimetion(
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            Vector2 camPos
        )
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
                else if (
                    (self.player.grasps[0] != null && self.player.grasps[0].grabbed is TubeWorm)
                    || (self.player.grasps[1] != null && self.player.grasps[1].grabbed is TubeWorm)
                )
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
