#if DEBUG
using SailwindConsole;
using SailwindConsole.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ScrambledSeas
{
    internal class GetWorldStatsCommand : Command
    {
        public override string Name => "GetWorldStats";

        public override string[] Aliases => new string[]{ "GWS" };

        public override string Usage => "";

        public override string Description => "";

        public override int MinArgs => 0;

        public override void OnRun(List<string> args)
        {
            string tabs = "\t\t";
            float div = 0.514444f / Sun.sun.initialTimescale;

            float intraAverage = 0;
            Dictionary<string, float[]> intraMinMax = new Dictionary<string, float[]>();
            foreach (var aa in WorldScrambler.regionDefs)
            {
                List<float> nums = new List<float>
                {
                    aa.islands.Count
                };
                if (aa.islands.Count > 1)
                {
                    float localAvg = AverageDist(IslandLocs(aa.islands.ToArray()));
                    intraAverage += localAvg;
                    nums.Add(localAvg);
                    var nf = NearFar(IslandLocs(aa.islands.ToArray()).ToArray());
                    nums.AddRange(new float[]{ nf.x, nf.y });
                }
                string name = aa.objectName.Length > 0 ? aa.objectName : "unnamed / " + Refs.islands[aa.islands[0]].name;
                intraMinMax.Add(name, nums.ToArray());
            }
            intraAverage /= WorldScrambler.regionDefs.Count;

            float globalIsleAvg = AverageDist(IslandLocs(Enumerable.Range(0, Refs.islands.Length).ToArray()));
            Vector2 globalNearFar = NearFar(IslandLocs(Enumerable.Range(0, Refs.islands.Length).ToArray()));
            float globalArchAvg = AverageDist(RegionLocs(WorldScrambler.regions.Values.ToArray()));
            Vector2 archNearFar = NearFar(RegionLocs(WorldScrambler.regions.Values.ToArray()));

            float xMin = 99999;
            float xMax = 0;
            float yMin = 99999;
            float yMax = 0;
            foreach (var isle in Refs.islands)
            {
                if (isle == null) continue;
                var pos = FloatingOriginManager.instance.GetGlobeCoords(isle);
                if (pos.x > xMax) xMax = pos.x;
                else if (pos.x < xMin) xMin = pos.x;
                if (pos.z > yMax) yMax = pos.z;
                else if (pos.z < yMin) yMin = pos.z;
            }

            string isScrambled = GameState.modData.ContainsKey("ScrambledSeas") ? "yes" : "no";
            string stats = "World stats:\n------------------------------------------------------------------------------";
            stats += $"\n{tabs}scrambled: {isScrambled}";
            stats += $"\n{tabs}outliers: min lat = {yMin}, max lat = {yMax}, min long = {xMin}, max long = {xMax}";
            stats += $"\n{tabs}average local island dist: {intraAverage / div} miles, or {intraAverage / 9000} degrees";
            stats += $"\n{tabs}shortest island dist: {globalNearFar.x / div} miles, or {globalNearFar.x / 9000} degrees";
            stats += $"\n{tabs}longest island dist: {globalNearFar.y / div} miles, or {globalNearFar.y / 9000} degrees";
            stats += $"\n{tabs}average island dist: {globalIsleAvg / div} miles, or {globalIsleAvg / 9000} degrees";
            stats += $"\n{tabs}shortest region distance: {archNearFar.x / div} miles, or {archNearFar.x / 9000} degrees";
            stats += $"\n{tabs}longest region distance: {archNearFar.y / div} miles, or {archNearFar.y / 9000} degrees";
            stats += $"\n{tabs}average region distance: {globalArchAvg / div} miles, or {globalArchAvg / 9000} degrees";
            stats += $"\n{tabs}archipelago stats:";
            foreach (var ar in intraMinMax)
            {
                stats += $"\n{tabs}\t{ar.Key}:";
                stats += $"\n{tabs}\t\tisland count: {ar.Value[0]}";
                if (ar.Value[0] > 1)
                {
                    stats += $"\n{tabs}\t\tshortest dist: {ar.Value[2] / div} miles, or {ar.Value[2] / 9000} degrees";
                    stats += $"\n{tabs}\t\tlongest dist: {ar.Value[3] / div} miles, or {ar.Value[3] / 9000} degrees";
                    stats += $"\n{tabs}\t\taverage: {ar.Value[1] / div} miles, or {ar.Value[1] / 9000} degrees";
                }
            }
            stats += "\n------------------------------------------------------------------------------";
            ModConsoleLog.Log(Main.instance.Info, stats);

        }

        private Vector2 NearFar(Vector3[] locations)
        {
            float nearest = 9999999;
            float farthest = 0;
            for (int i = 0; i < locations.Length; i++)
            {
                for (int j = 0; j < locations.Length; j++)
                {
                    if (i != j)
                    {
                        float dist = Vector3.Distance(locations[i], locations[j]);
                        nearest = Mathf.Min(nearest, dist);
                        farthest = Mathf.Max(farthest, dist);

                    }
                }
            }

            return new Vector2(nearest, farthest);
        }

        private static float AverageDist(Vector3[] locations)
        {
            float total = 0;
            int count = 0;
            for (int i = 0; i < locations.Length; i++)
            {

                for (int j = 0; j < locations.Length; j++)
                {
                    if (i != j)
                    {
                        float dist = Vector3.Distance(locations[i], locations[j]);
                        total += dist;
                        count++;

                    }
                }
            }

            return total / count;
        }
        private static Vector3[] IslandLocs(int[] isles)
        {
            List<Vector3> locs = new List<Vector3>();
            for (int i = 0; i < isles.Length;i++)
            {
                if (Refs.islands[isles[i]] == null)
                {
                    continue;
                }
                var isle = Refs.islands[isles[i]].GetComponent<IslandHorizon>();
                locs.Add(isle.overrideCenter != null ? isle.overrideCenter.transform.position : isle.transform.position);
            }

            return locs.ToArray();
        }
        private static Vector3[] RegionLocs(Region[] regions)
        {
            Vector3[] locs = new Vector3[regions.Length];
            for (int i = 0; i < regions.Length; i++)
            {
                locs[i] = regions[i].transform.position;
            }

            return locs;
        }
    }
}
#endif