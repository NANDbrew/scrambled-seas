using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static OVRPlugin;
using static UnityEngine.GraphicsBuffer;

namespace ScrambledSeas
{
    public static class Patches
    {
        [HarmonyPatch(typeof(IslandHorizon), "Start")]
        private static class IslandPatch
        {
            private static void Prefix(IslandHorizon __instance)
            {
                if (Main.pluginEnabled && __instance.islandIndex > 0)
                {
                    WorldScrambler.islandNames[__instance.islandIndex - 1] = __instance.gameObject.name;
                    WorldScrambler.islandOrigins[__instance.islandIndex - 1] = __instance.GetPosition();
                    //WorldScrambler.islandOriginPos[__instance.islandIndex - 1] = __instance.gameObject.transform.localPosition; ; //gameObject.transform.localPosition;
                    Main.Log("isl name" + __instance.gameObject.name + " ovr " + (bool)__instance.overrideCenter);
                    //Main.Log("isl lpos x:" + __instance.gameObject.transform.localPosition.x / 9000.0f + " isl lpos z: " + ((__instance.gameObject.transform.localPosition.z / 9000.0f)+36.0f));
                    Main.Log("isl pos x:" + __instance.GetPosition().x / 9000.0f + " isl pos z: " + ((__instance.GetPosition().z / 9000.0f)+36.0f));
                }
            }
        }
        [HarmonyPatch(typeof(PurchasableBoat), "Awake")]
        private static class BoatAwakePatch
        {
            private static void Prefix(PurchasableBoat __instance)
            {
                WorldScrambler.boatArray.Add(__instance);
            }
        }
        [HarmonyPatch(typeof(Recovery), "RegisterPort")]
        private static class RecoveryPortPatch
        {
            private static void Prefix(RecoveryPort port)
            {
                WorldScrambler.recoveryArray.Add(port);
            }
        }

        [HarmonyPatch(typeof(SaveLoadManager), "SaveModData")]
        private static class SavePatch
        {
            private static void Postfix()
            {
                if (Main.pluginEnabled)
                {
                    Main.saveContainer.version = WorldScrambler.version;
                    SaveFileHelper.Save(Main.saveContainer, "ScrambledSeas");
                }
            }
        }

        [HarmonyPatch(typeof(SaveLoadManager), "LoadModData")]
        private static class LoadGamePatch
        {
            private static void Postfix()
            {
                if (GameState.modData != null && GameState.modData.ContainsKey("ScrambledSeas"))
                {
                    Main.pluginEnabled = true;

                    //Load entire ScrambledSeasSaveContainer from save file
                    Main.saveContainer = SaveFileHelper.Load<ScrambledSeasSaveContainer>("ScrambledSeas");

                    if (Main.saveContainer.version < WorldScrambler.version)
                    { //TODO: update min version if save compatibility breaks again
                        NotificationUi.instance.ShowNotification("ERROR: This save is not\ncompatiblewith this version\nof Scrambled Seas");
                        throw new System.InvalidOperationException("ERROR: This save is not compatible with this version of Scrambled Seas");
                    }

                    //Re-generate world for the saved randomizer params

                    if (Main.saveContainer.islandOffsets.Length + 1 < Refs.islands.Length && Main.saveContainer.worldScramblerSeed != 0)
                    {
                        var savedArchOffsets = Main.saveContainer.archOffsets;
                        var savedIsleOffsets = Main.saveContainer.islandOffsets;
                        WorldScrambler.Scramble();
                        for (int i = 0; i < savedArchOffsets.Length; i++)
                        {
                            Main.saveContainer.archOffsets[i] = savedArchOffsets[i];
                        }
                        for (int i = 0; i < savedIsleOffsets.Length; i++)
                        {
                            Main.saveContainer.islandOffsets[i] = savedIsleOffsets[i];
                        }
                        Main.Log("Re-scrambled " + (Refs.islands.Length - savedIsleOffsets.Length) + " islands");
                    }

                    WorldScrambler.Move();
                    NotificationUi.instance.ShowNotification("Scrambled Seas:\nLoaded scrambled save", 5f);
                    if (Main.saveContainer.borderExpander == 1 && !Main.borderExpander)
                    {
                        NotificationUi.instance.ShowNotification("WARNING: This save was made\nwith Border Expander.\nSome islands may be inaccessible", 10f);
                    }
                }
                else
                {
                    Main.pluginEnabled = false;
                    NotificationUi.instance.ShowNotification("Scrambled Seas:\nThis save is unscrambled", 5f);
                    WorldScrambler.SaveCoordsToJSON("islandCoords");

                }
            }
        }

