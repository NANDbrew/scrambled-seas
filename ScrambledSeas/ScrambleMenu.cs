using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ScrambledSeas
{
    public class ScrambleMenu : MonoBehaviour
    {
        const int buttonsPerPage = 8;
        const float buttonSpacing = 0.25f;
        public static ScrambleMenu Instance { get; set; }

        private Vector3 menuOpenPos;
        private Vector3 menuClosedPos;

        public Transform fileButtonContainer;
        static string[] files;
        public List<ScrambleMenuButton> fileButtons;
        public GameObject fileMenu;
        public GameObject sliders;
        public GameObject loadOptions;
        static Vector3 fileContStart;
        static Vector3 fileContMax;
        bool error;
        TextMesh fileScale;
        TextMesh fileName;
        TextMesh extraText;
        GameObject upArrow;
        GameObject downArrow;
        GameObject controls;

        ScrambledSeasSaveContainer fromFile;

        public void Awake()
        {
            Instance = this;
            menuClosedPos = transform.localPosition;
            menuOpenPos = new Vector3(menuClosedPos.x, menuClosedPos.y + 0.6f, menuClosedPos.z);

            controls = transform.Find("controls").gameObject;
            sliders = controls.transform.Find("sliders").gameObject;
            fileMenu = controls.transform.Find("file list ui").gameObject;
            fileButtonContainer = fileMenu.transform.Find("buttons");
            fileContStart = fileButtonContainer.transform.localPosition;
            fileContMax = fileContStart;
            loadOptions = controls.transform.Find("load_options").gameObject;
            fileName = loadOptions.transform.Find("filename").GetComponent<TextMesh>();
            extraText = controls.transform.Find("error_text").GetComponent<TextMesh>();
            fileScale = loadOptions.transform.Find("file_scale/file_scale_num").GetComponent<TextMesh>();
            upArrow = fileMenu.transform.Find("arrow up").gameObject;
            downArrow = fileMenu.transform.Find("arrow down").gameObject;
            upArrow.GetComponent<Renderer>().sharedMaterial.renderQueue = 2452;


            fileButtons = new List<ScrambleMenuButton> { fileButtonContainer.GetComponentInChildren<ScrambleMenuButton>() };
        }

        public void OnEnable()
        {
            UpdatePanes();
        }

        public void UpdatePanes()
        {
            controls.gameObject.SetActive(Main.random_Enabled.Value);
            sliders.SetActive(fromFile == null);
            loadOptions.SetActive(fromFile != null);

            if (fromFile == null)
            {
                float latMin = Main.saveContainer.worldLatMin;
                string minLat = latMin < 0 ? $"{Mathf.Abs(latMin)}° S" : $"{latMin}° N";
                extraText.text = $"Latitude range: {minLat} to {Main.saveContainer.worldLatMax}° N\n" +
                    $"Longitude range: {Mathf.Abs(Main.saveContainer.worldLonMin)}° W to {Main.saveContainer.worldLonMax}° E";
            }
            Juicebox.juice.TweenLocalPosition(gameObject, Main.random_Enabled.Value ? menuOpenPos : menuClosedPos, 0.2f, JuiceboxTween.quadraticInOut);
        }

        public void LateUpdate()
        {
            if (fileMenu.activeInHierarchy)
            {
                float scroll = -GameInput.GetScrollAxis() * 0.5f;
                fileButtonContainer.Translate(0f, scroll, 0f, Space.Self);
                if (fileButtonContainer.localPosition.y < fileContStart.y)
                {
                    fileButtonContainer.localPosition = fileContStart;
                    //upArrow.SetActive(false);
                }
                else if (fileButtonContainer.localPosition.y > fileContMax.y)
                {
                    fileButtonContainer.localPosition = fileContMax;
                }
                ButtonScroll();
            }
        }
        public void ButtonScroll()
        {
            float y = fileButtonContainer.localPosition.y - buttonSpacing * 0.5f;
            foreach (var button in fileButtons)
            {
                button.gameObject.SetActive(y <= button.index * buttonSpacing && y + (buttonSpacing * buttonsPerPage) >= button.index * buttonSpacing);
            }
            upArrow.SetActive(fileButtonContainer.localPosition.y > fileContStart.y + 0.1f);
            downArrow.SetActive(fileButtonContainer.localPosition.y < fileContMax.y - 0.1f);

        }
        public ScrambleMenuButton AddFileButton()
        {
            int index = fileButtons.Count;
            var button = GameObject.Instantiate(fileButtons.First(), fileButtonContainer.transform).GetComponent<ScrambleMenuButton>();
            button.gameObject.name = "file button " + index;
            button.transform.localPosition = new Vector3(button.transform.localPosition.x, fileButtons.First().transform.localPosition.y - buttonSpacing * index, button.transform.localPosition.z);
            button.index = index;
            fileButtons.Add(button);
            return button;
        }

        public void CancelFileLoad()
        {
            Main.loadExternal = false;
            Main.loadedScramble = null;
            fromFile = null;
            UpdatePanes();
            fileMenu.SetActive(false);

        }

        public void OpenFileMenu()
        {
            fileMenu.SetActive(true);
            files = Directory.GetFiles(Path.Combine(Directory.GetParent(Main.instance.Info.Location).FullName), "*.xml");
            for (int i = 0; i < files.Length; i++)
            {
                ScrambleMenuButton button = i < fileButtons.Count ? fileButtons[i] : AddFileButton();

                //button.files = files;
                button.SetText(Wrap(Path.GetFileName(files[i]), 25));
                ButtonScroll();
            }
            fileContMax = new Vector3(fileContStart.x, Mathf.Max(fileContStart.y + buttonSpacing * 0.5f + (buttonSpacing * (files.Length - buttonsPerPage)), fileContStart.y), fileContStart.z);
        }

        public void SetWorldScale(float scale)
        {
            SetWorldScale(scale, Main.saveContainer);
        }

        public void SetWorldScale(float scale, ScrambledSeasSaveContainer container)
        {
            float baseY = Main.borderExpander ? 30 : 20;
            Rect rect = new Rect(-12, 26, 44 * scale, baseY * scale);
            rect.center = new Vector2(10, 36);
            if (rect.yMax > 70)
            {
                rect.y -= rect.yMax - 70;
            }

            container.worldLonMin = (int)rect.xMin;
            container.worldLonMax = (int)rect.xMax;
            container.worldLatMax = (int)rect.yMax;
            container.worldLatMin = (int)rect.yMin;
            container.minArchipelagoSeparation = (int)(Main.defaultMinArchipelagoSeparation * scale);
        }

        public void PreLoadFile(int index)
        {
            Main.loadExternal = true;
            string path = files[index];
            error = false;
            string filename = Path.GetFileName(path);
            string errorMessage = "";
            string message = "";

            // set save container to null so we don't get carryover
            Main.loadedScramble = null;

            fromFile = new ScrambledSeasSaveContainer();
            try
            {
                fromFile = SaveFileHelper.LoadXML<ScrambledSeasSaveContainer>(path);
            }
            catch (Exception ex)
            {
                Main.logSource.Log(LogLevel.Warning, ex.Message);
                error = true;
                //file = null;
            }

            if (fromFile.version > 0)
            {
                if (fromFile.worldScramblerSeed == 0)
                {
                    message = "island positions will be loaded directly";
                }
                else
                {
                    message = $"scramble will be recreated\nfrom seed: {fromFile.worldScramblerSeed}";
                }

                if (fromFile.version != WorldScrambler.version)
                {
                    error = true;
                    errorMessage = "this file is not compatible with this version of Scrambled Seas";
                    message = "";
                }
                else if (fromFile.borderExpander == 1 && !Main.borderExpander)
                {
                    if (fromFile.worldScramblerSeed == 0)
                    {
                        errorMessage = "this file was made with Border Expander.\nsome islands may be inaccessible";
                    }
                    else
                    {
                        error = true;
                        errorMessage = "this file requires Border Expander";
                    }

                }
                Main.loadedScramble = fromFile;
            }
            else
            {
                error = true;
                //filename = $"<color=#660000>{filename}</color>";
                errorMessage = "invalid scramble file";
                //Main.loadedScramble = null;
            }
            errorMessage = $"<color=#660000>{WrapLines(errorMessage, 45)}</color>";
            fileName.text = Wrap(filename, 28);
            extraText.text = WrapLines(message, 45) + "\n" + errorMessage;

            string fromWorldScale = Main.loadedScramble == null ? "n/a" : Math.Round((float)Main.loadedScramble.minArchipelagoSeparation / Main.defaultMinArchipelagoSeparation, 2) + "x";
            string fromArchScale = Main.loadedScramble == null ? "n/a" : Math.Round((float)Main.loadedScramble.islandSpread / Main.defaultIslandSpread, 2).ToString() + "x";
            
            fileScale.text = $"{fromWorldScale}\n\n{fromArchScale}";

            fileMenu.SetActive(false);
            UpdatePanes();
        }

        private string WrapLines(string text, int lineLength)
        {
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = Wrap(lines[i], lineLength);
            }
            return string.Join("\n", lines);
        }
        private string Wrap(string text, int lineLength)
        {
            if (text.Length > lineLength)
            {
                int cut = text.LastIndexOfAny(new char[] { ' ', '-', '_' }, lineLength);
                if (cut == -1) return text.Insert(lineLength, "\n");
                return text.Insert(cut, "\n");
            }
            return text;
        }
    }
}
