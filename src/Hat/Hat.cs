using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Compatibility;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace CowBoySlug
{
    public static class Hat
    {
        // 每个玩家可能有多个帽子，每个帽子有自己独立的 AbstractHatWearStick
        // 这样每个帽子的 stick 都保留在 player.stuckObjects 中，
        // GetAllConnectedObjects() 才能找到所有帽子，管道过渡时才会正确移除它们
        public static Dictionary<Player, List<AbstractHatWearStick>> modules = new Dictionary<Player, List<AbstractHatWearStick>>();

        public static void AddHat(Player player, AbstractHatWearStick stick)
        {
            if (!modules.ContainsKey(player))
                modules[player] = new List<AbstractHatWearStick>();
            modules[player].Add(stick);
        }

        public static void RemoveHat(Player player, AbstractHatWearStick stick)
        {
            if (modules.TryGetValue(player, out var list))
            {
                stick.Deactivate();
                list.Remove(stick);
                if (list.Count == 0)
                    modules.Remove(player);
            }
        }

        /// <summary>
        /// 移除玩家所有的帽子 stick 连接（用于玩家被销毁时）
        /// </summary>
        public static void RemoveAllHats(Player player)
        {
            if (modules.TryGetValue(player, out var list))
            {
                foreach (var stick in list)
                    stick.Deactivate();
                modules.Remove(player);
            }
        }

        public static bool TryGetHatSticks(Player player, out List<AbstractHatWearStick> sticks)
        {
            return modules.TryGetValue(player, out sticks);
        }

        /// <summary>
        /// 佩戴帽子的本地执行:设置佩戴者、建立 stick、加入玩家帽子列表。
        /// 所有者端由碰撞触发,远端由 Meadow 状态同步(HatSyncData)重建。
        /// </summary>
        public static void WearHatLocal(Player player, CowBoyHat hat)
        {
            if (hat.Weared)
                return;

            hat.wearers = player;
            hat.myStick = new AbstractHatWearStick(
                hat.abstractPhysicalObject,
                player.abstractPhysicalObject as AbstractCreature
            );
            AddHat(player, hat.myStick);
            player.GetCowBoyData().StackHat(hat);
        }

        /// <summary>
        /// 摘下帽子的本地执行:拆除 stick、移出玩家帽子列表、清空佩戴者。
        /// </summary>
        public static void UnwearHatLocal(Player player, CowBoyHat hat)
        {
            if (hat.myStick != null)
            {
                RemoveHat(player, hat.myStick);
                hat.myStick = null;
            }
            player.GetCowBoyData().UnstackHat(hat);
            hat.wearers = null;
        }

        public static void Hook()
        {
            On.RainWorld.OnModsInit += LoadHatTextrue; // 读取帽子形状贴图
            On.Player.CanBeSwallowed += Hat_CanBeSwallowed; // 让帽子物品可以吞下

            On.Player.Grabability += Player_Grabability; // 让帽子戴着的时候不会被抓到

            On.Player.ThrowObject += Player_ThrowObject; // 扔帽子的时候运行的方法

            IL.SharedPhysics.TraceProjectileAgainstBodyChunks += PatchWeaponForHatTargeting; // 防止打中自己头上的帽子

            // On.Creature.PlaceInRoom += Creature_PlaceInRoom;

            // On.Player.ctor +=
            // PlayerHat_ctor;//用老的增加玩家贴图的方式来初始化绘制帽子

            // On.PlayerGraphics.InitiateSprites += Hat_InitiateSprites;
            // On.PlayerGraphics.AddToContainer += Hat_AddToContainer;
            // On.PlayerGraphics.DrawSprites += Hat_DrawSprites;
        }

        private static void PatchWeaponForHatTargeting(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, i => i.MatchStloc(7)))
            {
                // 在 stloc.s 7 之后插入代码

                // 加载 item 到栈顶
                c.Emit(OpCodes.Ldloc, 6); // 加载 item

                // 加载 exemptObject 到栈顶
                c.Emit(OpCodes.Ldarg, 6); // 加载 exemptObject

                // 调用自定义方法 ModifyFlag
                c.EmitDelegate<Func<PhysicalObject, object, bool>>(
                    (item, exemptObject) =>
                    {
                        var hat = item as CowBoyHat;
                        // 这里是你的自定义逻辑
                        if (hat != null && hat.wearers == exemptObject)
                        {
                            return true; // 修改 flag 的值
                        }
                        return false; // 保持原值
                    }
                );

                // 将结果存储回 flag
                c.Emit(OpCodes.Stloc, 7); // 存储 flag
            }
        }

        // private static void Creature_PlaceInRoom(On.Creature.orig_PlaceInRoom orig,
        // Creature self, Room placeRoom)
        //{
        //     orig.Invoke(self,placeRoom);
        //     if (self is Player&&AbstractHatWearStick.GetHatModule(self as
        //     Player).HaveHat)
        //     {   Player).Hatlist; foreach (var hat in hatList)
        //         {
        //             if (hat.wearers == self)
        //             {
        //                 //hat.PlaceInRoom(placeRoom);
        //                 UnityEngine.Debug.Log("帽子放下");
        //                 placeRoom.AddObject(hat);
        //                 for (int j = 0; j < hat.bodyChunks.Length; j++)
        //                 {
        //                     hat.bodyChunks[j].pos = self.mainBodyChunk.pos;
        //                     hat.bodyChunks[j].lastPos = self.mainBodyChunk.pos;
        //                     hat.bodyChunks[j].lastLastPos = self.mainBodyChunk.pos;
        //                     hat.bodyChunks[j].setPos = default(Vector2?);
        //                     hat.bodyChunks[j].vel *= 0f;
        //                 }
        //             }
        //         }

        //    }
        //}

        private static void Player_ThrowObject(
            On.Player.orig_ThrowObject orig,
            Player self,
            int grasp,
            bool eu
        )
        {
            // 如果扔的是帽子就改变一下帽子的飞行方向(远端玩家对象的扔帽是 Meadow 驱动的,飞行方向由状态同步覆盖)
            CowBoyHat hat = self.grasps[grasp].grabbed as CowBoyHat;
            if (hat != null && self.IsLocal())
            {
                if (self.input[0].x == 0 && self.input[0].y > 0)
                {
                    hat.rotation = new Vector2(self.input[0].x, 0.3f * self.input[0].y).normalized;
                }
                else
                {
                    hat.rotation = new Vector2(
                        self.ThrowDirection,
                        0.3f * self.input[0].y
                    ).normalized;
                }
            }

            orig.Invoke(self, grasp, eu);
        }

        private static Player.ObjectGrabability Player_Grabability(
            On.Player.orig_Grabability orig,
            Player self,
            PhysicalObject obj
        )
        {
            CowBoyHat hat = obj as CowBoyHat;
            if (hat != null)
            {
                // 如果帽子被戴着而且被自己戴着,就不能拿自己的帽子
                if (hat.wearers != null && hat.wearers == self)
                    return Player.ObjectGrabability.CantGrab;

                // 在这个位置直接修改帽子的拿取来让他不会戴着的时候被抓
            }
            return orig.Invoke(self, obj);
        }

        private static bool Hat_CanBeSwallowed(
            On.Player.orig_CanBeSwallowed orig,
            Player self,
            PhysicalObject testObj
        )
        {
            if (testObj is CowBoyHat)
            {
                return true;
            }
            else
            {
                return orig.Invoke(self, testObj);
            }
        }

        private static void LoadHatTextrue(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            hatAtlas = Futile.atlasManager.LoadAtlas("illustrations/hatSharp");
        }

        public static FAtlas hatAtlas;

        public static float FixHatRotation(PlayerGraphics self)
        {
            var player = self.player;
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
            else if (
                self.player.bodyMode == Player.BodyModeIndex.Stand
                && self.player.input[0].x > 0
            )
            {
                return -20;
            }
            else if (
                self.player.bodyMode == Player.BodyModeIndex.Stand
                && self.player.input[0].x < 0
            )
            {
                return 20;
            }
            else
            {
                return 0;
            }
        }

        public static void DrawHatDecoratePice(
            HatType shape,
            TriangleMesh sprite,
            Vector2 vector,
            Vector2 per,
            Vector2 dir,
            PlayerGraphics player
        )
        {
            switch (shape)
            {
                case HatType.Strap:
                    // 绑带b版本
                    sprite.MoveVertice(0, vector + (per * -7) + (dir * -2));
                    sprite.MoveVertice(1, vector + (per * -6) + (dir * 0));
                    sprite.MoveVertice(2, vector + (per * 7) + (dir * -2));
                    sprite.MoveVertice(3, vector + (per * 6) + (dir * 0));
                    break;
                case HatType.Feather:
                    // 羽毛版本
                    if (WhenHatInRight(player))
                    {
                        sprite.alpha = 0;
                        return;
                    }
                    sprite.MoveVertice(0, vector - (per * 3) + (dir * -2));
                    sprite.MoveVertice(1, vector - (per * 9) + (dir * 2));
                    sprite.MoveVertice(2, vector - (per * 7) + (dir * 4));
                    sprite.MoveVertice(3, vector - (per * 13) + (dir * 5));
                    break;
                default:
                    string type = ("hatSharp-" + shape);
                    if (hatAtlas._elementsByName.TryGetValue(type, out var element))
                    {
                        if (WhenHatInRight(player))
                        {
                            sprite.alpha = 0;
                            return;
                        }

                        sprite.SetElementByName(type);
                        sprite.UVvertices[0] = hatAtlas._elementsByName[type].uvBottomLeft;
                        sprite.UVvertices[1] = hatAtlas._elementsByName[type].uvTopLeft;
                        sprite.UVvertices[2] = hatAtlas._elementsByName[type].uvBottomRight;
                        sprite.UVvertices[3] = hatAtlas._elementsByName[type].uvTopRight;

                        sprite.MoveVertice(0, vector - (per * 0) + (dir * -5));
                        sprite.MoveVertice(1, vector - (per * 3) + (dir * 4));
                        sprite.MoveVertice(2, vector - (per * 11) + (dir * -6));
                        sprite.MoveVertice(3, vector - (per * 13) + (dir * 4));
                    }
                    else
                    {
                        sprite.SetElementByName("Futile_White");
                        sprite.MoveVertice(0, vector + (per * -7) + (dir * -2));
                        sprite.MoveVertice(1, vector + (per * -6) + (dir * 0));
                        sprite.MoveVertice(2, vector + (per * 7) + (dir * -2));
                        sprite.MoveVertice(3, vector + (per * 6) + (dir * 0));
                    }

                    break;
            }
        }

        public static bool WhenHatInRight(PlayerGraphics self)
        {
            if (self == null)
            {
                return false;
            }
            var player = self.player;

            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (player.mainBodyChunk.pos.x > player.bodyChunks[1].pos.x)
                {
                    return true;
                }
            }
            else if (
                self.player.bodyMode == Player.BodyModeIndex.Stand
                && self.player.input[0].x > 0
            )
            {
                return true;
            }
            return false;
        }
    }

    public class AbstractHatWearStick : AbstractPhysicalObject.AbstractObjectStick
    {
        public AbstractPhysicalObject AbsHat => this.A;
        public AbstractPhysicalObject Wearer => this.B;

        AbstractCreature wearer;
        AbstractPhysicalObject hat;

        //public List<CowBoyHat> Hatlist = new List<CowBoyHat>();
        //public bool HaveHat => Hatlist.Count > 0;

        public AbstractHatWearStick(AbstractPhysicalObject hat, AbstractCreature wearer)
            : base(hat, wearer)
        {
            this.hat = hat;
            this.wearer = wearer;
            if(wearer.realizedCreature!=null&& wearer.realizedCreature as Player != null)
            {

            }
        }


        // public static AbstractHatWearStick GetHatModule(Player player) =>
        // Hat.modules.GetValue(player, (p) => new AbstractHatWearStick());

        // public static AbstractHatWearStick GetHatModule(Player player) =>
        // Hat.modules.GetValue(player, (p) => new AbstractHatWearStick());
    }

    public enum HatType
    {
        None,
        Strap,
        Feather,
        Bone,
        Star,
        Grass,
        Bone2,
        Spider,
        Love,
        Eye,
        Moon,
        Bug,
    }
}
