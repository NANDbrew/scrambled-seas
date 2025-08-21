using System.IO;
using UnityEngine;

namespace ScrambledSeas
{
    internal class AssetTools
    {
        public static AssetBundle bundle;
        //const string assetDir = "";
        const string assetFile = "scrambledseas.assets";
        static string combined;
        public static void LoadAssetBundles()    //Load the bundle
        {
            string dataPath = Directory.GetParent(Main.instance.Info.Location).FullName;
            //string firstTry = Path.Combine(dataPath, assetDir, assetFile);
            string secondTry = Path.Combine(dataPath, assetFile);
            //else { Debug.LogError("TowableBoats: can't find asset file"); return; }
            combined = secondTry;
            if (File.Exists(secondTry)) bundle = AssetBundle.LoadFromFile(secondTry);
            else { Debug.LogError("BULLSHITT!!"); }
            if (bundle == null)
            {
                Debug.LogError("testbed: Bundle not loaded! Did you place it in the correct folder?");
            }
            else { Debug.Log("testbed: loaded bundle " + bundle.ToString()); }
        }
    }
}
