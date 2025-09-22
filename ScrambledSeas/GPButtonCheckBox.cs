using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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



        public void Initialize()
        {
            if (type == 0)
            {
                on = Main.random_Enabled.Value;
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

        private void UpdateButton()
        {
            if (error)
            {
                text.text = "<color=#660000>--</color>";
            }
            else
            {
                if (on)
                {
                    text.text = "X";
                }
                else
                {
                    text.text = "";
                }
            }
            if (offMat != null)
            {
                if (on)
                {
                    GetComponent<Renderer>().sharedMaterial = onMat;
                }
                else
                {
                    GetComponent<Renderer>().sharedMaterial = offMat;
                }
            }
            SwapToggles(on);
        }

        public void SwapToggles(bool on)
        {
            if (extraToggleOn != null)
            {
                extraToggleOn.SetActive(on);
            }
            if (extraToggleOff != null)
            {
                extraToggleOff.SetActive(!on);
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
                if (on)
                {
                    //Main.saveScrambleExternal.Value = on;
                    LoadSave();
                }
            }
        }

        public override void OnActivate()
        {
            UISoundPlayer.instance.PlayUISound(UISounds.buttonClick, 1f, 1.4f);
            on = !on;
            SaveSetting();
            UpdateButton();
       
        }

        public void OnEnable()
        {
            if (type == 1 && on)
            {
                LoadSave();
            }

            UpdateButton();
        }

        public void LoadSave()
        {
            error = false;
            ScrambledSeasSaveContainer fromFile = SaveFileHelper.Load<ScrambledSeasSaveContainer>("ScrambledSeas");
            string filename = $"scramble_{SaveSlots.currentSlot}.xml";
            string errorMessage = "";
            if (fromFile.version <= 0)
            {
                error = true;
                //text.text = "<color=#660000>--</color>";
                filename = $"<color=#660000>{filename}</color>";
                errorMessage = "file not found";
            }
            else if (fromFile.borderExpander == 1 && !Main.borderExpander)
            {
                error = true;
                errorMessage = "scramble was made with Border Expander\nsome islands may be inaccessible";
            }

            Patches.StartMenuPatch.scramblerUI.Find("controls/load_options/filename").GetComponent<TextMesh>().text = filename;
            Patches.StartMenuPatch.scramblerUI.Find("controls/load_options/error_text").GetComponent<TextMesh>().text = errorMessage;
            //Patches.StartMenuPatch.scramblerUI.Find("controls/load_options/seed").GetComponent<TextMesh>().text = seed;
            Patches.StartMenuPatch.scramblerUI.Find("controls/load_options/file_scale/file_scale_num").GetComponent<TextMesh>().text = $"{((float)fromFile.minArchipelagoSeparation / 30000).ToString("0.0")}x\n\n{((float)fromFile.islandSpread / 10000).ToString("0.0")}x";


        }
    }
}
