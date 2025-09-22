using JetBrains.Annotations;
using OculusSampleFramework;
using OVRSimpleJSON;
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;

namespace ScrambledSeas
{
    internal static class WorldScrambler
    {
        static string path;
        //Update this with every version
        public const int version = 60;
        //These 2 constants should be future proof. Changing them will break save compatibility
        public const int nArchipelagos = 15;
        public const int nIslandsPer = 15;

        public static Vector3[] regionDisplacements = new Vector3[nArchipelagos];
        public static Vector3[] islandDisplacements = new Vector3[nArchipelagos * nIslandsPer];
        public static Vector3[] islandOrigins = new Vector3[nArchipelagos * nIslandsPer];
        public static string[] islandNames = new string[nArchipelagos * nIslandsPer];
        public static Dictionary<int, Region> regions = new Dictionary<int, Region>();
        public static List<RecoveryPort> recoveryArray = new List<RecoveryPort>();
        public static Dictionary<int, bool> marketVisited = new Dictionary<int,bool>();
        public static List<PurchasableBoat> boatArray = new List<PurchasableBoat>();
        static List<RegionDefinition> regionDefs = new List<RegionDefinition>();

        private static int eastwindIndx = 19;
        private static Vector3 eastwindMarketOffset = Vector3.zero;

        private static bool setupRan = false;
        
        public static void Setup()
        {
            if (Main.eastwindFix.Value)
            {
                Transform islandEastwindTransform = Refs.islands[eastwindIndx];
                IslandMarket eastwindMarket = null;

                IslandMarket[] markets = UnityEngine.Object.FindObjectsOfType(typeof(IslandMarket)) as IslandMarket[];

                foreach (IslandMarket m in markets)
                {
                    if (m.GetPortName() == "Eastwind")
                    {
                        eastwindMarket = m;
                        break;
                    }
                }
                eastwindMarket = Port.ports[eastwindIndx].GetComponent<IslandMarket>();
                if (eastwindMarket != null)
                {
                    //Main.Log("Save Eastwind market offset");
                    Vector3 pos = FloatingOriginManager.instance.GetGlobeCoords(eastwindMarket.gameObject.transform);
                    //Main.Log("market: x:" + pos.x + " z: " + pos.z);
                    pos = FloatingOriginManager.instance.GetGlobeCoords(islandEastwindTransform);
                    //Main.Log("island: x:" + pos.x + " z: " + pos.z);

                    eastwindMarketOffset = eastwindMarket.gameObject.transform.position - islandEastwindTransform.position;

                    //Main.Log("offset: x:" + eastwindMarketOffset.x / 9000.0f + " z: " + (eastwindMarketOffset.z / 9000.0f));

                }
            }

            path = Path.Combine(Directory.GetParent(Main.instance.Info.Location).FullName, $"regions.json");
            if (File.Exists(path))
            {
                //regionDefs = JsonConvert.DeserializeObject<List<RegionDefinition>>(File.ReadAllText(path));
                regionDefs = ReadRegionDefs(path);
            }
            else
            {
                regionDefs = GenerateDefs();
                WriteRegionDefs(regionDefs);
            }
            //Cache regions for RegionBlenderPatch
            var foundRegions = GameObject.FindObjectsOfType<Region>();
            regions = new Dictionary<int, Region>();
            int maxKey = 0;
            foreach (var region in foundRegions)
            {
                if (region.transform.position.magnitude < 10)
                {
                    continue;
                }
                bool found = false;
                foreach (var regionDef in regionDefs)
                {
                    if (regionDef.objectName == region.name && !regions.ContainsValue(region))
                    {
                        regions.Add(regionDef.index, region);
                        found = true;
                        break;
                    }
                }
                if (found == false)
                {
                    if (maxKey < regionDefs.Last().index) maxKey = regionDefs.Last().index;
                    regions.Add(maxKey + 1, region);
                    regionDefs.Add(new RegionDefinition { objectName = region.name, index = maxKey + 1, islands = new List<int>() });
                }
            }

            for (int i = 0; i < Refs.islands.Length; i++)
            {
                if (Refs.islands[i] == null) continue;
                bool found = false;
                foreach (var region in regionDefs)
                {
                    if (region.islands.Contains(i))
                    {
                        found = true;
                        break;
                    }
                }
                if (found == false)
                {
                    var closestRegion = ClosestRegion(Refs.islands[i]);
                    RegionDefinition found2 = null;
                    foreach (var regionDef in regionDefs)
                    {
                        if (regionDef.index == closestRegion)
                        {
                            found2 = regionDef;
                            break;
                        }
                    }
                    if (found2 == null)
                    {
                        found2 = new RegionDefinition { index = closestRegion, islands = new List<int>() };
                        regionDefs.Add(found2);
                    }
                    found2.islands.Add(i);
                }
            }

            setupRan = true;
        }
        public static void Scramble()
        {
            if (!setupRan) Setup();
            if (Main.borderExpander)
            {
                Main.saveContainer.borderExpander = 1;
            }
            #region scrambling
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

            foreach (var region in regionDefs)
            {
                //Calculate archipelago displacement vectors
                if (region.objectName == string.Empty)
                {
                    regionDisplacements[region.index] = archLocs[region.index] - islandOrigins[region.islands[0] - 1];
                }
                else
                {
                    regionDisplacements[region.index] = archLocs[region.index] - FloatingOriginManager.instance.ShiftingPosToRealPos(regions[region.index].transform.localPosition);
                }
                regionDisplacements[region.index].y = 0f;

                //Calculate island displacement vectors
                for (int i = 0; i < region.islands.Count; i++)
                {
                    islandDisplacements[region.islands[i] - 1] = islandLocs[region.index][i] - islandOrigins[region.islands[i] - 1];
                    islandDisplacements[region.islands[i] - 1].y = 0f;
                }
            }

            #endregion

            // write displacements to save
            Vector3[] regionDispList = new Vector3[regionDefs.Count];
            List<Vector3> isleDispList = new List<Vector3>();
            foreach (var region in regionDefs)
            {
                if (region.objectName != string.Empty)
                {
                    regionDispList[region.index] = regionDisplacements[region.index];
                }
               /* foreach (var island in region.Islands)
                {
                    isleDispList.Add(islandDisplacements[island]);
                }*/
            }

            Main.saveContainer.archOffsets = regionDispList;
            for (int i = 0; i < islandDisplacements.Length; i++)
            {
                if (i + 1 >= Refs.islands.Length) break;
                isleDispList.Add(islandDisplacements[i]);
            }
            Main.saveContainer.islandOffsets = isleDispList.ToArray();
            //Re-roll seed for gameplay
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

        }

