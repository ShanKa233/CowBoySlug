using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;
using Random = UnityEngine.Random;

namespace CowBoySlug
{
    
    public static class Camouflage
    {
        //用来获取特征做判断
        public static readonly PlayerFeature<bool> PlayerCamoflage = PlayerBool("camouflage");
        //用这个字典来保存读取一个放数据的类
        public static ConditionalWeakTable<Player, CmouflageModule> modules = new ConditionalWeakTable<Player, CmouflageModule>();

        //需要执行得内容
        public static void Hook()
        {
            On.Player.ctor += Player_ctor;//初始化迷彩能力

            On.PlayerGraphics.Update += PlayerGraphics_Update;//玩家显示内容的更新
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;//玩家显示的更新


        }

        
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            //执行正常流程
            orig.Invoke(self, abstractCreature, world);
            //在玩家初始化后如果有这个特征就作为键加入到module字典里值是迷彩模型
            if (PlayerCamoflage.TryGet(self, out var flag) && flag)
            {
                modules.Add(self, new CmouflageModule());
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            //因为初始化我们已经把所有有特征的玩家都加入字典了,所以我们现在只要查字典里面有没有这个玩家就可以
            if (modules.TryGetValue(self.player,out var cmouflageModule))
            {
                //如果玩家没死
                if (!self.player.dead)
                {
                    //测他有没有动,我这里是测上上个位置和这次更新的位置的距离是否小于0.3如果小于就说明没动
                     if (Vector2.Distance(self.player.mainBodyChunk.pos, self.player.mainBodyChunk.lastPos) < 0.3)
                    {
                        //如果没动就把迷彩现在的颜色渐渐往 迷彩时选择的颜色 靠拢
                        cmouflageModule.whiteCamoColor = Color.Lerp(cmouflageModule.whiteCamoColor, cmouflageModule.whitePickUpColor, 0.1f);
                    }
                    else
                    {//不然就把颜色往玩家靠拢
                        cmouflageModule.whiteCamoColor = Color.Lerp(cmouflageModule.whiteCamoColor, self.player.ShortCutColor(), 0.1f);
                    }
                }
            }
        }
        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (modules.TryGetValue(self.player, out var cmouflageModule))
            {
                //如果玩家动的距离很少
                if (Vector2.Distance(self.player.mainBodyChunk.pos, self.player.mainBodyChunk.lastPos) < 0.3)
                {
                    //就把玩家迷彩选择的目标色 设为相机在玩家位置获取到的像素颜色
                    cmouflageModule.whitePickUpColor = rCam.PixelColorAtCoordinate(self.player.mainBodyChunk.pos);
                }

                //然后给玩家的身体部件都染上迷彩现在颜色
                for (int i = 0; i < 12; i++)
                {
                    sLeaser.sprites[i].color = cmouflageModule.whiteCamoColor;
                }
            }
        }

       
    }


    public class CmouflageModule
    {
        //迷彩的实时颜色
        public Color whiteCamoColor = new Color(0f, 0f, 0f);

        //迷彩的目标颜色
        public Color whitePickUpColor;
        public CmouflageModule() { }
    }
}
