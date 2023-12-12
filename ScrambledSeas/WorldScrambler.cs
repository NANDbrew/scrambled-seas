using System.Collections.Generic;
using UnityEngine;

namespace ScrambledSeas
{
    internal static class WorldScrambler
    {
        //Update this with every version
        public const int version = 60;
        //These 2 constants should be future proof. Changing them will break save compatibility
        public const int nArchipelagos = 15;
        public const int nIslandsPer = 15;

        public static Vector3[] islandDisplacements = new Vector3[nArchipelagos * nIslandsPer];
        public static Vector3[] islandOrigins = new Vector3[nArchipelagos * nIslandsPer];
        public static string[] islandNames = new string[nArchipelagos * nIslandsPer];
        public static List<Region> regions = new List<Region>();

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
            regionToName.Add(1, "Region Emerald (new smaller)");
            regionToIslandIdxs.Add(1, new List<int>() { 9, 10, 11, 12, 13, 22 });
            bottomToRegion.Add("bottom plane E", 1);
            // Aestrin
            regionToName.Add(2, "Region Medi");
            regionToIslandIdxs.Add(2, new List<int>() { 15, 16, 17, 19, 21, 23 });

            bottomToRegion.Add("bottom plane M", 2);
            // Oasis
            regionToIslandIdxs.Add(3, new List<int>() { 20 });
            // Happy Bay
            regionToIslandIdxs.Add(4, new List<int>() { 18 });
            // Chronos
            regionToIslandIdxs.Add(5, new List<int>() { 25 });
            // Fire Town Lagoon
            regionToName.Add(6, "Region Emerald Lagoon");
            regionToIslandIdxs.Add(6, new List<int>() { 26, 27, 28, 29, 30, 31 });
            bottomToRegion.Add("bottom plane chronos", 5);

            //Convert stored ints to floats
            float islandSpread = Main.saveContainer.islandSpread;
            float minArchSeparation = Main.saveContainer.minArchipelagoSeparation;
            float minIslandSeparation = Main.saveContainer.minIslandSeparation;
            //Convert lat/lon to unity coords
            float archXMin = Main.saveContainer.worldLonMin * 9000f + 27000f;
            float archXMax = Main.saveContainer.worldLonMax * 9000f - 27000f;
            float archZMin = (Main.saveContainer.worldLatMin - 36f) * 9000f + 27000f;
            float archZMax = (Main.saveContainer.worldLatMax - 36f) * 9000f - 27000f;

            //Randomize locations until we pass test (This must remain deterministic!)
#if UMM
            Main.logger.Log("Scrambler Seed:" + Main.saveContainer.worldScramblerSeed);
#elif BepInEx
            Main.logSource.LogInfo("Scrambler Seed:" + Main.saveContainer.worldScramblerSeed);
#endif
            UnityEngine.Random.InitState(Main.saveContainer.worldScramblerSeed);
            Vector3[] archLocs;
            while (true)
            {
                archLocs = new Vector3[nArchipelagos];
                for (int i = 0; i < archLocs.Length; i++)
                {
                    archLocs[i] = new Vector3(UnityEngine.Random.Range(archXMin, archXMax), 0f, UnityEngine.Random.Range(archZMin, archZMax));
                }
                if (CheckScramble(archLocs, minArchSeparation))
                {
                    break;
                }
            }
            Vector3[][] islandLocs = new Vector3[nArchipelagos][];
            {
                int i = 0;
                while (i < nIslandsPer)
                {
                    islandLocs[i] = new Vector3[nIslandsPer];
                    for (int j = 0; j < nIslandsPer; j++)
                    {
                        islandLocs[i][j] = new Vector3(UnityEngine.Random.Range(-islandSpread, islandSpread), 0f, UnityEngine.Random.Range(-islandSpread, islandSpread)) + archLocs[i];
                    }
                    if (CheckScramble(islandLocs[i], minIslandSeparation))
                    {
                        i++;
                    }
                }
            }

            int completionVal = UnityEngine.Random.Range(0, 1000000);
#if UMM
            Main.logger.Log("Scrambler completion value:" + completionVal);
#elif BepInEx
            Main.logSource.LogInfo("Scrambler completion value:" + completionVal);
#endif