        [HarmonyPatch(typeof(StartMenu), "StartNewGame")]
        private static class StartNewGamePatch
        {
            private static bool Prefix(StartMenu __instance, ref bool ___fPressed, ref Transform ___playerObserver, ref GameObject ___playerController, ref int ___animsPlaying, ref int ___currentRegion, ref Transform ___startApos, ref Transform ___startEpos, ref Transform ___startMpos)
            {
                Main.pluginEnabled = Main.random_Enabled.Value;

                if (Main.pluginEnabled)
                {
                    if (Main.loadExternal)
                    {
                        ScrambledSeasSaveContainer loaded = SaveFileHelper.Load<ScrambledSeasSaveContainer>("ScrambledSeas");
                        if (loaded.version == 0)
                        {
                            NotificationUi.instance.ShowNotification("failed to load scramble");
                            return false;
                        }
                        Main.saveContainer = loaded;

                        if (Main.saveContainer.worldScramblerSeed != 0)
                        {
                            WorldScrambler.Scramble();
                        }
                    }
                    else
                    { 
                        //Create a randomized world with a new seed
                        Main.saveContainer.worldScramblerSeed = (int)System.DateTime.Now.Ticks;
                        WorldScrambler.Scramble();

                    }
                    WorldScrambler.Move();
                    //Move player start positions to new island locations
                    ___startApos.Translate(Main.saveContainer.islandOffsets[2], Space.World);
                    ___startEpos.Translate(Main.saveContainer.islandOffsets[10], Space.World);
                    ___startMpos.Translate(Main.saveContainer.islandOffsets[20], Space.World);

                    ___animsPlaying++;
                    Transform transform = null;
                    switch(___currentRegion)
                    {
                        default:
                        case 0:
                            transform = ___startApos;
                            GameState.newGameRegion = PortRegion.alankh;
                            break;
                        case 1:
                            transform = ___startEpos;
                            GameState.newGameRegion = PortRegion.emerald;
                            break;
                        case 2:
                            transform = ___startMpos;
                            GameState.newGameRegion = PortRegion.medi;
                            break;
                    }

                    __instance.InvokePrivateMethod("DisableIslandMenu");
                    __instance.StartCoroutine(MovePlayerToStartPos(__instance, transform, ___playerObserver, ___playerController));

                    return false;
                }
                return true;
            }

