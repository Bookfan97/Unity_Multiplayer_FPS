using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviour, IOnEventCallback
{
    public enum EventCodes: byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat
    }
    public static MatchManager instance;
    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    public int index;
    
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
        }
    }

    // Update is called once per frame
    void Update()
    {
       
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
        object[] package = new object[allPlayers.Count];
        for (int i = 0; i < allPlayers.Count; i++)
        {
            object[] piece = new object[4];
            piece[0] = allPlayers[i].name;
            piece[1] = allPlayers[i].actor;
            piece[2] = allPlayers[i].kills;
            piece[3] = allPlayers[i].deaths;

            package[i] = piece;
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
        for (int i = 0; i < objects.Length; i++)
        {
            object[] piece = (object[]) objects[i];
            PlayerInfo playerInfo = new PlayerInfo((string) piece[0], (int) piece[1], (int) piece[2], (int) piece[3]);
            allPlayers.Add(playerInfo);
            if (PhotonNetwork.LocalPlayer.ActorNumber == playerInfo.actor)
            {
                index = i;
            }
        }
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
                break;
            }
        }
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
