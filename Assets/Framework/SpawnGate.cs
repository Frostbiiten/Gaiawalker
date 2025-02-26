using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpawnGate : MonoBehaviour
{
    public TextMeshProUGUI highscoreText;

    void Start()
    {
        int score = PlayerPrefs.GetInt("Highscore");
        highscoreText.text = $"{score:D6}";
    }
}
