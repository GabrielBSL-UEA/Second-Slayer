using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace NGarden.Interface
{
    public class VictorySceneController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI pointsText;

        [Header("Scene Transition")]
        [SerializeField] private RectTransform transitionImage;
        [SerializeField] private RectTransform rectCanvas;

        private float m_TransitionImageIdlePosition;

        private void Awake()
        {
            pointsText.text = "Time - " + Mathf.FloorToInt(GameController.Instance.TotalTime);
            SceneManager.MoveGameObjectToScene(GameController.Instance.gameObject, SceneManager.GetActiveScene());

            SetTransitionImageSize();
            AnimateTransition(true);
        }

        private void SetTransitionImageSize()
        {
            float newSize = Mathf.Pow(Mathf.Pow(Screen.width / rectCanvas.localScale.x, 2) + Mathf.Pow(Screen.height / rectCanvas.localScale.y, 2), .5f);
            m_TransitionImageIdlePosition = ((Screen.width / rectCanvas.localScale.x) + newSize) / 2;

            transitionImage.sizeDelta = new Vector2(newSize, newSize);
        }

        private void Start()
        {
            AudioManager.instance.PlayMusic("MenuMusic");
        }

        public void ReturnToMenu()
        {
            AudioManager.instance.PlaySFX("UIButtonClick");
            AnimateTransition(false);
        }
        public void AnimateTransition(bool goingOut = false)
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
                        SceneManager.LoadScene(0);
                    });
            }
        }
    }
}
