using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using System.Reflection;
using System.Collections.Generic;

// This is good boilerplate code to paste into all your mods that need to store data in the save file:
public static class SaveFileHelper
{
    // How to use: 
    // Main.myModsSaveContainer = SaveFileHelper.Load<MyModsSaveContainer>("MyModName");
    public static T Load<T>(this string modName) where T : new()
    {
        string xmlStr;
        if (GameState.modData != null && GameState.modData.TryGetValue(modName, out xmlStr)) {
            Debug.Log("Proceeding to parse save data for " + modName);
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (System.IO.StringReader textReader = new System.IO.StringReader(xmlStr)) {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }
        Debug.Log("Cannot load data from save file. Using defaults for " + modName);
        return new T();
    }

    // How to use:
    // SaveFileHelper.Save(Main.myModsSaveContainer, "MyModName");
    public static void Save<T>(this T toSerialize, string modName)
    {
        System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
        using (System.IO.StringWriter textWriter = new System.IO.StringWriter()) {
            xmlSerializer.Serialize(textWriter, toSerialize);
            GameState.modData[modName] = textWriter.ToString();
            Debug.Log("Packed save data for " + modName);
        }
    }
}

namespace ScrambledSeas
{
    // This contains all the variables we will need to store in the save file. 
    // Default values are defined for saves that haven't seen this mod yet.
    public class ScrambledSeasSaveContainer
    {
        public int worldScramblerSeed { get; set; } = 0;
        public int archipelagoSpread { get; set; } = 50000;
        public int islandSpread { get; set; } = 10000;
        public int minArchipelagoSeparation { get; set; } = 30000;
        public int minIslandSeparation { get; set; } = 2000;
    }

    static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry.ModLogger logger;
        public static Vector3[] islandDisplacements = new Vector3[23];
        public static Vector3[] islandOrigins = new Vector3[23];
        public static string[] islandNames = new string[23];
        public static List<Region> regions = new List<Region>();
        public static ScrambledSeasSaveContainer mySaveContainer = new ScrambledSeasSaveContainer();

        static bool Load(UnityModManager.ModEntry modEntry)
        {   
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }
    }

    [HarmonyPatch(typeof(IslandHorizon), "Start")]
    static class IslandPatch
    {
        private static void Prefix(IslandHorizon __instance)
        {
            if (Main.enabled && __instance.islandIndex > 0) {
                Main.islandNames[__instance.islandIndex - 1] = __instance.gameObject.name;
                Main.islandOrigins[__instance.islandIndex - 1] = __instance.gameObject.transform.localPosition;
            }
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "SaveModData")]
    static class SavePatch
    {
        private static void Postfix()
        {
            if (Main.enabled) {
                SaveFileHelper.Save(Main.mySaveContainer, "ScrambledSeas");
            }
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "LoadModData")]
    static class LoadGamePatch
    {
        private static void Postfix()
        {
            if (Main.enabled) {
                //Load entire ScrambledSeasSaveContainer from save file
                Main.mySaveContainer = SaveFileHelper.Load<ScrambledSeasSaveContainer>("ScrambledSeas");
                //Re-generate world for the saved randomizer params
                WorldScrambler.Scramble();
            }
        }
    }

    [HarmonyPatch(typeof(StartMenu), "StartNewGame")]
    static class StartNewGamePatch
    {
        private static void Prefix(ref int ___currentRegion, ref Transform ___startApos, ref Transform ___startEpos, ref Transform ___startMpos)
        {
            if (Main.enabled) {
                //Create a randomized world with a new seed
                Main.mySaveContainer.worldScramblerSeed = (int)System.DateTime.Now.Ticks;
                WorldScrambler.Scramble();
                //Move player start positions to new island locations
                ___startApos.position += Main.islandDisplacements[2];
                ___startEpos.position += Main.islandDisplacements[10];
                ___startMpos.position += Main.islandDisplacements[20];
            }
        }
    }

    [HarmonyPatch(typeof(StartMenu), "MovePlayerToStartPos")]
    static class MovePlayerPatch
    {
        private static void Prefix(Transform startPos, StartMenu __instance, ref Transform ___playerObserver, ref GameObject ___playerController)
        {
            if (Main.enabled) {
                //Teleport player to shifted starting position
                ___playerObserver.transform.parent = __instance.gameObject.transform.parent;
                ___playerObserver.position = startPos.position;
                ___playerController.transform.position = startPos.position;
                //This will be followed by an animation performed by Juicebox.juice.TweenPosition(), but it messes the position up. I've disabled it below...
            }
        }
    }

