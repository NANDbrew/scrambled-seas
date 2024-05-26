#if UMM
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace ScrambledSeas
{
    internal static class Main
    {
        public static bool pluginEnabled;
        public static UnityModManager.ModEntry.ModLogger logger;

        public static ScrambledSeasSaveContainer saveContainer = new ScrambledSeasSaveContainer();

        public static Log(string msg)
        {
            Main.logger.Log(msg);
        }

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            pluginEnabled = value;
            return true;
        }
    }
}
#endif