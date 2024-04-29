using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using IL.MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;

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
            // Crate data is just floats separated by ; characters.
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
                //shape = int.TryParse(p[5], out var sp) ? (HatType)sp : (HatType)UnityEngine.Random.Range(0,10),
                shape = int.TryParse(p[5], out var sp) ? (HatType)sp :HatType.Bone,
                mainColor = seted ? Custom.hexToColor(p[6]) : Color.gray,
                decorateColor = seted ? Custom.hexToColor(p[7]) : Color.gray,




        };

            // If this is coming from a sandbox unlock, the hue and size should depend on the data value (see CrateIcon below).
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
            return new Color(229/255f, 136/255f, 70/255f);
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
            hue = 1f;


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
        public HatType shape;



        public override string ToString()
        {
            var color1 = Custom.colorToHex(mainColor);
            var color2 = Custom.colorToHex(decorateColor);





            return this.SaveToString($"{hue};{saturation};{scaleX};{scaleY};{setMainColor};{(int)shape};{color1};{color2}");
        }
    }

    public class CowBoyHat : PhysicalObject, IDrawable
    {
        int decorateIndex;
        CowBoyHatAbstract Abst { get; }

        public bool setMainColor = false;
        public Color mainColor=Color.blue;
        public Color decorateColor;
        public HatType shape;


        public Vector2 rotation;
        public Vector2 lastRotation=Vector2.zero;

        // Token: 0x04000C5F RID: 3167
        public float rotationSpeed;

        // Token: 0x04000C60 RID: 3168
        public Vector2? setRotation;    

        public CowBoyHat(CowBoyHatAbstract abstr) : base(abstr)
        {
            Abst = abstr;
            this.setMainColor = abstr.setMainColor;
            this.shape = abstr.shape;
            this.mainColor = abstr.mainColor;
            this.decorateColor = abstr.decorateColor;

            if (this.shape==HatType.None)
            {
                this.shape = abstr.shape=(HatType)Random.Range(1,40);
            }
            Debug.Log("[COWBOY]:HatSpawn:"+abstr.shape);


            float mass = 0.1f;
            var positions = new List<Vector2>();


            //添加各个部件的基本位置 
            positions.Add(Vector2.zero);
            positions.Add(Vector2.zero);

            //根据添加的位置创建体块
            bodyChunks = new BodyChunk[positions.Count];
            for (int i = 0; i < bodyChunks.Length; i++)
            {
                bodyChunks[i] = new BodyChunk(this, i, positions[i],6 , mass/bodyChunks.Length*1f);
            }
            decorateIndex = bodyChunks.Length - 1;

            //用于定位装饰
            bodyChunks[decorateIndex].rad = 0;
            bodyChunks[decorateIndex].mass = 0.02f;
            bodyChunks[decorateIndex].collideWithTerrain = false;;


            bodyChunkConnections = new BodyChunkConnection[bodyChunks.Length-1];

            for (int i = 0; i < bodyChunkConnections.Length; i++)
            {
                bodyChunkConnections[i] = new BodyChunkConnection(bodyChunks[i], bodyChunks[i + 1], 0f, BodyChunkConnection.Type.Normal, 0.9f, -1f);
            }




            

            //这个物体的基础属性
            airFriction = 0.85f;
            surfaceFriction = 0.02f;
            waterFriction = 0.3f;

            gravity = 0.85f;

            bounce = 1f;
            collisionLayer = 1;

            
            buoyancy = 0.999f;//浮力
            GoThroughFloors = true;
            canBeHitByWeapons = false;

        }


        public override void Update(bool eu)
        {
            //this.lastRotation = this.rotation;
            //if (this.setRotation != null)
            //{
            //    this.rotation = this.setRotation.Value;
            //    this.setRotation = null;
            //}
            //else
            //{
            //    float num2 = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), this.rotation);
            //    num2 += this.rotationSpeed;
            //    this.rotation = Custom.DegToVec(num2);
            //}
            base.Update(eu);
        }
        public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            base.Collide(otherObject, myChunk, otherChunk);

            if (this.firstChunk.vel.y<-1f&&otherObject is Player&&this.firstChunk.pos.y>otherObject.firstChunk.pos.y)
            {
                if (Hat.modules.TryGetValue(otherObject as Player,out var hatModule))
                {
                    if (hatModule.haveHat)
                    {
                        Hat.PlacePlayerHat(otherObject as Player, hatModule);
                    }
                    Hat.WearHat(this, hatModule);
                    
                }
            }
        }


        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);

            Vector2 center = placeRoom.MiddleOfTile(abstractPhysicalObject.pos);


            for (int i = 0; i < bodyChunks.Length; i++)
            {
                bodyChunks[i].HardSetPosition(center+Vector2.up);
            }
        }
        public  void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[3];

            for (int i = 0; i < bodyChunks.Length+1; i++)
                sLeaser.sprites[i] = new FSprite("Circle20");

            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
                new TriangleMesh.Triangle(0, 1, 2),
                new TriangleMesh.Triangle(1, 2, 3),
            };
            sLeaser.sprites[bodyChunks.Length]= new TriangleMesh("Futile_White",tris,true, true);

            AddToContainer(sLeaser, rCam, null);
        }
        public  void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!setMainColor)
            {
                Color color = Color.Lerp(palette.blackColor, mainColor, Random.Range(0.1f, 0.4f));
                mainColor= color;
                setMainColor = true;
                Abst.mainColor = this.mainColor;
                Abst.setMainColor = this.setMainColor;
                Abst.shape = this.shape;
            }
            foreach (var sprite in sLeaser.sprites)
                sprite.color = mainColor;
            decorateColor = decorateColor == null ? Color.white : decorateColor;

            Abst.setMainColor = this.setMainColor;
            Abst.mainColor = this.mainColor;
            Abst.decorateColor = this.decorateColor;
            Abst.shape = this.shape;
        }
        public  void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer)
        {
            newContainer ??= rCam.ReturnFContainer("Items");

            foreach (FSprite fsprite in sLeaser.sprites)
                newContainer.AddChild(fsprite);
        }
        public  void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            var rotationNow = Custom.DegToVec((Custom.VecToDeg(bodyChunks[0].Rotation)));
            for (int i = 0; i < 2; i++)
            {
                Vector2 showPos = Vector2.Lerp(bodyChunks[0].lastPos, bodyChunks[0].pos, timeStacker);
                Vector2 showPos2 = showPos - rotationNow * (this.bodyChunks[0].rad-2) ;
                if (i!=0)
                {
                    showPos = showPos2;
                }
                var spr = sLeaser.sprites[i];
                spr.SetPosition( showPos- camPos);
                spr.rotation = Custom.VecToDeg(rotationNow) ;
                spr.scale = bodyChunks[0].rad / 10f;
                if (i!=0)
                {
                    spr.scaleX *= 2.5f;
                    spr.scaleY *= 0.6f;
                }
                


            }
            lastRotation = rotationNow;



            sLeaser.sprites[2].color = decorateColor;
            Vector2 vector = Vector2.Lerp(bodyChunks[0].lastPos, bodyChunks[0].pos,timeStacker)-camPos;
            Vector2 dir = rotationNow;
            Vector2 per = Custom.PerpendicularVector(dir);

            //Hat.DrawHatDecoratePice(this.shape, sLeaser.sprites[2] as TriangleMesh,vector,per,dir,null);


            if (slatedForDeletetion || room != rCam.room)
                sLeaser.CleanSpritesAndRemove();
        }

    }

    sealed class CowBoyHatProperties : ItemProperties
    {
        public override void Throwable(Player player, ref bool throwable)
        => throwable = true;
        public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
        {
            // The shotStart should only be able to grab one Crate at a time
            grabability = Player.ObjectGrabability.OneHand;

        }
    }


   








}
