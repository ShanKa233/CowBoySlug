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
using CowBoySlug.Graphics;
namespace CowBoySlug
{
    /// <summary>
    /// 围巾功能的主类，处理围巾的渲染和物理效果
    /// </summary>
    public static class Scarf
    {
        // 定义围巾相关的特性
        public static readonly PlayerFeature<bool> HaveScarf = PlayerBool("cowboyslug/scarf"); //有围巾
        public static readonly PlayerColor ScarfColor = new PlayerColor("Scarf"); //围巾颜色


        /// <summary>
        /// 注册所有需要的钩子函数
        /// </summary>
        public static void Hook()
        {
            ScarfGraphics.Hook();
        }
    }

    /// <summary>
    /// 围巾模块类，存储与特定玩家相关的围巾数据
    /// </summary>
    public class ScarfModule
    {
        public Player player;           // 关联的玩家
        public PlayerGraphics playerGraphics => player.graphicsModule as PlayerGraphics; // 关联的玩家图形

        public int scarfIndex;          // 围巾主体精灵的索引
        public int ribbonIndex;         // 围巾飘带精灵的起始索引

        public GenericBodyPart[] ribbon; // 围巾飘带的物理部件

        /// <summary>
        /// 创建新的围巾模块
        /// </summary>
        /// <param name="player">关联的玩家</param>
        public ScarfModule(Player player,PlayerGraphics playerGraphics)
        {
            this.player = player;
            // 移除对playerGraphics的赋值,因为它是只读属性
            
            ribbon = new GenericBodyPart[2];
            for (int i = 0; i < ribbon.Length; i++)
            {
                ribbon[i] = new GenericBodyPart(
                    playerGraphics,
                    1,      // 重量
                    0.8f,   // 弹性
                    0.3f,   // 阻力
                    player.mainBodyChunk  // 连接到玩家的主体
                );
            }
        }

        public void Update()
        {

            var y = player.mainBodyChunk.Rotation;  // 获取玩家主体的旋转方向
            var x = -Custom.PerpendicularVector(y); // 获取垂直于旋转方向的向量

            //根据姿势改变丝带的终点位置
            for (int i = 0; i < 2; i++)
            {
                var ribbon = this.ribbon[i];
                ribbon.Update();
                // 根据上下飘带计算不同的方向
                Vector2 vel = (x + y * (i == 0 ? 0.3f : -0.5f)).normalized;

                // 计算飘带的连接点
                var ribbonPoint = player.mainBodyChunk.pos + Helper.ChangeRotation(player) * 5 * x;

                // 将飘带连接到计算出的点
                ribbon.ConnectToPoint(
                    ribbonPoint,
                    18,     // 连接长度
                    false,  // 不强制连接
                    0,      // 无额外旋转
                    player.mainBodyChunk.vel,  // 使用玩家的速度
                    0.1f,   // 速度影响
                    0       // 无额外重力
                );

                // 计算飘带的视觉目标位置
                Vector2 visualRibbon =
                    player.mainBodyChunk.pos
                    + x * (Helper.ChangeRotation(player) == 0 ? 35 : (Helper.ChangeRotation(player) * 30));

                // 根据距离目标位置的远近添加力，使飘带自然飘动
                ribbon.vel +=
                    vel
                    * Custom.LerpMap(
                        Vector2.Distance(ribbon.pos, visualRibbon),
                        10f,   // 最小距离
                        150f,  // 最大距离
                        0f,    // 最小力
                        14f,   // 最大力
                        0.7f   // 插值曲线
                    );
            }
        }
        /// <summary>
        /// 初始化围巾精灵
        /// </summary>
        /// <param name="sLeaser">精灵租赁器</param>
        /// <param name="rCam">渲染相机</param>
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {

            // 检查围巾元素是否已加载
            if (!Helper.elementLoaded())
                return;

            // 初始化围巾精灵的索引
            Helper.ctorIndex(sLeaser, this);
            // 初始化围巾精灵的代码将在这里实现
        }

