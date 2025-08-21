#if BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
//using SailwindModdingHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

namespace ScrambledSeas
{
    [BepInPlugin(GUID, NAME, VERSION)]
    //[BepInDependency(SailwindModdingHelperMain.GUID, "2.0.0")]
    [BepInDependency("com.nandbrew.borderexpander", BepInDependency.DependencyFlags.SoftDependency)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "com.nandbrew.scrambledseas";
        public const string NAME = "Scrambled Seas Fork";
        public const string VERSION = "6.1.0";

        internal static ConfigEntry<bool> random_Enabled;
        internal static ConfigEntry<bool> hideDestinationCoords_Enabled;
        internal static ConfigEntry<bool> saveCoordsToJSON_Enabled;
        internal static ConfigEntry<bool> eastwindFix;

        public static bool borderExpander;

        public static bool pluginEnabled = true;

        public ScrambledSeasSaveContainer _saveContainer = new ScrambledSeasSaveContainer();

        public static ScrambledSeasSaveContainer saveContainer { get { return instance._saveContainer; } set { instance._saveContainer = value; } }

        public static Main instance;

        internal static ManualLogSource logSource;

        private void Awake()
        {
            instance = this;
            logSource = Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
            random_Enabled = Config.Bind("Settings", "randomEN", true, "enable random");
            hideDestinationCoords_Enabled = Config.Bind("Settings", "hideDestinationCoords", true, "hide destination coords in mission screen");
            saveCoordsToJSON_Enabled = Config.Bind("Settings", "saveCoordsToJSON", true, "save islands coords to JSON file");
            eastwindFix = Config.Bind("World", "Eastwind Fix", true, new ConfigDescription("fix eastwind market position"));

            borderExpander = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.nandbrew.borderexpander");


            AssetTools.LoadAssetBundles();
        }
        
        public static void Log(string msg)
        {
            Main.logSource.LogInfo(msg);
        }
    }
}
#endif