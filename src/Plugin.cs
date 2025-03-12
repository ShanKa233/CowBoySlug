using System;
using BepInEx;
using CowBoySlug;
using CowBoySlug.Menu;
using Fisobs.Core;
using MonoMod.ModInterop;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Reflection;
using System.Linq;
using SlugBase.DataTypes;
using UnityEngine;

namespace CowBoySLug
{
    [BepInPlugin(MOD_ID, "CowBoySLug.ShanKa", "0.2.50")] // 版本号在 modinfo.json 和 workshopdata.json 中更新
    class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "CowBoySLug.ShanKa";

        public static readonly PlayerFeature<bool> RockShot = PlayerBool("cowboyslug/rock_shot"); //扔石头
        // 能使用这个能力的词条
        // 绳子颜色
        public static readonly PlayerColor RopeColor = new PlayerColor("Rope");



        //public static readonly PlayerFeature<bool> HaveScarf = PlayerBool("cowboyslug/scarf");//有围巾

        //public static readonly PlayerColor ScarfColor = new PlayerColor("Scarf");//围巾颜色

        #region 检查其他mod是否启用
        //检查猫拳是否启用
        public static bool enableCatPunchPunch = false;

        //检查Rain-Meadow是否启用
        public static bool enableRainMeadow = false;

        // Rain-Meadow程序集
        public static Assembly rainMeadowAssembly = null;
        #endregion

        // 插件实例
        public static Plugin instance;

        // Add hooks
        public void OnEnable()
        {
            instance = this;
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;

            Content.Register(new CowBoyHatFisob());
        }

        public static RemixMenu menu = new RemixMenu();

        public bool IsInit { get; private set; }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            
            if (IsInit)
            {
                return;
            }

            IsInit = true;

            // init
            // 检查其他mod是否启用
            Compatibility.ModCompat_Helpers.InitModCompat();

            CowBoySlug.Mechanics.RopeSkill.UserData.Hook();
            CowBoySlug.Mechanics.ShootSkill.SuperShootModule.OnHook();

            LoadHats.Hook();
            CowBoySlug.Mechanics.RopeSkill.RopeSpear.Hook();

            Hat.Hook();

            //控制绳子的能力的hook

            PlayerHook.Hook();
            PlayerGraphicsHook.Hook();

            CowBoySlug.Mechanics.Hands.Hook();

            SewHook.Hook();


            // Camouflage.Hook();
            WhiteDropWorm.Hook();

            MachineConnector.SetRegisteredOI("CowBoySLug.ShanKa", menu);

            Debug.Log("[CowBoySlug] 初始化完成");
        }
        private void LoadResources(RainWorld rainWorld)
        {
            Futile.atlasManager.LoadAtlas("atlases/CowBoyHead");
            Futile.atlasManager.LoadAtlas("fisobs/icon_CowBoyHat");
        }
    }

}
