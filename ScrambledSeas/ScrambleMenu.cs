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
        TextMesh errorText;
        GameObject upArrow;
        GameObject downArrow;

        ScrambledSeasSaveContainer fromFile;

        public void Awake()
        {
            Instance = this;
            menuClosedPos = transform.localPosition;
            menuOpenPos = new Vector3(menuClosedPos.x, menuClosedPos.y + 0.6f, menuClosedPos.z);

            sliders = transform.Find("controls/sliders").gameObject;
            fileMenu = transform.Find("controls/file list ui").gameObject;
            fileButtonContainer = fileMenu.transform.Find("buttons");
            fileContStart = fileButtonContainer.transform.localPosition;
            fileContMax = fileContStart;
            loadOptions = transform.Find("controls/load_options").gameObject;
            fileName = loadOptions.transform.Find("filename").GetComponent<TextMesh>();
            errorText = loadOptions.transform.Find("error_text").GetComponent<TextMesh>();
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
            sliders.SetActive(fromFile == null && Main.random_Enabled.Value);
            loadOptions.SetActive(fromFile != null && Main.random_Enabled.Value);

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

        public void PreLoadFile(int index)
        {
            string path = files[index];
            error = false;
            string filename = Path.GetFileName(path);
            string errorMessage = "";
            string message = "";

            fromFile = new ScrambledSeasSaveContainer();
            try
            {
                fromFile = SaveFileHelper.LoadXML<ScrambledSeasSaveContainer>(path);
            }
            catch (Exception ex) 
            {
                Main.logSource.Log(LogLevel.Warning, ex.Message);
                error = true;
            }

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
                Main.loadedScramble = fromFile;
            }
            else
            {
                error = true;
                filename = $"<color=#660000>{filename}</color>";
                errorMessage = "couldn't load file";
            }
            errorMessage = $"<color=#660000>{errorMessage}</color>";
            fileName.text = filename;
            errorText.text = message + errorMessage;
            fileScale.text = $"{((float)fromFile.minArchipelagoSeparation / Main.defaultMinArchipelagoSeparation).ToString("0.0")}x\n\n{((float)fromFile.islandSpread / Main.defaultIslandSpread).ToString("0.0")}x";

            fileMenu.SetActive(false);
            UpdatePanes();
        }

        private string Wrap(string text, int lineLength)
        {
            if (text.Length > lineLength)
            {
                return text.Insert(text.LastIndexOf(' ', lineLength), "\n");
            }
            return text;
        }
    }
}
