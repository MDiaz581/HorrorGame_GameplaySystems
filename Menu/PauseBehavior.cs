using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PauseBehavior : MonoBehaviour
{

    public PlayerBehavior playerBehavior;

    public FirstPersonCamera fpsCam;
    public GameObject pauseMenu;

    public bool b_paused = false;

    public void Start()
    {
        playerBehavior = GetComponent<PlayerBehavior>();
        fpsCam = GetComponent<FirstPersonCamera>();   
    }
    public void OnEnable()
    {
        InputSystem_Enabler.onPauseToggled += PauseGame;
    }

    public void OnDisable()
    {
        InputSystem_Enabler.onPauseToggled -= PauseGame;
    }

    public void PauseGame()
    {

        if(!NetworkClient.active)
        {
            Debug.Log("Quitting Game");
            //Application.Quit();            
        }

        if(NetworkClient.active)
        {
            if(!b_paused)
            {
            b_paused = true;
            } 
            else 
            {
            b_paused = false;
            }
            if(playerBehavior.specialState != PlayerBehavior.SpecialStates.Interacting)
            {
                playerBehavior.SetCursorState(!b_paused);
                Cursor.visible = b_paused;
            }            

            fpsCam.b_paused = b_paused;
            playerBehavior.b_playerPaused = b_paused;
            pauseMenu?.SetActive(b_paused);
            

        }

    }

    
}
