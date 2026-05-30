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
            //string[] mapPaths = { Path.Combine(dataPath, "map_ocean.png"), Path.Combine(dataPath, "map_alankh.png"), Path.Combine(dataPath, "map_emerald.png"), Path.Combine(dataPath, "map_aestrin.png"), Path.Combine(dataPath, "map_alankh.png") };
            string[] mapPaths = { "Assets/ScrambledSeas/map_ocean.png", "Assets/ScrambledSeas/map_alankh.png", "Assets/ScrambledSeas/map_emerald.png", "Assets/ScrambledSeas/map_aestrin.png", "Assets/ScrambledSeas/map_alankh.png" };
            mapTextures = new Dictionary<int, Texture2D>();
            for (int m = 0; m < mapPaths.Length; m++)
            {
                //if (!File.Exists(mapPaths[m])) { continue; }
                //if (PrefabsDirectory.instance.directory[m + 115] == null) { continue; }
/*                var tt = bundle.LoadAsset<Texture2D>(mapPaths[m]);
                Texture2D tex = new Texture2D(1, 1);
                tex.LoadImage(File.ReadAllBytes(mapPaths[m]));*/
                mapTextures[m + 115] = bundle.LoadAsset<Texture2D>(mapPaths[m]);
                //PrefabsDirectory.instance.directory[m + 115].transform.GetChild(0).GetComponent<Renderer>().material.mainTexture = tex;

            }
        }
    }
}
