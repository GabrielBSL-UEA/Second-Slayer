using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Presentation Objects")]
    [SerializeField] private RectTransform ninjaImageObject;
    [SerializeField] private RectTransform slimeImageGameObject;
    [SerializeField] private RectTransform ninjaGardenTextObject;
    [SerializeField] private RectTransform startButtonObject;
    [SerializeField] private RectTransform creditsButtonObject;
    [SerializeField] private RectTransform configButtonObject;
    [SerializeField] private RectTransform quitButtonObject;

    [Header("Presentation Timers")]
    [SerializeField] private float startAnimationDelayDuration;
    [SerializeField] private float ninjaGardenMoveDuration;
    [SerializeField] private float textToNinjaSlime;
    [SerializeField] private float ninjaSlimeMoveDuration;
    [SerializeField] private float ninjaSlimeToButtons;
    [SerializeField] private float buttonsMoveDuration;

    [Header("Canvas group")]
    [SerializeField] private CanvasGroup buttonsCanvasGroup;

    [Header("Scene Transition Animation")]
    [SerializeField] private SceneTransitionHandler sceneTransitionHelper;

    [Header("Canvas")]
    [SerializeField] private GameObject windowCanvas;

    [Header("Volumes")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;

    private void Awake()
    {
        var gameController = FindObjectOfType<GameController>();

        if (gameController)
        {
            Destroy(gameController.gameObject);
        }

        if (!PlayerPrefs.HasKey("sfx_volume"))
        {
            PlayerPrefs.SetFloat("sfx_volume", 1);
            PlayerPrefs.SetFloat("music_volume", 1);
        }

        sfxVolumeSlider.value = PlayerPrefs.GetFloat("sfx_volume");
        musicVolumeSlider.value = PlayerPrefs.GetFloat("music_volume");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GetSFXNewVolume(float volume)
    {
        AudioManager.instance.SetSFXVolume(volume);
    }

    public void GetMusicNewVolume(float volume)
    {

        AudioManager.instance.SetMusicVolume(volume);
    }
}