            public static IEnumerator MovePlayerToStartPos(StartMenu instance, Transform startPos, Transform playerObserver, GameObject playerController)
            {
                playerObserver.transform.parent = instance.gameObject.transform.parent;
                playerObserver.position = startPos.position;
                playerController.transform.position = startPos.position;
                instance.GetPrivateField<GameObject>("logo").SetActive(false);
                instance.GetPrivateField<Transform>("playerObserver").transform.parent = instance.transform.parent;
                float animTime = 0;
                Juicebox.juice.TweenPosition(instance.GetPrivateField<Transform>("playerObserver").gameObject, startPos.position, animTime, JuiceboxTween.quadraticInOut);
                for (float t = 0f; t < animTime; t += Time.deltaTime)
                {
                    instance.GetPrivateField<Transform>("playerObserver").rotation = Quaternion.Lerp(instance.GetPrivateField<Transform>("playerObserver").rotation, startPos.rotation, Time.deltaTime * 0.35f);
                    yield return new WaitForEndOfFrame();
                }
                instance.GetPrivateField<Transform>("playerObserver").rotation = startPos.rotation;
                instance.GetPrivateField<GameObject>("playerController").transform.position = instance.GetPrivateField<Transform>("playerObserver").position;
                instance.GetPrivateField<GameObject>("playerController").transform.rotation = instance.GetPrivateField<Transform>("playerObserver").rotation;
                yield return new WaitForEndOfFrame();
                instance.GetPrivateField<GameObject>("playerController").GetComponent<CharacterController>().enabled = true;
                instance.GetPrivateField<GameObject>("playerController").GetComponent<OVRPlayerController>().enabled = true;
                instance.GetPrivateField<Transform>("playerObserver").gameObject.GetComponent<PlayerControllerMirror>().enabled = true;
                MouseLook.ToggleMouseLookAndCursor(true);
                instance.GetPrivateField<PurchasableBoat[]>("startingBoats")[instance.GetPrivateField<int>("currentRegion")].LoadAsPurchased();
                instance.StartCoroutine(Blackout.FadeTo(1f, 0.2f));
                yield return new WaitForSeconds(0.2f);
                yield return new WaitForEndOfFrame();
                instance.GetPrivateField<GameObject>("disclaimer").SetActive(true);
                instance.SetPrivateField("waitingForFInput", true);
                while (!instance.GetPrivateField<bool>("fPressed"))
                {
                    yield return new WaitForEndOfFrame();
                }
                instance.GetPrivateField<GameObject>("disclaimer").SetActive(false);
                instance.StartCoroutine(Blackout.FadeTo(0f, 0.3f));
                yield return new WaitForEndOfFrame();
                SaveLoadManager.readyToSave = true;
                GameState.playing = true;
                GameState.justStarted = true;
                MouseLook.ToggleMouseLook(true);
                int animsPlaying = (int)Traverse.Create(instance).Field("animsPlaying").GetValue();
                Traverse.Create(instance).Field("animsPlaying").SetValue(animsPlaying - 1);
                yield return new WaitForSeconds(1f);
                GameState.justStarted = false;

                yield break;
            }
        }

        [HarmonyPatch(typeof(StartMenu), "MovePlayerToStartPos")]
        private static class MovePlayerPatch
        {
            private static void Prefix(Transform startPos, StartMenu __instance, ref Transform ___playerObserver, ref GameObject ___playerController)
            {
                if (Main.pluginEnabled)
                {
                    //Teleport player to shifted starting position
                    ___playerObserver.transform.parent = __instance.gameObject.transform.parent;
                    ___playerObserver.position = startPos.position;
                    ___playerController.transform.position = startPos.position;
                    //This will be followed by an animation performed by Juicebox.juice.TweenPosition(), but it messes the position up. I've disabled it below...
                }
            }
        }

        [HarmonyPatch(typeof(Juicebox), "TweenPosition", new System.Type[] { typeof(GameObject), typeof(Vector3), typeof(float), typeof(JuiceboxTween) })]
        private static class TweenPatch
        {
            private static bool Prefix()
            {
                if (Main.pluginEnabled)
                {
                    //This just disables the original method completely. Otherwise, it glitches out while moving the player over a large distance.
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(StarterSet), "InitiateStarterSet")]
        private static class StarterSetPatch
        {
            private static void Prefix(StarterSet __instance)
            {
                if (Main.pluginEnabled)
                {
                    //Figure out which island we start on
                    Vector3 startOffset = new Vector3();
                    if (__instance.region == PortRegion.alankh)
                    {
                        startOffset = WorldScrambler.islandDisplacements[2];
                        PlayerGold.currency[0] += 48;
                    }
                    if (__instance.region == PortRegion.emerald)
                    {
                        startOffset = WorldScrambler.islandDisplacements[10];
                        PlayerGold.currency[1] += Mathf.RoundToInt(48 * CurrencyMarket.instance.GetExchangeRate(0, 1, false));
                    }
                    if (__instance.region == PortRegion.medi)
                    {
                        startOffset = WorldScrambler.islandDisplacements[20];
                        PlayerGold.currency[2] += Mathf.RoundToInt(48 * CurrencyMarket.instance.GetExchangeRate(0, 2, false));
                    }
                    //Move starter set items to new island location
                    GameObject mapObject = null;
                    foreach (Transform starterItem in __instance.gameObject.transform)
                    {
                        starterItem.Translate(startOffset, Space.World);
                        if (starterItem.name.ToLower().Contains("map"))
                        {
                            mapObject = starterItem.gameObject;
                        }
                    }
                    if (mapObject)
                    {
                        GameObject.Destroy(mapObject);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerReputation), "GetMaxDistance")]
        private static class ReputationPatch
        {
            private static void Postfix(ref float __result)
            {
                //Islands tend to be farther apart in this mod. Ensure that the returned value is at least 300 miles
                if (Main.pluginEnabled)
                {
                    float islandSpread = Main.saveContainer.islandSpread;
                    float minArchDist = Main.saveContainer.minArchipelagoSeparation;
                    float maxDist = 150;
                    Debug.Log("maxDist starts at " +  maxDist);
                    maxDist *= islandSpread / 3000;
                    Debug.Log("maxDist operation 1: " + maxDist);
                    maxDist *= minArchDist / 30000;
                    Debug.Log("max dist operation 2: " + maxDist);
                    __result = Mathf.Max(100, maxDist);
                }
            }
        }

