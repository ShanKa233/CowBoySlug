using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CowBoySLug;
using RWCustom;
using SlugBase.DataTypes;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;

namespace CowBoySlug
{
    public static class Scarf
    {
        public static readonly PlayerFeature<bool> HaveScarf = PlayerBool("cowboyslug/scarf"); //有围巾
        public static readonly PlayerColor ScarfColor = new PlayerColor("Scarf"); //围巾颜色

        public static ConditionalWeakTable<Player, ScarfModule> modules =
            new ConditionalWeakTable<Player, ScarfModule>(); //字典

        public static void Hook()
        {
            On.PlayerGraphics.ctor += Ribbon_ctor;
            On.PlayerGraphics.Update += Ribbon_Update;

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

            On.PlayerGraphics.Reset += Ribbon_Reset;
        }

        private static void Ribbon_ctor(
            On.PlayerGraphics.orig_ctor orig,
            PlayerGraphics self,
            PhysicalObject ow
        )
        {
            orig.Invoke(self, ow);
            //加到字典里
            if (HaveScarf.TryGet(self.player, out var value) && value)
                modules.Add(self.player, new ScarfModule(self.player));
            if (!modules.TryGetValue(self.player, out var module))
                return;

            module.ribbon = new GenericBodyPart[2];
            for (int i = 0; i < module.ribbon.Length; i++)
            {
                module.ribbon[i] = new GenericBodyPart(
                    self,
                    1,
                    0.8f,
                    0.3f,
                    self.player.mainBodyChunk
                );
            }
        }

        private static void Ribbon_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            if (!modules.TryGetValue(self.player, out var module))
                return;
            var player = self.player;
            var y = player.mainBodyChunk.Rotation;
            var x = -Custom.PerpendicularVector(y);
            //根据姿势改变丝带的终点位置
            for (int i = 0; i < 2; i++)
            {
                var ribbon = module.ribbon[i];
                ribbon.Update();
                Vector2 vel = (x + y * (i == 0 ? 0.3f : -0.5f)).normalized;

                var ribbonPoint = player.mainBodyChunk.pos + ChangeRotation(player) * 5 * x;

                ribbon.ConnectToPoint(
                    ribbonPoint,
                    18,
                    false,
                    0,
                    self.player.mainBodyChunk.vel,
                    0.1f,
                    0
                );
                Vector2 visualRibbon =
                    player.mainBodyChunk.pos
                    + x * (ChangeRotation(player) == 0 ? 35 : (ChangeRotation(player) * 30));
                ribbon.vel +=
                    vel
                    * Custom.LerpMap(
                        Vector2.Distance(ribbon.pos, visualRibbon),
                        10f,
                        150f,
                        0f,
                        14f,
                        0.7f
                    );
            }
        }

