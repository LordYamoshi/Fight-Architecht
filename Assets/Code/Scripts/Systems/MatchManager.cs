using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchManager : MonoBehaviour
{
    [SerializeField] private FighterAI player;
    [SerializeField] private FighterAI opponent;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform opponentSpawn;
    [SerializeField] private TextMeshProUGUI matchDurationText;
    [SerializeField] private MatchStats matchStats;
    [SerializeField] private FeedBackSystem feedBackSystem;

    public bool matchRunning = false;
    private bool isPaused = false;
    private bool isSpedUp = false;

    [SerializeField] private float matchDuration = 60f;
    [SerializeField] private Button startButton;
    [SerializeField] private bool isInTrainingMode = false;



    private void Update()
    {
        if (!matchRunning) return;

        matchDuration -= Time.deltaTime;
        matchDurationText.text = $"{Mathf.CeilToInt(matchDuration)}";

        if (matchDuration <= 0 || player.currentHealth <= 0 || opponent.currentHealth <= 0)
        {
            EndMatch();
        }
    }

    public void StartMatch()
    {
        if (matchRunning) return;

        ResetMatch();
        startButton.interactable = false;
        matchRunning = true;
        Time.timeScale = 1;
        Debug.Log("Match has started!");
    }


    public void SpeedUpMatch()
    {
        if (!matchRunning) return;

        if (isSpedUp)
        {
            Time.timeScale = 1f;
            isSpedUp = false;
            Debug.Log("Match speed reset to normal!");
        }
        else
        {
            Time.timeScale = 2f;
            isSpedUp = true;
            Debug.Log("Match sped up!");
        }
    }


    public void PauseMatch()
    {
        if (!matchRunning) return;

        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
            Debug.Log("Match resumed!");
        }
        else
        {
            Time.timeScale = 0f;
            isPaused = true;
            Debug.Log("Match paused!");
        }
    }

    public void ReplayMatch()
    {
        ResetMatch();
        Debug.Log("Match Restarted!");
    }

    public bool isMatchRunning()
    {
        return matchRunning;
    }

    public void ResetMatch()
    {
        player.SetHealth(player.maxHealth);
        opponent.SetHealth(opponent.maxHealth);


        isPaused = false;
        isSpedUp = false;

        player.transform.position = playerSpawn.position;
        opponent.transform.position = opponentSpawn.position;

        matchDuration = 60f;

        matchStats.ResetStats();
        feedBackSystem.ClearFeedback();

        Debug.Log("Match has been reset!");
    }

    public void SetTrainingMode(bool trainingMode)
    {
        isInTrainingMode = trainingMode;
    }


    public void EndMatch()
    {
        matchRunning = false;

        // Determine winner for rewards
        FighterAI winner = null;

        if (player.currentHealth > opponent.currentHealth)
        {
            winner = player;
            player.CompleteEpisode(true);
            opponent.CompleteEpisode(false);
        }
        else if (opponent.currentHealth > player.currentHealth)
        {
            winner = opponent;
            opponent.CompleteEpisode(true);
            player.CompleteEpisode(false); 
        }
        else
        {
            // Draw - both get small negative reward
            player.CompleteEpisode(false);
            opponent.CompleteEpisode(false);
        }

        // Handle differently based on mode
        if (isInTrainingMode)
        {
            // Keep time running in training mode
            Time.timeScale = 1.0f;
        }
        else
        {
            // Pause in regular gameplay mode
            Time.timeScale = 0f;
            startButton.interactable = true;
        }

        isPaused = false;
        isSpedUp = false;

        // Display feedback
        if (feedBackSystem != null)
            feedBackSystem.DisplayFeedback();
    }
}