        public static void Move()
        {
            if (!setupRan) Setup();

            var regionOffsets = Main.saveContainer.archOffsets;
            var islandOffsets = Main.saveContainer.islandOffsets;
            #region moving code

            foreach (var region in regionDefs)
            {
                int regionId = region.index;
                //Move regions
                if (regions.ContainsKey(region.index))
                {
                    regions[regionId].transform.Translate(regionOffsets[regionId], Space.World);
                    if (Main.saveContainer.borderExpander == 1)
                    {
                        if (regionId != 5) // skip chronos region
                        {
                            regions[regionId].transform.localScale *= Main.saveContainer.islandSpread / 10000;
                        }
                    }
                }
                //Move bottom planes
                if (region.bottomPlane != string.Empty)
                {
                    GameObject.Find(region.bottomPlane).transform.Translate(regionOffsets[regionId], Space.World);
                }
            }

            //Move islands
            for (int i = 0; i < islandOffsets.Length; i++)
            {
                if (i + 1 >= Refs.islands.Length) break;
                if (Refs.islands[i + 1] != null)
                {
                    Refs.islands[i + 1].Translate(islandOffsets[i], Space.World);
                }
            }


            //Move recovery ports
            for (int i = 0; i < Refs.islands.Length; i++)
            {
                var island = Refs.islands[i];
                if (island == null) continue;
                Vector3 pos = FloatingOriginManager.instance.GetGlobeCoords(island.transform);
                Main.Log("i: " + i + " island: " + i + " name: " + island.name + " x: " + pos.x + " z: " + pos.z);
            }

            foreach (var recovery in recoveryArray)
            {
                int homeIndx = ClosestIsland(recovery.gameObject.transform.position);
                recovery.gameObject.transform.Translate(islandOffsets[homeIndx], Space.World);
            }

            if (Main.eastwindFix.Value)
            {
                Main.Log("check Markets positions");
                for (int i = 0; i < Port.ports.Length; i++)
                {
                    if (Port.ports[i] == null) continue;
                    Port port = Port.ports[i];
                    //Main.Log("---");
                    Vector3 pos = FloatingOriginManager.instance.GetGlobeCoords(port.gameObject.transform);
                    //Main.Log("market pos: x:" + pos.x + " z: " + pos.z);
                    pos = FloatingOriginManager.instance.GetGlobeCoords(port.transform);
                    //Main.Log("port pos: " + port.GetPortName() + " pos x:" + pos.x + " z: " + pos.z);

                    // make a mark for this that it not visited for now;
                    marketVisited.Add(port.portIndex, false);

                    if (Main.eastwindFix.Value && port.portIndex == eastwindIndx)
                    {
                        Main.Log("Move Eastwind market to correct position");
                        port.gameObject.transform.SetPositionAndRotation(Refs.islands[19].position, new Quaternion());
                        port.gameObject.transform.Translate(eastwindMarketOffset, Space.World);
                        pos = FloatingOriginManager.instance.GetGlobeCoords(Refs.islands[19]);
                        Main.Log("move market to " + pos.x + " " + pos.z);
                        pos = FloatingOriginManager.instance.GetGlobeCoords(port.gameObject.transform);
                        Main.Log("market new pos " + pos.x + " " + pos.z);
                    }
                }
            }

            //Move unpurchased boats
            foreach (var boat in boatArray)
            {
                if (!boat.isPurchased())
                {
                    int homeIsland = ClosestIsland(boat.gameObject.transform.position);
                    boat.gameObject.transform.Translate(islandOffsets[homeIsland], Space.World);
                }
            }
            #endregion
            if (Main.saveCoordsToJSON_Enabled.Value)
            {
                SaveCoordsToJSON($"scramble_{SaveSlots.currentSlot}");
            }
            if (Main.saveScrambleExternal.Value)
            {
                Main.saveContainer.archDescriptions = "archOffsets are meters from vanilla position. x is east, z is north. Regions: 0 = Al'ankh, 1 = Emerald, 2 = Aestrin, 3 = Oasis, 6 = FireFish";
                Main.saveContainer.islandDescriptions = "islandOffsets are meters from vanilla position. x is longitude, z is latitude.\nIslands: ";
                for (int i = 0; i < Refs.islands.Length; i++) 
                {
                    Main.saveContainer.islandDescriptions += islandNames[i] + ", ";//i + " = " + islandNames[i] + ", ";
                }
            }
        }
        public static void SaveCoordsToJSON(string fileName)
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
/*                            if (name_array[1] == "36")
                            {
                                point_color = "bluepoint";
                                island_name = "island 36";
                                skip = true;
                            }
*//*                                else if (name_array[1] == "42")
                            {
                                point_color = "yellowpoint";
                                island_name = "island 42";
                                skip = true;
                            }*//*
                            else
                            {*/
                                island_name = name;
                                skip = true;
                            //}
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
                        var isle = Refs.islands[Int32.Parse(name_array[1])].GetComponent<IslandHorizon>();
                        Transform center = isle.overrideCenter != null ? isle.overrideCenter.transform : isle.transform;
                        //Vector3 latlon = FloatingOriginManager.instance.GetGlobeCoords(GameObject.Find(name).transform);
                        Vector3 latlon = FloatingOriginManager.instance.GetGlobeCoords(center);
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

