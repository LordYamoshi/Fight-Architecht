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
    
    public bool matchRunning = false;
    private bool isPaused = false;
    private bool isSpedUp = false;
    
    [SerializeField] private float matchDuration = 60f;
    [SerializeField] private Button startButton;
    
    
    
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

    private void ResetMatch()
    {
        player.SetHealth(player.maxHealth);
        opponent.SetHealth(opponent.maxHealth);
        

        isPaused = false;
        isSpedUp = false;
        
        player.transform.position = playerSpawn.position;
        opponent.transform.position = opponentSpawn.position;

        matchDuration = 60f;

        Debug.Log("Match has been reset!");
    }

public void EndMatch()
{
    matchRunning = false;
    Time.timeScale = 0;
    startButton.interactable = true;
    isPaused = false;
    isSpedUp = false;

    if (player.currentHealth <= 0)
    {
        player.GetComponent<Animator>().SetBool("Die", true);
        Debug.Log("Player has been defeated!");
    }

    if (opponent.currentHealth <= 0)
    {
        opponent.GetComponent<Animator>().SetBool("Die", true);
        Debug.Log("Opponent has been defeated!");
    }

    if (player.currentHealth <= 0 && opponent.currentHealth <= 0)
    {
        Debug.Log("Match has ended in a draw!");
    }

    }
}
