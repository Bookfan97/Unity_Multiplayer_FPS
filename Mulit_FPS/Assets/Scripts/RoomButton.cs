using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class RoomButton : MonoBehaviour
{
    public TMP_Text buttonText;
    private RoomInfo info;

    public void SetButtonInfo(RoomInfo inputInfo)
    {
        info = inputInfo;
        buttonText.text = info.Name;
    }

    public void OpenRoom()
    {
        Launcher.instance.JoinRoom(info);
    }
}
