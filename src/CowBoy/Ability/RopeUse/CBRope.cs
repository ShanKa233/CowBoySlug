using CowBoySLug;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CowBoySlug.CowBoy.Ability.RopeUse
{
    public static class UseCowBoyRope
    {
        public static List<NewRope> RopeList=new List<NewRope>();//用于调用丝线的循环



        public static void SpawnRope(Spear spear, Player self, Color start, Color end)
        {
            if (!PlayerHook.cowboyModules.TryGetValue(self, out var cowBoyModule))
            {
                return;
            }
            var rope = new NewRope(spear.room, spear, self, spear.firstChunk.vel, cowBoyModule.ropeColor, cowBoyModule.ropeColor);//新建一个在矛上的丝线
            self.room.AddObject(rope);//召唤这个线
        }

    }

    public class NewRope : CosmeticSprite
    {
        public Spear spear;//所属矛
        public Player player;//所属玩家

        public Color colorStart;//线开始的颜色
        public Color colorEnd;//线结束的颜色

        public Vector2[,] points;


        public Color fogColor;
        public Color threadCol;
        public Color blackColor;

        public bool brocked = false;
        public NewRope(Room room, Spear spear, Player player, Vector2 shootVel, Color colorStart, Color colorEnd)
        {
            UseCowBoyRope.RopeList.Add(this);

            this.room = room;//所在房间
            this.spear = spear;//连接的矛
            this.player = player;//所属玩家
            points = new Vector2[Random.Range(18, 20), 4];//用来记录绳子长度弹性之类的数组



            for (int i = 0; i < points.GetLength(0); i++)
            {
                points[i, 0] = (player.graphicsModule as PlayerGraphics).tail[0].pos + Custom.RNV();
                points[i, 1] = (player.graphicsModule as PlayerGraphics).tail[0].pos;
                points[i, 2] = shootVel * 0.3f * Random.value + Custom.RNV() * Random.value * 1.5f;
                points[i, 3] = new Vector2(30f, Mathf.Lerp(150f, 200f, Mathf.Pow(Random.value, 0.3f)));
            }

            this.colorStart = colorStart;
            this.colorEnd = colorEnd;
        }

        #region 一些小方法
        public float LifeOfSegment(int i)
        {
            if (i > 0)
            {
                return Mathf.Min(points[i, 3].x, points[i - 1, 3].x);
            }
            return points[i, 3].x;
        }
        //清楚没有使用的线和这条线
        public static void ClearNullRope(NewRope self)
        {
            for (int i = UseCowBoyRope.RopeList.Count - 1; i >= 0; i--)
            {
                if (UseCowBoyRope.RopeList[i] == null || UseCowBoyRope.RopeList[i]==self)
                {
                    UseCowBoyRope.RopeList.RemoveAt(i);
                }
            }
        }

        #endregion

        public override void Destroy()
        {
           ClearNullRope(this);
            base.Destroy();
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            bool flag = true;
            for (int i = 0; i < points.GetLength(0); i++)
            {
                //如果这个线断了就快速让他消散
                if (brocked)
                {
                    points[i, 3].x *= 0.95f;
                    ClearNullRope(this);
                }

                points[i, 1] = points[i, 0];
                points[i, 0] += points[i, 2];
                points[i, 2] *= Custom.LerpMap(points[i, 2].magnitude, 1f, 30f, 0.99f, 0.8f);
                points[i, 2].y -= Mathf.Lerp(0.1f, 0.6f, LifeOfSegment(i));
                if (LifeOfSegment(i) > 0f)
                {
                    if (room.GetTile(points[i, 0]).Solid)
                    {
                        SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(points[i, 0], points[i, 1], points[i, 2], 1f, default, true);
                        terrainCollisionData = SharedPhysics.HorizontalCollision(room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.VerticalCollision(room, terrainCollisionData);
                        points[i, 0] = terrainCollisionData.pos;
                        points[i, 2] = terrainCollisionData.vel;//修改过
                    }
                    if (i > 0)
                    {
                        if (!Custom.DistLess(points[i, 0], points[i - 1, 0], 6f))
                        {
                            Vector2 a = Custom.DirVec(points[i, 0], points[i - 1, 0]) * (Vector2.Distance(points[i, 0], points[i - 1, 0]) - 6f);
                            points[i, 0] += a * 0.15f;
                            points[i, 2] += a * 0.25f;
                            points[i - 1, 0] -= a * 0.15f;
                            points[i - 1, 2] -= a * 0.25f;
                        }
                        if (!room.VisualContact(points[i, 0], points[i - 1, 0]))
                        {
                            points[i, 3].x -= 0.2f;
                        }
                    }
                    if (i > 1 && LifeOfSegment(i - 1) > 0f)
                    {
                        points[i, 2] += Custom.DirVec(points[i - 2, 0], points[i, 0]) * 0.6f;
                        points[i - 2, 2] -= Custom.DirVec(points[i - 2, 0], points[i, 0]) * 0.6f;
                    }
                    points[i, 3].x -= 1f / points[i, 3].y;
                    if (points[i, 3].x > 0f)
                    {
                        flag = false;
                    }
                }
                else
                {
                    brocked = true;
                }
            }
            if (LifeOfSegment(0) > 0f)
            {
                points[0, 0] = (player.graphicsModule as PlayerGraphics).tail[0].pos;
                points[0, 2] *= 0f;
                points[1, 2] += Custom.DirVec((player.graphicsModule as PlayerGraphics).tail[0].pos, (player.graphicsModule as PlayerGraphics).tail[0].pos) * 3f;
                points[3, 2] += Custom.DirVec((player.graphicsModule as PlayerGraphics).tail[0].pos, (player.graphicsModule as PlayerGraphics).tail[0].pos) * 1.5f;
                if (LifeOfSegment(1) <= 0f || player.enteringShortCut != null || player.room != room)
                {
                    points[0, 3].x -= 1f;
                }
            }
            if (LifeOfSegment(points.GetLength(0) - 1) > 0f)
            {
                Vector2 rotation = spear.rotation;
                rotation.Scale(new Vector2(25f, 25f));
                points[points.GetLength(0) - 1, 0] = spear.bodyChunks[0].pos - rotation;
                points[points.GetLength(0) - 1, 2] *= 0f;
                points[points.GetLength(0) - 2, 2] += Custom.DirVec(spear.bodyChunks[0].pos, spear.bodyChunks[0].pos - rotation) * 6f;
                points[points.GetLength(0) - 3, 2] += Custom.DirVec(spear.bodyChunks[0].pos, spear.bodyChunks[0].pos - rotation) * 1.5f;
                if (spear.slatedForDeletetion || spear.room != room)
                {
                    points[points.GetLength(0) - 1, 3].x -= 1f;
                }
            }
            if (flag)
            {
                Destroy();
            }
        }


        #region 显示相关
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(points.GetLength(0), false, true);
            AddToContainer(sLeaser, rCam, null);
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector;
            if (LifeOfSegment(0) > 0f && player != null)
            {
                vector = Vector2.Lerp((player.graphicsModule as PlayerGraphics).tail[0].lastPos, (player.graphicsModule as PlayerGraphics).tail[0].pos, timeStacker);
            }
            else
            {
                vector = Vector2.Lerp(points[0, 1], points[0, 0], timeStacker);
            }
            float num = 0f;
            float b = 1f;
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
                    vector2 = vector;
                }
                else if (i == points.GetLength(0) - 1 && LifeOfSegment(i) > 0f)
                {
                    Vector2 rotation = spear.rotation;
                    rotation.Scale(new Vector2(25f, 25f));
                    vector2 = Vector2.Lerp(spear.bodyChunks[0].pos - rotation, spear.bodyChunks[0].pos - rotation, timeStacker);
                }
                Vector2 a = Custom.PerpendicularVector(vector2, vector);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4, (vector + vector2) / 2f - a * (num3 + num) * 0.5f - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 1, (vector + vector2) / 2f + a * (num3 + num) * 0.5f - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * num3 - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * num3 - camPos);
                Color color = Color.Lerp(fogColor, Color.Lerp(colorStart, Color.Lerp(threadCol, colorEnd, rCam.room.WaterShinyness(vector2, timeStacker)), 0.1f + 0.9f * Mathf.Pow(f, 0.25f + num2)), Mathf.Min(num2, b));
                if (flag && num2 > 0f)
                {
                    color = Color.Lerp(color, blackColor, rCam.room.DarknessOfPoint(rCam, vector2));
                }
                for (int j = 0; j < 4; j++)
                {
                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[i * 4 + j] = Color.Lerp(color, Color.white, Mathf.Sin(Time.time * 4) * 0.4f + 0.4f); ;
                }
                vector = vector2;
                num = num3;
                b = num2;
            }
            for (int k = 0; k < 4; k++)
            {
                (sLeaser.sprites[0] as TriangleMesh).verticeColors[k] = Color.Lerp(fogColor, blackColor, LifeOfSegment(0));
            }
            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            blackColor = palette.blackColor;
            fogColor = palette.fogColor;
            threadCol = Color.Lerp(colorStart, palette.fogColor, 0.3f);
        }

        #endregion
    }

}
