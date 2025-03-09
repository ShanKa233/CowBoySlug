using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

namespace CowBoySlug.CowBoy.Ability.RopeUse
{
    public class CowRope : CosmeticSprite
    {
        #region 用来使用绳子的方法
        public static void SpawnRope(Player player, Spear spear, Color start, Color end)
        {
            try
            {
                if (!RopeMaster.modules.TryGetValue(player, out var module))
                    return;

                var rope = new CowRope(player, spear, start, end); //新建一个在矛上的丝线

                player.room.AddObject(rope); //召唤这个线
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 生成绳索时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion
        public CowRope(Player player, Spear spear, Color colorStart, Color colorEnd)
        {
            try
            {
                this.room = player.room; //所在房间

                this.spear = spear; //连接的矛
                this.player = player; //所属玩家

                ropeLength = Random.Range(40, 60); //随机一个绳子总长
                points = new Vector2[ropeLength, 4]; //记录段落信息

                rope = new Rope(room, playerPos, spearEndPos, 3);
                //debugRope = new Rope.RopeDebugVisualizer(rope);
                for (int i = 0; i < points.GetLength(0); i++)
                {
                    points[i, 0] = playerPos + Custom.RNV(); //pos
                    points[i, 1] = playerPos; //lastPos
                    points[i, 2] =
                        Custom.DirVec(playerPos, spearEndPos) * 0.3f * Random.value
                        + Custom.RNV() * Random.value * 1.5f; //vel
                    points[i, 3] = new Vector2(3f, Mathf.Lerp(50f, 80f, Mathf.Pow(Random.value, 0.3f))); //粗细啥的
                }

                this.colorStart = colorStart; //绳子的开始颜色
                this.colorEnd = colorEnd; //绳子的末端颜色

                spear.rope().GetRope(player, this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 创建绳索时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public Spear spear; //所属矛
        public Player player; //所属玩家

        public Color colorStart; //线开始的颜色
        public Color colorEnd; //线结束的颜色

        public Vector2[,] points; //绳子的段落的坐标,速度,和粗细
        public int ropeLength; //线段数量

        public Rope rope; //方便计算长度转角之类数据的线
        public List<int> usedPoints; //用来记录哪些点位已经使用
        public float MaxLength = 900; //最大长度

        //public Rope.RopeDebugVisualizer debugRope;


        public float loose = 0; //松紧度0是完全没有弹力1是百分之五十左右的弹力
        public bool used = false; //是否处在收紧中

        public bool limited = false; //线开始消散

        private Color blackColor;
        private Color fogColor;
        private Color threadCol;

        public Vector2 playerPos
        {
            get
            {
                try
                {
                    if (player != null && player.bodyChunks != null && player.bodyChunks.Length > 1)
                    {
                        return player.bodyChunks[1].pos;
                    }
                    return Vector2.zero;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CowBoySlug] 获取玩家位置时出错: {ex.Message}");
                    return Vector2.zero;
                }
            }
        }
        public Vector2 spearEndPos
        {
            get
            {
                try
                {
                    if (spear != null)
                    {
                        var rotation = spear.rotation;
                        rotation.Scale(
                            new Vector2(1, 1) * (spear.mode == Spear.Mode.StuckInCreature ? 50 : 25)
                        );
                        return spear.firstChunk.pos - rotation;
                    }
                    return Vector2.zero;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CowBoySlug] 获取矛位置时出错: {ex.Message}");
                    return Vector2.zero;
                }
            }
        }

        #region 一些小方法
        public Vector2 RopePos(int i)
        {
            try
            {
                if (rope == null) return Vector2.zero;
                
                if (i == this.rope.TotalPositions - 1 || i < 0)
                {
                    return spear?.firstChunk?.pos ?? Vector2.zero;
                }
                return this.rope.GetPosition(i);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 获取绳索位置时出错: {ex.Message}");
                return Vector2.zero;
            }
        }

        public Vector2 RopeShowPos(int i)
        {
            try
            {
                if (rope == null) return Vector2.zero;
                
                if (i == this.rope.TotalPositions - 1 || i < 0)
                {
                    return spearEndPos;
                }
                return this.rope.GetPosition(i);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 获取绳索显示位置时出错: {ex.Message}");
                return Vector2.zero;
            }
        }
        #endregion

        public override void Update(bool eu)
        {
            try
            {
                base.Update(eu);
                
                // 检查绳索是否应该被销毁
                if (player == null || spear == null || player.room != room || spear.room != room)
                {
                    Destroy();
                    return;
                }

                // 更新绳索
                UpdateRope();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 更新绳索时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void UpdateRope()
        {
            // 更新绳索逻辑
            rope.Update(playerPos, spearEndPos);

            // 如果绳索被限制或使用中，更新相应状态
            if (limited)
            {
                for (int i = 0; i < points.GetLength(0); i++)
                {
                    points[i, 3].x -= 0.1f;
                }
                if (points[0, 3].x <= 0)
                {
                    Destroy();
                }
            }
            else if (used)
            {
                // 处理绳索收紧逻辑
                HandleRopeTightening();
            }
        }

        private void HandleRopeTightening()
        {
            // 绳索收紧逻辑
            float distance = Vector2.Distance(playerPos, spearEndPos);
            if (distance > MaxLength)
            {
                limited = true;
            }
            
            // 如果距离小于一定值，停止收紧
            if (distance < 50)
            {
                used = false;
            }
        }

        public override void Destroy()
        {
            try
            {
                if (spear != null)
                {
                    spear.rope().RemoveRope();
                }
                base.Destroy();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 销毁绳索时出错: {ex.Message}\n{ex.StackTrace}");
                base.Destroy();
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            try
            {
                sLeaser.sprites = new FSprite[1];
                sLeaser.sprites[0] = new FSprite("Futile_White");
                sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["CatTentacle"];
                this.AddToContainer(sLeaser, rCam, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 初始化绳索精灵时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            try
            {
                if (newContatiner == null)
                {
                    newContatiner = rCam.ReturnFContainer("Items");
                }
                newContatiner.AddChild(sLeaser.sprites[0]);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 添加绳索到容器时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            try
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                
                if (rope == null || player == null || spear == null)
                {
                    return;
                }

                // 绘制绳索
                DrawRope(sLeaser, rCam, timeStacker, camPos);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 绘制绳索时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void DrawRope(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            // 绘制绳索逻辑
            if (sLeaser.sprites[0] is TriangleMesh triangleMesh)
            {
                // 如果已经是TriangleMesh，更新它
                UpdateTriangleMesh(triangleMesh, timeStacker, camPos);
            }
            else
            {
                // 如果不是TriangleMesh，创建一个新的
                sLeaser.sprites[0] = CreateTriangleMesh(rCam, timeStacker, camPos);
                this.AddToContainer(sLeaser, rCam, null);
            }
        }

        private void UpdateTriangleMesh(TriangleMesh mesh, float timeStacker, Vector2 camPos)
        {
            // 更新现有的TriangleMesh
            Vector2 startPos = Vector2.Lerp(points[0, 1], points[0, 0], timeStacker);
            float width = 0f;

            for (int i = 0; i < points.GetLength(0); i++)
            {
                float t = Mathf.InverseLerp(0f, points.GetLength(0) - 1, i);
                float thickness = 3f * Mathf.InverseLerp(0f, 0.3f, points[i, 3].x);
                
                Vector2 pos = Vector2.Lerp(points[i, 1], points[i, 0], timeStacker);
                if (i == 0)
                {
                    pos = startPos;
                }
                else if (i == points.GetLength(0) - 1)
                {
                    pos = Vector2.Lerp(spear.firstChunk.lastPos, spear.firstChunk.pos, timeStacker);
                }
                
                Vector2 dir = Custom.PerpendicularVector(pos - startPos).normalized;
                
                if (i * 4 + 3 < mesh.vertices.Length)
                {
                    mesh.MoveVertice(i * 4, (startPos + pos) / 2f - dir * (thickness + width) * 0.5f - camPos);
                    mesh.MoveVertice(i * 4 + 1, (startPos + pos) / 2f + dir * (thickness + width) * 0.5f - camPos);
                    mesh.MoveVertice(i * 4 + 2, pos - dir * thickness - camPos);
                    mesh.MoveVertice(i * 4 + 3, pos + dir * thickness - camPos);
                    
                    Color color = Color.Lerp(colorStart, colorEnd, t);
                    for (int j = 0; j < 4; j++)
                    {
                        mesh.verticeColors[i * 4 + j] = color;
                    }
                }
                
                startPos = pos;
                width = thickness;
            }
        }

        private FSprite CreateTriangleMesh(RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            // 创建一个新的TriangleMesh
            TriangleMesh mesh = TriangleMesh.MakeLongMesh(points.GetLength(0), false, true);
            mesh.shader = rCam.game.rainWorld.Shaders["CatTentacle"];
            
            // 初始化顶点和颜色
            UpdateTriangleMesh(mesh, timeStacker, camPos);
            
            return mesh;
        }
    }
}
