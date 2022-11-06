using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuInterface : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private PanelInterface mainCanvas;
    [SerializeField] private PanelInterface configurationCanvas;
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

        AudioManager.instance.PlayMusic("MenuMusic");
        SetTransitionImageSize();
        AnimateTransition(true);
    }

    public void ClickSound()
    {
        AudioManager.instance.PlaySFX("UI_Click");
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

    public void StartGame()
    {
        AnimateTransition(false, 1);
    }

    public void QuitGame()
    {
        AnimateTransition(false, -1);
    }

    private void SetTransitionImageSize()
    {
        float newSize = Mathf.Pow(Mathf.Pow(Screen.width / rectCanvas.localScale.x, 2) + Mathf.Pow(Screen.height / rectCanvas.localScale.y, 2), .5f);
        m_TransitionImageIdlePosition = ((Screen.width / rectCanvas.localScale.x) + newSize) / 2;

        transitionImage.sizeDelta = new Vector2(newSize, newSize);
    }

    public void AnimateTransition(bool goingOut = false, int levelIndex = 0)
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
                });
        }
        else
        {
            transitionImage.anchoredPosition = new Vector2(m_TransitionImageIdlePosition, 0);
            transitionImage.LeanMoveLocalX(0, 1f)
                .setEaseOutSine()
                .setIgnoreTimeScale(true)
                .setOnComplete(() => {
                    if(levelIndex == -1)
                    {
                        Application.Quit();
                    }

                    SceneManager.LoadScene(levelIndex);
                });
        }
    }
}
