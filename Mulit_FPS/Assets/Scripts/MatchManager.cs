using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class MatchManager : MonoBehaviour, IOnEventCallback
{
    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat,
        NextMatch,
        TimerSync
    }

    public static MatchManager instance;
    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    public int index;
    private List<LeaderboardPlayer> leaderboardPlayers = new List<LeaderboardPlayer>();
    public float matchLength = 180.0f;
    public float currentMatchTime;
    private float sendTimer;
    
    public enum GameState
    {
        Waiting,
        Playing,
        Ending
    }

    public int killsToWin = 1;
    public Transform mapCameraPoint;
    public GameState state = GameState.Waiting;
    public float waitAfterEnding = 5f;
    public bool perpetualMatch;
    
    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);
            state = GameState.Playing;
            SetupTimer();
            if (!PhotonNetwork.IsMasterClient)
            {
                UIController.instance.timerText.gameObject.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && state != GameState.Waiting)
        {
            if (UIController.instance.leaderboard.activeInHierarchy)
            {
                UIController.instance.leaderboard.SetActive(false);
            }
            else
            {
                ShowLeaderboard();
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (currentMatchTime > 0 && state == GameState.Playing)
            {
                currentMatchTime -= Time.deltaTime;
                if (currentMatchTime <= 0)
                {
                    currentMatchTime = 0;
                    state = GameState.Ending;
                        ListPlayerSend();
                        StateCheck();
                }
                UpdateTimerDisplay();
                sendTimer -= Time.deltaTime;
                if (sendTimer <= 0)
                {
                    sendTimer += 1;
                    TimerSend();
                }
            }
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code < 200)
        {
            EventCodes eventCode = (EventCodes) photonEvent.Code;
            object[] data = (object[]) photonEvent.CustomData;
            switch (eventCode)
            {
                case EventCodes.NewPlayer:
                {
                    NewPlayerReceive(data);
                    break;
                }
                case EventCodes.ListPlayers:
                {
                    ListPlayerReceive(data);
                    break;
                }
                case EventCodes.UpdateStat:
                {
                    UpdateStatsReceive(data);
                    break;
                }
                case EventCodes.NextMatch:
                {
                    NextMatchReceive();
                    break;
                }
                case EventCodes.TimerSync:
                {
                    TimerReceive(data);
                    break;
                }
            }
        }
    }

    public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string username)
    {
        object[] package = new object [4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.NewPlayer,
            package,
            new RaiseEventOptions {Receivers = ReceiverGroup.MasterClient},
            new SendOptions {Reliability = true}
        );
    }

    public void NewPlayerReceive(object[] data)
    {
        PlayerInfo playerInfo = new PlayerInfo((string) data[0], (int) data[1], (int) data[2], (int) data[3]);
        allPlayers.Add(playerInfo);
    }

    public void ListPlayerSend()
    {
        object[] package = new object[allPlayers.Count + 1];
        package[0] = state;
        for (int i = 0; i < allPlayers.Count; i++)
        {
            object[] piece = new object[4];
            piece[0] = allPlayers[i].name;
            piece[1] = allPlayers[i].actor;
            piece[2] = allPlayers[i].kills;
            piece[3] = allPlayers[i].deaths;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.ListPlayers,
            package,
            new RaiseEventOptions {Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true}
        );
    }

    public void ListPlayerReceive(object[] objects)
    {
        allPlayers.Clear();
        state = (GameState) objects[0]; 
        for (int i = 1; i < objects.Length; i++)
        {
            object[] piece = (object[]) objects[i];
            PlayerInfo playerInfo = new PlayerInfo((string) piece[0], (int) piece[1], (int) piece[2], (int) piece[3]);
            allPlayers.Add(playerInfo);
            if (PhotonNetwork.LocalPlayer.ActorNumber == playerInfo.actor)
            {
                index = i-1;
            }
        }
        StateCheck();
    }

    public void UpdateStatsSend(int actorSend, int statToUpdate, int amountToChange)
    {
        object[] package = new object[] {actorSend, statToUpdate, amountToChange};

        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.UpdateStat,
            package,
            new RaiseEventOptions {Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true}
        );
    }

    public void UpdateStatsReceive(object[] objects)
    {
        int actor = (int) objects[0];
        int statType = (int) objects[1];
        int amount = (int) objects[2];
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].actor == actor)
            {
                switch (statType)
                {
                    case 0:
                    {
                        allPlayers[i].kills += amount;
                        break;
                    }
                    case 1:
                    {
                        allPlayers[i].deaths += amount;
                        break;
                    }
                }

                if (i == index)
                {
                    UpdateStatsDisplay();
                }

                if (UIController.instance.leaderboard.activeInHierarchy)
                {
                    ShowLeaderboard();
                }
                break;
            }
        }
        ScoreCheck();
    }
    
    public void UpdateStatsDisplay()
    {
        if (allPlayers.Count > index)
        {
            UIController.instance.killsText.text = $"Kills: {allPlayers[index].kills}";
            UIController.instance.deathsText.text = $"Deaths: {allPlayers[index].deaths}";
        }
        else
        {
            UIController.instance.killsText.text = "Kills: 0";
            UIController.instance.deathsText.text = "Deaths: 0";
        }
    }

    void ShowLeaderboard()
    {
        UIController.instance.leaderboard.SetActive(true);
        foreach (LeaderboardPlayer leaderboardPlayer in leaderboardPlayers)
        {
            Destroy(leaderboardPlayer.gameObject);
        }
        leaderboardPlayers.Clear();
        UIController.instance.leaderboardPlayerDisplay.gameObject.SetActive(false);
        List<PlayerInfo> sorted = SortPlayers(allPlayers);
        foreach (PlayerInfo player in sorted)
        {
            LeaderboardPlayer newPlayerDisplay = Instantiate(UIController.instance.leaderboardPlayerDisplay, UIController.instance.leaderboardPlayerDisplay.transform.parent);
            newPlayerDisplay.SetDetails(player.name, player.kills, player.deaths);
            newPlayerDisplay.gameObject.SetActive(true);
            leaderboardPlayers.Add(newPlayerDisplay);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();
        while (sorted.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];
            foreach (PlayerInfo player in players)
            {
                if (!sorted.Contains(player))
                {
                    if (player.kills > highest)
                    {
                        selectedPlayer = player;
                        highest = player.kills;
                    }
                }
            }
            sorted.Add(selectedPlayer);
        }
        return sorted;
    }

    public void OnLeftRoom()
    {
        //base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }

    void ScoreCheck()
    {
        bool winnerFound = false;
        foreach (PlayerInfo player in allPlayers)
        {
            if (player.kills >= killsToWin && killsToWin > 0)
            {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                state = GameState.Ending;
                ListPlayerSend();
            }
        }
    }

    void StateCheck()
    {
        if (state == GameState.Ending)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        state = GameState.Ending;
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }
        UIController.instance.endScreen.SetActive(true);
        ShowLeaderboard();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Camera.main.transform.position = mapCameraPoint.position;
        Camera.main.transform.rotation = mapCameraPoint.rotation;
        StartCoroutine(EndCo());
    }

    private IEnumerator EndCo()
    {
        yield return new WaitForSeconds(waitAfterEnding);
        if (!perpetualMatch)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (!Launcher.instance.changeMapBetweenRounds)
                {
                    NextMatchSend();
                }
                else
                {
                    int newLevel = Random.Range(0, Launcher.instance.allMaps.Length);
                    if (Launcher.instance.allMaps[newLevel] == SceneManager.GetActiveScene().name)
                    {
                        NextMatchSend();
                    }
                    else
                    {
                        PhotonNetwork.LoadLevel(Launcher.instance.allMaps[newLevel]);
                    }
                }
            }
        }
    }

    public void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.NextMatch,
            null,
            new RaiseEventOptions {Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true}
        );
    }

    public void NextMatchReceive()
    {
        state = GameState.Playing;
        UIController.instance.endScreen.SetActive(false);
        UIController.instance.leaderboard.SetActive(false);
        foreach (PlayerInfo player in allPlayers)
        {
            player.kills = 0;
            player.deaths = 0;
        }
        UpdateStatsDisplay();
        PlayerSpawner.instance.SpawnPlayer();
        SetupTimer();
    }

    public void SetupTimer()
    {
        if (matchLength > 0)
        {
            currentMatchTime = matchLength;
            UpdateTimerDisplay();
        }
    }

    public void UpdateTimerDisplay()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);
        UIController.instance.timerText.text = $"{timeToDisplay.Minutes.ToString("00")}:{timeToDisplay.Seconds.ToString("00")}";
    }

    public void TimerSend()
    {
        object[] package = new object[] { (int) currentMatchTime, state };
        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.TimerSync,
            package,
            new RaiseEventOptions {Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true}
        );
    }

    public void TimerReceive(object[] dataReceived)
    {
        currentMatchTime = (int) dataReceived[0];
        state = (GameState) dataReceived[1];
        UpdateTimerDisplay();
        UIController.instance.timerText.gameObject.SetActive(true);
    }
}

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kills, deaths;

    public PlayerInfo(string name, int actor, int kills, int deaths)
    {
        this.name = name;
        this.actor = actor;
        this.kills = kills;
        this.deaths = deaths;
    }
}
