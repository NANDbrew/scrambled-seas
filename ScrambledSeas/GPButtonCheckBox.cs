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
        public int type = 0;

        public TextMesh text;

        public bool on;

        public GameObject extraToggle;

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
                on = Main.hideDestinationCoords_Enabled.Value;
            }
            if ((bool)offMat)
            {
                onMat = GetComponent<Renderer>().sharedMaterial;
            }

            UpdateButton();
        }

        private void UpdateButton()
        {
            if (on)
            {
                text.text = "X";
            }
            else
            {
                text.text = "";
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
        }


        private void SaveSetting()
        {
            if (type == 0)
            {
                Main.random_Enabled.Value = on;
            }
            else if (type == 1)
            {
                Main.hideDestinationCoords_Enabled.Value = on;
            }
            if (extraToggle != null)
            {
                extraToggle.SetActive(on);
            }
        }

        public override void OnActivate()
        {
            UISoundPlayer.instance.PlayUISound(UISounds.buttonClick, 1f, 1.4f);
            on = !on;
            SaveSetting();
            UpdateButton();
        }
    }
}
