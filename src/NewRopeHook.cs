using CowBoySLug;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CowBoySlug
{
    public static class UseCowBoyRope
    {
        public static void SpawnRope(Spear spear, Player self, Color start, Color end)
        {
            if (!PlayerHook.modules.TryGetValue(self, out var cowBoyModule))
            {
                return;
            }
            //新建一个在矛上的丝线
            var rope = new NewRope(spear.room, spear, self, spear.firstChunk.vel,cowBoyModule.ropeColor,cowBoyModule.ropeColor);
            self.room.AddObject(rope);//召唤这个凝胶之线
        }


        public static void SpawnSpeedLine(Weapon weapon, Player self, Color color)
        {
            //新建一个丝线
            var rope = new SuperRockLine(weapon.room, weapon, self, weapon.firstChunk.vel, color);
            self.room.AddObject(rope);//召唤这个线
        }
    }

    public class NewRope : CosmeticSprite
    {

        public NewRope(Room room, Spear spear, Player player, Vector2 shootVel, Color colorStart, Color colorEnd) 
        {
            this.room = room;
            this.spear = spear;
            this.player = player;
            this.points = new Vector2[Random.Range(18, 20), 4];
            for (int i = 0; i < this.points.GetLength(0); i++)
            {
                this.points[i, 0] = (player.graphicsModule as PlayerGraphics).tail[0].pos + Custom.RNV();
                this.points[i, 1] = (player.graphicsModule as PlayerGraphics).tail[0].pos;
                this.points[i, 2] = shootVel * 0.3f * Random.value + Custom.RNV() * Random.value * 1.5f;
                this.points[i, 3] = new Vector2(30f, Mathf.Lerp(150f, 200f, Mathf.Pow(Random.value, 0.3f)));
            }

            this.colorStart = colorStart;
            this.colorEnd = colorEnd;
        }

        public Spear spear;
        public Player player;

        public Color colorStart;
        public Color colorEnd;

        public Vector2[,] points;

        // Token: 0x0400267F RID: 9855
        public Color fogColor;

        // Token: 0x04002680 RID: 9856
        public Color threadCol;

        // Token: 0x04002681 RID: 9857
        public Color blackColor;

        public bool brocked = false;
        public float LifeOfSegment(int i)
        {
            if (i > 0)
            {
                return Mathf.Min(this.points[i, 3].x, this.points[i - 1, 3].x);
            }
            return this.points[i, 3].x;
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            bool flag = true;
            for (int i = 0; i < this.points.GetLength(0); i++)
            {
                if (brocked)
                {
                    this.points[i, 3].x*=0.95f;
                }

                this.points[i, 1] = this.points[i, 0];
                this.points[i, 0] += this.points[i, 2];
                this.points[i, 2] *= Custom.LerpMap(this.points[i, 2].magnitude, 1f, 30f, 0.99f, 0.8f);
                this.points[i, 2].y -= Mathf.Lerp(0.1f, 0.6f, this.LifeOfSegment(i));
                if (this.LifeOfSegment(i) > 0f)
                {
                    if (this.room.GetTile(this.points[i, 0]).Solid)
                    {
                        SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(this.points[i, 0], this.points[i, 1], this.points[i, 2], 1f, default(IntVector2), true);
                        terrainCollisionData = SharedPhysics.HorizontalCollision(this.room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.VerticalCollision(this.room, terrainCollisionData);
                        this.points[i, 0] = terrainCollisionData.pos;
                        this.points[i, 2] = terrainCollisionData.vel ;//修改过
                    }
                    if (i > 0)
                    {
                        if (!Custom.DistLess(this.points[i, 0], this.points[i - 1, 0], 6f))
                        {
                            Vector2 a = Custom.DirVec(this.points[i, 0], this.points[i - 1, 0]) * (Vector2.Distance(this.points[i, 0], this.points[i - 1, 0]) - 6f);
                            this.points[i, 0] += a * 0.15f;
                            this.points[i, 2] += a * 0.25f;
                            this.points[i - 1, 0] -= a * 0.15f;
                            this.points[i - 1, 2] -= a * 0.25f;
                        }
                        if (!this.room.VisualContact(this.points[i, 0], this.points[i - 1, 0]))
                        {
                            this.points[i, 3].x -= 0.2f;
                        }
                    }
                    if (i > 1 && this.LifeOfSegment(i - 1) > 0f)
                    {
                        this.points[i, 2] += Custom.DirVec(this.points[i - 2, 0], this.points[i, 0]) * 0.6f;
                        this.points[i - 2, 2] -= Custom.DirVec(this.points[i - 2, 0], this.points[i, 0]) * 0.6f;
                    }
                    this.points[i, 3].x -= 1f / this.points[i, 3].y;
                    if (this.points[i, 3].x > 0f)
                    {
                        flag = false;
                    }
                }
                else
                {
                    brocked = true;
                }
            }
            if (this.LifeOfSegment(0) > 0f)
            {
                this.points[0, 0] = (this.player.graphicsModule as PlayerGraphics).tail[0].pos;
                this.points[0, 2] *= 0f;
                this.points[1, 2] += Custom.DirVec((this.player.graphicsModule as PlayerGraphics).tail[0].pos, (this.player.graphicsModule as PlayerGraphics).tail[0].pos) * 3f;
                this.points[3, 2] += Custom.DirVec((this.player.graphicsModule as PlayerGraphics).tail[0].pos, (this.player.graphicsModule as PlayerGraphics).tail[0].pos) * 1.5f;
                if (this.LifeOfSegment(1) <= 0f || this.player.enteringShortCut != null || this.player.room != this.room)
                {
                    this.points[0, 3].x -= 1f;
                }
            }
            if (this.LifeOfSegment(this.points.GetLength(0) - 1) > 0f)
            {
                Vector2 rotation = this.spear.rotation;
                rotation.Scale(new Vector2(25f, 25f));
                this.points[this.points.GetLength(0) - 1, 0] = this.spear.bodyChunks[0].pos - rotation;
                this.points[this.points.GetLength(0) - 1, 2] *= 0f;
                this.points[this.points.GetLength(0) - 2, 2] += Custom.DirVec(this.spear.bodyChunks[0].pos, this.spear.bodyChunks[0].pos - rotation) * 6f;
                this.points[this.points.GetLength(0) - 3, 2] += Custom.DirVec(this.spear.bodyChunks[0].pos, this.spear.bodyChunks[0].pos - rotation) * 1.5f;
                if (this.spear.slatedForDeletetion || this.spear.room != this.room)
                {
                    this.points[this.points.GetLength(0) - 1, 3].x -= 1f;
                }
            }
            if (flag)
            {
                this.Destroy();
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(this.points.GetLength(0), false, true);
            this.AddToContainer(sLeaser, rCam, null);
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector;
            if (this.LifeOfSegment(0) > 0f && this.player != null)
            {
                vector = Vector2.Lerp((this.player.graphicsModule as PlayerGraphics).tail[0].lastPos, (this.player.graphicsModule as PlayerGraphics).tail[0].pos, timeStacker);
            }
            else
            {
                vector = Vector2.Lerp(this.points[0, 1], this.points[0, 0], timeStacker);
            }
            float num = 0f;
            float b = 1f;
            bool flag = rCam.room.Darkness(this.pos) > 0.2f;
            for (int i = 0; i < this.points.GetLength(0); i++)
            {
                float f = Mathf.InverseLerp(0f, (float)(this.points.GetLength(0) - 1), (float)i);
                float num2 = this.LifeOfSegment(i);
                if (i < this.points.GetLength(0) - 1)
                {
                    num2 = Mathf.Min(num2, this.LifeOfSegment(i + 1));
                }
                float num3 = 0.5f * Mathf.InverseLerp(0f, 0.3f, num2);
                Vector2 vector2 = Vector2.Lerp(this.points[i, 1], this.points[i, 0], timeStacker);
                if (i == 0 && this.LifeOfSegment(0) > 0f)
                {
                    vector2 = vector;
                }
                else if (i == this.points.GetLength(0) - 1 && this.LifeOfSegment(i) > 0f)
                {
                    Vector2 rotation = this.spear.rotation;
                    rotation.Scale(new Vector2(25f, 25f));
                    vector2 = Vector2.Lerp(this.spear.bodyChunks[0].pos - rotation, this.spear.bodyChunks[0].pos - rotation, timeStacker);
                }
                Vector2 a = Custom.PerpendicularVector(vector2, vector);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4, (vector + vector2) / 2f - a * (num3 + num) * 0.5f - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 1, (vector + vector2) / 2f + a * (num3 + num) * 0.5f - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * num3 - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * num3 - camPos);
                Color color = Color.Lerp(this.fogColor, Color.Lerp(colorStart, Color.Lerp(this.threadCol, colorEnd, rCam.room.WaterShinyness(vector2, timeStacker)), 0.1f + 0.9f * Mathf.Pow(f, 0.25f + num2)), Mathf.Min(num2, b));
                if (flag && num2 > 0f)
                {
                    color = Color.Lerp(color, this.blackColor, rCam.room.DarknessOfPoint(rCam, vector2));
                }
                for (int j = 0; j < 4; j++)
                {
                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[i * 4 + j] = Color.Lerp(color, Color.white, (Mathf.Sin(Time.time * 4) * 0.4f + 0.4f)); ;
                }
                vector = vector2;
                num = num3;
                b = num2;
            }
            for (int k = 0; k < 4; k++)
            {
                (sLeaser.sprites[0] as TriangleMesh).verticeColors[k] = Color.Lerp(this.fogColor, this.blackColor, this.LifeOfSegment(0));
            }
            if (base.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.blackColor = palette.blackColor;
            this.fogColor = palette.fogColor;
            this.threadCol = Color.Lerp(colorStart, palette.fogColor, 0.3f);
        }
    }

    public class SuperRockLine : CosmeticSprite
    {

        public SuperRockLine(Room room, Weapon shotEnd, Player shotStart, Vector2 shootVel, Color color)
        {
            this.room = room;
            this.shotEnd = shotEnd;
            this.shotStart = shotStart;
            this.points = new Vector2[Random.Range(6,8), 4];
            for (int i = 0; i < this.points.GetLength(0); i++)
            {
                this.points[i, 0] = (this.shotStart.graphicsModule as PlayerGraphics).tail[0].pos + Custom.RNV();
                this.points[i, 1] = (this.shotStart.graphicsModule as PlayerGraphics).tail[0].pos;
                this.points[i, 2] = shootVel * 0.3f * Random.value + Custom.RNV() * Random.value * 1.5f;
                this.points[i, 3] = new Vector2(30f, Mathf.Lerp(150f, 0.1f, Mathf.Pow(Random.value, 0.3f)));
            }

            this.color = color;
        }

        public Weapon shotEnd;
        public Player shotStart;

        public Color color;

        public Vector2[,] points;

        // Token: 0x0400267F RID: 9855
        public Color fogColor;

        // Token: 0x04002680 RID: 9856
        public Color threadCol;

        // Token: 0x04002681 RID: 9857
        public Color blackColor;
        public float LifeOfSegment(int i)
        {
            if (i > 0)
            {
                return Mathf.Min(this.points[i, 3].x, this.points[i - 1, 3].x);
            }
            return this.points[i, 3].x;
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            bool flag = true;
            for (int i = 0; i < this.points.GetLength(0); i++)
            {
                this.points[i, 3].x *= 0.7f;

                this.points[i, 1] = this.points[i, 0];
                this.points[i, 0] += this.points[i, 2];
                this.points[i, 2] *= Custom.LerpMap(this.points[i, 2].magnitude, 1f, 30f, 0.99f, 0.8f);
                this.points[i, 2].y -= Mathf.Lerp(0.1f, 0.6f, this.LifeOfSegment(i));
                if (this.LifeOfSegment(i) > 0f)
                {
                    if (this.room.GetTile(this.points[i, 0]).Solid)
                    {
                        SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(this.points[i, 0], this.points[i, 1], this.points[i, 2], 1f, default(IntVector2), true);
                        terrainCollisionData = SharedPhysics.HorizontalCollision(this.room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.VerticalCollision(this.room, terrainCollisionData);
                        this.points[i, 0] = terrainCollisionData.pos;
                        this.points[i, 2] = terrainCollisionData.vel;//修改过
                    }
                    if (i > 0)
                    {
                        if (!Custom.DistLess(this.points[i, 0], this.points[i - 1, 0], 6f))
                        {
                            Vector2 a = Custom.DirVec(this.points[i, 0], this.points[i - 1, 0]) * (Vector2.Distance(this.points[i, 0], this.points[i - 1, 0]) - 6f);
                            this.points[i, 0] += a * 0.15f;
                            this.points[i, 2] += a * 0.25f;
                            this.points[i - 1, 0] -= a * 0.15f;
                            this.points[i - 1, 2] -= a * 0.25f;
                        }
                        if (!this.room.VisualContact(this.points[i, 0], this.points[i - 1, 0]))
                        {
                            this.points[i, 3].x -= 0.2f;
                        }
                    }
                    if (i > 1 && this.LifeOfSegment(i - 1) > 0f)
                    {
                        this.points[i, 2] += Custom.DirVec(this.points[i - 2, 0], this.points[i, 0]) * 0.6f;
                        this.points[i - 2, 2] -= Custom.DirVec(this.points[i - 2, 0], this.points[i, 0]) * 0.6f;
                    }
                    this.points[i, 3].x -= 1f / this.points[i, 3].y;
                    if (this.points[i, 3].x > 0f)
                    {
                        flag = false;
                    }
                }
            }
            if (this.LifeOfSegment(0) > 0f)
            {
                this.points[0, 0] = (this.shotStart.graphicsModule as PlayerGraphics).tail[0].pos;
                this.points[0, 2] *= 0f;
                this.points[1, 2] += Custom.DirVec((this.shotStart.graphicsModule as PlayerGraphics).tail[0].pos, (this.shotStart.graphicsModule as PlayerGraphics).tail[0].pos) * 3f;
                this.points[3, 2] += Custom.DirVec((this.shotStart.graphicsModule as PlayerGraphics).tail[0].pos, (this.shotStart.graphicsModule as PlayerGraphics).tail[0].pos) * 1.5f;
                if (this.LifeOfSegment(1) <= 0f || this.shotStart.enteringShortCut != null || this.shotStart.room != this.room)
                {
                    this.points[0, 3].x -= 1f;
                }
            }
            if (this.LifeOfSegment(this.points.GetLength(0) - 1) > 0f)
            {
                Vector2 rotation = this.shotEnd.rotation;
                rotation.Scale(new Vector2(25f, 25f));
                this.points[this.points.GetLength(0) - 1, 0] = this.shotEnd.bodyChunks[0].pos ;
                this.points[this.points.GetLength(0) - 1, 2] *= 0f;
                this.points[this.points.GetLength(0) - 2, 2] += Custom.DirVec(this.shotEnd.bodyChunks[0].pos, this.shotEnd.bodyChunks[0].pos ) * 6f;
                this.points[this.points.GetLength(0) - 3, 2] += Custom.DirVec(this.shotEnd.bodyChunks[0].pos, this.shotEnd.bodyChunks[0].pos) * 1.5f;
                if (this.shotEnd.slatedForDeletetion || this.shotEnd.room != this.room)
                {
                    this.points[this.points.GetLength(0) - 1, 3].x -= 1f;
                }
            }
            if (flag)
            {
                this.Destroy();
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(this.points.GetLength(0), false, true);
            this.AddToContainer(sLeaser, rCam, null);
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector;
            if (this.LifeOfSegment(0) > 0f && this.shotStart != null)
            {
                vector = Vector2.Lerp((this.shotStart.graphicsModule as PlayerGraphics).tail[0].lastPos, (this.shotStart.graphicsModule as PlayerGraphics).tail[0].pos, timeStacker);
            }
            else
            {
                vector = Vector2.Lerp(this.points[0, 1], this.points[0, 0], timeStacker);
            }
            float num = 0f;
            float b = 1f;
            bool flag = rCam.room.Darkness(this.pos) > 0.2f;
            for (int i = 0; i < this.points.GetLength(0); i++)
            {
                float f = Mathf.InverseLerp(0f, (float)(this.points.GetLength(0) - 1), (float)i);
                float num2 = this.LifeOfSegment(i);
                if (i < this.points.GetLength(0) - 1)
                {
                    num2 = Mathf.Min(num2, this.LifeOfSegment(i + 1));
                }
                float num3 = 0.5f * Mathf.InverseLerp(0f, 0.3f, num2);
                Vector2 vector2 = Vector2.Lerp(this.points[i, 1], this.points[i, 0], timeStacker);
                if (i == 0 && this.LifeOfSegment(0) > 0f)
                {
                    vector2 = vector;
                }
                else if (i == this.points.GetLength(0) - 1 && this.LifeOfSegment(i) > 0f)
                {
                    Vector2 rotation = this.shotEnd.rotation;
                    rotation.Scale(new Vector2(25f, 25f));
                    vector2 = Vector2.Lerp(this.shotEnd.bodyChunks[0].pos, this.shotEnd.bodyChunks[0].pos, timeStacker);
                }
                Vector2 a = Custom.PerpendicularVector(vector2, vector);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4, (vector + vector2) / 2f - a * (num3 + num) * 0.5f - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 1, (vector + vector2) / 2f + a * (num3 + num) * 0.5f - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * num3 - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * num3 - camPos);
                Color color = Color.Lerp(this.fogColor, Color.Lerp(this.color, Color.Lerp(this.threadCol, this.color, rCam.room.WaterShinyness(vector2, timeStacker)), 0.1f + 0.9f * Mathf.Pow(f, 0.25f + num2)), Mathf.Min(num2, b));
                if (flag && num2 > 0f)
                {
                    color = Color.Lerp(color, this.blackColor, rCam.room.DarknessOfPoint(rCam, vector2));
                }
                for (int j = 0; j < 4; j++)
                {
                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[i * 4 + j] = Color.Lerp(color, Color.white, (Mathf.Sin(Time.time * 4) * 0.4f + 0.4f)); ;
                }
                vector = vector2;
                num = num3;
                b = num2;
            }
            for (int k = 0; k < 4; k++)
            {
                (sLeaser.sprites[0] as TriangleMesh).verticeColors[k] = Color.Lerp(this.fogColor, this.blackColor, this.LifeOfSegment(0));
            }
            if (base.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.blackColor = palette.blackColor;
            this.fogColor = palette.fogColor;
            this.threadCol = Color.Lerp(this.color, palette.fogColor, 0.3f);
        }
    }
}
