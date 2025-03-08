using System;
using BepInEx;
using CowBoySlug;
using CowBoySlug.Compatibility;
using CowBoySlug.CowBoy.Ability.RopeUse;
using CowBoySlug.ExAbility;
using CowBoySlug.Menu;
using Fisobs.Core;
using MonoMod.ModInterop;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;

namespace CowBoySLug
{
    [BepInPlugin(MOD_ID, "CowBoySLug.ShanKa", "0.2.36")]
    class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "CowBoySLug.ShanKa";

        public static readonly PlayerFeature<bool> RockShot = PlayerBool("cowboyslug/rock_shot"); //扔石头

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

        public static RemixMenu menu = new RemixMenu();

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            LoadHats.Hook();

            RopeSpear.Hook();

            Hat.Hook();

            //控制绳子的能力的hook
            RopeMaster.Hook();

            PlayerHook.Hook();
            PlayerGraphicsHook.Hook();

            Hands.Hook();

            SewHook.Hook();

            SuperShootModule.OnHook();

            //Camouflage.Hook();
            WhiteDropWorm.Hook();
            
            // 初始化兼容性管理器
            CowBoySlug.Compatibility.CompatibilityManager.Initialize();

            // if (!enableGhostPlayer)
            // // {
            //     foreach (var mod in ModManager.ActiveMods)
            //     {
            //         if (mod.id == "ghostplayer")
            //             enableGhostPlayer = true;
            //     }
            //     if (enableGhostPlayer)
            //     {
            //         GhostPlayerImports.Register(typeof(CowBoyData));
            //     }
            // }

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
        public byte type = 0;
    }
}
