using SailwindConsole;
using SailwindConsole.Commands;
using System.Collections.Generic;

namespace ScrambledSeas
{
    internal class GetScrambleInfoCommand : Command
    {
        public override string Name => "GetScrambleInfo";

        public override string[] Aliases => new string[]{ "GSI" };

        public override string Usage => "GetScrambleInfo";

        public override string Description => "Show ScrambledSeas scramble data, such as world seed, scale, lat/lon range, etc.";

        public override int MinArgs => 0;

        public override void OnRun(List<string> args)
        {
            string tabs = "\t\t\t\t\t\t\t\t\t\t";
            var scramble = Main.saveContainer;
            string text = $"Scrambler data, version {scramble.version}:";
            text += $"\n{tabs}seed: {scramble.worldScramblerSeed}";
            text += $"\n{tabs}world scale: {Main.GetWorldScale()}";
            text += $"\n{tabs}archipelago scale: {Main.GetArchipelagoScale()}";
            text += $"\n{tabs}island spread: {scramble.islandSpread}";
            text += $"\n{tabs}min island separation: {scramble.minIslandSeparation}";
            text += $"\n{tabs}min archipelago distance: {scramble.minArchipelagoSeparation}";
            text += $"\n{tabs}latitude range: {scramble.worldLatMin} to {scramble.worldLatMax}";
            text += $"\n{tabs}longitude range: {scramble.worldLonMin} to {scramble.worldLonMax}";
            string be = scramble.borderExpander == 1 ? "yes" : "no";
            text += $"\n{tabs}border expander: {be}";
            string filename = Main.saveCoordsToJSON_Enabled.Value ? $"scramble_{SaveSlots.currentSlot}.json" : "unsaved";
            text += $"\n{tabs}coordinates file: {filename}";
            ModConsoleLog.Log(Main.instance.Info, text);


        }
        
    }
}