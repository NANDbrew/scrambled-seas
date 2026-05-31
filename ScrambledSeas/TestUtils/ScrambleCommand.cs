#if DEBUG
using SailwindConsole;
using SailwindConsole.Commands;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScrambledSeas
{
    internal class ScrambleCommand : Command
    {
        public override string Name => "Scramble";

        //public override string[] Aliases => new string[]{ "" };

        public override string Usage => "Scramble <world scale> <arch scale> [seed] [borderExpander(-be, +be)]";

        public override string Description => "";

        public override int MinArgs => 2;

        public override void OnRun(List<string> args)
        {
            ScrambledSeasSaveContainer save = new ScrambledSeasSaveContainer();

            if (args.Count >= 3 && int.TryParse(args[2], out int seed))
            {
                save.worldScramblerSeed = seed;
            }
            else 
            {
                save.worldScramblerSeed = (int)System.DateTime.Now.Ticks; 
            }

            if (args.Last().ToLower() == "-be")
            {
                save.borderExpander = 0;
            }
            else if (args.Last().ToLower() == "+be")
            {
                save.borderExpander = 1;
            }
            else
            {
                save.borderExpander = Main.borderExpander? 1 : 0;
            }

            if (float.TryParse(args[0], out float scaleW))
            {
                int maxLat = save.borderExpander == 1 ? 70 : 46;
                int minLat = save.borderExpander == 1 ? -70 : 26;
                save.worldLonMin = (int)(-12 * scaleW);
                save.worldLonMax = (int)(32 * scaleW);
                save.worldLatMin = (int)Mathf.Max(minLat, (26 - 10 * scaleW));
                save.worldLatMax = (int)Mathf.Min(maxLat, (46 + 10 * scaleW));
                save.minArchipelagoSeparation = (int)(Main.defaultMinArchipelagoSeparation * scaleW);
                //Main.worldScale.Value = val;
            }
            else
            {
                ModConsoleLog.Error(Main.instance.Info, "invalid world scale");
                return;
            }

            if (float.TryParse(args[1], out float scaleA))
            {
                save.islandSpread = (int)(Main.defaultIslandSpread * scaleA);
                save.minIslandSeparation = (int)(Main.defaultMinIslandSeparation * scaleA);
            }
            else
            {
                ModConsoleLog.Error(Main.instance.Info, "invalid archipelago scale");
                return;
            }

            ModConsoleLog.Log(Main.instance.Info, "Scrambling world with seed " + save.worldScramblerSeed);
            Main.saveContainer = save;

            WorldScrambler.Scramble();
            WorldScrambler.Move();
        }

    }
}
#endif