        /// <summary>
        /// 将围巾精灵添加到适当的容器中
        /// </summary>
        /// <param name="sLeaser">精灵租赁器</param>
        /// <param name="rCam">渲染相机</param>
        /// <param name="newContainer">新容器</param>
        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            // 将围巾精灵添加到容器的代码将在这里实现

            //防止重复执行的flag
            bool flag = this.scarfIndex > 0 && sLeaser.sprites.Length > this.scarfIndex;
            if (!flag)
                return;

            var scarfIndex = this.scarfIndex;
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

        /// <summary>
        /// 绘制围巾精灵
        /// </summary>
        /// <param name="sLeaser">精灵租赁器</param>
        /// <param name="rCam">渲染相机</param>
        /// <param name="timeStacker">时间堆栈器</param>
        /// <param name="camPos">相机位置</param>
        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            // 绘制围巾精灵的代码将在这里实现

            var scarfIndex = this.scarfIndex;
            //颜色设定 - 从玩家特性中获取围巾颜色
            Color scarfColor = Scarf.ScarfColor.GetColor(playerGraphics) ?? PlayerGraphics.JollyColor(player.playerState.playerNumber, 2);

            sLeaser.sprites[scarfIndex].color = scarfColor;

            //材质赋值 - 使用与头部相匹配的围巾材质
            string headName = sLeaser.sprites[3].element.name;
            headName = headName.Replace("HeadC", "HeadA"); //防止小孩的头带不了围巾
            sLeaser.sprites[scarfIndex].element = new FSprite("CowBoy-" + headName, true).element;

            // 获取头部和围巾精灵
            var head = sLeaser.sprites[3];
            var scarf = sLeaser.sprites[scarfIndex];

            // 使围巾的缩放、旋转和位置与头部一致
            scarf.scaleX = head.scaleX;
            scarf.scaleY = head.scaleY;
            scarf.rotation = head.rotation;
            scarf.SetPosition(head.GetPosition());

            //绘制丝巾飘带部分
            Vector2 dir = player.mainBodyChunk.Rotation;
            Vector2 per = Custom.PerpendicularVector(dir);

            // 处理两条飘带（上下）
            for (int i = 0; i < 2; i++)
            {
                // 获取飘带网格并设置颜色（下方飘带略深）
                var scar = sLeaser.sprites[scarfIndex + 1 + i] as TriangleMesh;
                scar.color = i == 0 ? Color.Lerp(scarfColor, Color.black, 0.2f) : scarfColor;

                // 计算飘带起始位置（头部附近）
                Vector2 vector =
                    Vector2.Lerp(
                        player.mainBodyChunk.lastPos,
                        player.mainBodyChunk.pos,
                        timeStacker
                    ) - dir;
                vector =
                    sLeaser.sprites[3].GetPosition()
                    - dir * 6
                    - per * 2 * Helper.ChangeRotation(player)
                    + camPos;

                // 计算飘带当前位置（使用物理模拟的结果）
                Vector2 ribbonpos = Vector2.Lerp(
                    this.ribbon[i].lastPos,
                    this.ribbon[i].pos,
                    timeStacker
                );

                // 设置飘带三角形网格的四个顶点位置，形成飘带形状
                scar.MoveVertice(0, vector - (per * 4) - camPos);
                scar.MoveVertice(1, Vector2.Lerp(vector, ribbonpos + dir * 3f, 0.65f) - camPos);
                scar.MoveVertice(2, Vector2.Lerp(vector, ribbonpos + dir * -3f, 0.7f) - camPos);
                scar.MoveVertice(
                    3,
                    Vector2.Lerp(ribbon[i].lastPos, ribbon[i].pos, timeStacker)
                        - camPos
                );

                // 根据玩家朝向调整飘带的渲染顺序（在头部前面或后面）
                if (Helper.ChangeRotation(player) >= 0)
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
        /// <summary>
        /// 重置围巾飘带的位置，防止拉丝现象
        /// </summary>
        public void ribbonReset()
        {
            for (int i = 0; i < ribbon.Length; i++)
                ribbon[i].Reset(player.mainBodyChunk.pos);
        }

        // 上下飘带的索引属性
        public int ribbonUp => ribbonIndex;
        public int ribbonDown => ribbonIndex + 1;
    }
}
