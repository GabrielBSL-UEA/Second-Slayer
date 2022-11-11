using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneTimeCountdownAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform timeObject;
    [SerializeField] private float animationTime;

    [SerializeField] private float delayToStartAnimation;

    private void Start()
    {
        if (PlayerPrefs.HasKey("OneTimeCountdownAnimation"))
        {
            return;
        }
        StartCoroutine(StartAnimation());
    }

    private IEnumerator StartAnimation()
    {
        PlayerPrefs.SetInt("OneTimeCountdownAnimation", 1);

        var originalWidth = timeObject.rect.width;
        var originalHeight = timeObject.rect.height;

        timeObject.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
        timeObject.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

        yield return new WaitForSeconds(delayToStartAnimation);

        LeanTween.value(0, originalWidth, animationTime)
            .setIgnoreTimeScale(true)
            .setEaseOutBack()
            .setOnUpdate((float value) =>
            {
                timeObject.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
            });

        LeanTween.value(0, originalHeight, animationTime)
            .setIgnoreTimeScale(true)
            .setEaseOutBack()
            .setOnUpdate((float value) =>
            {
                timeObject.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value);
            });
    }
}