        private static void Ribbon_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            if (!modules.TryGetValue(self.player, out var module))
                return;
            module.ribbonReset(); //重置丝巾位置防止拉丝
        }

        private static void PlayerGraphics_InitiateSprites(
            On.PlayerGraphics.orig_InitiateSprites orig,
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam
        )
        {
            orig.Invoke(self, sLeaser, rCam);
            if (!modules.TryGetValue(self.player, out var module))
                return;
            if (!elementLoaded())
                return;
            ctorIndex(sLeaser, module);
            self.AddToContainer(sLeaser, rCam, null);
        }

        private static void PlayerGraphics_AddToContainer(
            On.PlayerGraphics.orig_AddToContainer orig,
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            FContainer newContatiner
        )
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            if (!modules.TryGetValue(self.player, out var module))
                return;
            if (!elementLoaded())
                return;
            //防止重复执行的flag
            bool flag = module.scarfIndex > 0 && sLeaser.sprites.Length > module.scarfIndex;
            if (!flag)
                return;

            var scarfIndex = module.scarfIndex;
            //将材质精灵添加到背景图层
            FContainer fContainer2 = rCam.ReturnFContainer("Midground");
            for (int i = 0; i < 3; i++)
            {
                fContainer2.AddChild(sLeaser.sprites[scarfIndex + i]);
            }
            //让材质覆盖其他身体部件
            for (int i = 0; i < scarfIndex; i++)
            {
                for (int j = 0; j < 3; j++)
                    sLeaser.sprites[scarfIndex + j].MoveInFrontOfOtherNode(sLeaser.sprites[i]);
            }
        }

        private static void PlayerGraphics_DrawSprites(
            On.PlayerGraphics.orig_DrawSprites orig,
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            UnityEngine.Vector2 camPos
        )
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (!modules.TryGetValue(self.player, out var module))
                return;
            if (!elementLoaded())
                return;

            var scarfIndex = module.scarfIndex;
            //颜色设定
            Color scarfColor = ScarfColor.GetColor(self).Value;
            sLeaser.sprites[scarfIndex].color = scarfColor;

            //材质赋值
            string headName = sLeaser.sprites[3].element.name;
            headName = headName.Replace("HeadC", "HeadA"); //防止小孩的头带不了围巾
            sLeaser.sprites[scarfIndex].element = new FSprite("CowBoy-" + headName, true).element;

            var head = sLeaser.sprites[3];
            var scarf = sLeaser.sprites[scarfIndex];

            scarf.scaleX = head.scaleX;
            scarf.scaleY = head.scaleY;
            scarf.rotation = head.rotation;
            scarf.SetPosition(head.GetPosition());

            //绘制丝巾
            Vector2 dir = self.player.mainBodyChunk.Rotation;
            Vector2 per = Custom.PerpendicularVector(dir);

            for (int i = 0; i < 2; i++)
            {
                var scar = sLeaser.sprites[scarfIndex + 1 + i] as TriangleMesh;
                scar.color = i == 0 ? Color.Lerp(scarfColor, Color.black, 0.2f) : scarfColor;

                Vector2 vector =
                    Vector2.Lerp(
                        self.player.mainBodyChunk.lastPos,
                        self.player.mainBodyChunk.pos,
                        timeStacker
                    ) - dir;
                vector =
                    sLeaser.sprites[3].GetPosition()
                    - dir * 6
                    - per * 2 * ChangeRotation(self.player)
                    + camPos;
                Vector2 ribbonpos = Vector2.Lerp(
                    module.ribbon[i].lastPos,
                    module.ribbon[i].pos,
                    timeStacker
                );

                scar.MoveVertice(0, vector - (per * 4) - camPos);

                scar.MoveVertice(1, Vector2.Lerp(vector, ribbonpos + dir * 3f, 0.65f) - camPos);
                scar.MoveVertice(2, Vector2.Lerp(vector, ribbonpos + dir * -3f, 0.7f) - camPos);

                scar.MoveVertice(
                    3,
                    Vector2.Lerp(module.ribbon[i].lastPos, module.ribbon[i].pos, timeStacker)
                        - camPos
                );

                if (ChangeRotation(self.player) >= 0)
                {
                    scar.MoveInFrontOfOtherNode(sLeaser.sprites[3]);
                }
                else
                {
                    scar.MoveBehindOtherNode(sLeaser.sprites[3]);
                    scar.MoveToBack();
                }
            }
        }

        #region 一些方法

        public static bool elementLoaded()
        {
            return true;
            //return PlayerGraphicsHook._elementsByName.TryGetValue("CowBoy-HeadA0", out var element);
        }

        /// <summary>
        /// 初始化围巾材质的下标
        /// </summary>
        /// <param name="sLeaser"></param>
        /// <param name="module"></param>
        public static void ctorIndex(RoomCamera.SpriteLeaser sLeaser, ScarfModule module)
        {
            //值数等于原本扩容前的身体精灵数组长度
            module.scarfIndex = sLeaser.sprites.Length;
            module.ribbonIndex = module.scarfIndex + 1;
            int index = module.scarfIndex;

            //给原本的身体精灵扩容
            Array.Resize<FSprite>(ref sLeaser.sprites, sLeaser.sprites.Length + 3);

            //给扩容的身体新建一个精灵,并使用材质
            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
                new TriangleMesh.Triangle(0, 1, 2),
                new TriangleMesh.Triangle(1, 2, 3),
            };
            //添加贴图材质
            sLeaser.sprites[index] = new FSprite("CowBoy-" + sLeaser.sprites[3].element.name, true);
            //添加2个飘带的材质
            for (int i = 0; i < 2; i++)
                sLeaser.sprites[index + i + 1] = new TriangleMesh(
                    "Futile_White",
                    tris,
                    false,
                    false
                );
        }

        public static int HeadRotation(
            PlayerGraphics self,
            Vector2 vector,
            Vector2 per,
            Vector2 dir,
            out Vector2 vector2
        )
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

        public static float ChangeRotation(Player player)
        {
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.bodyChunks[0].pos.x < player.bodyChunks[1].pos.x)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            if (player.bodyMode == Player.BodyModeIndex.Stand)
            {
                if (player.input[0].x <= 0)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            return 1;
        }
        #endregion
    }

    public class ScarfModule
    {
        public Player player;
        public int scarfIndex;
        public int ribbonIndex;

        public GenericBodyPart[] ribbon;

        public ScarfModule(Player player)
        {
            this.player = player;
        }

        public void ribbonReset()
        {
            for (int i = 0; i < ribbon.Length; i++)
                ribbon[i].Reset(player.mainBodyChunk.pos);
        }

        public int ribbonUp => ribbonIndex;
        public int ribbonDown => ribbonIndex + 1;
    }
}