        [HarmonyPatch(typeof(MissionDetailsUI), "UpdateMap")]
        private static class MissionMapPatch
        {
            private static void Postfix(ref Mission ___currentMission, ref Renderer ___mapRenderer, ref TextMesh ___locationText)
            {
                if (Main.pluginEnabled)
                {

                    ___mapRenderer.gameObject.SetActive(value: false);
                    if (Main.destinationHint.Value == DestinationHint.Coords)
                    {
                        Vector3 globeCoords = FloatingOriginManager.instance.GetGlobeCoords(___currentMission.destinationPort.transform);
                        float num = globeCoords.x;
                        float num2 = globeCoords.z; // Mathf.RoundToInt(globeCoords.z);
                        string text = ((num < 0) ? "W" : "E");
                        string text2 = ((num2 < 0) ? "S" : "N");
                        //___locationText.text = "(map unavailable) (visited " + WorldScrambler.marketVisited[___currentMission.destinationPort.portIndex] + ")\n\napproximate location:\n" + num2.ToString("0.00") + " " + text2 + ", " + num.ToString("0.00") + " " + text;
                        ___locationText.text = "(map unavailable)\n\napproximate location:\n" + num2.ToString("0.00") + " " + text2 + ", " + num.ToString("0.00") + " " + text;
                    }
                    else if (Main.destinationHint.Value == DestinationHint.Heading)
                    {
                        Vector2 origin = new Vector2(___currentMission.originPort.transform.position.x, ___currentMission.originPort.transform.position.z);
                        Vector2 destination = new Vector2(___currentMission.destinationPort.transform.position.x, ___currentMission.destinationPort.transform.position.z);
                        //float heading = Vector3.SignedAngle(___currentMission.originPort.transform.position, ___currentMission.destinationPort.transform.position, Vector3.up);
                        float heading = Vector2.SignedAngle((origin - destination).normalized, Vector2.down);
                        if (heading < 0f) heading += 360;                        //___locationText.text = "(map unavailable)\n\nheading: " + heading.ToString();
                        string text = "(map unavailable)\n\napproximate location:\n" +
                                ___currentMission.distance.ToString("0") + " miles\n" +
                                RadRefinements.CompassRose.GetCardinalDirection(heading, Main.cardinalPrecisionLevel.Value).ToLower() +
                                "\n";

                        ___locationText.text = text;
                    }
                    else
                    {
                        ___locationText.text = "(map unavailable)\n\nlocation unknown\n";
                    }
                    
                }

            }
        }

        [HarmonyPatch(typeof(RegionBlender), "Update")]
        private static class RegionBlenderPatch
        {
            private static float regionUpdateCooldown = 0f;

