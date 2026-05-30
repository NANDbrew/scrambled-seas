using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using static OVRHaptics;

namespace ScrambledSeas
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.nandbrew.borderexpander", BepInDependency.DependencyFlags.SoftDependency)]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "com.nandbrew.scrambledseas";
        public const string NAME = "Scrambled Seas: NAND edition";
        public const string VERSION = "7.1.10";

        internal const int defaultMinArchipelagoSeparation = 30000;
        internal const int defaultIslandSpread = 5000;
        internal const int defaultMinIslandSeparation = 1500;

        internal static ConfigEntry<bool> random_Enabled;
        internal static ConfigEntry<bool> saveCoordsToJSON_Enabled;
        internal static ConfigEntry<bool> eastwindFix;
        internal static ConfigEntry<bool> saveScrambleExternal;
        internal static ConfigEntry<DestinationHint> destinationHint;
        internal static ConfigEntry<int> cardinalPrecisionLevel;
        internal static ConfigEntry<int> coordinatePrecisionLevel;
        internal static ConfigEntry<bool> hideMissionDistance;

        public static bool borderExpander;
        public static bool loadExternal = false;
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

            random_Enabled = Config.Bind("Settings", "randomEN", true, "enable random.\nalso controlled by checkbox in 'new game' menu");
            saveCoordsToJSON_Enabled = Config.Bind("Settings", "saveCoordsToJSON", true, "save islands coords to JSON file for online map\nwrites the file when a save is started or loaded");
            eastwindFix = Config.Bind("World", "Eastwind Fix", true, new ConfigDescription("fix eastwind market position"));
            saveScrambleExternal = Config.Bind("Settings", "ExternalSave", false, new ConfigDescription("save and load island/archipelago offsets to xml file to allow manual editing"));
            destinationHint = Config.Bind("Settings", "Destination Hint", DestinationHint.None, new ConfigDescription(""));
            cardinalPrecisionLevel = Config.Bind("Settings", "Number of ordinal directions", 16, new ConfigDescription("Number of ordinal heading directions given in the mission screen.", new AcceptableValueList<int>(8, 16, 32)));
            coordinatePrecisionLevel = Config.Bind("Settings", "Coordinate precision", 0, new ConfigDescription("Number of decimal places in destination coordinates", new AcceptableValueRange<int>(0, 2)));
            hideMissionDistance = Config.Bind("Settings", "Hide mission distance", false, new ConfigDescription("Intended for use with Destination Hint"));

            borderExpander = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.nandbrew.borderexpander");

            AssetTools.LoadAssetBundles();
        }
        
        public static void Log(string msg)
        {
            Main.logSource.LogInfo(msg);
        }

        internal static float GetWorldScale()
        {
            return saveContainer.minArchipelagoSeparation / defaultMinArchipelagoSeparation;
        }
        internal static float GetArchipelagoScale()
        {
            return saveContainer.islandSpread / defaultIslandSpread;
        }

    }
    internal enum DestinationHint
    {
        None = 0,
        Heading = 1,
        Coords = 2,
    }
}
