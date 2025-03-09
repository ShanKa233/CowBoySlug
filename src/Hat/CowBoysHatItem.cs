using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CowBoySLug;
using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using IL.MoreSlugcats;
using RWCustom;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

namespace CowBoySlug
{
    public class CowBoyHatFisob : Fisob
    {
        // 定义帽子的抽象物体类型
        public static readonly AbstractPhysicalObject.AbstractObjectType AbstrCrate = new(
            "CowBoyHat",
            true
        );

        // 定义帽子的多人解锁ID
        public static readonly MultiplayerUnlocks.SandboxUnlockID mCrate = new("CowBoyHat", true);

        public CowBoyHatFisob()
            : base(AbstrCrate)
        {
            Icon = new CowBoyHatIcon(); // 设置帽子的图标

            // 设置沙盒性能成本
            SandboxPerformanceCost = new(linear: 0.2f, 0f);

            // 注册解锁
            RegisterUnlock(mCrate, parent: MultiplayerUnlocks.SandboxUnlockID.Slugcat, data: 0);
        }

        public override AbstractPhysicalObject Parse(
            World world,
            EntitySaveData saveData,
            SandboxUnlock? unlock
        )
        {
            // 解析保存的数据
            string[] p = saveData.CustomData.Split(';');

            // 确保数组长度至少为8
            if (p.Length < 8)
            {
                p = new string[8];
            }

            // 创建帽子的抽象实例
            var result = new CowBoyHatAbstract(world, saveData.Pos, saveData.ID)
            {
                hue = float.TryParse(p[0], out var h) ? h : 0,
                saturation = float.TryParse(p[1], out var s) ? s : 1,
                scaleX = float.TryParse(p[2], out var x) ? x : 1,
                scaleY = float.TryParse(p[3], out var y) ? y : 1,

                setMainColor = bool.TryParse(p[4], out var seted) ? seted : false,

                shapeID = p[5], // 帽子的形状ID

                mainColor = seted
                    ? Custom.hexToColor(p[6]) // 如果设置了主颜色，则从保存数据中获取
                    : Custom.HSL2RGB(Random.Range(0.01f, 0.9f), 0.5f, 0.3f), // 否则随机生成颜色
                decorateColor = seted
                    ? Custom.hexToColor(p[7]) // 如果设置了装饰颜色，则从保存数据中获取
                    : Custom.HSL2RGB(Random.Range(0.01f, 0.9f), 0.5f, 0.3f), // 否则随机生成颜色
            };

            // 如果有解锁信息，则更新颜色和缩放
            if (unlock is SandboxUnlock u)
            {
                result.hue = u.Data / 1000f;

                if (u.Data == 0)
                {
                    result.scaleX += 0.2f;
                    result.scaleY += 0.2f;
                }
            }

            return result; // 返回解析后的帽子实例
        }

        private static readonly CowBoyHatProperties properties = new();

        public override ItemProperties Properties(PhysicalObject forObject)
        {
            // 返回帽子的属性
            return properties;
        }
    }

    sealed class CowBoyHatIcon : Icon
    {
        // 返回帽子的颜色数据
        public override int Data(AbstractPhysicalObject apo)
        {
            return apo is CowBoyHatAbstract crate ? (int)(crate.hue * 1000f) : 0;
        }

        // 返回帽子的精灵颜色
        public override Color SpriteColor(int data)
        {
            return new Color(229 / 255f, 136 / 255f, 70 / 255f);
        }

        // 返回帽子的精灵名称
        public override string SpriteName(int data)
        {
            return "icon_CowBoyHat"; // 自动加载的资源名称
        }
    }

    public class CowBoyHatAbstract : AbstractPhysicalObject
    {
        public CowBoyHatAbstract(World world, WorldCoordinate pos, EntityID ID)
            : base(world, CowBoyHatFisob.AbstrCrate, null, pos, ID)
        {
            scaleX = 1; // 默认缩放
            scaleY = 1;
            saturation = 0.5f; // 默认饱和度
            hue = 0.9f; // 默认色调
        }

        public override void Realize()
        {
            base.Realize();
            if (realizedObject == null)
                realizedObject = new CowBoyHat(this); // 实现帽子对象
        }

