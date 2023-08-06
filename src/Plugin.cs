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

namespace CowBoySLug
{
    [BepInPlugin(MOD_ID, "CowBoySLug.ShanKa", "0.2.0")]
    class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "CowBoySLug.ShanKa";

        public static readonly PlayerFeature<bool> RockShot = PlayerBool("cowboyslug/rock_shot");//扔石头
        public static readonly PlayerFeature<bool> RopeMaster = PlayerBool("cowboyslug/rope_master");//控制绳子

        public static readonly PlayerFeature<bool> HaveScarf = PlayerBool("cowboyslug/scarf");//有围巾

        public static readonly PlayerColor ScarfColor = new PlayerColor("Scarf");//围巾颜色
        public static readonly PlayerColor RopeColor = new PlayerColor("Rope");//绳子颜色

        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            On.RainWorld.OnModsInit += RainWorld_OnModsInit1;


            Content.Register(new CowBoyHatFisob());
            // Put your custom hooks here!
            Hat.Hook();

            PlayerHook.Hook();
            PlayerGraphicsHook.Hook();
            RopeUseHook.Hook();

            SewHook.Hook();

            DroneJumpHook.Hook();


            //Camouflage.Hook();
            WhiteDropWorm.Hook();

        }
        public  static  RemixMenu menu = new RemixMenu();
        private void RainWorld_OnModsInit1(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            
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
        }

   
    }
}