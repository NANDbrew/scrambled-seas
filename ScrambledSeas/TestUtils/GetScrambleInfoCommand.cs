using SailwindConsole;
using SailwindConsole.Commands;
using System;
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
            text += $"{Environment.NewLine + tabs}seed: {scramble.worldScramblerSeed}";
            text += $"{Environment.NewLine + tabs}world scale: {Main.GetWorldScale()}";
            text += $"{Environment.NewLine + tabs}archipelago scale: {Main.GetArchipelagoScale()}";
            text += $"{Environment.NewLine + tabs}island spread: {scramble.islandSpread}";
            text += $"{Environment.NewLine + tabs}min island separation: {scramble.minIslandSeparation}";
            text += $"{Environment.NewLine + tabs}min archipelago distance: {scramble.minArchipelagoSeparation}";
            text += $"{Environment.NewLine + tabs}latitude range: {scramble.worldLatMin} to {scramble.worldLatMax}";
            text += $"{Environment.NewLine + tabs}longitude range: {scramble.worldLonMin} to {scramble.worldLonMax}";
            string be = scramble.borderExpander == 1 ? "yes" : "no";
            text += $"{Environment.NewLine + tabs}border expander: {be}";
            string filename = Main.saveCoordsToJSON_Enabled.Value ? $"scramble_{SaveSlots.currentSlot}.json" : "unsaved";
            text += $"{Environment.NewLine + tabs}coordinates file: {filename}";
            ModConsoleLog.Log(Main.instance.Info, text);


        }
        
    }
}