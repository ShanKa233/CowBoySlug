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
            if (!RopeMaster.modules.TryGetValue(player, out var module))
                return;

            var rope = new CowRope(player, spear, start, end); //新建一个在矛上的丝线

            player.room.AddObject(rope); //召唤这个线
        }

        #endregion
        public CowRope(Player player, Spear spear, Color colorStart, Color colorEnd)
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
                return player.bodyChunks[1].pos;
                //return (player.graphicsModule as PlayerGraphics).tail[0].pos;
            }
        }
        public Vector2 spearEndPos
        {
            get
            {
                var rotation = spear.rotation;
                rotation.Scale(
                    new Vector2(1, 1) * (spear.mode == Spear.Mode.StuckInCreature ? 50 : 25)
                );
                return spear.firstChunk.pos - rotation;
            }
        }

        #region 一些小方法
        public Vector2 RopePos(int i)
        {
            if (i == this.rope.TotalPositions - 1 || i < 0)
            {
                return spear.firstChunk.pos;
            }
            return this.rope.GetPosition(i);
        }

        public Vector2 RopeShowPos(int i)
        {
            if (i == this.rope.TotalPositions - 1 || i < 0)
            {
                return spearEndPos;
                ;
            }
            return this.rope.GetPosition(i);
        }

        public void GatherTogether()
        {
            if (rope.TotalPositions < 1)
            {
                return;
            }

            float totalLength = this.rope.totalLength;
            float distance = 0f;

            for (int i = 0; i < this.rope.TotalPositions - 1; i++)
            {
                if (i > 0)
                {
                    distance += Vector2.Distance(this.RopePos(i - 1), this.RopePos(i));
                } //上个点和这个点的距离
                int startLockedIndex = Custom.IntClamp(
                    (int)(distance / totalLength * (float)this.points.GetLength(0)),
                    0,
                    this.points.GetLength(0) - 1
                ); //计算出这个点位的index

                float distance2 = distance + Vector2.Distance(this.RopePos(i), this.RopePos(i + 1)); //下个点和下下个点的距离
                int endLockedIndex = Custom.IntClamp(
                    (int)(distance2 / totalLength * (float)this.points.GetLength(0)),
                    0,
                    this.points.GetLength(0) - 1
                ); //计算出下个点位的index

                for (int j = startLockedIndex + 1; j < endLockedIndex; j++) // 循环让所有在这个固定点位内的线都朝可以平均的分散在这个线段之间的位置移动
                {
                    var t = Mathf.InverseLerp(startLockedIndex, endLockedIndex, j); //计算t
                    var destinationPos = Vector2.Lerp(
                        points[startLockedIndex, 0],
                        points[endLockedIndex, 0],
                        t
                    ); //计算完美情况下的位置

                    points[j, 2] +=
                        Custom.DirVec(points[j, 0], destinationPos)
                        * Custom.LerpMap(
                            Vector2.Distance(points[j, 0], destinationPos),
                            0f,
                            150f,
                            0f,
                            120f * loose
                        ); //改变速度让线往最理想的位置变动
                }
            }
        }

        public float LifeOfSegment(int i)
        {
            if (i > 0)
            {
                return Mathf.Min(points[i, 3].x, points[i - 1, 3].x);
            }
            return points[i, 3].x;
        }
        #endregion

        public override void Destroy()
        {
            if (spear.rope().rope == this)
            {
                spear.rope().rope = null;
            }
            // spear.rope().RemoveRope();
            //if (RopeMaster.modules.TryGetValue(player, out var ropeMaster))
            //{
            //    ropeMaster.ropes.Remove(this);
            //}
            base.Destroy();
        }

        public void LimitUpdate()
        {
            if (limited)
                return;

            //如果玩家不在就让线变成极限状态
            if (player == null || player.room != room)
            {
                limited = true;
                return;
            }

            //如果矛不在就进入线的极限状态
            if (spear == null || spear.room != room)
            {
                limited = true;
                return;
            }

            //如果线太多圈就开始断
            if (rope.TotalPositions > points.GetLength(0) / 3)
            {
                limited = true;
                return; //如果转折数量大于可承受范围就让其进入崩坏状态
            }

            //如果上面的矛没有连接线
            if (!spear.rope().IsRopeSpear)
            {
                limited = true;
                return;
            }

            //如果矛上有其他线
            if (spear.rope().rope != this)
            {
                limited = true;
                return;
            }
        }

        /// <summary>
        /// 对其显示的线和计算出的弯折顶点
        /// </summary>
        public void AlignPoints()
        {
            usedPoints = new List<int>();

            if (this.rope.TotalPositions < 1)
            {
                return;
            }
            float totalLength = this.rope.totalLength; //线段总长
            float distance = 0f; //用于计算总距离
            for (int i = 0; i < this.rope.TotalPositions; i++)
            {
                if (i > 0)
                {
                    distance += Vector2.Distance(this.RopePos(i - 1), this.RopePos(i)); //上个点和这个点的距离
                }
                int num2 = Custom.IntClamp(
                    (int)(distance / totalLength * (float)this.points.GetLength(0)),
                    0,
                    this.points.GetLength(0) - 1
                );

                this.points[num2, 1] = this.points[num2, 0];
                this.points[num2, 0] = this.RopePos(i);

                this.points[num2, 2] *= 0f;

                usedPoints.Add(num2);
            }
        }

        public override void Update(bool eu)
        {
            bool brocked = true; //需不需要让绳子完全消失

            LimitUpdate(); //是否断裂更新

            if (!limited)
                rope.Update(playerPos, spear.firstChunk.pos); //如果没有溃散就更新用于计算弯折的线条和同步point和绳子的位置

            if (!limited)
                AlignPoints(); //没断裂就对其线段

            //debugRope.Update();//用来debug的时候显示的东西


            if (limited)
                points[0, 3].x = 0; //让超过极限的绳子脱离玩家的身体

            //让开头和结尾的绳子对其玩家和矛
            points[0, 0] = rope.A;
            points[points.GetLength(0) - 1, 0] = spearEndPos;

            for (int i = 0; i < points.GetLength(0); i++)
            {
                //如果玩家不在这个房间就急速消散
                if (player == null || spear == null || player.room != room || spear.room != room)
                {
                    points[i, 3].x *= 0.9f;
                    limited = true;
                }
            }

            //让线聚拢
            if (!limited)
                GatherTogether();

            for (int i = 0; i < points.GetLength(0); i++)
            {
                points[i, 1] = points[i, 0]; //lastPos=pos

                if (i != 0 && i != points.GetLength(0) - 1) //防止对两端的绳子进行
                {
                    points[i, 0] += points[i, 2]; //pos+=vel
                }

                points[i, 2] *= Custom.LerpMap(points[i, 2].magnitude, 1f, 30f, 0.99f, 0.8f); //vel

                points[i, 2].y -= Mathf.Lerp(0.1f, 0.6f, LifeOfSegment(i)); //模拟重力

                if (LifeOfSegment(i) > 0f)
                {
                    if (Custom.DistLess(this.points[i, 0], this.points[i, 1], 200))
                    {
                        SharedPhysics.TerrainCollisionData terrainCollisionData =
                            new SharedPhysics.TerrainCollisionData(
                                points[i, 0],
                                points[i, 1],
                                points[i, 2],
                                3f,
                                default,
                                true
                            );
                        terrainCollisionData = SharedPhysics.HorizontalCollision(
                            room,
                            terrainCollisionData
                        );
                        terrainCollisionData = SharedPhysics.VerticalCollision(
                            room,
                            terrainCollisionData
                        );
                        points[i, 0] = terrainCollisionData.pos;
                        points[i, 2] = terrainCollisionData.vel; //修改过
                    } //如果两个点在一定范围内就计算和墙面的碰撞

                    if (i > 0)
                    {
                        if (!Custom.DistLess(points[i, 0], points[i - 1, 0], loose))
                        {
                            Vector2 shrink =
                                Custom.DirVec(points[i, 0], points[i - 1, 0])
                                * (Vector2.Distance(points[i, 0], points[i - 1, 0]) - loose);
                            var firm = 0.25f;
                            if (!usedPoints.Contains(i))
                            {
                                points[i, 0] += shrink * firm;
                                points[i, 2] += shrink * 0.15f;
                            }
                            if (!usedPoints.Contains(i - 1))
                            {
                                points[i - 1, 0] -= shrink * firm;
                                points[i - 1, 2] -= shrink * 0.15f;
                            }
                        }
                        //让无法连上的线变薄
                        if (!room.VisualContact(points[i, 0], points[i - 1, 0]) && limited)
                            points[i, 3].x -= 0.2f;
                    }

                    if (limited)
                        points[i, 3].x -= 1f / points[i, 3].y; //如果超过限制就让线消散

                    brocked = false; //防止在消散前坏掉
                }
            }

            loose = Custom.LerpAndTick(loose, (used) ? 1f : 0.2f, 0.07f, 0.1f / 3f);
            used = false;

            if (brocked)
                Destroy();
        }

        #region 显示相关
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(points.GetLength(0), false, true);
            AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            Vector2 camPos
        )
        {
            Vector2 startPos = Vector2.Lerp(points[0, 1], points[0, 0], timeStacker);

            float num = 0f;
            bool flag = rCam.room.Darkness(pos) > 0.2f;

            for (int i = 0; i < points.GetLength(0); i++)
            {
                float f = Mathf.InverseLerp(0f, points.GetLength(0) - 1, i);

                float num2 = LifeOfSegment(i);
                if (i < points.GetLength(0) - 1)
                {
                    num2 = Mathf.Min(num2, LifeOfSegment(i + 1));
                }

                float num3 = 0.5f * Mathf.InverseLerp(0f, 0.3f, num2);
                Vector2 vector2 = Vector2.Lerp(points[i, 1], points[i, 0], timeStacker);
                if (i == 0 && LifeOfSegment(0) > 0f)
                {
                    vector2 = startPos;
                }
                else if (i == points.GetLength(0) - 1 && LifeOfSegment(i) > 0f)
                {
                    Vector2 rotation = spear.rotation;
                    rotation.Scale(
                        new Vector2(25f, 25f) * (spear.mode == Spear.Mode.StuckInCreature ? 2 : 1)
                    );
                    vector2 = Vector2.Lerp(
                        spear.bodyChunks[0].lastPos - rotation,
                        spear.bodyChunks[0].pos - rotation,
                        timeStacker
                    );
                }
                Vector2 a = Custom.PerpendicularVector(vector2, startPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(
                    i * 4,
                    (startPos + vector2) / 2f - a * (num3 + num) * 0.5f - camPos
                );
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(
                    i * 4 + 1,
                    (startPos + vector2) / 2f + a * (num3 + num) * 0.5f - camPos
                );
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(
                    i * 4 + 2,
                    vector2 - a * num3 - camPos
                );
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(
                    i * 4 + 3,
                    vector2 + a * num3 - camPos
                );

                Color color = Color.Lerp(
                    colorStart,
                    colorEnd,
                    rCam.room.WaterShinyness(vector2, timeStacker) * 0.1f
                        + 0.9f * Mathf.Pow(f, 0.25f + num2)
                );

                if (flag && num2 > 0f)
                {
                    color = Color.Lerp(
                        colorStart,
                        blackColor,
                        rCam.room.DarknessOfPoint(rCam, vector2)
                    );
                }
                for (int j = 0; j < 4; j++)
                {
                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[i * 4 + j] = Color.Lerp(
                        color,
                        Color.white,
                        Mathf.Sin(Time.time * 4) * 0.4f + 0.4f
                    );
                    ;
                }
                startPos = vector2;
                num = num3;
            }

            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public override void ApplyPalette(
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            RoomPalette palette
        )
        {
            blackColor = palette.blackColor;
            fogColor = palette.fogColor;

            threadCol = Color.Lerp(colorStart, palette.fogColor, 0.3f);
        }

        #endregion
    }
}