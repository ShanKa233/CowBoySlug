using CowBoySLug;
using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using IL.MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

namespace CowBoySlug
{
    public class CowBoyHatFisob : Fisob
    {
        public static readonly AbstractPhysicalObject.AbstractObjectType AbstrCrate = new("CowBoyHat", true);
        public static readonly MultiplayerUnlocks.SandboxUnlockID mCrate = new("CowBoyHat", true);

        public CowBoyHatFisob() : base(AbstrCrate)
        {
            Icon = new CowBoyHatIcon();

            SandboxPerformanceCost = new(linear: 0.2f, 0f);

            RegisterUnlock(mCrate, parent: MultiplayerUnlocks.SandboxUnlockID.Slugcat, data: 0);
        }


        public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock? unlock)
        {
            string[] p = saveData.CustomData.Split(';');

            if (p.Length < 8)
            {
                p = new string[8];
            }

            var result = new CowBoyHatAbstract(world, saveData.Pos, saveData.ID)
            {
                hue = float.TryParse(p[0], out var h) ? h : 0,
                saturation = float.TryParse(p[1], out var s) ? s : 1,
                scaleX = float.TryParse(p[2], out var x) ? x : 1,
                scaleY = float.TryParse(p[3], out var y) ? y : 1,

                setMainColor = bool.TryParse(p[4], out var seted) ? seted : false,

                //shapeID = int.TryParse(p[5], out var sp) ? ((HatType)sp).ToString() : p[5],

                shapeID = p[5],

                mainColor = seted ? Custom.hexToColor(p[6]) : Custom.HSL2RGB(Random.Range(0.01f, 0.9f), 0.5f, 0.3f),
                decorateColor = seted ? Custom.hexToColor(p[7]) : Custom.HSL2RGB(Random.Range(0.01f, 0.9f), 0.5f, 0.3f),




            };

            if (unlock is SandboxUnlock u)
            {
                result.hue = u.Data / 1000f;

                if (u.Data == 0)
                {
                    result.scaleX += 0.2f;
                    result.scaleY += 0.2f;
                }
            }


            return result;
        }


        private static readonly CowBoyHatProperties properties = new();

