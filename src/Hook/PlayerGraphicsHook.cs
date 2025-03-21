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
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            
            
            if (self.player.IsCowBoys(out var cowBoy))
            {
                DrawUseRopeAnimetion(self, sLeaser, rCam, timeStacker, camPos);
            }
        }
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
