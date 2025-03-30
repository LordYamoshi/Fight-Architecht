using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using TMPro;


public class TrainingManager : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private FighterAI playerAgent;
    [SerializeField] private FighterAI opponentAgent;
    [SerializeField] private TextMeshProUGUI episodeText;
    
    [Header("Training Settings")]
    [SerializeField] private float maxEpisodeLength = 60f;
    [SerializeField] private float episodeResetDelay = 0.5f;
    [SerializeField] private float trainingTimeScale = 1.0f;
    [SerializeField] private bool showDebugLogs = true;
    
    // Internal tracking variables
    private float episodeTimer = 0f;
    private int episodeCount = 1;
    private bool isResetting = false;
    private int stuckCounter = 0;
    private Vector3 lastPlayerPosition;
    private Vector3 lastOpponentPosition;
    private float stuckCheckTimer = 0f;
    private StatsRecorder statsRecorder;
    
    private void Start()
    {
        // Set appropriate time scale for training
        Time.timeScale = trainingTimeScale;
        LogMessage($"Training started with Time.timeScale = {Time.timeScale}");
        
        // Get the stats recorder for custom metrics
        statsRecorder = Academy.Instance.StatsRecorder;
        
        // Initialize position tracking
        if (playerAgent != null) lastPlayerPosition = playerAgent.transform.position;
        if (opponentAgent != null) lastOpponentPosition = opponentAgent.transform.position;
        
        // Tell match manager we're in training mode
        if (matchManager != null)
            matchManager.SetTrainingMode(true);
            
        // Start the first episode
        StartNewEpisode();
    }
    
    private void Update()
    {
        if (isResetting) return;
        
        // Update episode timer
        episodeTimer += Time.deltaTime;
        
        // Update the display
        UpdateEpisodeDisplay();
        
        // Record custom stats for TensorBoard
        if (statsRecorder != null && Time.frameCount % 100 == 0)
        {
            statsRecorder.Add("TrainingManager/Episode", episodeCount);
            statsRecorder.Add("TrainingManager/EpisodeTime", episodeTimer);
        }
        
        // Check for stuck fighters every 5 seconds
        stuckCheckTimer += Time.deltaTime;
        if (stuckCheckTimer >= 5f)
        {
            CheckForStuckFighters();
            stuckCheckTimer = 0f;
        }
        
        // Force end episode if it exceeds maximum length
        if (episodeTimer >= maxEpisodeLength)
        {
            LogMessage($"Episode {episodeCount} force ended after timeout ({maxEpisodeLength} seconds)");
            ForceEndEpisode();
            return;
        }
        
        // Check if match has ended naturally
        if (matchManager != null && !matchManager.isMatchRunning() && episodeTimer > 1.0f)
        {
            LogMessage($"Episode {episodeCount} ended naturally after {episodeTimer:F2} seconds");
            StartCoroutine(StartNewEpisodeDelayed());
        }
    }
    

    private void UpdateEpisodeDisplay()
    {
        if (episodeText != null)
        {
            episodeText.text = $"EPISODE: {episodeCount}\nTIME: {episodeTimer:F2}";
        }
    }
    

    private void CheckForStuckFighters()
    {
        bool fightersStuck = false;
        
        // Check if player is stuck
        if (playerAgent != null)
        {
            if (Vector3.Distance(playerAgent.transform.position, lastPlayerPosition) < 0.01f)
            {
                stuckCounter++;
                fightersStuck = true;
            }
            else
            {
                lastPlayerPosition = playerAgent.transform.position;
            }
        }
        
        // Check if opponent is stuck
        if (opponentAgent != null)
        {
            if (Vector3.Distance(opponentAgent.transform.position, lastOpponentPosition) < 0.01f)
            {
                stuckCounter++;
                fightersStuck = true;
            }
            else
            {
                lastOpponentPosition = opponentAgent.transform.position;
            }
        }
        
        // Reset if fighters are stuck for too long
        if (fightersStuck && stuckCounter >= 3)
        {
            LogMessage("Fighters appear to be stuck. Forcing episode reset.", LogType.Warning);
            ForceEndEpisode();
            stuckCounter = 0;
        }
        else if (!fightersStuck)
        {
            stuckCounter = 0;
        }
    }

    private void ForceEndEpisode()
    {
        if (matchManager != null && matchManager.isMatchRunning())
        {
            matchManager.EndMatch();
        }
        
        // End episodes for both agents
        if (playerAgent != null)
        {
            playerAgent.AddReward(-0.5f);
            playerAgent.EndEpisode();      
        }
        
        if (opponentAgent != null)
        {
            opponentAgent.AddReward(-0.5f); 
            opponentAgent.EndEpisode();  
        }
        
        StartCoroutine(StartNewEpisodeDelayed());
    }
    
    private IEnumerator StartNewEpisodeDelayed()
    {
        isResetting = true;
        
        // Wait a short period to ensure all processes complete
        yield return new WaitForSecondsRealtime(episodeResetDelay);
        
        // Increment episode counter
        episodeCount++;
        episodeTimer = 0f;
        stuckCounter = 0;
        
        // Reset positions
        if (playerAgent != null) lastPlayerPosition = playerAgent.transform.position;
        if (opponentAgent != null) lastOpponentPosition = opponentAgent.transform.position;
        
        // Reset match state
        if (matchManager != null)
        {
            matchManager.ResetMatch();
            matchManager.StartMatch();
        }
        
        LogMessage($"Started Episode {episodeCount}");
        isResetting = false;
    }
    

    private void StartNewEpisode()
    {
        episodeTimer = 0f;
        
        // Ensure time is running
        if (Time.timeScale == 0)
        {
            Time.timeScale = trainingTimeScale;
            LogMessage("Time scale was 0! Reset to " + trainingTimeScale, LogType.Warning);
        }
        
        // Reset match state
        if (matchManager != null)
        {
            matchManager.ResetMatch();
            matchManager.StartMatch();
        }
        
        LogMessage($"Started Episode {episodeCount}");
    }
    

    public void NotifyEpisodeComplete(FighterAI agent)
    {
        // If we're not already resetting, start a new episode
        if (!isResetting && matchManager != null && matchManager.isMatchRunning())
        {
            LogMessage($"Agent '{agent.name}' completed episode {episodeCount} after {episodeTimer:F2} seconds");
            StartCoroutine(StartNewEpisodeDelayed());
        }
    }

    private void LogMessage(string message, LogType logType = LogType.Log)
    {
        if (!showDebugLogs) return;
        
        string formattedMsg = $"[TrainingManager] {message}";
        
        switch (logType)
        {
            case LogType.Warning:
                Debug.LogWarning(formattedMsg);
                break;
            case LogType.Error:
                Debug.LogError(formattedMsg);
                break;
            default:
                Debug.Log(formattedMsg);
                break;
        }
    }
}