using UnityEngine;
using Mirror;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuUI : MonoBehaviour
{

    public enum SettingsMenuState
    {
        Graphics,
        Audio,
        Controls
    }

    public SettingsMenuState settingsMenuState;

    public GameObject settingsButtonObj;
    private Button settingsButton;
    public GameObject settingsMenu;
    public GameObject defaultMenu;

    public Button backButton;

    public GameObject graphicsMenu;
    public GameObject audioMenu;
    public GameObject controlsMenu;

    public Button quitButton;

    public GameObject currentMenu;


    private void Awake()
    {

    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindObjects();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // clean up
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindObjects();
    }

    public void FindObjects()
    {
        settingsButtonObj = null;

        settingsButtonObj = GameObject.FindGameObjectWithTag("SettingButton");

        if (settingsButtonObj != null)
        {
            settingsButton = settingsButtonObj.GetComponent<Button>();
        }
        else
        {
            settingsButton = null; // optional, just makes it explicit
        }


        if (defaultMenu == null)
        {
            defaultMenu = GameObject.FindGameObjectWithTag("DefaultMenu");
        }

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() =>
        {
            DisableMenu(defaultMenu);
            ActivateMenu(settingsMenu);
        });

        if (backButton != null)
            backButton.onClick.AddListener(() =>
            {
                ActivateMenu(defaultMenu);
                DisableMenu(settingsMenu);
            });
    }


    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }


    public void ChangeState(int stateInt)
    {
        switch (stateInt)
        {
            case 0:
                SettingsMenu(SettingsMenuState.Graphics);
                break;
            case 1:
                SettingsMenu(SettingsMenuState.Audio);
                break;
            case 2:
                SettingsMenu(SettingsMenuState.Controls);
                break;
        }
    }

    private void SettingsMenu(SettingsMenuState state)
    {
        switch (state)
        {
            case SettingsMenuState.Graphics:
                graphicsMenu.SetActive(true);
                audioMenu.SetActive(false);
                controlsMenu.SetActive(false);
                break;
            case SettingsMenuState.Audio:
                audioMenu.SetActive(true);
                controlsMenu.SetActive(false);
                graphicsMenu.SetActive(false);
                break;
            case SettingsMenuState.Controls:
                controlsMenu.SetActive(true);
                audioMenu.SetActive(false);
                graphicsMenu.SetActive(false);
                break;
        }
    }

    public void DisableMenu(GameObject initialMenu)
    {
        initialMenu.SetActive(false);
    }

    public void ActivateMenu(GameObject newMenu)
    {
        newMenu.SetActive(true);
    }
}
