using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController instance;
    public GameObject deathScreen;
    public TMP_Text deathText, deathsText, killsText;
    public Slider weaponTempSlider, playerHealthSlider;
    public GameObject leaderboard;
    public LeaderboardPlayer leaderboardPlayerDisplay;
    public GameObject endScreen;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
