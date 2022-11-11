using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    private event Action<int> onPointsUpdate;
    private event Action<float> onHealthUpdate;
    private event Action<float> onDashUpdate;

    public event Action<bool> onPauseResume;

    public int CurrentPonts { get; private set; }
    private GameInterface gameInterface;

    private int TargetsRemaing;
    private float timeRemaing;
    private int timeRemaingInt;

    private bool countdownStarted;
    public Action onStageStart;

    public PlayerController Player { get; private set; }

    public float TotalTime { get; private set; }
    public bool CanCountdown { get; set; } = true;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        TargetsRemaing = 0;
        TotalTime = 0;
        timeRemaing = 11;
        timeRemaingInt = 11;
        Player = FindObjectOfType<PlayerController>();

        SceneManager.sceneLoaded += SetVariablesOnNewScene;
    }

    private void Update()
    {
        if (!countdownStarted || !CanCountdown)
        {
            return;
        }

        TotalTime += Time.deltaTime;

        timeRemaing = Mathf.Max(timeRemaing - Time.deltaTime, 0);
        int timeToInt = Mathf.FloorToInt(timeRemaing);

        if(timeToInt != timeRemaingInt)
        {
            timeRemaingInt = timeToInt;
            gameInterface.UpdateTimeText(timeRemaingInt);
        }

        if(timeRemaing == 0)
        {
            countdownStarted = false;
            Player.TryGetComponent(out IHittable hittable);
            hittable.ReceiveHit(1000, AttackType.melee);
        }
    }

    public void StartStage()
    {
        onStageStart?.Invoke();
    }

    public void PlayerInputCallback()
    {
        countdownStarted = true;
    }

    private void EnableActionSubscriptionsAndMusic()
    {
        gameInterface = FindObjectOfType<GameInterface>();

        if (gameInterface == null)
        {
            return;
        }

        AudioManager.instance.PlayMusic("GameplayMusic");
    }

    public void PlayerHitted(float healthRatio)
    {
        onHealthUpdate?.Invoke(healthRatio);

        if (healthRatio == 0)
        {

        }
    }

    public void ReceiveDashConfirmation(float timeToRecharge)
    {
        onDashUpdate?.Invoke(timeToRecharge);
    }

    public void EnemyDefeated(int points)
    {
        UpdatePoints(points);
    }

    public void UpdatePoints(int points)
    {
        CurrentPonts += points;
        onPointsUpdate?.Invoke(CurrentPonts);
    }

    public void PauseGame(bool toPause)
    {
        onPauseResume?.Invoke(toPause);

        Time.timeScale = toPause ? 0 : 1;
    }

    public void RestartStage()
    {
        gameInterface.AnimateTransition(false, 0);
    }

    public void StartEndStageSequence()
    {
        Time.timeScale = 0;
    }

    //Contexto:
    //0 = recarregar cena
    //1 - carregar próxima cena
    //2 - carregar menu
    public void LoadSceneByIndex(int sceneContext)
    {
        Time.timeScale = 1;

        if (sceneContext == 0)
        {
            CurrentPonts = 0;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        if (sceneContext == 1)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        if (sceneContext == 2)
        {
            SceneManager.LoadScene(0);
        }
    }

    public void AddTarget()
    {
        TargetsRemaing++;
    }

    public void RemoveTarget()
    {
        TargetsRemaing--;
        AudioManager.instance.PlaySFX("Target_Hit");

        if(TargetsRemaing > 0)
        {
            return;
        }

        AudioManager.instance.PlaySFX("Stage_Complete");
        PauseGame(true);
        gameInterface.OpenStageCompletePanel();
    }

    private void SetVariablesOnNewScene(Scene scene, LoadSceneMode loadSceneMode)
    {
        TargetsRemaing = 0;
        timeRemaing = 11;
        timeRemaingInt = 11;
        countdownStarted = false;
        Player = FindObjectOfType<PlayerController>();
        EnableActionSubscriptionsAndMusic();
        onPointsUpdate?.Invoke(CurrentPonts);
    }
}