using System.IO;
using UnityEngine;

namespace ScrambledSeas
{
    internal class AssetTools
    {
        public static AssetBundle bundle;
        const string assetFile = "scrambledseas.assets";

        public static void LoadAssetBundles()    //Load the bundle
        {
            string dataPath = Directory.GetParent(Main.instance.Info.Location).FullName;
            string filePath = Path.Combine(dataPath, assetFile);

            if (File.Exists(filePath)) bundle = AssetBundle.LoadFromFile(filePath);
            else { Debug.LogError(Main.NAME + ": File not found!"); }
            if (bundle == null)
            {
                Debug.LogError(Main.NAME + ": Bundle not loaded! Did you place it in the correct folder?");
            }
            else { Debug.Log(Main.NAME + ": loaded bundle " + bundle.ToString()); }
        }
    }
}
