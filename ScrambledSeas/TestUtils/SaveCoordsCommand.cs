using SailwindConsole;
using SailwindConsole.Commands;
using System;
using System.Collections.Generic;
using System.IO;

namespace ScrambledSeas
{
    internal class SaveCoordsCommand : Command
    {
        public override string Name => "SaveCoords";

        //public override string[] Aliases => new string[]{ "GSI" };

        public override string Usage => "SaveCoords <file type (json, xml)> [filename]";

        public override string Description => "Export the current scramble (or vanilla island coords) to either an xml for editing or a json for the online map. If filename is unspecified, file will be named \"scramble_(save slot)\"";

        public override int MinArgs => 0;

        public override void OnRun(List<string> args)
        {
            string filename = args.Count > 1 ? args[1] : $"scramble_{SaveSlots.currentSlot}";
            try
            {
                if (args[0].ToLower() == "xml")
                {
                    System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(ScrambledSeasSaveContainer));
                    using (System.IO.StringWriter textWriter = new System.IO.StringWriter())
                    {
                        string path = Path.Combine(Directory.GetParent(Main.instance.Info.Location).FullName, filename + ".xml");
                        xmlSerializer.Serialize(textWriter, Main.saveContainer);
                        File.WriteAllText(path, textWriter.ToString());
                        ModConsoleLog.Log(Main.instance.Info, "Saved file to: " + path);
                    }
                }
                if (args[0].ToLower() == "json")
                {
                    WorldScrambler.SaveCoordsToJSON(filename);
                    ModConsoleLog.Log(Main.instance.Info, Path.Combine(Directory.GetParent(Main.instance.Info.Location).FullName, $"{filename}.json"));
                }
            }
            catch (Exception e)
            {
                ModConsoleLog.Error(Main.instance.Info, e.Message);
            }

        }
        
    }
}