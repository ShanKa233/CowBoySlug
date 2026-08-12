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
    [BepInPlugin(MOD_ID, "CowBoySLug.ShanKa", "0.2.65")] // 版本号在 modinfo.json 和 workshopdata.json 中更新
    class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "CowBoySLug.ShanKa";


        public static readonly PlayerFeature<bool> RockShot = PlayerBool("cowboyslug/rock_shot"); //扔石头
        // 能使用这个能力的词条
        // 绳子颜色
        public static readonly PlayerColor RopeColor = new PlayerColor("Rope");


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
            Content.Register(new CowBoyHatFisob());
            //兼容其他mod用的东西
            Compatibility.ModCompat_Helpers.InitModCompat();


            PlayerHook.Hook();
            PlayerGraphicsHook.Hook();

            CowBoySlug.Mechanics.RopeSkill.UserData.Hook();
            CowBoySlug.Mechanics.ShootSkill.SuperShootModule.OnHook();
            CowBoySlug.Mechanics.RopeSkill.RopeSpear.Hook();

            CowBoySlug.Mechanics.Hands.Hook();

            //控制绳子的能力的hook
            LoadHats.Hook();
            Hat.Hook();
            SewHook.Hook();



            WhiteDropWorm.Hook();

            MachineConnector.SetRegisteredOI("CowBoySLug.ShanKa", menu);

            // LizardOnBackHook.Hook();//测试使用之后需要删除
            // Camouflage.Hook();//迷彩之类的东西仅用于测试
        }
        private void LoadResources(RainWorld rainWorld)
        {
            Futile.atlasManager.LoadAtlas("atlases/CowBoyHead");
            Futile.atlasManager.LoadAtlas("fisobs/icon_CowBoyHat");
        }
    }

}