        public float hue; // 色调
        public float saturation; // 饱和度
        public float scaleX; // X轴缩放
        public float scaleY; // Y轴缩放

        public bool setMainColor; // 是否设置主颜色
        public Color mainColor; // 主颜色
        public Color decorateColor; // 装饰颜色

        public string shapeID; // 帽子形状ID

        public override string ToString()
        {
            var color1 = Custom.colorToHex(mainColor);
            var color2 = Custom.colorToHex(decorateColor);

            // 返回保存字符串
            return this.SaveToString(
                $"{hue};{saturation};{scaleX};{scaleY};{setMainColor};{shapeID};{color1};{color2}"
            );
        }
    }

    sealed class CowBoyHatProperties : ItemProperties
    {
        // 设置可投掷性
        public override void Throwable(Player player, ref bool throwable) => throwable = true;

        // 设置抓取能力
        public override void Grabability(Player player, ref Player.ObjectGrabability grabability) =>
            grabability = Player.ObjectGrabability.OneHand;
    }

    public class CowBoyHat : PhysicalObject, IDrawable
    {
        // 佩戴者
        public PhysicalObject wearers;

        // 飞行方向
        public Vector2 rotation;
        public Vector2 lastRotation = Vector2.zero;

        // 水平角度从0到2为360度
        public float levelAngle = 360;

        // 旋转角度限制
        public static float RotatingLevel(float levelAngle)
        {
            while (levelAngle > 360 || levelAngle < 0)
            {
                if (levelAngle > 360)
                    levelAngle %= 360;
                if (levelAngle < 0)
                    levelAngle = 360 + levelAngle;
            }
            return levelAngle;
        }

        // 旋转装饰
        public void RotatingDexorate(float angle)
        {
            levelAngle += angle;
            while (levelAngle > 360 || levelAngle < 0)
            {
                if (levelAngle > 360)
                    levelAngle %= 360;
                if (levelAngle < 0)
                    levelAngle = 360 + levelAngle;
            }
        }

        public float minSpeed = 3; // 最小速度
        CowBoyHatAbstract Abst { get; }

        public bool setMainColor = false; // 是否设置主颜色
        public Color mainColor = Color.blue; // 主颜色
        public Color decorateColor; // 装饰颜色

        public string shapeID; // 帽子形状ID

        public float rotationSpeed; // 旋转速度

        public CowBoyHat(CowBoyHatAbstract abstr)
            : base(abstr)
        {
            Abst = abstr;
            this.setMainColor = abstr.setMainColor;

            this.shapeID = abstr.shapeID;

            this.mainColor = abstr.mainColor;
            this.decorateColor = abstr.decorateColor;

            // 随机选择帽子形状ID
            if (this.shapeID == null)
            {
                this.shapeID = abstr.shapeID = HatData
                    .HatsDictionary.ToArray()[Random.Range(0, HatData.HatsDictionary.Count - 1)]
                    .Value.id;
            }

            Debug.Log("[COWBOY]:HatSpawn:" + abstr.shapeID);

            float mass = 0.1f; // 质量
            var positions = new List<Vector2>();

            // 添加各个部件的基本位置
            positions.Add(Vector2.zero);
            positions.Add(Vector2.zero);

            // 根据添加的位置创建体块
            bodyChunks = new BodyChunk[positions.Count];
            for (int i = 0; i < bodyChunks.Length; i++)
            {
                bodyChunks[i] = new BodyChunk(
                    this,
                    i,
                    positions[i],
                    6,
                    mass / bodyChunks.Length * 1f
                );
            }

            var decorateIndex = bodyChunks.Length - 1;

            // 用于定位装饰
            bodyChunks[decorateIndex].rad = 0;
            bodyChunks[decorateIndex].mass = 0.02f;
            bodyChunks[decorateIndex].collideWithTerrain = false;

            bodyChunkConnections = new BodyChunkConnection[bodyChunks.Length - 1];

            for (int i = 0; i < bodyChunkConnections.Length; i++)
            {
                bodyChunkConnections[i] = new BodyChunkConnection(
                    bodyChunks[i],
                    bodyChunks[i + 1],
                    0f,
                    BodyChunkConnection.Type.Normal,
                    0.9f,
                    -1f
                );
            }

            // 这个物体的基础属性
            airFriction = 0.999f; // 空气摩擦
            surfaceFriction = 0.02f; // 表面摩擦
            waterFriction = 0.3f; // 水摩擦

            gravity = 0.85f; // 重力
            bounce = 1f; // 弹性
            collisionLayer = 1; // 碰撞层

            buoyancy = 0.999f; // 浮力
            GoThroughFloors = true; // 穿过地板
            canBeHitByWeapons = true; // 可以被武器击中
        }