    [HarmonyPatch(typeof(Juicebox), "TweenPosition", new System.Type[] { typeof(GameObject), typeof(Vector3), typeof(float), typeof(JuiceboxTween) })]
    static class TweenPatch
    {
        private static bool Prefix()
        {
            if (Main.enabled) {
                //This just disables the original method completely. Otherwise, it glitches out while moving the player over a large distance.
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StarterSet), "InitiateStarterSet")]
    static class StarterSetPatch
    {
        private static void Prefix(StarterSet __instance)
        {
            if (Main.enabled) {
                // Increase starting gold to 500
                PlayerGold.gold += 400;
                //Figure out which island we start on
                Vector3 startOffset = new Vector3();
                if (__instance.region == PortRegion.alankh) {
                    startOffset = Main.islandDisplacements[2];
                }
                if (__instance.region == PortRegion.emerald) {
                    startOffset = Main.islandDisplacements[10];
                }
                if (__instance.region == PortRegion.medi) {
                    startOffset = Main.islandDisplacements[20];
                }
                //Move starter set items to new island location
                foreach (object starterItem in __instance.gameObject.transform) {
                    Transform tr = (Transform)starterItem;
                    tr.transform.Translate(startOffset, Space.World);
                }
            }
        }
    }



    [HarmonyPatch(typeof(PlayerReputation), "GetMaxDistance")]
    static class ReputationPatch
    {
        private static void Postfix(ref float __result)
        {
            //Islands tend to be farther apart in this mod. Ensure that the returned value is at least 300 miles
            if (Main.enabled && __result < 300f) {
                __result = 300f;
            }
        }
    }

    [HarmonyPatch(typeof(MissionDetailsUI), "UpdateMap")]
    static class MissionMapPatch
    {
        private static void Postfix(ref LineRenderer ___routeLine, ref Transform ___destinationMarker)
        {
            if (Main.enabled) {
                //Disable the red line on the mission map
                ___destinationMarker.localPosition = new Vector3(50000f, 0f, 50000f);
                ___routeLine.SetPosition(0, new Vector3(50000f, 0f, 50000f));
                ___routeLine.SetPosition(1, new Vector3(50000f, 0f, 50000f));
            }
        }
    }

    [HarmonyPatch(typeof(RegionBlender), "Update")]
    static class RegionBlenderPatch
    {
        private static float regionUpdateCooldown = 0f;

        private static void Prefix(ref Region ___currentTargetRegion, ref Transform ___player)
        {
            if (Main.enabled) {
                if (regionUpdateCooldown <= 0f) {
                    regionUpdateCooldown = 100f;
                    float minDist = 100000000f;
                    Region closestRegion = null;
                    foreach (Region region in Main.regions) {
                        float dist = Vector3.Distance(___player.position, region.transform.position);
                        if (dist < minDist) {
                            minDist = dist;
                            closestRegion = region;
                        }
                    }
                    if (closestRegion != null) {
                        ___currentTargetRegion = closestRegion;
                    }
                } else {
                    regionUpdateCooldown -= Time.deltaTime;
                }
            }
        }
    }

