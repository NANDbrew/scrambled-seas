using SailwindConsole;
using SailwindConsole.Commands;
using System;
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

        public override string Description => "Show distance stats for islands and archipelagos";

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
            string stats = $"World stats:{Environment.NewLine}------------------------------------------------------------------------------";
            stats += $"{Environment.NewLine + tabs}scrambled: {isScrambled}";
            stats += $"{Environment.NewLine + tabs}outliers: min lat = {yMin}, max lat = {yMax}, min long = {xMin}, max long = {xMax}";
            stats += $"{Environment.NewLine + tabs}average local island dist: {intraAverage} meters, or {intraAverage / div} miles, or {intraAverage / 9000} degrees";
            stats += $"{Environment.NewLine + tabs}shortest island dist: {globalNearFar.x} meters, or {globalNearFar.x / div} miles, or {globalNearFar.x / 9000} degrees";
            stats += $"{Environment.NewLine + tabs}longest island dist: {globalNearFar.y} meters, or {globalNearFar.y / div} miles, or {globalNearFar.y / 9000} degrees";
            stats += $"{Environment.NewLine + tabs}average island dist: {globalIsleAvg} meters, or {globalIsleAvg / div} miles, or {globalIsleAvg / 9000} degrees";
            stats += $"{Environment.NewLine + tabs}shortest region distance: {archNearFar.x} meters, or {archNearFar.x / div} miles, or {archNearFar.x / 9000} degrees";
            stats += $"{Environment.NewLine + tabs}longest region distance: {archNearFar.y} meters, or {archNearFar.y / div} miles, or {archNearFar.y / 9000} degrees";
            stats += $"{Environment.NewLine + tabs}average region distance: {globalArchAvg} meters, or {globalArchAvg / div} miles, or {globalArchAvg / 9000} degrees";
            stats += $"{Environment.NewLine + tabs}archipelago stats:";
            foreach (var ar in intraMinMax)
            {
                stats += $"{Environment.NewLine + tabs}\t{ar.Key}:";
                stats += $"{Environment.NewLine + tabs}\t\tisland count: {ar.Value[0]}";
                if (ar.Value[0] > 1)
                {
                    stats += $"{Environment.NewLine + tabs}\t\tshortest dist: {ar.Value[2] / div} miles, or {ar.Value[2] / 9000} degrees";
                    stats += $"{Environment.NewLine + tabs}\t\tlongest dist: {ar.Value[3] / div} miles, or {ar.Value[3] / 9000} degrees";
                    stats += $"{Environment.NewLine + tabs}\t\taverage: {ar.Value[1] / div} miles, or {ar.Value[1] / 9000} degrees";
                }
            }
            stats += Environment.NewLine + "------------------------------------------------------------------------------";
            ModConsoleLog.Log(Main.instance.Info, stats);

        }

        private Vector2 NearFar(Vector3[] locations)
        {
            float nearest = 9999999;
            float farthest = 0;
            for (int i = 0; i < locations.Length; i++)
            {
                if (locations[i] == Vector3.zero) { continue; }
                for (int j = 0; j < locations.Length; j++)
                {
                    if (locations[j] == Vector3.zero) { continue; }
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
                if (locations[i] == Vector3.zero) { continue; }
                for (int j = 0; j < locations.Length; j++)
                {
                    if (locations[j] == Vector3.zero) { continue; }
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