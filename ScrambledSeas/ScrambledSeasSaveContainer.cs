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
        public static int border_offset_deg { get; set; } = 2;
        public int worldLonMin { get; set; } = -12 + border_offset_deg;
        public int worldLonMax { get; set; } = 32 - border_offset_deg;
        public int worldLatMin { get; set; } = 26 + border_offset_deg;
        public int worldLatMax { get; set; } = 46 - border_offset_deg;
    }


}