using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchManager : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject opponentObject;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform opponentSpawn;
    [SerializeField] private TextMeshProUGUI matchDurationText;
    [SerializeField] private MatchStats matchStats;
    [SerializeField] private FeedBackSystem feedBackSystem;

    private FighterHealth playerHealth;
    private FighterHealth opponentHealth;
    private FighterAgent playerAgent;
    private FighterAgent opponentAgent;

    public bool matchRunning = false;
    private bool isPaused = false;
    private bool isSpedUp = false;

    [SerializeField] private float matchDuration = 60f;
    [SerializeField] private Button startButton;
    [SerializeField] private bool isInTrainingMode = false;

    [SerializeField] private float initialFighterDistance = 2.5f;

    private void Awake()
    {
        if (playerObject != null)
        {
            playerHealth = playerObject.GetComponent<FighterHealth>();
            playerAgent = playerObject.GetComponent<FighterAgent>();
        }
        
        if (opponentObject != null)
        {
            opponentHealth = opponentObject.GetComponent<FighterHealth>();
            opponentAgent = opponentObject.GetComponent<FighterAgent>();
        }
    }

    private void Update()
    {
        if (!matchRunning) return;

        matchDuration -= Time.deltaTime;
        matchDurationText.text = $"{Mathf.CeilToInt(matchDuration)}";

        bool playerDefeated = playerHealth != null && playerHealth.CurrentHealth <= 0;
        bool opponentDefeated = opponentHealth != null && opponentHealth.CurrentHealth <= 0;

        if (matchDuration <= 0 || playerDefeated || opponentDefeated)
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
        // Reset health
        if (playerHealth != null)
            playerHealth.ResetHealth();
            
        if (opponentHealth != null)
            opponentHealth.ResetHealth();

        isPaused = false;
        isSpedUp = false;

        // Reset positions
        if (playerObject != null && opponentObject != null && playerSpawn != null && opponentSpawn != null)
        {
            Vector3 midpoint = (playerSpawn.position + opponentSpawn.position) / 2;
        
            // Set positions closer to center
            Vector3 playerPos = midpoint + new Vector3(-initialFighterDistance/2, 0, 0);
            Vector3 opponentPos = midpoint + new Vector3(initialFighterDistance/2, 0, 0);
        
            // Keep original Y values
            playerPos.y = playerSpawn.position.y;
            opponentPos.y = opponentSpawn.position.y;
        
            playerObject.transform.position = playerPos;
            opponentObject.transform.position = opponentPos;
        }

        matchDuration = 60f;

        matchStats?.ResetStats();
        feedBackSystem?.ClearFeedback();

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
        bool playerWon = false;
        bool draw = false;

        if (playerHealth != null && opponentHealth != null)
        {
            if (playerHealth.CurrentHealth > opponentHealth.CurrentHealth)
            {
                playerWon = true;
            }
            else if (playerHealth.CurrentHealth == opponentHealth.CurrentHealth)
            {
                draw = true;
            }
        }

        // Notify agents of results
        if (playerAgent != null)
        {
            playerAgent.CompleteEpisode(playerWon);
        }
        
        if (opponentAgent != null)
        {
            opponentAgent.CompleteEpisode(!playerWon && !draw);
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
        feedBackSystem?.DisplayFeedback();
    }
}