using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneTransitionHandler : MonoBehaviour
{
    [SerializeField] private RectTransform transitionCircle;
    [SerializeField] private float cirlceMovementDuration;

    private float screenCenterToVertice;

    // Start is called before the first frame update
    void Awake()
    {
        TryGetComponent(out CanvasScaler scaler);

        screenCenterToVertice = Mathf.Sqrt(Mathf.Pow(scaler.referenceResolution.x, 2) + Mathf.Pow(scaler.referenceResolution.y, 2));
        transitionCircle.sizeDelta = new Vector2(screenCenterToVertice, screenCenterToVertice);

        SceneLoadedAnimation();
    }

    private void SceneLoadedAnimation()
    {
        transitionCircle.anchoredPosition = Vector2.zero;

        transitionCircle.LeanMoveX(-screenCenterToVertice, cirlceMovementDuration)
            .setEaseOutCubic();
    }

    public IEnumerator SceneTransitionAnimation()
    {
        transitionCircle.anchoredPosition = new Vector2(screenCenterToVertice, 0);

        transitionCircle.LeanMoveX(0, cirlceMovementDuration)
            .setIgnoreTimeScale(true)
            .setEaseOutCubic();

        yield return new WaitForSecondsRealtime(cirlceMovementDuration + .1f);
    }
}
