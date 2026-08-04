using BepInEx;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScrambledSeas
{
    internal class AssetTools
    {
        public static AssetBundle bundle;
        const string assetFile = "scrambledseas.assets";

        internal static Dictionary<int, Texture2D> mapTextures;


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

            // blank maps
            string[] mapPaths = { "Assets/ScrambledSeas/map_ocean.png", "Assets/ScrambledSeas/map_alankh.png", "Assets/ScrambledSeas/map_emerald.png", "Assets/ScrambledSeas/map_aestrin.png", "Assets/ScrambledSeas/map_alankh.png" };
            mapTextures = new Dictionary<int, Texture2D>();
            for (int m = 0; m < mapPaths.Length; m++)
            {
                mapTextures[m + 115] = bundle.LoadAsset<Texture2D>(mapPaths[m]);

            }
        }
    }
}
