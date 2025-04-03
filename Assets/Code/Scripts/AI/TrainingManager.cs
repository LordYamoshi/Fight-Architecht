using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using TMPro;

public class TrainingManager : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject opponentObject;
    [SerializeField] private TextMeshProUGUI episodeText;
    
    private FighterAgent playerAgent;
    private FighterAgent opponentAgent;
    
    [Header("Training Settings")]
    [SerializeField] private float maxEpisodeLength = 60f;
    [SerializeField] private float episodeResetDelay = 0.5f;
    [SerializeField] private float trainingTimeScale = 1.0f;
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private int statsRecordingInterval = 100;
    [SerializeField] private float stuckCheckInterval = 5f;

    private float episodeTimer = 0f;
    private int episodeCount = 1;
    private bool isResetting = false;
    private int stuckCounter = 0;
    private Vector3 lastPlayerPosition;
    private Vector3 lastOpponentPosition;
    private float stuckCheckTimer = 0f;
    private StatsRecorder statsRecorder;
    private StringBuilder logBuilder = new StringBuilder(256);
    
    private string episodeDisplayCache = string.Empty;
    
    
    
    private void Awake()
    {
        if (playerObject != null)
            playerAgent = playerObject.GetComponent<FighterAgent>();
            
        if (opponentObject != null)
            opponentAgent = opponentObject.GetComponent<FighterAgent>();
            
        statsRecorder = Academy.Instance.StatsRecorder;
    }
    
    
    private void Start()
    {
        Time.timeScale = trainingTimeScale;
        LogMessage("Training started with Time.timeScale = " + trainingTimeScale);
        
        // Initialize position tracking
        if (playerObject != null) lastPlayerPosition = playerObject.transform.position;
        if (opponentObject != null) lastOpponentPosition = opponentObject.transform.position;
        
        // Tell match manager we're in training mode
        if (matchManager != null)
            matchManager.SetTrainingMode(true);
            
        // Start the first episode
        StartNewEpisode();
    }
    
    private void Update()
    {
        if (isResetting) return;
        
        episodeTimer += Time.deltaTime;
        
        // Only update UI every few frames
        if (Time.frameCount % 5 == 0)
        {
            UpdateEpisodeDisplay();
        }
        
        // Record custom stats for TensorBoard less frequently
        if (statsRecorder != null && Time.frameCount % statsRecordingInterval == 0)
        {
            statsRecorder.Add("TrainingManager/Episode", episodeCount);
            statsRecorder.Add("TrainingManager/EpisodeTime", episodeTimer);
        }
        
        // Check for stuck fighters less frequently
        stuckCheckTimer += Time.deltaTime;
        if (stuckCheckTimer >= stuckCheckInterval)
        {
            CheckForStuckFighters();
            stuckCheckTimer = 0f;
        }
        
        // Force end episode if it exceeds maximum length
        if (episodeTimer >= maxEpisodeLength)
        {
            LogMessage("Episode " + episodeCount + " force ended after timeout (" + maxEpisodeLength + " seconds)");
            ForceEndEpisode();
            return;
        }
        
        // Check if match has ended naturally
        if (matchManager != null && !matchManager.isMatchRunning() && episodeTimer > 1.0f)
        {
            LogMessage("Episode " + episodeCount + " ended naturally after " + episodeTimer.ToString("F2") + " seconds");
            StartCoroutine(StartNewEpisodeDelayed());
        }
    }

    
    private void UpdateEpisodeDisplay()
    {
        if (episodeText != null)
        {
            episodeDisplayCache = "EPISODE: " + episodeCount + "\nTIME: " + episodeTimer.ToString("F2");
            episodeText.text = episodeDisplayCache;
        }
    }

    private void CheckForStuckFighters()
    {
        bool fightersStuck = false;
        
        // Check if player is stuck
        if (playerObject != null)
        {
            Transform playerTransform = playerObject.transform;
            if (Vector3.SqrMagnitude(playerTransform.position - lastPlayerPosition) < 0.0001f) // Use SqrMagnitude instead of Distance
            {
                stuckCounter++;
                fightersStuck = true;
            }
            else
            {
                lastPlayerPosition = playerTransform.position;
            }
        }
        
        // Check if opponent is stuck 
        if (opponentObject != null)
        {
            Transform opponentTransform = opponentObject.transform;
            if (Vector3.SqrMagnitude(opponentTransform.position - lastOpponentPosition) < 0.0001f) // Use SqrMagnitude instead of Distance
            {
                stuckCounter++;
                fightersStuck = true;
            }
            else
            {
                lastOpponentPosition = opponentTransform.position;
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
        if (isResetting) return;
        
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
        if (isResetting) yield break; 
        
        isResetting = true;
        
        // Wait a short period to ensure all processes complete
        yield return new WaitForSecondsRealtime(episodeResetDelay);
        
        // Increment episode counter
        episodeCount++;
        episodeTimer = 0f;
        stuckCounter = 0;
        
        // Reset positions
        if (playerObject != null) lastPlayerPosition = playerObject.transform.position;
        if (opponentObject != null) lastOpponentPosition = opponentObject.transform.position;
        
        // Reset match state
        if (matchManager != null)
        {
            matchManager.ResetMatch();
            matchManager.StartMatch();
        }
        
        LogMessage("Started Episode " + episodeCount);
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
        
        LogMessage("Started Episode " + episodeCount);
    }
    
    public void NotifyEpisodeComplete(FighterAgent agent)
    {
        // If we're not already resetting, start a new episode
        if (!isResetting && matchManager != null && matchManager.isMatchRunning())
        {
            string agentName = agent != null ? agent.name : "Unknown";
            LogMessage("Agent '" + agentName + "' completed episode " + episodeCount + " after " + episodeTimer.ToString("F2") + " seconds");
            StartCoroutine(StartNewEpisodeDelayed());
        }
    }

    private void LogMessage(string message, LogType logType = LogType.Log)
    {
        if (!showDebugLogs) return;
        
        logBuilder.Length = 0;
        logBuilder.Append("[TrainingManager] ");
        logBuilder.Append(message);
        string formattedMsg = logBuilder.ToString();
        
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