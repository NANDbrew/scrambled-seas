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
        public const string GUID = "com.app24.scrambledseas";
        public const string NAME = "Scrambled Seas";
        public const string VERSION = "6.0.1";

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