    public class WorldScrambler
    {
        public static void Scramble()
        {
            //Convert stored ints to floats
            float archSpread = Main.mySaveContainer.archipelagoSpread;
            float islandSpread = Main.mySaveContainer.islandSpread;
            float minArchSeparation = Main.mySaveContainer.minArchipelagoSeparation;
            float minIslandSeparation = Main.mySaveContainer.minIslandSeparation;
            Main.logger.Log("Scrambler Seed:" + Main.mySaveContainer.worldScramblerSeed);

            //Randomize locations until we pass test (This must remain deterministic!)
            UnityEngine.Random.InitState(Main.mySaveContainer.worldScramblerSeed);
            Vector3[] archLocs;
            Vector3[] islandLocs;
            while (true) {
                archLocs = new Vector3[5];
                for (int i = 0; i < archLocs.Length; i++) {
                    archLocs[i] = new Vector3(UnityEngine.Random.Range(-archSpread, archSpread), 0f, UnityEngine.Random.Range(-archSpread, archSpread));
                }
                if (!CheckScramble(archLocs, minArchSeparation)) {
                    continue;
                }
                islandLocs = new Vector3[23];
                //AlAnkh islands
                foreach (int num3 in new int[] { 1, 2, 3, 4, 5, 6, 7, 8 }) {
                    islandLocs[num3 - 1] = new Vector3(UnityEngine.Random.Range(-islandSpread, islandSpread), 0f, UnityEngine.Random.Range(-islandSpread, islandSpread)) + archLocs[0];
                }
                //Emerald islands
                foreach (int num4 in new int[] { 9, 10, 11, 12, 13, 22 }) {
                    islandLocs[num4 - 1] = new Vector3(UnityEngine.Random.Range(-islandSpread, islandSpread), 0f, UnityEngine.Random.Range(-islandSpread, islandSpread)) + archLocs[1];
                }
                //Aestrin islands
                foreach (int i in new int[] { 14, 15, 16, 17, 19, 21, 23 }) {
                    islandLocs[i - 1] = new Vector3(UnityEngine.Random.Range(-islandSpread, islandSpread), 0f, UnityEngine.Random.Range(-islandSpread, islandSpread)) + archLocs[2];
                }
                //Happy Bay and Oasis
                islandLocs[19] = archLocs[3];
                islandLocs[17] = archLocs[4];
                //Exit loop if we pass test
                if (CheckScramble(islandLocs, minIslandSeparation)) {
                    break;
                }
            }
            int completionVal = UnityEngine.Random.Range(0, 1000000);
            Main.logger.Log("Scrambler completion value:" + completionVal);

            //Calculate displacement vectors
            string[] regionNames = new string[] { "Region Al'ankh", "Region Emerald", "Region Medi" };
            Vector3[] regionDisplacements = new Vector3[regionNames.Length];
            Main.regions = new List<Region>();
            for (int i = 0; i < regionNames.Length; i++) {
                regionDisplacements[i] = archLocs[i] - FloatingOriginManager.instance.ShiftingPosToRealPos(GameObject.Find(regionNames[i]).transform.localPosition);
                regionDisplacements[i].y = 0f;
                Main.regions.Add(GameObject.Find(regionNames[i]).GetComponent<Region>());
            }
            for (int i = 0; i < Main.islandOrigins.Length; i++) {
                Main.islandDisplacements[i] = islandLocs[i] - Main.islandOrigins[i];
                Main.islandDisplacements[i].y = 0f;
            }

            //Move regions
            for (int i = 0; i < regionNames.Length; i++) {
                GameObject.Find(regionNames[i]).transform.Translate(regionDisplacements[i], Space.World);
            }
            //Move islands
            for (int i = 0; i < Main.islandDisplacements.Length; i++) {
                if (!string.IsNullOrEmpty(Main.islandNames[i])) {
                    GameObject.Find(Main.islandNames[i]).transform.Translate(Main.islandDisplacements[i], Space.World);
                }
            }

            //Move misc objects
            GameObject.Find("bottom plane A").transform.Translate(regionDisplacements[0], Space.World);
            GameObject.Find("bottom plane E").transform.Translate(regionDisplacements[1], Space.World);
            GameObject.Find("bottom plane M").transform.Translate(regionDisplacements[2], Space.World);
            GameObject.Find("recovery port (gold rock) (1)").transform.Translate(Main.islandDisplacements[0], Space.World);
            GameObject.Find("recovery port (dragon cliffs)").transform.Translate(Main.islandDisplacements[8], Space.World);
            GameObject.Find("recovery port (fort)").transform.Translate(Main.islandDisplacements[14], Space.World);

            //Move unpurchased boats
            string[] boatNames = new string[] {
                "BOAT dhow small (10)",
                "BOAT dhow medium (20)",
                "BOAT junk small singleroof(90)",
                "BOAT junk medium (80)",
                "BOAT medi small (40)",
                "BOAT medi medium (50)"
            };
            int[] boatIslands = new int[] { 2, 0, 10, 8, 20, 14 };
            for (int n = 0; n < boatIslands.Length; n++) {
                if (!GameObject.Find(boatNames[n]).GetComponent<PurchasableBoat>().isPurchased()) {
                    GameObject.Find(boatNames[n]).transform.Translate(Main.islandDisplacements[boatIslands[n]], Space.World);
                }
            }

            //Re-roll seed for gameplay
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        }

        private static bool CheckScramble(Vector3[] locations, float minDist)
        {
            for (int i = 0; i < locations.Length; i++) {
                for (int j = 0; j < locations.Length; j++) {
                    if (i != j && Vector3.Distance(locations[i], locations[j]) < minDist) {
                        return false;
                    }
                }
            }
            return true;
        }
    }


}
