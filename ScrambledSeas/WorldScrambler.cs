using OculusSampleFramework;
using OVRSimpleJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Profiling;

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

        public static Dictionary<int, bool> marketVisited = new Dictionary<int,bool>();

        public static void Scramble()
        {
            //These constants need to be updated when new islands/regions are added to game
            var regionToName = new Dictionary<int, string>();
            var regionToIslandIdxs = new Dictionary<int, List<int>>();
            var bottomToRegion = new Dictionary<string, int>();

            int eastwindIndx = 19;
            Transform islandEastwindTransform = Refs.islands[eastwindIndx];
            IslandMarket eastwindMarket = null;
            Vector3 eastwindMarketOffset = Vector3.zero;
            IslandMarket[] markets = UnityEngine.Object.FindObjectsOfType(typeof(IslandMarket)) as IslandMarket[];

            foreach(IslandMarket m in markets)
            {
                if(m.GetPortName() == "Eastwind")
                {
                    eastwindMarket = m;
                    break;
                }
            }

            if (eastwindMarket != null)
            {
                Main.Log("Save Eastwind market offset");
                Vector3 pos = FloatingOriginManager.instance.GetGlobeCoords(eastwindMarket.gameObject.transform);
                Main.Log("market: x:" + pos.x + " z: " + pos.z);
                pos = FloatingOriginManager.instance.GetGlobeCoords(islandEastwindTransform);
                Main.Log("island: x:" + pos.x + " z: " + pos.z);

                eastwindMarketOffset = eastwindMarket.gameObject.transform.position - islandEastwindTransform.position;

                Main.Log("offset: x:" + eastwindMarketOffset.x/9000.0f + " z: " + (eastwindMarketOffset.z/9000.0f));

            }


            // Al Ankh
            regionToName.Add(0, "Region Al'ankh");
            regionToIslandIdxs.Add(0, new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, });
            bottomToRegion.Add("bottom plane A", 0);
            // Emerald
            regionToName.Add(1, "Region Emerald (new smaller)");
            regionToIslandIdxs.Add(1, new List<int>() { 9, 10, 11, 12, 13, 22, 37, 38, 39 });
            bottomToRegion.Add("bottom plane E", 1);
            // Aestrin
            regionToName.Add(2, "Region Medi");
            regionToIslandIdxs.Add(2, new List<int>() { 15, 16, 17, 19, 21, 23, 33, 34, 35, 36 });

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
            // Rock of Despair
            regionToIslandIdxs.Add(7, new List<int>() { 30 });
            // Rock of Despair
            regionToIslandIdxs.Add(8, new List<int>() { 32 });
            bottomToRegion.Add("bottom plane chronos", 5);

            //Convert stored ints to floats
            float islandSpread = Main.saveContainer.islandSpread;
            float minArchSeparation = Main.saveContainer.minArchipelagoSeparation;
            float minIslandSeparation = Main.saveContainer.minIslandSeparation;
            //Convert lat/lon to unity coords

            float border_offset = 27000f; // 270 mile
            float archXMin = Main.saveContainer.worldLonMin * 9000f + border_offset;
            float archXMax = Main.saveContainer.worldLonMax * 9000f - border_offset;
            float archZMin = (Main.saveContainer.worldLatMin - 36f) * 9000f + border_offset;
            float archZMax = (Main.saveContainer.worldLatMax - 36f) * 9000f - border_offset;

            //Randomize locations until we pass test (This must remain deterministic!)

            Main.Log("Scrambler Seed:" + Main.saveContainer.worldScramblerSeed);

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

            Main.Log("Scrambler completion value:" + completionVal);

            if (Main.random_Enabled.Value)
            {
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
            }            
            //Move recovery ports
            IslandHorizon[] islands = UnityEngine.Object.FindObjectsOfType(typeof(IslandHorizon)) as IslandHorizon[];
            RecoveryPort[] recoveryArray = UnityEngine.Object.FindObjectsOfType(typeof(RecoveryPort)) as RecoveryPort[];

            for (int i = 0; i < islands.Length; i++)
            {
                IslandHorizon island = islands[i];
                if (island == null) continue;
                Vector3 pos = FloatingOriginManager.instance.GetGlobeCoords(island.transform);
                Main.Log("i: " + i+ " island: " + island.islandIndex + " name: " + island.name + " x: " + pos.x + " z: " + pos.z);
            }

            if (Main.random_Enabled.Value)
            {
                foreach (var recovery in recoveryArray)
                { 
                    int homeIndx = ClosestIsland(recovery.gameObject.transform.position);
                    recovery.gameObject.transform.Translate(islandDisplacements[homeIndx], Space.World);
                }
            }

            Main.Log("check Markets positions");
            foreach (IslandMarket mr in markets)
            {
                Main.Log("---");
                Vector3 pos = FloatingOriginManager.instance.GetGlobeCoords(mr.gameObject.transform);
                Main.Log("market pos: x:" + pos.x + " z: " + pos.z);
                Port port = mr.GetPort();
                if(port == null) continue;
                pos = FloatingOriginManager.instance.GetGlobeCoords(port.transform);
                Main.Log("port pos: " + mr.GetPortName() + " pos x:" + pos.x + " z: " + pos.z);

                // make a mark for this that it not visited for now;
                marketVisited.Add(port.portIndex, false);

                if (mr.GetPortName() == "Eastwind")
                {
                    Main.Log("Move Eastwind market to correct position");
                    mr.gameObject.transform.SetPositionAndRotation(Refs.islands[19].position, new Quaternion());
                    mr.gameObject.transform.Translate(eastwindMarketOffset, Space.World);
                    pos = FloatingOriginManager.instance.GetGlobeCoords(Refs.islands[19]);
                    Main.Log("move market to " + pos.x + " " + pos.z);
                    pos = FloatingOriginManager.instance.GetGlobeCoords(mr.gameObject.transform);
                    Main.Log("market new pos " + pos.x + " " + pos.z);
                }
            }

            if (Main.random_Enabled.Value)
            {

                //Move unpurchased boats
                PurchasableBoat[] boatArray = UnityEngine.Object.FindObjectsOfType(typeof(PurchasableBoat)) as PurchasableBoat[];
                foreach (var boat in boatArray)
                {
                    if (!boat.isPurchased())
                    {
                        int homeIsland = ClosestIsland(boat.gameObject.transform.position);
                        boat.gameObject.transform.Translate(islandDisplacements[homeIsland], Space.World);
                    }
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
            if (Main.saveCoordsToJSON_Enabled.Value)
            {
                try
                {
                    JSONObject json = new JSONObject();
                    

                    JSONNode empty = new JSONArray();
                    json.Add("lines", empty);
                    json.Add("path", empty);
                    
                    JSONNode points = new JSONArray();

                    Dictionary<string,string> arch_colors = new Dictionary<string,string>();
                    arch_colors.Add("A", "yellowpoint");
                    arch_colors.Add("E", "greenpoint");
                    arch_colors.Add("M", "bluepoint");
                    arch_colors.Add("Lagoon", "orangepoint");

                    foreach (var name in islandNames)
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            // 0      1 2 3
                            // island 1 A (gold rock)
                            string[] name_array = name.Split(' ');
                            string point_color = "bluepoint";
                            string island_name = "";
                            bool skip = false;

                            if (name_array.Length < 3)
                            {
                                if (name_array[1] == "36")
                                {
                                    point_color = "bluepoint";
                                    island_name = "island 36";
                                    skip = true;
                                }
                            }
                            else if (arch_colors.ContainsKey(name_array[2]))
                            {
                                point_color = arch_colors[name_array[2]];
                            }
                            else
                            {
                                point_color = "bluepoint";
                                if (name_array[2] == "rock")
                                {
                                    island_name = "Rock Of Despair";
                                    skip = true;
                                }
                            }

                            if (!skip)
                            {
                                // search (island name)
                                Match island = Regex.Match(name, "(\\(.*\\))");
                                if (island.Success)
                                {
                                    island_name = island.Value.Trim(new Char[] { ' ', '(', ')' });
                                }
                                else
                                {
                                    island_name = name_array[3];
                                }
                            }
                            island_name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(island_name.ToLower());

                            Vector3 latlon = FloatingOriginManager.instance.GetGlobeCoords(GameObject.Find(name).transform);
                            Main.Log(name + "," + latlon.x + "," + latlon.z);
                            JSONObject point = new JSONObject();
                            point.Add("description", new JSONString(island_name));
                            JSONArray pos = new JSONArray();
                            pos.Add(new JSONNumber(latlon.x));
                            pos.Add(new JSONNumber(latlon.z));
                            point.Add("pos",pos);
                            point.Add("colour", point_color);
                            point.Add("day", 0);
                            point.Add("time", 0);
                            point.Add("winddir", "NE");                            
                            points.Add(point);
                        }
                    }

                    json.Add("points", points);
                    json.Add("goals", empty);

                    string jsonString = json.ToString();

                    Main.Log(jsonString);
                    File.WriteAllText(Path.Combine(Directory.GetParent(Main.instance.Info.Location).FullName, $"scramble_{SaveSlots.currentSlot}.json"), jsonString);
                }
                catch(Exception ex)
                {
                
                    Main.Log(ex.Message);
                    Main.Log(ex.InnerException.Message);
                }
            }


            //for (int i = 0; i < archLocs.Length; i++)
            //{
            //    Main.Log("arch: #" + i + ",   " + archLocs[i].x + ",   " + archLocs[i].z);
            //    if (Main.saveCoordsToJSON_Enabled.Value)
            //    {
            //        File.AppendAllText(Path.Combine(Main.instance.Info.GetFolderLocation(), $"scramble_{SaveSlots.currentSlot}.txt"), $"arch: {i}| {archLocs[i].x}, {archLocs[i].z}" + Environment.NewLine);
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
