using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static OVRPlugin;

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
                    //Load entire ScrambledSeasSaveContainer from save file
                    Main.saveContainer = SaveFileHelper.Load<ScrambledSeasSaveContainer>("ScrambledSeas");

                    if (Main.saveContainer.version < 60)
                    { //TODO: update min version if save compatibility breaks again
                        NotificationUi.instance.ShowNotification("ERROR: This save is not\ncompatiblewith this version\nof Scrambled Seas");
                        throw new System.InvalidOperationException("ERROR: This save is not compatible with this version of Scrambled Seas");
                    }
                    //Re-generate world for the saved randomizer params
                    WorldScrambler.Scramble();
                    NotificationUi.instance.ShowNotification("Scrambled Seas:\nLoaded scrambled save", 5f);

                }
                else
                {
                    Main.pluginEnabled = false;
                    NotificationUi.instance.ShowNotification("Scrambled Seas:\nThis save is unscrambled", 5f);
                }
            }
        }

        [HarmonyPatch(typeof(StartMenu), "StartNewGame")]
        private static class StartNewGamePatch
        {
            private static bool Prefix(StartMenu __instance, ref bool ___fPressed, ref Transform ___playerObserver, ref GameObject ___playerController, ref int ___animsPlaying, ref int ___currentRegion, ref Transform ___startApos, ref Transform ___startEpos, ref Transform ___startMpos)
            {
                if (Main.pluginEnabled)
                {
                    // adjust world limits if Border Expander mod is present
                    if (Main.borderExpander)
                    {
                        Main.saveContainer.worldLonMin = (int)(-12 * Main.worldScale.Value);
                        Main.saveContainer.worldLonMax = (int)(32 * Main.worldScale.Value);
                        Main.saveContainer.worldLatMin = (int)(26 - 10 * Main.worldScale.Value);
                        Main.saveContainer.worldLatMax = (int)Mathf.Min(70, (46 + 10 * Main.worldScale.Value));
                        Main.saveContainer.islandSpread = (int)(10000 * Main.archipelagoScale.Value);
                        Main.saveContainer.minArchipelagoSeparation = (int)(30000 * Main.worldScale.Value);
                    }
                    //Create a randomized world with a new seed
                    Main.saveContainer.worldScramblerSeed = (int)System.DateTime.Now.Ticks;
                    WorldScrambler.Scramble();
                    //Move player start positions to new island locations
                    ___startApos.position += WorldScrambler.islandDisplacements[2];
                    ___startEpos.position += WorldScrambler.islandDisplacements[10];
                    ___startMpos.position += WorldScrambler.islandDisplacements[20];

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
                        PlayerGold.currency[1] += 48;
                    }
                    if (__instance.region == PortRegion.medi)
                    {
                        startOffset = WorldScrambler.islandDisplacements[20];
                        PlayerGold.currency[2] += 48;
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

        //[HarmonyPatch(typeof(PlayerReputation), "GetMaxDistance")]
        //private static class ReputationPatch
        //{
        //    private static void Postfix(ref float __result)
        //    {
        //        //Islands tend to be farther apart in this mod. Ensure that the returned value is at least 300 miles
        //        if (Main.pluginEnabled && __result < 400f)
        //        {
        //            __result = 400f;
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(MissionDetailsUI), "UpdateMap")]
        private static class MissionMapPatch
        {
            private static void Postfix(ref Mission ___currentMission, ref Renderer ___mapRenderer, ref TextMesh ___locationText)
            {
                if (Main.pluginEnabled)
                {
                    //___mapRenderer.gameObject.SetActive(false);
                    //___locationText.text = "Map Unavailable\n\nWelcome to ScrambledSeas :)";

                    //        bool flag = false;
                    //        bool flag2 = false;
                    //        if ((!currentMission.originPort.oceanMapLocation && !currentMission.originPort.localMapLocation) || (!currentMission.destinationPort.oceanMapLocation && !currentMission.destinationPort.localMapLocation))
                    //        {
                    //            flag2 = true;
                    //        }

                    //        if (currentMission.originPort.localMap != currentMission.destinationPort.localMap && (currentMission.originPort.oceanMapLocation == null || currentMission.destinationPort.oceanMapLocation == null))
                    //        {
                    //            flag2 = true;
                    //        }

                    //if (flag2)
                    //{


                    ___mapRenderer.gameObject.SetActive(value: false);
                    //if (!Main.hideDestinationCoords_Enabled.Value && WorldScrambler.marketVisited[___currentMission.destinationPort.portIndex])
                    if (!Main.hideDestinationCoords_Enabled.Value)
                    {
                        Vector3 globeCoords = FloatingOriginManager.instance.GetGlobeCoords(___currentMission.destinationPort.transform);
                        float num = globeCoords.x;
                        float num2 = globeCoords.z; // Mathf.RoundToInt(globeCoords.z);
                        string text = ((num < 0) ? "W" : "E");
                        string text2 = ((num2 < 0) ? "S" : "N");
                        //___locationText.text = "(map unavailable) (visited " + WorldScrambler.marketVisited[___currentMission.destinationPort.portIndex] + ")\n\napproximate location:\n" + num2.ToString("0.00") + " " + text2 + ", " + num.ToString("0.00") + " " + text;
                        ___locationText.text = "(map unavailable)\n\napproximate location:\n" + num2.ToString("0.00") + " " + text2 + ", " + num.ToString("0.00") + " " + text;
                    }
                    else
                    {
                        ___locationText.text = "(map unavailable)\n\nlocation unknown\n";
                    }
                    //return;
                    //}

                    //if (currentMission.UseOceanMap())
                    //{
                    //    flag = true;
                    //    SetMapTexture(oceanMap);
                    //}
                    //else if (currentMission.originPort.localMap == LocalMap.alankh)
                    //{
                    //    SetMapTexture(alankhMap);
                    //}
                    //else if (currentMission.originPort.localMap == LocalMap.emerald)
                    //{
                    //    SetMapTexture(emeraldMap);
                    //}
                    //else if (currentMission.originPort.localMap == LocalMap.medi)
                    //{
                    //    SetMapTexture(mediMap);
                    //}
                    //else
                    //{
                    //    if (currentMission.originPort.localMap != LocalMap.lagoon)
                    //    {
                    //        return;
                    //    }

                    //    SetMapTexture(lagoonMap);
                    //}

                    //if (flag)
                    //{
                    //    routeLine.SetPosition(0, currentMission.originPort.oceanMapLocation.localPosition);
                    //    routeLine.SetPosition(1, currentMission.destinationPort.oceanMapLocation.localPosition);
                    //    destinationMarker.localPosition = currentMission.destinationPort.oceanMapLocation.localPosition;
                    //}
                    //else
                    //{
                    //    routeLine.SetPosition(0, currentMission.originPort.localMapLocation.localPosition);
                    //    routeLine.SetPosition(1, currentMission.destinationPort.localMapLocation.localPosition);
                    //    destinationMarker.localPosition = currentMission.destinationPort.localMapLocation.localPosition;
                    //}
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
    }
}