                //Main.Log(jsonString);
                File.WriteAllText(Path.Combine(Directory.GetParent(Main.instance.Info.Location).FullName, $"{fileName}.json"), jsonString);
                //Main.saveCoordsToJSON_Instant.Value = false;
            }
            catch(Exception ex)
            {
                
                Main.Log(ex.Message);
                Main.Log(ex.InnerException.Message);
            }

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

        private static int ClosestRegion(Transform transform)
        {
            float minD = 999999f;
            int idx = -1;
            foreach (var region in regions)
            {
                float d = FlatDistance(transform.position, region.Value.transform.position);
                if (d < minD)
                {
                    minD = d;
                    idx = region.Key;
                }

            }
            return idx;
        }
        private static List<RegionDefinition> GenerateDefs()
        {
            List<RegionDefinition> regionDefinitions = new List<RegionDefinition>();
            //These constants need to be updated when new islands/regions are added to game
            Dictionary<int, string> regionToName = new Dictionary<int, string>();
            Dictionary<int, List<int>> regionToIslandIdxs = new Dictionary<int, List<int>>();
            Dictionary<string, int> bottomToRegion = new Dictionary<string, int>();
            // Al Ankh
            regionToName.Add(0, "Region Al'ankh");
            regionToIslandIdxs.Add(0, new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 40 });
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
            regionToIslandIdxs.Add(3, new List<int>() { 20, 41, 42 });
            // Happy Bay
            regionToIslandIdxs.Add(4, new List<int>() { 18 });
            // Chronos
            regionToName.Add(5, "Region Medi East");
            regionToIslandIdxs.Add(5, new List<int>() { 25 });
            bottomToRegion.Add("bottom plane chronos", 5);
            // Fire Fish Lagoon
            regionToName.Add(6, "Region Emerald Lagoon");
            regionToIslandIdxs.Add(6, new List<int>() { 26, 27, 28, 29, 30, 31 });
            // Rock of Despair
            regionToIslandIdxs.Add(7, new List<int>() { 30 });
            // Hideout
            regionToIslandIdxs.Add(8, new List<int>() { 32 });