            private static void Prefix(ref Region ___currentTargetRegion, ref Transform ___player)
            {
                if (Main.pluginEnabled && !Main.borderExpander)
                {
                    if (regionUpdateCooldown <= 0f)
                    {
                        regionUpdateCooldown = 100f;
                        float minDist = 100000000f;
                        Region closestRegion = null;
                        foreach (Region region in WorldScrambler.regions.Values)
                        {
                            float dist = Vector3.Distance(___player.position, region.transform.position);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                closestRegion = region;
                            }
                        }
                        if (closestRegion != null)
                        {
                            ___currentTargetRegion = closestRegion;
                        }
                    }
                    else
                    {
                        regionUpdateCooldown -= Time.deltaTime;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StartMenu))]
        internal static class StartMenuPatch
        {
            internal static Transform scramblerUI;
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            private static void Postfix(GameObject ___chooseIslandUI)
            {
                scramblerUI = UnityEngine.GameObject.Instantiate(AssetTools.bundle.LoadAsset<GameObject>("Assets/ScrambledSeas/ScramblerUI.prefab"), ___chooseIslandUI.transform).transform;
                scramblerUI.transform.Translate(0f, 0.15f, 0f, Space.Self);
                var oldCheckbox = scramblerUI.Find("checkbox").GetComponent<GPButtonSettingsCheckbo>();
                var newCheckBox = oldCheckbox.gameObject.AddComponent<GPButtonCheckBox>();
                newCheckBox.text = oldCheckbox.text;
                newCheckBox.type = 0;
                newCheckBox.extraToggleOn = scramblerUI.Find("controls").gameObject;
                Component.Destroy(oldCheckbox);
                newCheckBox.Initialize();

                var oldCheckbox2 = scramblerUI.Find("controls/checkbox (1)").GetComponent<GPButtonSettingsCheckbo>();
                var newCheckBox2 = oldCheckbox2.gameObject.AddComponent<GPButtonCheckBox>();
                newCheckBox2.text = oldCheckbox2.text;
                newCheckBox2.type = 1;
                newCheckBox2.extraToggleOff = scramblerUI.Find("controls/sliders").gameObject;
                newCheckBox2.extraToggleOn = scramblerUI.Find("controls/load_options").gameObject;
                Component.Destroy(oldCheckbox2);
                newCheckBox2.Initialize();

/*                var oldCheckbox3 = scramblerUI.Find("controls/load_options/seed_only_box").GetComponent<GPButtonSettingsCheckbo>();
                var newCheckBox3 = oldCheckbox3.gameObject.AddComponent<GPButtonCheckBox>();
                newCheckBox3.text = oldCheckbox3.text;
                newCheckBox3.type = 2;
                newCheckBox3.extraToggleOff = scramblerUI.Find("controls/load_options/file_scale").gameObject;
                newCheckBox3.extraToggleOn = scramblerUI.Find("controls/sliders").gameObject;
                Component.Destroy(oldCheckbox3);
                newCheckBox3.Initialize();*/

                var oldSlider1 = scramblerUI.Find("controls/sliders/slider world scale").GetComponent<GPButtonSliderVolume>();

                var oldSlider2 = scramblerUI.Find("controls/sliders/slider arch scale").GetComponent<GPButtonSliderVolume>();

                var newSlider1 = oldSlider1.gameObject.AddComponent<GPButtonSliderScale>();
                newSlider1.text = oldSlider1.text;
                newSlider1.extraText = oldSlider1.extraText;
                newSlider1.bar = oldSlider1.bar;
                newSlider1.type = 0;
                Component.Destroy(oldSlider1);
                newSlider1.Initialize();

                var newSlider2 = oldSlider2.gameObject.AddComponent<GPButtonSliderScale>();
                newSlider2.text = oldSlider2.text;
                newSlider2.extraText = oldSlider2.extraText;
                newSlider2.bar = oldSlider2.bar;
                newSlider2.type = 1;
                Component.Destroy(oldSlider2);
                newSlider2.Initialize();

            }

            [HarmonyPatch("EnableIslandMenu")]
            [HarmonyPostfix]
            private static void IslandMenuPatch()
            {
                if (Main.loadExternal)
                {
                }
            }

        }

    }
}
