using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class SettingsBehavior : MonoBehaviour
{

    public static SettingsBehavior instance { get; private set; }

    #region SaveFile
    [Header("Settings")]
    private string settingsFilePath;
    public GameSettings gameSettings;
    #endregion

    #region Graphics
    [Header("UI Elements")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown displayDropdown;
    public TMP_Dropdown windowTypeDropdown;
    public TMP_Dropdown graphicsQualityDropdown;
    public TMP_Text text_fovValue;
    public TMP_Text consoleText;
    public Slider fOVSlider;
    public Toggle vsyncToggleBox;

    public GameObject confirmationBar;

    [Header("Graphics")]
    public bool vsyncToggle;

    public enum graphicsQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    public graphicsQuality gQuality;

    public enum windowMode
    {
        Full,
        WindowedFull,
        Windowed
    }

    public windowMode wMode;
    private List<DisplayInfo> m_Displays = new List<DisplayInfo>();

    private DisplayInfo display;
    private int int_savedDisplay;
    private Resolution[] resolutions;
    private bool bool_revertingResolution;
    private Resolution previousResolution;
    private int previousResolutionIndex;
    private Resolution selectedResolution;
    private List<Resolution> uniqueResolutions = new List<Resolution>();

    #endregion

    #region Audio
    [Header("Audio")]
    public Slider slider_MasterVolume;
    public Slider slider_sfxVolume;
    public AudioMixer audioMixer;
    public TMP_Text text_MasterVolume;
    public TMP_Text text_sfxVolume;
    public float receivingVolume;
    public enum chatType
    {
        None,
        PushToTalk,
        VoiceActivation
    }

    #endregion


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        settingsFilePath = Path.Combine(Application.persistentDataPath, "GameSettings.json");

        CreateInputDictionary();
        LoadBindings();

        LoadSettings(); // Load settings from JSON file or create a new default

        // Populate dropdowns
        PopulateResolutionDropdown();
        PopulateDisplayDropdown();

        // Add listener to resolution dropdown
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChange);
        displayDropdown.onValueChanged.AddListener(OnDisplayChange);
        windowTypeDropdown.onValueChanged.AddListener(OnWindowTypeChange);
        mouseSlider.onValueChanged.AddListener(SetMouseSensitivity);
        //graphicsQualityDropdown.onValueChanged.AddListener(OnGraphicsChange);
    }

    #region Resolution
    void PopulateResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();

        // Get all screen resolutions
        resolutions = Screen.resolutions;

        var options = new List<string>();
        uniqueResolutions.Clear();

        int currentResolutionIndex = -1; // Default to -1 to track if a match is found
        Resolution highestResolution = Screen.currentResolution; // Assume current resolution is the highest

        foreach (var res in resolutions)
        {
            string option = $"{res.width} x {res.height}";

            // Add unique resolutions only
            if (!uniqueResolutions.Exists(r => r.width == res.width && r.height == res.height))
            {
                uniqueResolutions.Add(res);
                options.Add(option);

                // Match saved resolution
                if (res.width == gameSettings.resolutionWidth && res.height == gameSettings.resolutionHeight)
                {
                    currentResolutionIndex = uniqueResolutions.Count - 1;

                    previousResolutionIndex = currentResolutionIndex;
                    previousResolution = resolutions[currentResolutionIndex];

                    Debug.Log("Previous Resolution = " + previousResolution + " " + previousResolutionIndex);
                }
                // Update highest resolution
                if (res.width >= highestResolution.width && res.height >= highestResolution.height)
                {
                    highestResolution = res;
                }
            }
        }

        // Fallback to the highest available resolution if no match is found
        if (currentResolutionIndex == -1 && uniqueResolutions.Count > 0)
        {
            string highestResolutionString = $"{highestResolution.width} x {highestResolution.height}";

            // Find the index of the highest resolution in the dropdown
            currentResolutionIndex = options.FindIndex(option => option == highestResolutionString);

            gameSettings.resolutionWidth = highestResolution.width;
            gameSettings.resolutionHeight = highestResolution.height;

            selectedResolution = uniqueResolutions[currentResolutionIndex];

            Debug.LogWarning($"Saved resolution not found. Defaulting to highest available resolution: {gameSettings.resolutionWidth} x {gameSettings.resolutionHeight}");
        }


        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

    }

    public void OnResolutionChange(int resolutionIndex)
    {
        selectedResolution = uniqueResolutions[resolutionIndex];
        //check if reverting, if not pull up the confirmation bar
        if (!bool_revertingResolution)
        {
            confirmationBar.SetActive(true);
            bool_revertingResolution = false;
        }
        ApplyResolution(selectedResolution.width, selectedResolution.height);


    }

    #endregion

    #region WindowType
    private void OnWindowTypeChange(int modeIndex)
    {
        wMode = (windowMode)modeIndex;

        ApplyResolution(selectedResolution.width, selectedResolution.height);

        //check if reverting, if not pull up the confirmation bar
        if (!bool_revertingResolution)
        {
            confirmationBar.SetActive(true);
            bool_revertingResolution = false;
        }

        Debug.Log("Changed WindowType");
    }


    private void ApplyResolution(int width, int height)
    {

        switch (wMode)
        {
            case windowMode.Full:
                Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);
                break;
            case windowMode.WindowedFull:
                Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
                break;
            case windowMode.Windowed:
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
                break;
        }
    }
    #endregion

    #region Graphic Quality
    public void OnGraphicsChange(int modeIndex)
    {
        gQuality = (graphicsQuality)modeIndex;
        QualitySettings.SetQualityLevel((int)gQuality);
        gameSettings.gQuality = gQuality;
        SaveSettings();
        Debug.Log("Changed to: " + $"{gQuality}" + $" = {modeIndex}");
    }
    #endregion

    #region Confirmation and Reverting

    //These settings are for big noticable changes such as resolution, and window type.
    public void RevertSettings()
    {
        bool_revertingResolution = true;
        //return the resolution to what it was to before it was saved.
        ApplyResolution(previousResolution.width, previousResolution.height);

        resolutionDropdown.SetValueWithoutNotify(previousResolutionIndex);

        //Return display value to what was saved;
        displayDropdown.value = int_savedDisplay;
        OnDisplayChange(int_savedDisplay);

        //Change the value of the drop down to match the enum value which was saved.
        windowTypeDropdown.value = (int)gameSettings.wMode;

        confirmationBar.SetActive(false);
    }

    //On click confirm save all settings.
    public void ConfirmAndSave()
    {
        if (selectedResolution.width == 0 || selectedResolution.height == 0)
        {
            Debug.LogError("Error: Selected resolution is invalid. Aborting save.");
            return;
        }
        //Set previous resolution to current resolution so if we want to revert we revert to this.
        previousResolution = selectedResolution;

        previousResolutionIndex = resolutionDropdown.value;

        //Set savedDisplay as the current dropdown value
        int_savedDisplay = displayDropdown.value;

        // Save to GameSettings
        gameSettings.int_displayValue = int_savedDisplay;
        gameSettings.resolutionWidth = selectedResolution.width;
        gameSettings.resolutionHeight = selectedResolution.height;
        gameSettings.wMode = wMode;

        Debug.LogWarning($"Saving settings: {selectedResolution.width} x {selectedResolution.height}, Window Mode: {wMode}");

        SaveSettings();
        confirmationBar.SetActive(false);
    }
    #endregion

    #region Display
    // Populates the display dropdown based on available displays.
    void PopulateDisplayDropdown()
    {
        displayDropdown.ClearOptions();

        var hash = new HashSet<string>();

        var options = new List<string>();

        for (int i = 0; i < Display.displays.Length; i++)
        {
            display = m_Displays[i];
            string option = display.name;
            options.Add(option);
        }

        displayDropdown.AddOptions(options);
        displayDropdown.SetValueWithoutNotify(gameSettings.int_displayValue);
        displayDropdown.RefreshShownValue();
    }

    // Handles display changes from the dropdown.
    public void OnDisplayChange(int displayIndex)
    {
        if (Display.displays.Length > displayIndex)
        {
            display = m_Displays[displayIndex];

            Screen.MoveMainWindowTo(display, new Vector2Int(display.width / 2, display.height / 2));

            gameSettings.int_displayValue = displayIndex;

            //int_savedDisplay = displayIndex;

            Debug.Log($"Switched to Display {displayIndex + 1}");

            //SaveSettings();
        }

        StartCoroutine(RefreshResolutions());
    }

    public IEnumerator RefreshResolutions()
    {
        yield return new WaitForSeconds(0.5f);
        PopulateResolutionDropdown();
        ApplyResolution(selectedResolution.width, selectedResolution.height);
    }
    #endregion

    #region Vsync
    public void SetVSync(bool enabled)
    {
        vsyncToggle = enabled;
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        Debug.Log("VSync " + (enabled ? "Enabled" : "Disabled"));
        gameSettings.vsyncEnabled = enabled;
        SaveSettings();
    }
    #endregion

    #region FOV

    public void SetFOV(float fov)
    {
        fov = Mathf.Clamp(fov, 60, 90);
        Camera camera = Camera.main;
        camera.fieldOfView = fov;
        gameSettings.int_fieldOfView = (int)fov;
        text_fovValue.text = $"{fov}";
        SaveSettings();

    }

    #endregion

    #region Audio

    public void setMasterVolume(float volume)
    {
        gameSettings.int_masterVolume = (int)volume;
        // Normalize slider value to 0-1 range
        float normalizedVolume = volume / 100f;

        // Convert normalized volume to decibels
        float dB = (normalizedVolume > 0) ? Mathf.Log10(normalizedVolume) * 30 : -80f;

        // Set the volume parameter in the mixer
        audioMixer.SetFloat("MasterVolume", dB);
        text_MasterVolume.text = $"{volume}";
        SaveSettings();
    }

    public void setSFXVolume(float volume)
    {
        gameSettings.int_sfxVolume = (int)volume;
        // Normalize slider value to 0-1 range
        float normalizedVolume = volume / 100f;

        // Convert normalized volume to decibels
        float dB = (normalizedVolume > 0) ? Mathf.Log10(normalizedVolume) * 30 : -80f;

        // Set the volume parameter in the mixer
        audioMixer.SetFloat("SFXVolume", dB);
        text_sfxVolume.text = $"{volume}";
        SaveSettings();
    }

    //Once dissonance is working adjust the outgoing and receive volume. 


    #endregion

    #region Controls

    [Header("Controls")]
    public TMP_Text text_mouseValue;
    public Slider mouseSlider;
    public void SetMouseSensitivity(float sensitivity)
    {
        gameSettings.float_mouseSensitivity = sensitivity;
        text_mouseValue.text = $"{sensitivity:F2}";
        SaveSettings();
    }

    //This struct is necessary for the button to manipulate more variables than OnClick allows.
    [System.Serializable]
    public struct struct_inputButtonInfo
    {
        public string actionName;
        public int bindingIndex;
        public TMP_Text buttonText;
        public GameObject changeKeyText;
    }

    //Create an easily modifiable list within the inspector. 
    //This is necessary to hold the all the information I need to be changed by a single button press. This is also necessary for the dictionary to search through.
    public List<struct_inputButtonInfo> list_buttonInfo;

    //Create a dictionary to be able to find the action name within the list rather than looking for its integer position in the list, and to avoid repeatedly looping through the list to find the index.
    private Dictionary<string, struct_inputButtonInfo> dict_inputMap;

    //Interesting quirk about rebinding controls, the player input which is necessary to drive all inputs, although not necessary to rebind it cannot be active while the player rebinds.

    public PlayerInput playerInput;
    public InputActionAsset inputActions;
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private void CreateInputDictionary()
    {
        //Build the dictionary of inputs. 
        dict_inputMap = new Dictionary<string, struct_inputButtonInfo>();

        Debug.LogWarning($"CreatedInputDictionary");

        //Loop through the list 
        foreach (var input in list_buttonInfo)
        {
            //Make sure this is the only Key
            if (!dict_inputMap.ContainsKey(input.actionName))
            {
                //Add to the dictionary, first puts in the string that we're looking for then the list its from which right now within the foreach loop is inputName
                dict_inputMap.Add(input.actionName, input);
            }
            else
            {
                Debug.LogWarning($"Duplicate action name detected: {input.actionName}");
            }

        }
    }

    // I added the ability to rebind based on the binding index, but I have no way for the code to understand which action name to actually modify, so in my struct Interact has 2 indexes, but they'll both be called Interact this only looks for the string name.
    // if we add a bindingindex and they're both called Interact there's ambiguity, and since unity only allows for single parameter buttons we're stuck.
    public void StartRebinding(string actionName)
    {
        rebindingOperation?.Cancel();

        void CleanUp()
        {
            rebindingOperation?.Dispose();
            rebindingOperation = null;
        }
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        //Search using the given string by the button within the dictionary and then output the struct, if not stop here and return an ERROR.
        if (!dict_inputMap.TryGetValue(actionName, out var rebindableAction))
        {
            Debug.LogError($"Action '{actionName}' not found!");
            return;
        }

        var action = inputActions.FindAction(rebindableAction.actionName);

        if (action == null)
        {
            Debug.LogError($"Action '{actionName}' not found in Input Actions!");
            return;
        }

        Debug.Log($"Rebinding '{rebindableAction.actionName}'...");

        rebindableAction.changeKeyText.SetActive(true);

        rebindingOperation = action.PerformInteractiveRebinding(rebindableAction.bindingIndex)
            .WithControlsExcluding("escape") // Remove Escape from suggested bindings
            .OnPotentialMatch(operation =>
            {
                var control = operation.selectedControl;
                /* This is for unbind, doesn't work entirely and is buggy. 
                // Check if the control is the Escape key and cancel the rebinding
                if (control.name == "backspace")
                {

                    // Force Unity to refresh the input system's state
                    action.ApplyBindingOverride(rebindableAction.bindingIndex, "");

                    rebindableAction.buttonText.text = $"{GetBindingName(rebindableAction.actionName, rebindableAction.bindingIndex)}";
                    Debug.Log("Backspace pressed, erasing key.");

                    CleanUp();


                    rebindableAction.changeKeyText.SetActive(false);
                }
                */
                if (control.name == "escape")
                {
                    Debug.Log($"'{rebindableAction.actionName}' rebind canceled!");
                    rebindingOperation.Cancel();
                    rebindableAction.changeKeyText.SetActive(false);
                }
            })
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                SaveBindings();

                Debug.Log($"'{rebindableAction.actionName}' rebind complete! New action: {GetBindingName(rebindableAction.actionName, rebindableAction.bindingIndex)}");

                rebindableAction.buttonText.text = $"{GetBindingName(rebindableAction.actionName, rebindableAction.bindingIndex)}";

                CleanUp();

                rebindableAction.changeKeyText.SetActive(false);

                if (playerInput != null)
                {
                    playerInput.enabled = true;
                }
            })
            .OnCancel(operation =>
            {
                Debug.Log($"'{rebindableAction.actionName}' rebind canceled!");

                CleanUp();

                rebindableAction.changeKeyText.SetActive(false);

                if (playerInput != null)
                {
                    playerInput.enabled = true;
                }
            })
            .Start();
    }

    public string GetBindingName(string actionName, int bindingIndex)
    {
        var action = inputActions.FindAction(actionName);
        if (action != null && bindingIndex < action.bindings.Count)
        {
            return action.bindings[bindingIndex].ToDisplayString();
        }
        return "N/A";
    }

    public void StopRebinding()
    {

        rebindingOperation?.Cancel();
    }

    public void OnResetToDefaultButtonPressed()
    {
        ResetAllBindingsToDefault();
        UpdateAllKeybindUI(); // Refresh the displayed keybinds in your UI
    }

    private void ResetAllBindingsToDefault()
    {
        inputActions.RemoveAllBindingOverrides();
        Debug.Log("All binding overrides have been removed for the entire input system.");
    }

    //IF THIS FAILS CLEAR ALL PLAYER PREFS. I'm not 100% certain whats the cause of the issue for the null reference exception, but something within playerprefs messes up and it can no longer be read.
    //It's a catastrophic failure that could cause issues down the line. 
    private void UpdateAllKeybindUI()
    {
        if (dict_inputMap == null)
        {
            Debug.LogError("dict_inputMap is null. Make sure CreateInputDictionary() is called before this.");
            return;
        }

        foreach (var pair in dict_inputMap)
        {
            var action = inputActions.FindAction(pair.Value.actionName);
            if (action != null)
            {
                pair.Value.buttonText.text = action.GetBindingDisplayString(pair.Value.bindingIndex);
            }
        }
    }


    public void SaveBindings()
    {
        PlayerPrefs.SetString("rebinds", inputActions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        Debug.Log("Bindings saved.");
    }

    public void LoadBindings()
    {
        if (inputActions == null)
        {
            Debug.LogError("inputActions is null. Make sure it is assigned before calling LoadBindings.");
            return;
        }

        if (PlayerPrefs.HasKey("rebinds"))
        {
            inputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString("rebinds"));
            UpdateAllKeybindUI();
            Debug.Log("Bindings loaded.");
        }
    }
    #endregion

    #region SaveFunctions

    //This functionality might have to be added to a separate script, I'm realizing that despite doing all this the settings aren't actually initialized until the menu is opened. 
    //We need to load settings at the beginning as soon as the game is launched.


    private void SaveSettings()
    {
        string json = JsonUtility.ToJson(gameSettings, true);
        File.WriteAllText(settingsFilePath, json);
        //Debug.Log("Saving Settings");
    }

    private void LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            string json = File.ReadAllText(settingsFilePath);
            gameSettings = JsonUtility.FromJson<GameSettings>(json);
        }
        else
        {
            // Initialize a new GameSettings object with default values
            gameSettings = new GameSettings
            {
                resolutionWidth = Screen.currentResolution.width,
                resolutionHeight = Screen.currentResolution.height,
                wMode = windowMode.Full, // Default to fullscreen
                vsyncEnabled = vsyncToggle,
                gQuality = graphicsQuality.High,
                int_fieldOfView = 75,
                int_displayValue = 0,
                int_masterVolume = 75,
                int_sfxVolume = 100,
                int_voiceIncomingVolume = 100,
                int_voiceOutgoingVolume = 100,
                float_mouseSensitivity = 1f
            };
        }
        InitializeSettings();
    }



    private void InitializeSettings()
    {
        Debug.LogError("INITIALIZING AT START!!!"); // **************** None of this is initialized until we move load settings out of the settings menu. These functions are only called when the settings menu is opened. Not when game starts.
        //Initialize all settings:

        Application.targetFrameRate = 200; //Set a max framerate to prevent GPU from running at 100%

        //Initialize Display

        //Get the display Info and add them to the list.
        Screen.GetDisplayLayout(m_Displays);

        display = m_Displays[gameSettings.int_displayValue];

        //if Display is available from the start.
        if (Display.displays.Length > gameSettings.int_displayValue && gameSettings.int_displayValue >= 0)
        {
            //This isn't necessarily needed as Unity remembers the previous monitor used for the game. All we need is the int to store which monitor is currently selected.
            //Screen.MoveMainWindowTo(m_Displays[gameSettings.int_displayValue], new Vector2Int(m_Displays[gameSettings.int_displayValue].width / 2, m_Displays[gameSettings.int_displayValue].height / 2));

            Debug.Log($"Loaded preferred Display {gameSettings.int_displayValue + 1}");
            if (consoleText != null)
            {
                consoleText.text = $"Loaded preferred Display {gameSettings.int_displayValue + 1} with name {display.name}";
            }
        }
        else
        {
            gameSettings.int_displayValue = 0;

            Debug.LogWarning($"Preferred Display {gameSettings.int_displayValue + 1} is unavailable. Defaulting to Display 1.");

            if (consoleText != null)
            {
                consoleText.text = $"Preferred Display {gameSettings.int_displayValue + 1} is unavailable. Defaulting to Display 1. with name {m_Displays[0].name}";
            }
            //This needs to be tested but this is the fallback, this might not be necessary due to the assumption that unity would automatically swap to a present monitor.
            Screen.MoveMainWindowTo(m_Displays[gameSettings.int_displayValue], new Vector2Int(m_Displays[gameSettings.int_displayValue].width / 2, m_Displays[gameSettings.int_displayValue].height / 2));
        }

        int_savedDisplay = gameSettings.int_displayValue;

        //Initialize FOV

        //Prevents player from manually setting FOV over or below limit
        gameSettings.int_fieldOfView = Mathf.Clamp(gameSettings.int_fieldOfView, 60, 90);
        SetFOV(gameSettings.int_fieldOfView);
        fOVSlider.value = gameSettings.int_fieldOfView;

        //Initialize WindowType
        wMode = gameSettings.wMode;
        windowTypeDropdown.value = (int)gameSettings.wMode;

        //Initialize Graphics Quality
        graphicsQualityDropdown.value = (int)gameSettings.gQuality;

        //Initialize VSync
        vsyncToggleBox.isOn = gameSettings.vsyncEnabled;
        vsyncToggle = gameSettings.vsyncEnabled;


        // Set initial selectedResolution
        selectedResolution = new Resolution
        {
            width = gameSettings.resolutionWidth,
            height = gameSettings.resolutionHeight
        };

        // Set resolution on start
        ApplyResolution(selectedResolution.width, selectedResolution.height);

        //Initialize Audio
        slider_MasterVolume.value = gameSettings.int_masterVolume;
        slider_sfxVolume.value = gameSettings.int_sfxVolume;

        mouseSlider.value = gameSettings.float_mouseSensitivity;
        SetMouseSensitivity(gameSettings.float_mouseSensitivity);
        text_mouseValue.text = $"{gameSettings.float_mouseSensitivity:F2}";
    }
    #endregion
}