        // 让被打了之后帽子掉落
        public override void HitByWeapon(Weapon weapon)
        {
            base.HitByWeapon(weapon);
            if (Weared)
            {
                wearers = null; // 如果被佩戴，清空佩戴者
            }
        }

        // 是否被戴着
        public bool Flying =>
            firstChunk.vel.magnitude > minSpeed && grabbedBy.Count == 0 && !Weared;
        public bool Weared => wearers != null; // 检查是否被佩戴

        public void WearersUpdate()
        {
            // 如果帽子有佩戴者而且佩戴者不存在就不再记录佩戴者
            if (wearers != null && wearers.slatedForDeletetion)
            {
                if (wearers is Player player)
                {
                    var exPlayer = player.GetCowBoyData();
                    exPlayer.UnstackHat(this);
                    Hat.RemoveHat(player);
                }
                wearers = null;
            }
            if (Weared && wearers is Player)
            {
                var player = (Player)wearers;
                bool flag2 =
                    player.wantToPickUp > 0
                    && player.input[0].y < 0
                    && player.grasps[0] == null
                    && player.grasps[1] == null;
                if (flag2)
                {
                    var exPlayer = player.GetCowBoyData();

                    // 只取下列表中最后一个帽子（即索引最大的帽子）
                    if (exPlayer.hatList.Count > 0)
                    {
                        // 获取最后一个帽子
                        CowBoyHat lastHat = exPlayer.hatList[exPlayer.hatList.Count - 1];

                        // 如果当前帽子不是最后一个帽子，则不取下
                        if (lastHat != this)
                        {
                            return;
                        }

                        // 如果是最后一个帽子，则取下
                        room.PlaySound(SoundID.Big_Spider_Spit, firstChunk);

                        if (Hat.TryGetHat(player, out var abstractHatWearStick) && abstractHatWearStick != null)
                        {
                            abstractHatWearStick.Deactivate(); // 取消佩戴
                            Hat.RemoveHat(player);
                        }

                        // 从玩家的帽子列表中移除
                        exPlayer.UnstackHat(this);

                        wearers = null; // 清空佩戴者
                    }
                }
            }
            if (Weared && Vector2.Distance(wearers.firstChunk.pos, firstChunk.pos) > 300)
            {
                if (wearers is Player player)
                {
                    var exPlayer = player.GetCowBoyData();

                    // 只取下列表中最后一个帽子（即索引最大的帽子）
                    if (exPlayer.hatList.Count > 0)
                    {
                        // 获取最后一个帽子
                        CowBoyHat lastHat = exPlayer.hatList[exPlayer.hatList.Count - 1];

                        // 如果当前帽子不是最后一个帽子，则不取下
                        if (lastHat != this)
                        {
                            return;
                        }

                        // 如果是最后一个帽子，则取下
                        room.PlaySound(SoundID.Big_Spider_Spit, firstChunk);

                        if (Hat.TryGetHat(player, out var abstractHatWearStick) && abstractHatWearStick != null)
                        {
                            abstractHatWearStick.Deactivate(); // 取消佩戴
                            Hat.RemoveHat(player);
                        }

                        // 从玩家的帽子列表中移除
                        exPlayer.UnstackHat(this);

                        wearers = null; // 清空佩戴者
                    }
                }
            }

            // 如果佩戴者还被记录着就执行下面的update
            if (Weared)
            {
                var distance = Vector2.Distance(wearers.firstChunk.pos, firstChunk.pos);

                firstChunk.vel =
                    Custom.LerpMap(distance, 1, 30, 1, 25)
                    * Custom.DirVec(firstChunk.pos, wearers.firstChunk.pos);

                this.CollideWithObjects = false; // 不与其他物体碰撞
            }
            else
            {
                this.CollideWithObjects = true; // 与其他物体碰撞
            }
        }

