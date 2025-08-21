using System.Collections.Generic;
using UnityEngine;

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

        public Dictionary<int, Vector3> archDisps { get; set; }
        public Vector3[] islandDisps { get; set; }
        public int borderExpander {  get; set; } = 0;
    }
}