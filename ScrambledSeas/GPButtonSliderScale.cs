using System;
using UnityEngine;

namespace ScrambledSeas
{

    internal class GPButtonSliderScale : GoPointerButton
    {
        public int type = 0;
        public TextMesh text;

        public string bar = "▋";

        public TextMesh extraText;

        float min = 0.5f;
        float max = 4.0f;

        public void Awake()
        {
            //GetComponent<BoxCollider>().size = new Vector3(1, 1, 0.0001f);
            //bar = text.text[0].ToString();
            if (type == 0) max = Main.borderExpander ? 4 : 1;
            else if (type == 1) max = Main.borderExpander ? 4 : 3;
            SetFromValue(1f);
        }
        public void SetFromValue(float value)
        {
            SetBar(Mathf.InverseLerp(min, max, value));
            SetValue(value);
        }

        public override void OnActivateHit(RaycastHit hit)
        {
            UISoundPlayer.instance.PlayUISound(UISounds.buttonClick, 1f, 1.4f);
            float linearPoint = base.transform.InverseTransformPoint(hit.point).x;
            linearPoint *= 1.1f;
            linearPoint += 0.5f;

            linearPoint = Mathf.Clamp01(linearPoint);

            var val = Mathf.Lerp(min, max, linearPoint);

            SetBar(linearPoint);
            SetValue(val);
        }

        private void SetBarDirect(int barCount)
        {
            text.text = "";
            for (int i = 0; i < barCount; i++)
            {
                text.text += bar;
            }
        }

        private void SetBar(float val)
        {
            SetBarDirect(Mathf.RoundToInt(val * 35));
        }

        private void SetValue(float val)
        {
            var step = (max - min) / 35;
            val = Mathf.Round(val / step) * step;
            val = (float)Math.Round(val, 2);

            if (type == 0)
            {
                ScrambleMenu.Instance.SetWorldScale(val);
            }
            else if (type == 1)
            {
                Main.saveContainer.islandSpread = (int)(Main.defaultIslandSpread * val);

                // might adjust this later. leaving it for now for compatibility
                Main.saveContainer.minIslandSeparation = (int)(Main.defaultMinIslandSeparation * val);
            }
            extraText.text = Math.Round(val, 2).ToString() + "x";

            ScrambleMenu.Instance.UpdatePanes();
        }

    }
}