            //Calculate archipelago displacement vectors
            Vector3[] regionDisplacements = new Vector3[nArchipelagos];
            foreach (var kv in regionToName)
            {
                regionDisplacements[kv.Key] = archLocs[kv.Key] - FloatingOriginManager.instance.ShiftingPosToRealPos(GameObject.Find(kv.Value).transform.localPosition);
                regionDisplacements[kv.Key].y = 0f;
            }
            //Calculate island displacement vectors
            foreach (var kv in regionToIslandIdxs)
            {
                int regionIdx = kv.Key;
                List<int> islandIdxs = kv.Value;
                if (regionDisplacements[regionIdx] == null && islandIdxs.Count == 1)
                {
                    regionDisplacements[regionIdx] = archLocs[kv.Key] - islandOrigins[islandIdxs[0] - 1];
                    regionDisplacements[regionIdx].y = 0f;
                    islandDisplacements[islandIdxs[0] - 1] = regionDisplacements[regionIdx];
                }
                else
                {
                    for (int i = 0; i < islandIdxs.Count; i++)
                    {
                        islandDisplacements[islandIdxs[i] - 1] = islandLocs[regionIdx][i] - islandOrigins[islandIdxs[i] - 1];
                        islandDisplacements[islandIdxs[i] - 1].y = 0f;
                    }
                }
            }
            //Move regions
            foreach (var kv in regionToName)
            {
                GameObject.Find(kv.Value).transform.Translate(regionDisplacements[kv.Key], Space.World);
            }
            //Move islands
            for (int i = 0; i < islandDisplacements.Length; i++)
            {
                if (!string.IsNullOrEmpty(islandNames[i]))
                {
                    GameObject.Find(islandNames[i]).transform.Translate(islandDisplacements[i], Space.World);
                }
            }
            //Move bottom planes:
            foreach (var kv in bottomToRegion)
            {
                GameObject.Find(kv.Key).transform.Translate(regionDisplacements[kv.Value], Space.World);
            }
            //Move recovery ports
            RecoveryPort[] recoveryArray = Object.FindObjectsOfType(typeof(RecoveryPort)) as RecoveryPort[];
            foreach (var recovery in recoveryArray)
            {
                int homeIsland = ClosestIsland(recovery.gameObject.transform.position);
                recovery.gameObject.transform.Translate(islandDisplacements[homeIsland], Space.World);
            }
            //Move unpurchased boats
            PurchasableBoat[] boatArray = Object.FindObjectsOfType(typeof(PurchasableBoat)) as PurchasableBoat[];
            foreach (var boat in boatArray)
            {
                if (!boat.isPurchased())
                {
                    int homeIsland = ClosestIsland(boat.gameObject.transform.position);
                    boat.gameObject.transform.Translate(islandDisplacements[homeIsland], Space.World);
                }
            }

            //Re-roll seed for gameplay
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

            //Cache regions for RegionBlenderPatch
            regions = new List<Region>();
            foreach (var kv in regionToName)
            {
                regions.Add(GameObject.Find(kv.Value).GetComponent<Region>());
            }

            // //Debug
            //foreach (var name in islandNames) {
            //    if (!string.IsNullOrEmpty(name)) {
            //        Vector3 latlon = FloatingOriginManager.instance.GetGlobeCoords(GameObject.Find(name).transform);
            //        Main.logger.Log(name +","+ latlon.x +","+ latlon.z);
            //    }
            //}
        }

        private static bool CheckScramble(Vector3[] locations, float minDist)
        {
            for (int i = 0; i < locations.Length; i++)
            {
                for (int j = 0; j < locations.Length; j++)
                {
                    if (i != j && Vector3.Distance(locations[i], locations[j]) < minDist)
                    {
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
            for (int i = 0; i < islandOrigins.Length; i++)
            {
                if (!string.IsNullOrEmpty(islandNames[i]))
                {
                    float d = FlatDistance(target, islandOrigins[i]);
                    if (d < minD)
                    {
                        minD = d;
                        idx = i;
                    }
                }
            }
            return idx;
        }
    }


}