        public void ChangeOverlap(CowBoyHat otherHat)
        {
            try
            {
                // 只在帽子被佩戴时调用
                if (!Weared || otherHat == null || !otherHat.Weared || otherHat == this) return;

                // 确保两个帽子都在同一个玩家身上
                if (wearers != otherHat.wearers) return;

                // 获取玩家的帽子列表
                if (wearers is Player player)
                {
                    var exPlayer = player.GetCowBoyData();
                    if (exPlayer != null)
                    {
                        // 获取两个帽子的索引
                        int myIndex = exPlayer.hatList.IndexOf(this);
                        int otherIndex = exPlayer.hatList.IndexOf(otherHat);

                        if (myIndex != -1 && otherIndex != -1)
                        {
                            // 根据索引调整精灵的层级
                            foreach (var sprite in sLeaser.sprites)
                            {
                                if (sprite != null)
                                {
                                    // 索引越大，层级越高
                                    if (myIndex > otherIndex)
                                    {
                                        sprite.MoveInFrontOfOtherNode(otherHat.sLeaser.sprites[0]);
                                    }
                                }
                            }

                            Debug.Log($"[CowBoySlug] 调整帽子层级 - {abstractPhysicalObject.ID}(索引:{myIndex}) 与 {otherHat.abstractPhysicalObject.ID}(索引:{otherIndex})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CowBoySlug] 调整帽子层级时出错: {ex}");
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            RotatingDexorate(rotationSpeed); // 旋转装饰
            this.lastRotation = this.rotation;
            rotationSpeed *= 0.8f; // 减少旋转速度

            WearersUpdate(); // 更新佩戴者状态

            if (firstChunk.vel.magnitude > minSpeed && grabbedBy.Count == 0 && !Weared)
            {
                firstChunk.vel = firstChunk.vel.magnitude * rotation.normalized; // 根据旋转方向调整速度
                rotation = (rotation - new Vector2(0, g * 0.01f)).normalized; // 更新旋转方向

                // 让他在飞的时候提升转速
                rotationSpeed += (firstChunk.vel.x * 0.6f);
            }
            else
            {
                rotation = Vector2.up; // 重置旋转方向
            }

            if (!Weared && g > 0)
            {
                firstChunk.vel.y *= 0.55f; // 减少下落速度
            }
        }

        public override void TerrainImpact(
            int chunk,
            IntVector2 direction,
            float speed,
            bool firstContact
        )
        {
            base.TerrainImpact(chunk, direction, speed, firstContact);
            if (direction.x != 0 && direction.y == 0 && Flying)
            {
                rotation.x *= -1; // 反转旋转方向
                room.PlaySound(SoundID.Weapon_Skid, firstChunk, false, 0.4f, 0.4f);
            }
        }

        public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            base.Collide(otherObject, myChunk, otherChunk);
            if (
                otherObject is Player
                && this.firstChunk.vel.y - otherObject.firstChunk.vel.y < 0
                && this.firstChunk.pos.y > otherObject.firstChunk.pos.y
                && otherChunk == 0
            )
            {
                WearHat(otherObject); // 佩戴帽子
            }
        }

        public void WearHat(PhysicalObject wearer)
        {
            if (!Weared)
            {
                room.PlaySound(SoundID.Big_Spider_Spit, firstChunk);
                wearers = wearer; // 设置佩戴者
                if (wearer is Player player)
                {
                    // 使用新的AddHat方法添加帽子和玩家的关系
                    Hat.AddHat(
                        player,
                        new AbstractHatWearStick(
                            this.abstractPhysicalObject,
                            player.abstractPhysicalObject as AbstractCreature
                        )
                    );

                    // 将帽子添加到玩家的帽子列表中
                    var exPlayer = player.GetCowBoyData();
                    exPlayer.StackHat(this);

                 
                }
            }
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);

            Vector2 center = placeRoom.MiddleOfTile(abstractPhysicalObject.pos);

            for (int i = 0; i < bodyChunks.Length; i++)
            {
                bodyChunks[i].HardSetPosition(center + Vector2.up); // 设置位置
            }
        }

        private RoomCamera.SpriteLeaser sLeaser;

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            this.sLeaser = sLeaser;  // 保存SpriteLeaser引用
            sLeaser.sprites = new FSprite[4];

            for (int i = 0; i < 2; i++)
                sLeaser.sprites[i] = new FSprite("Circle20"); // 创建基本精灵

            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
                new TriangleMesh.Triangle(0, 1, 2),
                new TriangleMesh.Triangle(1, 2, 3),
            };

            sLeaser.sprites[2] = new TriangleMesh("Futile_White", tris, true, true); // 创建白色三角形精灵

            if (HatData.HatsDictionary.TryGetValue(Abst.shapeID, out var hatData))
            {
                var imagiName =
                    LoadHats.cowBoyHatFolderName
                    + Path.DirectorySeparatorChar
                    + hatData.sprite_name; // 加载帽子图像
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

            AddToContainer(sLeaser, rCam, null); // 添加到容器
        }

        public void ApplyPalette(
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            RoomPalette palette
        )
        {
            if (!setMainColor)
            {
                Color color = Color.Lerp(
                    palette.blackColor,
                    palette.skyColor,
                    Random.Range(0.01f, 0.2f)
                );
                mainColor = color; // 设置主颜色
                setMainColor = true;
                Abst.mainColor = this.mainColor;
                Abst.setMainColor = this.setMainColor;

                Abst.shapeID = this.shapeID; // 设置形状ID
            }
            foreach (var sprite in sLeaser.sprites)
                sprite.color = mainColor; // 应用颜色

            decorateColor = decorateColor == null ? Color.white : decorateColor;

            Abst.setMainColor = this.setMainColor;
            Abst.mainColor = this.mainColor;
            Abst.decorateColor = this.decorateColor;
            Abst.shapeID = this.shapeID;
        }

        public void AddToContainer(
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            FContainer? newContainer
        )
        {
            newContainer ??= rCam.ReturnFContainer("Items"); // 获取容器

            // 如果帽子被佩戴，使用特殊的容器
            if (Weared && wearers is Player player)
            {
                // 获取帽子在列表中的索引位置
                var exPlayer = player.GetCowBoyData();
                int hatIndex = exPlayer.hatList.IndexOf(this);

                // 使用特殊的容器，确保帽子按照正确的顺序显示
                // 下标更大的帽子（后添加的）显示在下标更小的帽子（先添加的）之上
                newContainer = rCam.ReturnFContainer("HUD");

                // 先从容器中移除所有精灵，然后按照正确的顺序重新添加
                foreach (FSprite fsprite in sLeaser.sprites)
                {
                    if (fsprite.container != null)
                    {
                        fsprite.RemoveFromContainer();
                    }
                }
            }

            // 添加精灵到容器
            foreach (FSprite fsprite in sLeaser.sprites)
            {
                newContainer.AddChild(fsprite);

                // 如果帽子被佩戴，将精灵移到容器的最前面
                if (Weared)
                {
                    fsprite.MoveToFront();
                }
            }
        }

        public void DrawSprites(
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            Vector2 camPos
        )
        {
            if (Weared)
            {
                WearDraw(sLeaser, rCam, timeStacker, camPos);
                return;
            }

            var rotationNow = Vector2.Lerp(lastRotation, rotation, timeStacker);
            if (firstChunk.vel.magnitude > minSpeed)
            {
                rotationNow =
                    Custom.PerpendicularVector(rotationNow)
                    * (Custom.VecToDeg(rotationNow) > 0.5 ? 1 : -1);
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

            DrawDecorate(
                sLeaser.sprites,
                sLeaser.sprites[3] as TriangleMesh,
                rotationNow,
                centerPos - camPos,
                timeStacker
            );

            if (slatedForDeletetion || room != rCam.room)
                sLeaser.CleanSpritesAndRemove(); // 清理精灵
        }

        public float PlayerHeadInfo(Player player, float timeStacker, ref Vector2 headPosition)
        {
            var playerGra = player.graphicsModule as PlayerGraphics;

            float num =
                0.5f
                + 0.5f
                    * Mathf.Sin(
                        Mathf.Lerp(playerGra.lastBreath, playerGra.breath, timeStacker)
                            * 3.1415927f
                            * 2f
                    );

            Vector2 vector = Vector2.Lerp(
                playerGra.drawPositions[0, 1],
                playerGra.drawPositions[0, 0],
                timeStacker
            );
            Vector2 vector2 = Vector2.Lerp(
                playerGra.drawPositions[1, 1],
                playerGra.drawPositions[1, 0],
                timeStacker
            );
            headPosition = Vector2.Lerp(playerGra.head.lastPos, playerGra.head.pos, timeStacker);

            if (player.aerobicLevel > 0.5f)
            {
                vector +=
                    Custom.DirVec(vector2, vector)
                    * Mathf.Lerp(-1f, 1f, num)
                    * Mathf.InverseLerp(0.5f, 1f, player.aerobicLevel)
                    * 0.5f;
                headPosition -=
                    Custom.DirVec(vector2, vector)
                    * Mathf.Lerp(-1f, 1f, num)
                    * Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, player.aerobicLevel), 1.5f)
                    * 0.75f;
            }

            float num3 = Custom.AimFromOneVectorToAnother(
                Vector2.Lerp(vector2, vector, 0.5f),
                headPosition
            );
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
            return num3; // 返回头部旋转角度
        }

