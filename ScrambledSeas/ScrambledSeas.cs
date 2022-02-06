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
        public int version { get; set; } = 0;
        public int worldScramblerSeed { get; set; } = 0;
        public int islandSpread { get; set; } = 10000;
        public int minArchipelagoSeparation { get; set; } = 30000;
        public int minIslandSeparation { get; set; } = 2000;
        //These defaults change if WorldBorder changes
        public int worldLonMin { get; set; } = -12;
        public int worldLonMax { get; set; } = 32;
        public int worldLatMin { get; set; } = 26;
        public int worldLatMax { get; set; } = 46;
    }

    static class Main
    {
        //Update this with every version
        public const int version = 60;
        //These 2 constants should be future proof. Changing them will break save compatibility
        public const int nArchipelagos = 15;
        public const int nIslandsPer = 15;

        public static bool enabled;
        public static UnityModManager.ModEntry.ModLogger logger;
        public static Vector3[] islandDisplacements = new Vector3[nArchipelagos*nIslandsPer];
        public static Vector3[] islandOrigins = new Vector3[nArchipelagos*nIslandsPer];
        public static string[] islandNames = new string[nArchipelagos*nIslandsPer];
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
                Main.mySaveContainer.version = Main.version;
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
                
                if (Main.mySaveContainer.version < 60) { //TODO: update min version if save compatibility breaks again
                    NotificationUi.instance.ShowNotification("ERROR: This save is not\ncompatiblewith this version\nof Scrambled Seas");
                    throw new System.InvalidOperationException("ERROR: This save is not compatible with this version of Scrambled Seas");
                }
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
        private static void Postfix(ref Renderer ___mapRenderer, ref TextMesh ___locationText)
        {
            if (Main.enabled) {
                ___mapRenderer.gameObject.SetActive(false);
                ___locationText.text = "Map Unavailable\n\nWelcome to ScrambledSeas :)";
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

    static class WorldScrambler
    {
        public static void Scramble()
        {
            //These constants need to be updated when new islands/regions are added to game
            var regionToName = new Dictionary<int, string>();
            var regionToIslandIdxs = new Dictionary<int, List<int>>();
            var bottomToRegion = new Dictionary<string, int>();

            // Al Ankh
            regionToName.Add(0, "Region Al'ankh");
            regionToIslandIdxs.Add(0, new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 });
            bottomToRegion.Add("bottom plane A", 0);
            // Emerald
            regionToName.Add(1, "Region Emerald");
            regionToIslandIdxs.Add(1, new List<int>() { 9, 10, 11, 12, 13, 22 });
            bottomToRegion.Add("bottom plane E", 1);
            // Aestrin
            regionToName.Add(2, "Region Medi");
            regionToIslandIdxs.Add(2, new List<int>() { 14, 15, 16, 17, 19, 21, 23 });
            bottomToRegion.Add("bottom plane M", 2);
            // Oasis
            regionToIslandIdxs.Add(3, new List<int>() { 20 });
            // Happy Bay
            regionToIslandIdxs.Add(4, new List<int>() { 18 });
            // Chronos
            regionToIslandIdxs.Add(5, new List<int>() { 25 });
            bottomToRegion.Add("bottom plane chronos", 5);
            
            //Convert stored ints to floats
            float islandSpread = Main.mySaveContainer.islandSpread;
            float minArchSeparation = Main.mySaveContainer.minArchipelagoSeparation;
            float minIslandSeparation = Main.mySaveContainer.minIslandSeparation;
            //Convert lat/lon to unity coords
            float archXMin = Main.mySaveContainer.worldLonMin*9000f + 27000f;
            float archXMax = Main.mySaveContainer.worldLonMax*9000f - 27000f;
            float archZMin = (Main.mySaveContainer.worldLatMin-36f)*9000f + 27000f;
            float archZMax = (Main.mySaveContainer.worldLatMax-36f)*9000f - 27000f;
            
            //Randomize locations until we pass test (This must remain deterministic!)
            Main.logger.Log("Scrambler Seed:" + Main.mySaveContainer.worldScramblerSeed);
            UnityEngine.Random.InitState(Main.mySaveContainer.worldScramblerSeed);
            Vector3[] archLocs;
            while (true) {
                archLocs = new Vector3[Main.nArchipelagos];
                for (int i = 0; i < archLocs.Length; i++) {
                    archLocs[i] = new Vector3(UnityEngine.Random.Range(archXMin, archXMax), 0f, UnityEngine.Random.Range(archZMin, archZMax));
                }
                if (CheckScramble(archLocs, minArchSeparation)) {
                    break;
                }
            }
            Vector3[][] islandLocs = new Vector3[Main.nArchipelagos][];
            {
                int i = 0;
                while (i < Main.nIslandsPer) {
                    islandLocs[i] = new Vector3[Main.nIslandsPer];
                    for (int j = 0; j<Main.nIslandsPer; j++) {
                        islandLocs[i][j] = new Vector3(UnityEngine.Random.Range(-islandSpread, islandSpread), 0f, UnityEngine.Random.Range(-islandSpread, islandSpread)) + archLocs[i];
                    }
                    if (CheckScramble(islandLocs[i], minIslandSeparation)) {
                        i++;
                    }
                }
            }

            int completionVal = UnityEngine.Random.Range(0, 1000000);
            Main.logger.Log("Scrambler completion value:" + completionVal);

            //Calculate archipelago displacement vectors
            Vector3[] regionDisplacements = new Vector3[Main.nArchipelagos];
            foreach (var kv in regionToName) {
                regionDisplacements[kv.Key] = archLocs[kv.Key] - FloatingOriginManager.instance.ShiftingPosToRealPos(GameObject.Find(kv.Value).transform.localPosition);
                regionDisplacements[kv.Key].y = 0f;
            }
            //Calculate island displacement vectors
            foreach (var kv in regionToIslandIdxs) {
                int regionIdx = kv.Key;
                List<int> islandIdxs = kv.Value;
                if (regionDisplacements[regionIdx] == null && islandIdxs.Count == 1) {
                    regionDisplacements[regionIdx] = archLocs[kv.Key] - Main.islandOrigins[islandIdxs[0] - 1];
                    regionDisplacements[regionIdx].y = 0f;
                    Main.islandDisplacements[islandIdxs[0] - 1] = regionDisplacements[regionIdx];
                } else {
                    for (int i=0; i<islandIdxs.Count; i++) {
                        Main.islandDisplacements[islandIdxs[i] - 1] = islandLocs[regionIdx][i] - Main.islandOrigins[islandIdxs[i] - 1];
                        Main.islandDisplacements[islandIdxs[i] - 1].y = 0f;
                    }
                }
            }
            //Move regions
            foreach (var kv in regionToName) {
                GameObject.Find(kv.Value).transform.Translate(regionDisplacements[kv.Key], Space.World);
            }
            //Move islands
            for (int i = 0; i < Main.islandDisplacements.Length; i++) {
                if (!string.IsNullOrEmpty(Main.islandNames[i])) {
                    GameObject.Find(Main.islandNames[i]).transform.Translate(Main.islandDisplacements[i], Space.World);
                }
            }
            //Move bottom planes:
            foreach (var kv in bottomToRegion) {
                GameObject.Find(kv.Key).transform.Translate(regionDisplacements[kv.Value], Space.World);
            }
            //Move recovery ports
            RecoveryPort[] recoveryArray = Object.FindObjectsOfType(typeof(RecoveryPort)) as RecoveryPort[];
            foreach (var recovery in recoveryArray) {
                int homeIsland = ClosestIsland(recovery.gameObject.transform.position);
                recovery.gameObject.transform.Translate(Main.islandDisplacements[homeIsland], Space.World);
            }
            //Move unpurchased boats
            PurchasableBoat[] boatArray = Object.FindObjectsOfType(typeof(PurchasableBoat)) as PurchasableBoat[];
            foreach (var boat in boatArray) {
                if (!boat.isPurchased()) {
                    int homeIsland = ClosestIsland(boat.gameObject.transform.position);
                    boat.gameObject.transform.Translate(Main.islandDisplacements[homeIsland], Space.World);
                }
            }

            //Re-roll seed for gameplay
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

            //Cache regions for RegionBlenderPatch
            Main.regions = new List<Region>();
            foreach (var kv in regionToName) {
                Main.regions.Add(GameObject.Find(kv.Value).GetComponent<Region>());
            }

            // //Debug
            //foreach (var name in Main.islandNames) {
            //    if (!string.IsNullOrEmpty(name)) {
            //        Vector3 latlon = FloatingOriginManager.instance.GetGlobeCoords(GameObject.Find(name).transform);
            //        Main.logger.Log(name +","+ latlon.x +","+ latlon.z);
            //    }
            //}
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
        
        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static int ClosestIsland(Vector3 pos)
        {
            Vector3 target = FloatingOriginManager.instance.ShiftingPosToRealPos(pos);
            float minD = 999999f;
            int idx = -1;
            for (int i = 0; i < Main.islandOrigins.Length; i++) {
                if (!string.IsNullOrEmpty(Main.islandNames[i])) {
                    float d = FlatDistance(target, Main.islandOrigins[i]);
                    if (d<minD) {
                        minD = d;
                        idx = i;
                    }
                }
            }
            return idx;
        }
    }


}
