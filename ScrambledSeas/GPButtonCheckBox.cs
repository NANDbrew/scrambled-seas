using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace ScrambledSeas
{
    internal class GPButtonCheckBox : GoPointerButton
    {
        public int type = -1;

        public TextMesh text;

        public bool on;

        bool error = false;

        public GameObject extraToggleOn;
        public GameObject extraToggleOff;

        public Material offMat;
        private Material onMat;

        public Transform menu;
        private Vector3 menuOpenPos;
        private Vector3 menuClosedPos;

        private JuiceboxTween tweenType = JuiceboxTween.quadraticInOut;

        public void Initialize()
        {
            if (type == 0)
            {
                on = Main.random_Enabled.Value;
                menu = transform.parent;
                menuClosedPos = menu.localPosition;
                menuOpenPos = new Vector3(menuClosedPos.x, menuClosedPos.y + 0.6f, menuClosedPos.z);

            }
            else if (type == 1)
            {
                on = Main.loadExternal;
            }

            if ((bool)offMat)
            {
                onMat = GetComponent<Renderer>().sharedMaterial;
            }

            UpdateButton();

        }

        public void OnEnable()
        {
            UpdateButton();
        }

        private void UpdateButton()
        {
            if (error)
            {
                text.text = "<color=#660000>--</color>";
            }
            else
            {
                text.text = on ? "X" : "";
            }
            if (offMat != null)
            {
                GetComponent<Renderer>().sharedMaterial = on ? onMat : offMat;
            }
            extraToggleOn?.SetActive(on);
            extraToggleOff?.SetActive(!on);

            if (type == 0)
            {
                Juicebox.juice.TweenLocalPosition(menu.gameObject, on ? menuOpenPos : menuClosedPos, 0.2f, tweenType);
            }
            else if (type == 1)
            {
                if (on)
                {
                    LoadSave();
                }
            }
        }

        private void SaveSetting()
        {
            error = false;
            if (type == 0)
            {
                Main.random_Enabled.Value = on;
            }
            else if (type == 1)
            {
                Main.loadExternal = on;
            }
        }

        public override void OnActivate()
        {
            UISoundPlayer.instance.PlayUISound(UISounds.buttonClick, 1f, 1.4f);
            on = !on;
            SaveSetting();
            UpdateButton();
       
        }

        public void LoadSave()
        {
            error = false;
            ScrambledSeasSaveContainer fromFile = SaveFileHelper.Load<ScrambledSeasSaveContainer>("ScrambledSeas");
            string filename = $"scramble_{SaveSlots.currentSlot}.xml";
            string errorMessage = "";
            string message = "";

            if (fromFile.version > 0)
            {
                if (fromFile.worldScramblerSeed == 0)
                {
                    message = "island positions will be loaded directly\n";
                }
                else
                {
                    message = $"scramble will be recreated\nfrom seed: {fromFile.worldScramblerSeed}\n";
                }
                if (fromFile.version != WorldScrambler.version)
                {
                    error = true;
                    errorMessage = "this file is not compatible with this\nversion of Scrambled Seas";
                    message = "";
                }
                else if (fromFile.borderExpander == 1 && !Main.borderExpander)
                {
                    if (fromFile.worldScramblerSeed == 0)
                    {
                        errorMessage = "this file was made with Border Expander\nsome islands may be inaccessible";
                    }
                    else
                    {
                        error = true;
                        errorMessage = "this file requires Border Expander";
                    }

                }
            }
            else
            {
                error = true;
                //text.text = "<color=#660000>--</color>";
                filename = $"<color=#660000>{filename}</color>";
                errorMessage = "file not found";
            }
            errorMessage = $"<color=#660000>{errorMessage}</color>";
            Patches.StartMenuPatch.scramblerUI.Find("controls/load_options/filename").GetComponent<TextMesh>().text = filename;
            Patches.StartMenuPatch.scramblerUI.Find("controls/load_options/error_text").GetComponent<TextMesh>().text = message + errorMessage;
            //Patches.StartMenuPatch.scramblerUI.Find("controls/load_options/seed").GetComponent<TextMesh>().text = seed;
            Patches.StartMenuPatch.scramblerUI.Find("controls/load_options/file_scale/file_scale_num").GetComponent<TextMesh>().text = $"{((float)fromFile.minArchipelagoSeparation / Main.defaultMinArchipelagoSeparation).ToString("0.0")}x\n\n{((float)fromFile.islandSpread / Main.defaultIslandSpread).ToString("0.0")}x";


        }
    }
}