        public void WearDraw(
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            Vector2 camPos
        )
        {
            // 调整层级
            if (wearers is Player player2)
            {

                var exPlayer = player2.GetCowBoyData();
                if (exPlayer != null)
                {
                    int myIndex = exPlayer.hatList.IndexOf(this);
                    if (myIndex != -1)
                    {
                        // 遍历所有帽子
                        for (int i = 0; i < exPlayer.hatList.Count; i++)
                        {
                            if (i == myIndex) continue; // 跳过自己

                            CowBoyHat otherHat = exPlayer.hatList[i];
                            if (otherHat?.sLeaser?.sprites != null && otherHat.sLeaser.sprites.Length > 0)
                            {
                                // 根据索引调整精灵的层级
                                foreach (var sprite in sLeaser.sprites)
                                {
                                    if (sprite != null)
                                    {
                                        // 索引越大，层级越高
                                        if (myIndex > i)
                                        {
                                            sprite.MoveInFrontOfOtherNode(otherHat.sLeaser.sprites[0]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var body = firstChunk;
            Vector2 headPosition = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            float rotation = PlayerHeadInfo(wearers as Player, timeStacker, ref headPosition);

            headPosition -= camPos;
            Vector2 vector =
                headPosition + Custom.DegToVec(rotation + FixHatRotation(wearers as Player)) * (7f);

            // 获取帽子在列表中的索引位置
            int hatIndex = 0;
            float heightOffset = 0f;

            if (wearers is Player player3)
            {
                var exPlayer = player3.GetCowBoyData();
                hatIndex = exPlayer.hatList.IndexOf(this);

                // 根据索引计算高度偏移，每顶帽子向上偏移8个单位
                heightOffset = hatIndex * 8f;
            }

            // 根据高度偏移调整帽子位置
            vector += Custom.DegToVec(rotation + FixHatRotation(wearers as Player)) * heightOffset;

            for (int i = 0; i < 2; i++)
            {
                Vector2 showPos = vector;
                Vector2 showPos2 =
                    showPos
                    - Custom.DegToVec(rotation + FixHatRotation(wearers as Player)) * (6 - 4);
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

                spr.color = mainColor; // 设置颜色
                DrawDecorate(
                    sLeaser.sprites,
                    sLeaser.sprites[3] as TriangleMesh,
                    Custom.DegToVec(rotation + FixHatRotation(wearers as Player)).normalized,
                    vector,
                    levelAngle + FixHatLevelAngle(wearers as Player),
                    timeStacker
                );
            }
        }

        public void DrawDecorate(
            FSprite[] sprites,
            TriangleMesh mesh,
            Vector2 rotationDir,
            Vector2 centerPos,
            float timeStacker
        )
        {
            DrawDecorate(sprites, mesh, rotationDir, centerPos, levelAngle, timeStacker);
        }

        public void DrawDecorate(
            FSprite[] sprites,
            TriangleMesh mesh,
            Vector2 rotationDir,
            Vector2 centerPos,
            float levelAngle,
            float timeStacker
        )
        {
            levelAngle = RotatingLevel(levelAngle);
            sprites[2].color = Color.Lerp(decorateColor, mainColor, 0.5f); // 设置装饰颜色
            mesh.color = decorateColor; // 设置网格颜色

            // 绘制绑带
            var strap = sprites[2] as TriangleMesh;
            var per = Custom.PerpendicularVector(-rotationDir);
            strap.MoveVertice(0, centerPos + (per * -7) + (rotationDir * -2));
            strap.MoveVertice(1, centerPos + (per * -6) + (rotationDir * 0));
            strap.MoveVertice(2, centerPos + (per * 7) + (rotationDir * -2));
            strap.MoveVertice(3, centerPos + (per * 6) + (rotationDir * 0));

            var size = 7f; // 装饰大小

            // 装饰最远伸出的距离
            Vector2 brim = Custom.PerpendicularVector(rotationDir) * (this.bodyChunks[0].rad) * 2f;

            // 装饰的位置
            var decoratePos = Vector2.Lerp(
                centerPos - brim,
                centerPos + brim,
                levelAngle >= 180 ? 2 - levelAngle / 180 : levelAngle / 180
            );

            decoratePos += rotationDir * 2;

            if (levelAngle >= 180)
            {
                mesh.MoveVertice(2, decoratePos - rotationDir * size - per * size);
                mesh.MoveVertice(3, decoratePos + rotationDir * size - per * size);
                mesh.MoveVertice(0, decoratePos - rotationDir * size + per * size);
                mesh.MoveVertice(1, decoratePos + rotationDir * size + per * size);

                mesh.MoveBehindOtherNode(sprites[0]); // 绘制在后面
            }
            else
            {
                mesh.MoveVertice(0, decoratePos - rotationDir * size - per * size);
                mesh.MoveVertice(1, decoratePos + rotationDir * size - per * size);
                mesh.MoveVertice(2, decoratePos - rotationDir * size + per * size);
                mesh.MoveVertice(3, decoratePos + rotationDir * size + per * size);

                mesh.MoveInFrontOfOtherNode(sprites[2]); // 绘制在前面
            }
        }

        public static float FixHatRotation(Player player)
        {
            // 根据玩家状态调整帽子旋转
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.mainBodyChunk.pos.x > player.bodyChunks[1].pos.x)
                {
                    return -70; // 向左旋转
                }
                else
                {
                    return 70; // 向右旋转
                }
            }
            else if (player.bodyMode == Player.BodyModeIndex.Stand && player.input[0].x > 0)
            {
                return -20; // 向左旋转
            }
            else if (player.bodyMode == Player.BodyModeIndex.Stand && player.input[0].x < 0)
            {
                return 20; // 向右旋转
            }
            else
            {
                return 0; // 不旋转
            }
        }

        public static float FixHatLevelAngle(Player player)
        {
            // 根据玩家状态调整帽子水平角度
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.mainBodyChunk.pos.x > player.bodyChunks[1].pos.x)
                {
                    return -90; // 向左倾斜
                }
                else
                {
                    return 90; // 向右倾斜
                }
            }
            else if (player.bodyMode == Player.BodyModeIndex.Stand && player.input[0].x > 0)
            {
                return -90; // 向左倾斜
            }
            else if (player.bodyMode == Player.BodyModeIndex.Stand && player.input[0].x < 0)
            {
                return 90; // 向右倾斜
            }
            else
            {
                return 0; // 不倾斜
            }
        }

        public override void Destroy()
        {
            // 如果帽子被佩戴，从玩家的帽子列表中移除
            if (Weared && wearers is Player player)
            {
                var exPlayer = player.GetCowBoyData();
                exPlayer.UnstackHat(this);
                Hat.RemoveHat(player);
            }

            base.Destroy();
        }
    }
}
