using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using CowBoySlug;
using SlugBase.DataTypes;
using SlugBase;
using Fisobs.Core;
using CowBoySlug.Menu;
using CowBoySlug.CowBoy.Ability.RopeUse;

using MonoMod.ModInterop;
using static CowBoySLug.GhostPlayerImports;
using CatPunchPunchDP;
using CowBoySlug.CatPunch;
using CowBoySlug.ExAbility;

namespace CowBoySLug
{
    [BepInPlugin(MOD_ID, "CowBoySLug.ShanKa", "0.2.36")]
    class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "CowBoySLug.ShanKa";

        public static readonly PlayerFeature<bool> RockShot = PlayerBool("cowboyslug/rock_shot");//扔石头


        //public static readonly PlayerFeature<bool> HaveScarf = PlayerBool("cowboyslug/scarf");//有围巾

        //public static readonly PlayerColor ScarfColor = new PlayerColor("Scarf");//围巾颜色
        


        #region 检查其他mod是否启用
        //检查GhostPlayer是否启用
        public static bool enableGhostPlayer = false;
        //检查猫拳是否启用
        public static bool enableCatPunchPunch = false;

        #endregion
        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;

            Content.Register(new CowBoyHatFisob());

            //加载GhostPlayer扩展
            typeof(GhostPlayerImports).ModInterop();
        }
        public  static  RemixMenu menu = new RemixMenu();
        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            LoadHats.Hook();

            RopeSpear.Hook();
            
            Hat.Hook();

            RopeMaster.Hook();

            PlayerHook.Hook();
            PlayerGraphicsHook.Hook();


            Hands.Hook();


            SewHook.Hook();

            SuperShootModule.OnHook();
            DroneJumpHook.Hook();


            //Camouflage.Hook();
            WhiteDropWorm.Hook();

            //鬼玩联动
            if (!enableGhostPlayer)
            {
                foreach (var mod in ModManager.ActiveMods)
                {
                    if (mod.id == "ghostplayer") enableGhostPlayer = true;
                }
                if (enableGhostPlayer)
                {
                    GhostPlayerImports.Register(typeof(CowBoyData));
                }
            }

            ////猫拳联动
            //if (!enableCatPunchPunch)
            //{
            //    foreach (var mod in ModManager.ActiveMods)
            //    {
            //        if (mod.id == "harvie.catpunchpunch") enableCatPunchPunch = true;
            //    }
            //    if (enableCatPunchPunch)
            //    {
            //        PunchExtender.RegisterPunch(new HatPunch());
            //    }
            //}

            try
            {
                MachineConnector.SetRegisteredOI("CowBoySLug.ShanKa", menu);
            }
            catch (Exception ex)
            {
                Debug.Log($"CowBoySLug.ShanKa options failed init error {menu}{ex}");
            }
            
        }
        private void LoadResources(RainWorld rainWorld)
        {
            Futile.atlasManager.LoadAtlas("atlases/CowBoyHead");
            Futile.atlasManager.LoadAtlas("fisobs/icon_CowBoyHat");
        }


    }

    //GhostPlayer联机API
    [ModImportName("GhostPlayerExtension")]
    public static class GhostPlayerImports
    {
        public delegate bool TryGetImportantValueDel(Type type, out object obj);
        public delegate bool TryGetValueForPlayerDel(Player player, Type type, out object obj);

        public static Func<Type, bool> Register;

        public static TryGetValueForPlayerDel TryGetValueForPlayer;
        public static Func<Player, object, bool> TrySetValueForPlayer;

        public static TryGetImportantValueDel TryGetImportantValue;
        public static Func<object, bool, bool> TrySendImportantValue;

        public static Func<Player, string, bool> SendMessage;
        //public static Func<Player, string, bool> SendConsoleMessage;

        public static Action<Action<string[]>> RegisterCommandEvent;

        public static Func<Player, int> GetPlayerNetID;
        public static Func<Player, string> GetPlayerNetName;
        public static Func<Player, bool> IsNetworkPlayer;
        public static Func<bool> IsConnected;

        //public static Func<string, string> GetPlayerRoom;
        //public static Func<string, string> GetPlayerRegion;

    }
    public class CowBoyData
    {
        public int id;
        public byte type=0;
    }
}

