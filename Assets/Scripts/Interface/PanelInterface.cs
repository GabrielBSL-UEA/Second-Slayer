using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelInterface : MonoBehaviour
{
    [SerializeField] private float transitionTime = .5f;

    private CanvasGroup canvasGroup;

    private float originalWidth;
    private float originalHeight;

    private RectTransform rect;


    // Start is called before the first frame update
    void Awake()
    {
        TryGetComponent(out rect);
        TryGetComponent(out canvasGroup);

        originalWidth = rect.rect.width;
        originalHeight = rect.rect.height;
    }

    private void OnEnable()
    {
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

        canvasGroup.interactable = false;

        LeanTween.value(0, originalWidth, transitionTime)
            .setIgnoreTimeScale(true)
            .setEaseOutCubic()
            .setOnUpdate((float value) =>
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
            });

        LeanTween.value(0, originalHeight, transitionTime)
            .setIgnoreTimeScale(true)
            .setEaseOutCubic()
            .setOnUpdate((float value) =>
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value);
            })
            .setOnComplete(() => {
                canvasGroup.interactable = true;
            });
    }

    public void DeactivatePanel(bool unpauseAfter)
    {
        canvasGroup.interactable = false;

        LeanTween.value(originalWidth, 0, transitionTime)
            .setIgnoreTimeScale(true)
            .setEaseOutCubic()
            .setOnUpdate((float value) =>
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
            });

        LeanTween.value(originalHeight, 0, transitionTime)
            .setEaseOutCubic()
            .setIgnoreTimeScale(true)
            .setOnUpdate((float value) =>
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value);
            })
            .setOnComplete(() => {
                if (unpauseAfter)
                {
                    GameController.Instance.PauseGame(false);
                }
                gameObject.SetActive(false);
            });
    }
}