        public override ItemProperties Properties(PhysicalObject forObject)
        {
            // If you need to use the forObject parameter, pass it to your ItemProperties class's constructor.
            // The Mosquitoes example from the Fisobs github demonstrates this.
            return properties;
        }
    }

    sealed class CowBoyHatIcon : Icon
    {
        // In vanilla, you only have one int value to store custom data.
        // In this example, that is the hue of the object, which is scaled by 1000
        // For example, 0 is red, 70 is orange
        public override int Data(AbstractPhysicalObject apo)
        {
            return apo is CowBoyHatAbstract crate ? (int)(crate.hue * 1000f) : 0;
        }

        public override Color SpriteColor(int data)
        {
            return new Color(229 / 255f, 136 / 255f, 70 / 255f);
        }

        public override string SpriteName(int data)
        {
            // Fisobs autoloads the embedded resource named `icon_{Type}` automatically
            // For Crates, this is `icon_Crate`
            return "icon_CowBoyHat";
        }

    }

    public class CowBoyHatAbstract : AbstractPhysicalObject
    {
        public CowBoyHatAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, CowBoyHatFisob.AbstrCrate, null, pos, ID)
        {
            scaleX = 1;
            scaleY = 1;
            saturation = 0.5f;
            hue = 0.9f;

        }

        public override void Realize()
        {
            base.Realize();
            if (realizedObject == null)
                realizedObject = new CowBoyHat(this);
        }

        public float hue;
        public float saturation;
        public float scaleX;
        public float scaleY;

        public bool setMainColor;
        public Color mainColor;
        public Color decorateColor;

        public string shapeID;



        public override string ToString()
        {
            var color1 = Custom.colorToHex(mainColor);
            var color2 = Custom.colorToHex(decorateColor);





            //return this.SaveToString($"{hue};{saturation};{scaleX};{scaleY};{setMainColor};{(int)shapeID};{color1};{color2}");
            return this.SaveToString($"{hue};{saturation};{scaleX};{scaleY};{setMainColor};{shapeID};{color1};{color2}");
            //return this.SaveToString($"{hue};{saturation};{scaleX};{scaleY};{setMainColor};{shapeID};{color1};{color2}");
        }
    }


    sealed class CowBoyHatProperties : ItemProperties
    {
        public override void Throwable(Player player, ref bool throwable)
            => throwable = true;

        public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
            => grabability = Player.ObjectGrabability.OneHand;

    }

    public class CowBoyHat : PhysicalObject, IDrawable
    {

        //佩戴者
        public PhysicalObject wearers;

        //飞行方向
        public Vector2 rotation;
        public Vector2 lastRotation = Vector2.zero;

        //水平角度从0到2为360度
        public float levelAngle = 360;

        public static float RotatingLevel(float levelAngle)
        {
            while (levelAngle > 360 || levelAngle < 0)
            {
                if (levelAngle > 360) levelAngle %= 360;
                if (levelAngle < 0) levelAngle = 360 + levelAngle;
            }
            return levelAngle;
        }
        public void RotatingDexorate(float angle)
        {
            levelAngle += angle;
            while (levelAngle > 360 || levelAngle < 0)
            {
                if (levelAngle > 360) levelAngle %= 360;
                if (levelAngle < 0) levelAngle = 360 + levelAngle;
            }
        }

        public float minSpeed = 3;
        CowBoyHatAbstract Abst { get; }


        public bool setMainColor = false;
        public Color mainColor = Color.blue;
        public Color decorateColor;

        public string shapeID;

        //public HatType shapeID;




        public float rotationSpeed;
        //public Vector2? setRotation;



        public CowBoyHat(CowBoyHatAbstract abstr) : base(abstr)
        {
            Abst = abstr;
            this.setMainColor = abstr.setMainColor;

            this.shapeID = abstr.shapeID;

            this.mainColor = abstr.mainColor;
            this.decorateColor = abstr.decorateColor;

            if (this.shapeID == null)
            {
                //this.shapeID = abstr.shapeID = "NoAdorn";
                //this.shapeID = abstr.shapeID = "flower";
                this.shapeID = abstr.shapeID = HatData.HatsDictionary.ToArray()[Random.Range(0, HatData.HatsDictionary.Count - 1)].Value.id;
            }

            Debug.Log("[COWBOY]:HatSpawn:" + abstr.shapeID);


            float mass = 0.1f;
            var positions = new List<Vector2>();


            //添加各个部件的基本位置 
            positions.Add(Vector2.zero);
            positions.Add(Vector2.zero);

            //根据添加的位置创建体块
            bodyChunks = new BodyChunk[positions.Count];
            for (int i = 0; i < bodyChunks.Length; i++)
            {
                bodyChunks[i] = new BodyChunk(this, i, positions[i], 6, mass / bodyChunks.Length * 1f);
            }


            var decorateIndex = bodyChunks.Length - 1;

            //用于定位装饰
            bodyChunks[decorateIndex].rad = 0;
            bodyChunks[decorateIndex].mass = 0.02f;
            bodyChunks[decorateIndex].collideWithTerrain = false; ;


            bodyChunkConnections = new BodyChunkConnection[bodyChunks.Length - 1];

            for (int i = 0; i < bodyChunkConnections.Length; i++)
            {
                bodyChunkConnections[i] = new BodyChunkConnection(bodyChunks[i], bodyChunks[i + 1], 0f, BodyChunkConnection.Type.Normal, 0.9f, -1f);
            }




            //这个物体的基础属性
            airFriction = 0.999f;

            surfaceFriction = 0.02f;
            waterFriction = 0.3f;

            gravity = 0.85f;

            bounce = 1f;
            collisionLayer = 1;


            buoyancy = 0.999f;//浮力
            GoThroughFloors = true;
            canBeHitByWeapons = false;

        }

        //是否被戴着
        public bool Flying => firstChunk.vel.magnitude > minSpeed && grabbedBy.Count == 0 && !Weared;
        public bool Weared => wearers != null;
        public void WearersUpdate()
        {
            //如果帽子有佩戴者而且佩戴者不存在就不再记录佩戴者
            if (wearers != null && wearers.slatedForDeletetion)
            {
                wearers = null;
            }
            if (Weared && wearers is Player)
            {
                var player = (Player)wearers;
                bool flag2 = player.wantToPickUp > 0 && player.input[0].y < 0 && player.grasps[0] == null && player.grasps[1] == null;
                if (flag2)
                {
                    room.PlaySound(SoundID.Big_Spider_Spit, firstChunk);

                    if (Hat.modules.TryGetValue(wearers as Player, out var abstractHatWearStick) && abstractHatWearStick != null)
                    {
                        abstractHatWearStick.Deactivate();
                    }
                    wearers = null;
                }

            }
            if (Weared && Vector2.Distance(wearers.firstChunk.pos, firstChunk.pos) > 300)
            {
                room.PlaySound(SoundID.Big_Spider_Spit, firstChunk);
                if (Hat.modules.TryGetValue(wearers as Player,out var abstractHatWearStick)&&abstractHatWearStick!=null)
                {
                    abstractHatWearStick.Deactivate();
                }
                wearers = null;
            }

            //如果佩戴者还被记录着就执行下面的update
            if (Weared)
            {

                //firstChunk.pos = wearers.firstChunk.pos;
                var distance = Vector2.Distance(wearers.firstChunk.pos, firstChunk.pos);

                firstChunk.vel = Custom.LerpMap(distance, 1, 30, 1, 25) * Custom.DirVec(firstChunk.pos, wearers.firstChunk.pos);

                this.CollideWithObjects = false;
            }
            else
            {
                this.CollideWithObjects = true;
            }

        }
        public override void Update(bool eu)
        {
            base.Update(eu);

            //PlumeUpdate();

            RotatingDexorate(rotationSpeed);
            this.lastRotation = this.rotation;
            rotationSpeed *= 0.8f;

            WearersUpdate();


            if (firstChunk.vel.magnitude > minSpeed && grabbedBy.Count == 0 && !Weared)
            {
                firstChunk.vel = firstChunk.vel.magnitude * rotation.normalized;
                rotation = (rotation - new Vector2(0, g * 0.01f)).normalized;

                //让他在飞的时候提升转速
                rotationSpeed += (firstChunk.vel.x * 0.6f);
            }
            else
            {
                rotation = Vector2.up;
            }


            if (!Weared && g > 0)
            {
                firstChunk.vel.y *= 0.55f;
            }

        }
        public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            base.TerrainImpact(chunk, direction, speed, firstContact);
            if (direction.x != 0 && direction.y == 0 && Flying)
            {
                rotation.x *= -1;
                room.PlaySound(SoundID.Weapon_Skid, firstChunk, false, 0.4f, 0.4f);
            }
        }
        public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            base.Collide(otherObject, myChunk, otherChunk);
            if (otherObject is Player && this.firstChunk.vel.y - otherObject.firstChunk.vel.y < 0 && this.firstChunk.pos.y > otherObject.firstChunk.pos.y && otherChunk == 0)
            {

                WearHat(otherObject);

            }
        }

        public void WearHat(PhysicalObject wearer)
        {
            if (!Weared)
            {
                room.PlaySound(SoundID.Big_Spider_Spit, firstChunk);
                wearers = wearer;
                if (wearer is Player)
                {
                    //AbstractHatWearStick.GetHatModule(wearer as Player).Hatlist.Add(this);
                    Hat.modules.Add(wearer as Player, new AbstractHatWearStick(this.abstractPhysicalObject, wearer.abstractPhysicalObject as AbstractCreature));
                    

                }
            }
        }


        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);

            Vector2 center = placeRoom.MiddleOfTile(abstractPhysicalObject.pos);


            for (int i = 0; i < bodyChunks.Length; i++)
            {
                bodyChunks[i].HardSetPosition(center + Vector2.up);
            }
        }


        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[4];

            for (int i = 0; i < 2; i++)
                sLeaser.sprites[i] = new FSprite("Circle20");

            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
                new TriangleMesh.Triangle(0, 1, 2),
                new TriangleMesh.Triangle(1, 2, 3),
            };

            sLeaser.sprites[2] = new TriangleMesh("Futile_White", tris, true, true);

            if (HatData.HatsDictionary.TryGetValue(Abst.shapeID, out var hatData))
            {
                var imagiName = LoadHats.cowBoyHatFolderName + Path.DirectorySeparatorChar + hatData.sprite_name;
                var hatImagi = new FSprite(imagiName);
                sLeaser.sprites[3] = new TriangleMesh(imagiName, tris, true, true);

                var mesh = (sLeaser.sprites[3] as TriangleMesh);
                mesh.UVvertices[0] = new Vector2(0, 0);
                mesh.UVvertices[1] = new Vector2(0, 1);
                mesh.UVvertices[2] = new Vector2(1, 0);
                mesh.UVvertices[3] = new Vector2(1, 1);
            }
            else
            {
                sLeaser.sprites[3] = new TriangleMesh("Futile_White", tris, true, true);
            }


            AddToContainer(sLeaser, rCam, null);
        }
        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!setMainColor)
            {
                Color color = Color.Lerp(palette.blackColor, palette.skyColor, Random.Range(0.01f, 0.2f));
                mainColor = color;
                setMainColor = true;
                Abst.mainColor = this.mainColor;
                Abst.setMainColor = this.setMainColor;

                Abst.shapeID = this.shapeID;
            }
            foreach (var sprite in sLeaser.sprites) sprite.color = mainColor;

            decorateColor = decorateColor == null ? Color.white : decorateColor;

            Abst.setMainColor = this.setMainColor;
            Abst.mainColor = this.mainColor;
            Abst.decorateColor = this.decorateColor;
            Abst.shapeID = this.shapeID;
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer)
        {
            newContainer ??= rCam.ReturnFContainer("Items");

            foreach (FSprite fsprite in sLeaser.sprites)
                newContainer.AddChild(fsprite);
        }
        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (Weared)
            {
                WearDraw(sLeaser, rCam, timeStacker, camPos);
                return;
            }


            var rotationNow = Vector2.Lerp(lastRotation, rotation, timeStacker);
            if (firstChunk.vel.magnitude > minSpeed)
            {
                rotationNow = Custom.PerpendicularVector(rotationNow) * (Custom.VecToDeg(rotationNow) > 0.5 ? 1 : -1);

            }

            Vector2 centerPos = Vector2.Lerp(bodyChunks[0].lastPos, bodyChunks[0].pos, timeStacker);
            for (int i = 0; i < 2; i++)
            {
                Vector2 showPos = centerPos;
                Vector2 showPos2 = showPos - rotationNow * (this.bodyChunks[0].rad - 2);
                if (i != 0)
                {
                    showPos = showPos2;
                }
                var spr = sLeaser.sprites[i];
                spr.SetPosition(showPos - camPos);
                spr.rotation = Custom.VecToDeg(rotationNow);
                spr.scale = bodyChunks[0].rad / 10f;
                if (i != 0)
                {
                    spr.scaleX *= 2.5f;
                    spr.scaleY *= 0.6f;
                }

            }

            DrawDecorate(sLeaser.sprites, sLeaser.sprites[3] as TriangleMesh, rotationNow, centerPos - camPos, timeStacker);

            if (slatedForDeletetion || room != rCam.room)
                sLeaser.CleanSpritesAndRemove();
        }

        public float PlayerHeadInfo(Player player, float timeStacker, ref Vector2 headPosition)
        {
            var playerGra = player.graphicsModule as PlayerGraphics;

            float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(playerGra.lastBreath, playerGra.breath, timeStacker) * 3.1415927f * 2f);

            Vector2 vector = Vector2.Lerp(playerGra.drawPositions[0, 1], playerGra.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(playerGra.drawPositions[1, 1], playerGra.drawPositions[1, 0], timeStacker);
            headPosition = Vector2.Lerp(playerGra.head.lastPos, playerGra.head.pos, timeStacker);

            if (player.aerobicLevel > 0.5f)
            {
                vector += Custom.DirVec(vector2, vector) * Mathf.Lerp(-1f, 1f, num) * Mathf.InverseLerp(0.5f, 1f, player.aerobicLevel) * 0.5f;
                headPosition -= Custom.DirVec(vector2, vector) * Mathf.Lerp(-1f, 1f, num) * Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, player.aerobicLevel), 1.5f) * 0.75f;
            }

            float num3 = Custom.AimFromOneVectorToAnother(Vector2.Lerp(vector2, vector, 0.5f), headPosition);
            if (player.sleepCurlUp > 0f)
            {
                num3 = Mathf.Lerp(num3, 45f * Mathf.Sign(vector.x - vector2.x), player.sleepCurlUp);

                headPosition.y += 1f * player.sleepCurlUp;
                headPosition.x += Mathf.Sign(vector.x - vector2.x) * 2f * player.sleepCurlUp;
            }
            if (ModManager.CoopAvailable && player.bool1)
            {
                headPosition.y -= 1.9f;
                num3 = Mathf.Lerp(num3, 45f * Mathf.Sign(vector.x - vector2.x), 0.7f);
            }
            return num3;
        }
        public void WearDraw(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            var body = firstChunk;
            Vector2 headPosition = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            float rotation = PlayerHeadInfo(wearers as Player, timeStacker, ref headPosition);
            headPosition -= camPos;
            Vector2 vector = headPosition + Custom.DegToVec(rotation + FixHatRotation(wearers as Player)) * (7f);

            for (int i = 0; i < 2; i++)
            {

                Vector2 showPos = vector;
                Vector2 showPos2 = showPos - Custom.DegToVec(rotation + FixHatRotation(wearers as Player)) * (6 - 4);
                if (i != 0)
                {
                    showPos = showPos2;
                }
                var spr = sLeaser.sprites[i];
                spr.SetPosition(showPos);

                spr.rotation = rotation + FixHatRotation(wearers as Player);
                spr.scale = 6 / 10f;
                if (i != 0)
                {
                    spr.scaleX *= 2.5f;
                    spr.scaleY *= 0.6f;
                }

                spr.color = mainColor;
                DrawDecorate(sLeaser.sprites, sLeaser.sprites[3] as TriangleMesh, Custom.DegToVec(rotation + FixHatRotation(wearers as Player)).normalized, vector, levelAngle + FixHatLevelAngle(wearers as Player), timeStacker);

            }
        }
        public void DrawDecorate(FSprite[] sprites, TriangleMesh mesh, Vector2 rotationDir, Vector2 centerPos, float timeStacker)
        {
            DrawDecorate(sprites, mesh, rotationDir, centerPos, levelAngle, timeStacker);
        }
        public void DrawDecorate(FSprite[] sprites, TriangleMesh mesh, Vector2 rotationDir, Vector2 centerPos, float levelAngle, float timeStacker)
        {
            levelAngle = RotatingLevel(levelAngle);
            sprites[2].color = Color.Lerp(decorateColor, mainColor, 0.5f);
            mesh.color = decorateColor;

            //绘制绑带
            var strap = sprites[2] as TriangleMesh;
            var per = Custom.PerpendicularVector(-rotationDir);
            strap.MoveVertice(0, centerPos + (per * -7) + (rotationDir * -2));
            strap.MoveVertice(1, centerPos + (per * -6) + (rotationDir * 0));
            strap.MoveVertice(2, centerPos + (per * 7) + (rotationDir * -2));
            strap.MoveVertice(3, centerPos + (per * 6) + (rotationDir * 0));

            var size = 7f;

            //装饰最远伸出的距离
            Vector2 brim = Custom.PerpendicularVector(rotationDir) * (this.bodyChunks[0].rad) * 2f;

            //装饰的位置
            var decoratePos = Vector2.Lerp(centerPos - brim, centerPos + brim, levelAngle >= 180 ? 2 - levelAngle / 180 : levelAngle / 180);

            decoratePos += rotationDir * 2;

            if (levelAngle >= 180)
            {
                mesh.MoveVertice(2, decoratePos - rotationDir * size - per * size);
                mesh.MoveVertice(3, decoratePos + rotationDir * size - per * size);
                mesh.MoveVertice(0, decoratePos - rotationDir * size + per * size);
                mesh.MoveVertice(1, decoratePos + rotationDir * size + per * size);

                mesh.MoveBehindOtherNode(sprites[0]);
            }
            else
            {
                mesh.MoveVertice(0, decoratePos - rotationDir * size - per * size);
                mesh.MoveVertice(1, decoratePos + rotationDir * size - per * size);
                mesh.MoveVertice(2, decoratePos - rotationDir * size + per * size);
                mesh.MoveVertice(3, decoratePos + rotationDir * size + per * size);

                mesh.MoveInFrontOfOtherNode(sprites[2]);
            }

        }

        public static float FixHatRotation(Player player)
        {
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.mainBodyChunk.pos.x > player.bodyChunks[1].pos.x)
                {
                    return -70;
                }
                else
                {
                    return 70;
                }

            }
            else if (player.bodyMode == Player.BodyModeIndex.Stand && player.input[0].x > 0)
            {
                return -20;
            }
            else if (player.bodyMode == Player.BodyModeIndex.Stand && player.input[0].x < 0)
            {
                return 20;
            }
            else
            {
                return 0;
            }
        }
        public static float FixHatLevelAngle(Player player)
        {
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.mainBodyChunk.pos.x > player.bodyChunks[1].pos.x)
                {
                    return -90;
                }
                else
                {
                    return 90;
                }

            }
            else if (player.bodyMode == Player.BodyModeIndex.Stand && player.input[0].x > 0)
            {
                return -90;
            }
            else if (player.bodyMode == Player.BodyModeIndex.Stand && player.input[0].x < 0)
            {
                return 90;
            }
            else
            {
                return 0;
            }
        }



    }










}
