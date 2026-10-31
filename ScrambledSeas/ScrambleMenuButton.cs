using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static OVRInput;
using static UnityEngine.GraphicsBuffer;

namespace ScrambledSeas
{
    public class ScrambleMenuButton : GoPointerButton
    {
        public static ScrambleMenuButton showListButton;
        public static ScrambleMenuButton cancelButton;
        public int index;

        public int type = -1;

        private TextMesh text;

        public Material clickedMat;
        public Material clickableMat;

        public void SetClicked(bool state)
        {
            if (state) { GetComponent<Renderer>().material = clickedMat; }
            else { GetComponent<Renderer>().material = clickableMat; }
        }

        private void Awake()
        {
            text = GetComponentInChildren<TextMesh>();
            if (type == 0) 
            {
                showListButton = this;
                clickableMat = GetComponent<Renderer>().material;
            }
            else if (type == 1)
            {
                cancelButton = this;
                clickedMat = GetComponent<Renderer>().material;
            }
            else if (type == 2)
            {
                text.GetComponent<Renderer>().sharedMaterial.renderQueue = 2451;
            }
        }

        public override void OnActivate()
        {
            UISoundPlayer.instance.PlayUISound(UISounds.buttonClick, 1f, 1.4f);

            if (type == 0) // show file menu button
            {
                ScrambleMenu.Instance.OpenFileMenu();
                SetClicked(true);
            }
            else if (type == 1) // cancel file load button
            {
                ScrambleMenu.Instance.CancelFileLoad();
                showListButton.SetClicked(false);
                SetClicked(true);
            }
            if (type == 2) // load file button
            {
                ScrambleMenu.Instance.PreLoadFile(index);
                showListButton.SetClicked(false);
                cancelButton.SetClicked(false);
            }
        }

        public void SetText(string newText)
        {
            text.text = newText;
        }

    }
}
