﻿////////////////////////////////////////////////////////////////////////////////////////
// Action At Sea - GameOverScript.cs
////////////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameOverScript : MonoBehaviour 
{   
    public GameObject gameLostImage;
    public GameObject gameWonImage;
    public UnityEngine.UI.Text replayGameText;
    public UnityEngine.UI.Text toMenuText;
    public RectTransform replayGameButton;
    public RectTransform toMenuButton;
    public bool forceLoseGame = false;
    public bool forceWinGame = false;
    public Color mouseOverColor = new Color(1.0f, 1.0f, 1.0f);
    public Color disabledColour = new Color(0.73f, 0.54f, 0.35f);

    private Color m_textColour;
    private NetworkMatchmaker m_network = null;
    private bool m_isGameOver = false;       // Whether game over is active for the player
    private bool m_hasLostGame = false;      // whether the player has lost the game
    private bool m_toMenuRequest = false;
    private bool m_toPlayRequest = false;
    private bool m_levelComplete = false;
    private CameraMovement m_camera = null;

    /// <summary>
    /// Initialises the script
    /// </summary>
    void Start()
    {
        m_network = NetworkMatchmaker.Get();

        m_camera = FindObjectOfType<CameraMovement>();
        if(m_camera == null)
        {
            Debug.LogError("Could not find camera script");
        }

        if(replayGameText.color != toMenuText.color)
        {
            Debug.LogError("Colours do not match for game over text");
        }

        m_textColour = replayGameText.color;
    }

    /// <summary>
    /// Sets the the current level has finished
    /// </summary>
    public void SetLevelComplete()
    {
        m_levelComplete = true;
        var player = PlayerManager.GetControllablePlayer();
        List<GameObject> players = PlayerManager.GetAllPlayersByScore();
        
        m_hasLostGame = players.Count == 0 || player == null ||
            Utilities.GetPlayerID(players[0]) != Utilities.GetPlayerID(player);
        
        SetGameOver(true);
    }

    /// <summary>
    /// Logic for when in game over state
    /// </summary>
    void UpdateOnGameOver()
    {
        // If currently processing a button press request
        if(m_toMenuRequest || m_toPlayRequest)
        {
            var gameFader = FadeGame.Get();
            if(gameFader.IsFadedIn())
            {
                gameFader.FadeOut();
                
                if(m_toMenuRequest)
                {
                    Debug.Log("Leaving game room from game over");
                    NetworkMatchmaker.Get().LeaveGameLevel();
                    Application.LoadLevel((int)SceneID.LOBBY);
                }
                else
                {
                    SetGameOver(false);
                }
            }
        }
        else /*Check if used has clicked/mouseover on the buttons*/
        {
            if(!m_levelComplete)
            {
                if(IsOverImage(replayGameButton))
                {
                    replayGameText.color = mouseOverColor;
                    if(Input.GetMouseButtonDown(0))
                    {
                        Debug.Log("Clicked Play Game");
                        PlayGameButton();
                    }
                }
                else
                {
                    replayGameText.color = m_textColour;
                }
            }
            
            if(IsOverImage(toMenuButton))
            {
                toMenuText.color = mouseOverColor;
                if(Input.GetMouseButtonDown(0))
                {
                    Debug.Log("Clicked To Menu");
                    GoToMenuButton();
                }
            }
            else
            {
                toMenuText.color = m_textColour;
            }
        }
    }

    /// <summary>
    /// Updates the game over ai logic
    /// </summary>
    void UpdateAI()
    {
        // Must do this here as health component gets turned off when it dies
        var ai = PlayerManager.GetAllAI();
        for (int i = 0; i < ai.Count; ++i)
        {
            if (Utilities.IsControllableAI(ai[i]))
            {
                // This includes all AI contolled by the client (including Rogues)
                var health = ai[i].GetComponent<AIHealth>();
                var network = ai[i].GetComponent<NetworkedAI>();

                bool assignedPlayerDead = network.GetAssignedPlayer() != null &&
                    !network.IsAssignedPlayerIsAlive();

                if (health.IsAlive && assignedPlayerDead)
                {
                    health.AssignedPlayerDead = true;
                    network.SetVisible(false, true);
                }

                if (!health.IsAlive)
                {
                    if (health.AssignedPlayerDead)
                    {
                        // Wait until assigned player is no longer dead to respawn
                        if (!assignedPlayerDead)
                        {
                            health.AssignedPlayerDead = false;
                            network.SetVisible(true, false);
                        }
                    }
                    else
                    {
                        // No assigned player, immediate respawn
                        network.SetVisible(false, true);
                        network.SetVisible(true, false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates the game over logic
    /// </summary>
    void Update () 
    {
        UpdateAI();

        if (m_isGameOver)
        {
            UpdateOnGameOver();
        }
        else if(m_network.IsConnectedToLevel())
        {
            if(Input.GetKeyDown(KeyCode.Escape) || forceLoseGame)
            {
                m_hasLostGame = true;
                SetGameOver(true);
            }
            else if(forceWinGame)
            {
                m_hasLostGame = false;
                SetGameOver(true);
            }
            else
            {
                var player = PlayerManager.GetControllablePlayer();
                if(player != null && !player.GetComponent<Health>().IsAlive)
                {
                    m_hasLostGame = true;
                    SetGameOver(true);
                }
            }
        }
    }

    /// <summary>
    /// Gets whether a mouse is over the image
    /// </summary>
    bool IsOverImage(RectTransform image)
    {
        var halfWidth = image.sizeDelta.x / 2;
        var halfHeight = image.sizeDelta.y / 2;
        var x = image.position.x;
        var y = image.position.y;

        return Input.mousePosition.x <= x + halfWidth &&
            Input.mousePosition.x >= x - halfWidth &&
            Input.mousePosition.y <= y + halfHeight &&
            Input.mousePosition.y >= y - halfHeight;
    }

    /// <summary>
    /// Sets whether game over or not
    /// </summary>
    void SetGameOver(bool gameover)
    {
        m_toMenuRequest = false;
        m_toPlayRequest = false;
        var player = PlayerManager.GetControllablePlayer();

        if(!gameover)
        {
            m_camera.enabled = true;
            m_isGameOver = false;
            m_hasLostGame = false;
            replayGameText.enabled = false;
            toMenuText.enabled = false;
            gameLostImage.GetComponent<UnityEngine.UI.Image>().enabled = false;
            gameWonImage.GetComponent<UnityEngine.UI.Image>().enabled = false;
            player.GetComponent<NetworkedPlayer>().SetVisible(true, false);
        }
        else
        {
            m_camera.enabled = false;
            m_isGameOver = true;
            replayGameText.enabled = true;
            replayGameText.color = m_levelComplete ? disabledColour : m_textColour;
            toMenuText.enabled = true;
            toMenuText.color = m_textColour;
            gameLostImage.GetComponent<UnityEngine.UI.Image>().enabled = m_hasLostGame;
            gameWonImage.GetComponent<UnityEngine.UI.Image>().enabled = !m_hasLostGame;
            player.GetComponent<NetworkedPlayer>().SetVisible(false, !m_levelComplete);

            var soundManager = SoundManager.Get();
            soundManager.StopMusic(SoundManager.MusicID.GAME_TRACK);
            soundManager.StopMusic(SoundManager.MusicID.GAME_AMBIENCE);
            soundManager.PlayMusic(SoundManager.MusicID.MENU_TRACK);
        }
    }
    
    /// <summary>
    /// On Click replay Game when game over is active
    /// </summary>
    public void PlayGameButton()
    {
        if (m_isGameOver && !m_toPlayRequest)
        {
            var soundManager = SoundManager.Get();
            soundManager.PlaySound(SoundManager.SoundID.BUTTON_CLICK);
            soundManager.StopMusic(SoundManager.MusicID.MENU_TRACK);
            soundManager.PlayMusic(SoundManager.MusicID.GAME_TRACK);
            soundManager.PlayMusic(SoundManager.MusicID.GAME_AMBIENCE);
            FadeGame.Get().FadeIn();
            m_toPlayRequest = true;
        }
    }
    
    /// <summary>
    /// On Click Go To Menu when game over is active
    /// </summary>
    public void GoToMenuButton()
    {
        if (m_isGameOver && !m_toMenuRequest)
        {
            var soundManager = SoundManager.Get();
            soundManager.PlaySound(SoundManager.SoundID.BUTTON_CLICK);
            soundManager.PlayMusic(SoundManager.MusicID.MENU_TRACK);
            FadeGame.Get().FadeIn();
            Crosshair.Get().ShowCursor();
            m_toMenuRequest = true;
        }
    }

    /// <summary>
    /// Gets whether the game is over
    /// </summary>
    public bool IsGameOver()
    {
        return m_isGameOver;
    }

    /// <summary>
    /// Gets the game over script instance from the scene
    /// </summary>
    public static GameOverScript Get()
    {
        var gameover = FindObjectOfType<GameOverScript>();
        if(gameover == null)
        {
            Debug.LogError("Could not find Game Over Script in scene");
        }
        return gameover;
    }
}
