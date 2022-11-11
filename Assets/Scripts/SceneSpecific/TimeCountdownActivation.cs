using UnityEngine;

public class TimeCountdownActivation : MonoBehaviour
{
    [SerializeField] private bool toCountdown;

    void Start()
    {
        GameController.Instance.CanCountdown = toCountdown;
    } 
}
