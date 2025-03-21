using System;
using CowBoySLug;
using UnityEngine;

namespace CowBoySlug.Graphics
{
    public static class ScarfGraphics
    {
        public static void Hook()
        {
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;

            On.PlayerGraphics.Update += Ribbon_Update;
            // 重置围巾状态的钩子
            On.PlayerGraphics.Reset += Ribbon_Reset;

            On.PlayerGraphics.InitiateSprites += CowBoy_InitiateSprites;
            On.PlayerGraphics.AddToContainer += CowBoy_AddToContainer;
            On.PlayerGraphics.DrawSprites += CowBoy_DrawSprites;
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig.Invoke(self, ow);
            if (self.player.IsCowBoys())
            {
                self.player.GetCowBoyData().scarf = new ScarfModule(self.player,self);
            }
        }

        /// <summary>
        /// 更新围巾的物理效果和位置
        /// </summary>
        private static void Ribbon_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            // 调用原始更新方法
            orig.Invoke(self);
            if (self.player.IsCowBoys())
            {
                self.player.GetCowBoyData().scarf.Update();
            }
        }

        /// <summary>
        /// 重置围巾状态，防止拉丝现象
        /// </summary>
        private static void Ribbon_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            if (self.player.IsCowBoys())
            {
                self.player.GetCowBoyData().scarf.ribbonReset(); //重置丝巾位置防止拉丝
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
            if (self.player.IsCowBoys())
            {
                self.player.GetCowBoyData().scarf.InitiateSprites(sLeaser, rCam);
            }
            if (Mechanics.RopeSkill.UserData.modules.TryGetValue(self.player, out var cowBoy))
            {
                cowBoy.ropeColor = Plugin.RopeColor.GetColor(self) ?? PlayerGraphics.JollyColor(self.player.playerState.playerNumber, 3);
            }


            // 将围巾添加到容器中
            self.AddToContainer(sLeaser, rCam, null);
        }
        private static void CowBoy_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            if (self.player.IsCowBoys())
            {
                self.player.GetCowBoyData().scarf.AddToContainer(sLeaser, rCam, newContatiner);
            }
        }
        private static void CowBoy_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
              orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (self.player.IsCowBoys())
            {
                self.player.GetCowBoyData().scarf.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }
        }



        //精灵图位置
        //public static FAtlas atlas;
        //public static FAtlas hatAtlas;
        #endregion


    }
}