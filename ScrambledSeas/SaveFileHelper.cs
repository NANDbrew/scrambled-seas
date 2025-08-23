using OVRSimpleJSON;
using System.IO;
using UnityEngine;

namespace ScrambledSeas
{
    // This is good boilerplate code to paste into all your mods that need to store data in the save file:
    public static class SaveFileHelper
    {
        // How to use: 
        // Main.myModsSaveContainer = SaveFileHelper.Load<MyModsSaveContainer>("MyModName");
        public static T Load<T>(this string modName) where T : new()
        {
            string xmlStr;

            if (Main.saveScrambleExternal.Value)
            {
                string path = Path.Combine(Directory.GetParent(Main.instance.Info.Location).FullName, $"scramble_{SaveSlots.currentSlot}.xml");
                if (File.Exists(path))
                {
                    xmlStr = File.ReadAllText(path);
                    Debug.Log("Proceeding to parse save data from file for " + modName);
                    System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                    using (System.IO.StringReader textReader = new System.IO.StringReader(xmlStr))
                    {
                        return (T)xmlSerializer.Deserialize(textReader);
                    }
                }
            }

            if (GameState.modData != null && GameState.modData.TryGetValue(modName, out xmlStr))
            {
                Debug.Log("Proceeding to parse save data for " + modName);
                System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                using (System.IO.StringReader textReader = new System.IO.StringReader(xmlStr))
                {
                    return (T)xmlSerializer.Deserialize(textReader);
                }
            }
            Debug.Log("Cannot load data from save file. Using defaults for " + modName);
            return new T();
        }

        // How to use:
        // SaveFileHelper.Save(Main.myModsSaveContainer, "MyModName");
        public static void Save<T>(this T toSerialize, string modName)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (System.IO.StringWriter textWriter = new System.IO.StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                GameState.modData[modName] = textWriter.ToString();
                Debug.Log("Packed save data for " + modName);

                if (Main.saveScrambleExternal.Value)
                {
                    File.WriteAllText(Path.Combine(Directory.GetParent(Main.instance.Info.Location).FullName, $"scramble_{SaveSlots.currentSlot}.xml"), textWriter.ToString());
                }
            }
        }
    }
}