            #region def export
            foreach (var kv in regionToIslandIdxs)
            {
                regionDefinitions.Add(new RegionDefinition { index = kv.Key, islands = kv.Value });
            }
            foreach (var kv in regionToName)
            {
                regionDefinitions[kv.Key].objectName = kv.Value;
            }
            foreach (var kv in bottomToRegion)
            {
                regionDefinitions[kv.Value].bottomPlane = kv.Key;
            }
            //string json = JsonConvert.SerializeObject(regionDefinitions);
            //File.WriteAllText(path, json);




            #endregion
            return regionDefinitions;
        }
/*        private static void ExportDefs(List<RegionDefinition> regionDefinitions)
        {
            string json = JsonConvert.SerializeObject(regionDefinitions);
            File.WriteAllText(path, json);

        }*/

        public static void WriteRegionDefs(List<RegionDefinition> regions)
        {
            JSONArray arr = new JSONArray();
            foreach (var reg in regions)
            {
                JSONObject obj2 = new JSONObject();
                obj2.Add("index", new JSONNumber(reg.index));
                obj2.Add("objectName", new JSONString(reg.objectName));
                obj2.Add("bottomPlane", new JSONString(reg.bottomPlane));
                JSONArray islands = new JSONArray();
                foreach (int isle in reg.islands)
                {
                    islands.Add(new JSONNumber(isle));
                }
                obj2.Add("islands", islands);
                arr.Add(obj2);
            }
            //Debug.Log(arr.ToString());
            string json2 = arr.ToString();
            File.WriteAllText(Path.Combine(Directory.GetParent(Main.instance.Info.Location).FullName, $"regions.json"), arr.ToString());
        }
        public static List<RegionDefinition> ReadRegionDefs(string path)
        {
            if (!File.Exists(path)) { Debug.LogError("missing file"); return null; }

            List<RegionDefinition> output = new List<RegionDefinition>();
            string json = File.ReadAllText(path);
            var blah = JSON.Parse(json).AsArray;
            foreach (var b in blah)
            {
                var reg = new RegionDefinition();
                var f = b.Value.Linq;
                foreach (var f2 in f)
                {
                    if (f2.Key == "index") reg.index = f2.Value;
                    else if (f2.Key == "objectName") reg.objectName = f2.Value;
                    else if (f2.Key == "bottomPlane") reg.bottomPlane = f2.Value;
                    else if (f2.Key == "islands")
                    {
                        List<int> ints = new List<int>();
                        foreach (var f3 in f2.Value.Children)
                        {
                            ints.Add(f3.AsInt);
                        }
                        reg.islands = ints;
                    }
                }
                output.Add(reg);
            }

            return output;
        }
    }



}
