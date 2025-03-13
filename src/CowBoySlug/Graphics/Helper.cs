using System;
using UnityEngine;

namespace CowBoySlug.Graphics
{
    public class Helper
    {
        /// <summary>
        /// 检查围巾元素是否已加载
        /// </summary>
        /// <returns>围巾元素是否已加载</returns>
        public static bool elementLoaded()
        {
            // 使用Futile.atlasManager检查元素是否存在
            return Futile.atlasManager.DoesContainElementWithName("CowBoy-HeadA0");
        }

        /// <summary>
        /// 初始化围巾材质的下标并创建相应的精灵
        /// </summary>
        /// <param name="sLeaser">精灵租赁器</param>
        /// <param name="module">围巾模块</param>
        public static void ctorIndex(RoomCamera.SpriteLeaser sLeaser, ScarfModule module)
        {
            //值数等于原本扩容前的身体精灵数组长度
            module.scarfIndex = sLeaser.sprites.Length;
            module.ribbonIndex = module.scarfIndex + 1;
            int index = module.scarfIndex;

            //给原本的身体精灵扩容，增加3个新精灵（1个围巾+2个飘带）
            Array.Resize<FSprite>(ref sLeaser.sprites, sLeaser.sprites.Length + 3);

            //创建飘带的三角形网格
            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
                new TriangleMesh.Triangle(0, 1, 2),  // 第一个三角形
                new TriangleMesh.Triangle(1, 2, 3),  // 第二个三角形
            };

            //添加围巾主体贴图材质
            sLeaser.sprites[index] = new FSprite("CowBoy-" + sLeaser.sprites[3].element.name, true);

            //添加2个飘带的材质（使用三角形网格）
            for (int i = 0; i < 2; i++)
                sLeaser.sprites[index + i + 1] = new TriangleMesh(
                    "Futile_White",  // 使用白色纹理，后续会上色
                    tris,            // 三角形定义
                    false,           // 不使用硬边
                    false            // 不使用纹理坐标
                );
        }

        /// <summary>
        /// 根据玩家姿势计算头部旋转
        /// </summary>
        /// <param name="self">玩家图形</param>
        /// <param name="vector">当前位置</param>
        /// <param name="per">垂直向量</param>
        /// <param name="dir">方向向量</param>
        /// <param name="vector2">输出的调整后位置</param>
        /// <returns>头部旋转系数</returns>
        public static int HeadRotation(
            PlayerGraphics self,
            Vector2 vector,
            Vector2 per,
            Vector2 dir,
            out Vector2 vector2
        )
        {
            var player = self.player;
            // 根据爬行状态调整头部位置和旋转
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.mainBodyChunk.pos.x > player.bodyChunks[1].pos.x)
                {
                    vector2 = vector - per * 2 + dir;
                    return -1;  // 向左爬行
                }
                else
                {
                    vector2 = vector - per + dir;
                    return 1;   // 向右爬行
                }
            }
            else
            {
                vector2 = vector;
                return 1;       // 站立状态
            }
        }

        /// <summary>
        /// 根据玩家姿势和朝向计算围巾旋转系数
        /// </summary>
        /// <param name="player">玩家对象</param>
        /// <returns>围巾旋转系数</returns>
        public static float ChangeRotation(Player player)
        {
            // 爬行状态下的围巾旋转
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.bodyChunks[0].pos.x < player.bodyChunks[1].pos.x)
                {
                    return 0;   // 向右爬行
                }
                else
                {
                    return -1;  // 向左爬行
                }
            }
            // 站立状态下的围巾旋转
            if (player.bodyMode == Player.BodyModeIndex.Stand)
            {
                if (player.input[0].x <= 0)
                {
                    return 0;   // 向左站立或不动
                }
                else
                {
                    return -1;  // 向右站立
                }
            }
            return 1;           // 其他状态
        }
    }
}