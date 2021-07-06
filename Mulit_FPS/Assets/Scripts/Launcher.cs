﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Random = UnityEngine.Random;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;
    public GameObject loadingScreen;
    public GameObject menuButtons;
    public TMP_Text loadingText;
    public GameObject createRoomScreen;
    public TMP_InputField roomNameInput;
    public GameObject RoomScreen;
    public TMP_Text roomNameText, playerNameLabel;
    public GameObject errorScreen;
    public TMP_Text errorText;
    public GameObject roomBrowserScreen;
    public RoomButton RoomButton;
    private List<RoomButton> allRoomButtons;
    private List<TMP_Text> allPlayerNames = new List<TMP_Text>();
    public GameObject nameInputScreen;
    public TMP_InputField nameInput;
    private bool hasSetNickName;
    public string levelToPlay;
    public GameObject startButton;
    public GameObject roomTestButton;
    
    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        allRoomButtons = new List<RoomButton>();
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting to Network...";
        PhotonNetwork.ConnectUsingSettings();
#if UNITY_EDITOR
        roomTestButton.SetActive(true);
#endif
    }

    void CloseMenus()
    {
        loadingScreen.SetActive(false);
        createRoomScreen.SetActive(false);
        RoomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nameInputScreen.SetActive(false);
        menuButtons.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
        loadingText.text = "Joining Lobby...";
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
        PhotonNetwork.NickName = Random.Range(0, 1000f).ToString();
        if (!hasSetNickName)
        {
            CloseMenus();
            nameInputScreen.SetActive(true);
            if (PlayerPrefs.HasKey("playerName"))
            {
                nameInput.text = PlayerPrefs.GetString("playerName");
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }

    public void OpenRoomCreate()
    {
        CloseMenus();
        createRoomScreen.SetActive(true);
    }

    public void CreateRoom()
    {
        if (!string.IsNullOrEmpty(roomNameInput.text))
        {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 8;
            PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
            CloseMenus();
            loadingText.text = "Creating Room...";
            loadingScreen.SetActive(true);
        }
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        RoomScreen.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        ListAllPlayers();
        SetStartButton();
    }

    private void SetStartButton()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    private void ListAllPlayers()
    {
        foreach (TMP_Text player in allPlayerNames)
        {
            Destroy(player.gameObject);
        }
        allPlayerNames.Clear();
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            TMP_Text newPlayerLabel = Instantiate(playerNameLabel, 
                playerNameLabel.transform.parent);
            newPlayerLabel.text = players[i].NickName;
            newPlayerLabel.gameObject.SetActive(true);
            allPlayerNames.Add(newPlayerLabel);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel, 
            playerNameLabel.transform.parent);
        newPlayerLabel.text = newPlayer.NickName;
        newPlayerLabel.gameObject.SetActive(true);
        allPlayerNames.Add(newPlayerLabel);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Failed to create room: " + message;
        CloseMenus();
        errorScreen.SetActive(true);
    }

    public void CloseErrorScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText.text = "Leaving Room...";
        loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        CloseMenus();
        roomBrowserScreen.SetActive(true);
    }
    
    public void CloseRoomBrowser()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomButton roomButton in allRoomButtons)
        {
            Destroy(roomButton.gameObject);
        }
        allRoomButtons.Clear();
        RoomButton.gameObject.SetActive(false);
        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].PlayerCount != roomList[i].MaxPlayers && 
                !roomList[i].RemovedFromList)
            {
                RoomButton newRoomButton = Instantiate(RoomButton, RoomButton.transform.parent);
                newRoomButton.SetButtonInfo(roomList[i]);
                newRoomButton.gameObject.SetActive(true);
                allRoomButtons.Add(newRoomButton);
            }
        }
    }

    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        CloseMenus();
        loadingText.text = $"Joining Room: {info.Name}...";
        loadingScreen.SetActive(true);
    }

    public void SetNickname()
    {
        if (!string.IsNullOrEmpty(nameInput.text))
        {
            PhotonNetwork.NickName = nameInput.text;
            PlayerPrefs.SetString("playerName", nameInput.text);
            CloseMenus();
            menuButtons.SetActive(true);
            hasSetNickName = true;
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        SetStartButton();
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(levelToPlay);
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }

    public void QuickJoin()
    {
        PhotonNetwork.CreateRoom("Test");
        CloseMenus();
        loadingText.text = "Creating test room";
        loadingScreen.SetActive(true);
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;
    }
}
