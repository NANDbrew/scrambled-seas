#if BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SailwindModdingHelper;
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
    [BepInDependency(SailwindModdingHelperMain.GUID, "2.0.0")]
    public class Main : BaseUnityPlugin
    {
        public const string GUID = "com.vitalijbeam.scrambledseas";
        public const string NAME = "Scrambled Seas Fork";
        public const string VERSION = "6.0.2";

        internal static ConfigEntry<bool> random_Enabled;
        internal static ConfigEntry<bool> hideDestinationCoords_Enabled;
        internal static ConfigEntry<bool> saveCoordsToJSON_Enabled;


        public readonly static bool pluginEnabled = true;

        public ScrambledSeasSaveContainer _saveContainer = new ScrambledSeasSaveContainer();

        public static ScrambledSeasSaveContainer saveContainer { get { return instance._saveContainer; } set { instance._saveContainer = value; } }

        public static Main instance;

        internal static ManualLogSource logSource;

        private void Awake()
        {
            instance = this;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
            random_Enabled = Config.Bind("Settings", "randomEN", true, "enable random");
            hideDestinationCoords_Enabled = Config.Bind("Settings", "hideDestinationCoords", true, "hide cestination coords in missions");
            saveCoordsToJSON_Enabled = Config.Bind("Settings", "saveCoordsToJSON", true, "save islands coords to JSON file");
            logSource = Logger;
        }
        
        public static void Log(string msg)
        {
            Main.logSource.LogInfo(msg);
        }
    }
}
#endif