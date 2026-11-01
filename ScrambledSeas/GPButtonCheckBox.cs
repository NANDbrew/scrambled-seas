using UnityEngine;

namespace ScrambledSeas
{
    internal class GPButtonCheckBox : GoPointerButton
    {
        public int type = -1;
        public TextMesh text;

        public bool on;

        public Material offMat;
        private Material onMat;

        public void Awake()
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

        public void OnEnable()
        {
            UpdateButton();
        }

        private void UpdateButton()
        {
            //if (error)
            //{
            //    text.text = "<color=#660000>--</color>";
            //}

                text.text = on ? "X" : "";

            if (offMat != null)
            {
                GetComponent<Renderer>().sharedMaterial = on ? onMat : offMat;
            }
        }


        public override void OnActivate()
        {
            UISoundPlayer.instance.PlayUISound(UISounds.buttonClick, 1f, 1.4f);
            on = !on;
            if (type == 0)
            {
                Main.random_Enabled.Value = on;
            }
            else if (type == 1)
            {
                Main.loadExternal = on;
                if (!on) Main.loadedScramble = null;
            }
            ScrambleMenu.Instance.UpdatePanes();

            UpdateButton();
        }

    }
}
