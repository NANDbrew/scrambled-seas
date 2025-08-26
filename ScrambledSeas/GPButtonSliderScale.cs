using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScrambledSeas
{

    internal class GPButtonSliderScale : GoPointerButton
    {
        public int type = 0;
        public TextMesh text;

        public string bar;

        public TextMesh extraText;

        float min = 0.5f;
        float max = 4.0f;

        public void Initialize()
        {
            //bar = text.text[0].ToString();
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
            Vector3 point = hit.point;
            float num = base.transform.InverseTransformPoint(point).x / base.transform.localScale.x;
            num *= 1.2f;
            num += 0.5f;

            num = Mathf.Clamp01(num);

            var val = Mathf.Lerp(min, max, num);

            SetBar(num);
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
            SetBarDirect(Mathf.RoundToInt(val * 20));
        }

        private void SetValue(float val)
        {
            var step = (max - min) / 20;
            val = Mathf.Round(val / step) * step;
            val = (float)Math.Round(val, 1);
            if (type == 0)
            {
                Main.saveContainer.worldLonMin = (int)(-12 * val);
                Main.saveContainer.worldLonMax = (int)(32 * val);
                Main.saveContainer.worldLatMin = (int)(26 - 10 * val);
                Main.saveContainer.worldLatMax = (int)Mathf.Min(70, (46 + 10 * val));
                Main.saveContainer.minArchipelagoSeparation = (int)(30000 * val);
                //Main.worldScale.Value = val;
            }
            else if (type == 1)
            {
                //Main.archipelagoScale.Value = val;
                Main.saveContainer.islandSpread = (int)(10000 * val);
            }
            extraText.text = Math.Round(val, 2).ToString() + "x";

        }

    }
}
