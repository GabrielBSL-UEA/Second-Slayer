using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameInterface : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Panels")]
    [SerializeField] private CanvasGroup bgCanvasGroup;
    [SerializeField] private PanelInterface pauseCanvas;
    [SerializeField] private PanelInterface configurationCanvas;
    [SerializeField] private PanelInterface levelCompleteCanvas;
    [SerializeField] private PanelInterface quitCanvas;

    [Header("SFX and Music")]
    [SerializeField] private Image SFXButtonImage;
    [SerializeField] private Image MusicButtonImage;
    [SerializeField] private Sprite muteSprite;
    [SerializeField] private Sprite audioSprite;

    [Header("Timers")]
    [SerializeField] private float bgAlphaTransition = .5f;

    [Header("Scene Transition")]
    [SerializeField] private RectTransform transitionImage;
    [SerializeField] private RectTransform rectCanvas;

    private float m_TransitionImageIdlePosition;

    private void Start()
    {
        SFXButtonImage.sprite = AudioManager.instance.SFXVolume == 1 ? audioSprite : muteSprite;
        MusicButtonImage.sprite = AudioManager.instance.MusicVolume == 1 ? audioSprite : muteSprite;

        SetTransitionImageSize();
        AnimateTransition(true);
    }

    public void ClickSound()
    {
        AudioManager.instance.PlaySFX("UI_Click");
    }

    public void UpdateTimeText(float value)
    {
        if(timeText == null)
        {
            return;
        }

        timeText.text = value.ToString();
        timeText.color = value <= 3 ? Color.red : Color.white;
    }

    public void OpenStageCompletePanel()
    {
        bgCanvasGroup.LeanAlpha(1, bgAlphaTransition).setIgnoreTimeScale(true);
        levelCompleteCanvas.gameObject.SetActive(true);
    }

    public void MuteUnmuteSFX()
    {
        AudioManager.instance.SetSFXVolume((AudioManager.instance.SFXVolume + 1) % 2);
        SFXButtonImage.sprite = AudioManager.instance.SFXVolume == 1 ? audioSprite : muteSprite;
    }

    public void MuteUnmuteMusic()
    {
        AudioManager.instance.SetMusicVolume((AudioManager.instance.MusicVolume + 1) % 2);
        MusicButtonImage.sprite = AudioManager.instance.MusicVolume == 1 ? audioSprite : muteSprite;
    }

    public void PauseGame()
    {
        bgCanvasGroup.LeanAlpha(1, bgAlphaTransition).setIgnoreTimeScale(true);

        GameController.Instance.PauseGame(true);
        pauseCanvas.gameObject.SetActive(true);
    }

    public void UnpauseGame()
    {
        bgCanvasGroup.LeanAlpha(0, bgAlphaTransition).setIgnoreTimeScale(true);
        pauseCanvas.DeactivatePanel(true);
    }

    public void GoToMenu()
    {
        AnimateTransition(false, 2);
    }

    public void GoToNextLevel()
    {
        AnimateTransition(false, 1);
    }

    private void SetTransitionImageSize()
    {
        float newSize = Mathf.Pow(Mathf.Pow(Screen.width / rectCanvas.localScale.x, 2) + Mathf.Pow(Screen.height / rectCanvas.localScale.y, 2), .5f);
        m_TransitionImageIdlePosition = ((Screen.width / rectCanvas.localScale.x) + newSize) / 2;

        transitionImage.sizeDelta = new Vector2(newSize, newSize);
    }

    public void AnimateTransition(bool goingOut = false, int sceneContext = 0)
    {
        transitionImage.gameObject.SetActive(true);

        if (goingOut)
        {
            transitionImage.anchoredPosition = Vector2.zero;
            transitionImage.LeanMoveLocalX(-m_TransitionImageIdlePosition, 1f)
                .setEaseOutSine()
                .setIgnoreTimeScale(true)
                .setOnComplete(() => {
                    transitionImage.gameObject.SetActive(false);
                    GameController.Instance.StartStage();
                });
        }
        else
        {
            transitionImage.anchoredPosition = new Vector2(m_TransitionImageIdlePosition, 0);
            transitionImage.LeanMoveLocalX(0, 1f)
                .setEaseOutSine()
                .setIgnoreTimeScale(true)
                .setOnComplete(() => {
                    GameController.Instance.LoadSceneByIndex(sceneContext);

                });
        }
    }
}