using UnityEngine;

public class PlayerHealth : MonoBehaviour, IHittable
{
    public PlayerController controller { get; set; }

    [SerializeField] private float health;
    [SerializeField] private float hurtLockStateTime;
    [SerializeField] private float invunerabilityTime;

    private float currentHealth;
    private float invunerabilityTimer;

    private void Awake()
    {
        currentHealth = health;
    }

    public void ReceiveHit(float damage, AttackType attackType)
    {
        if (invunerabilityTimer > Time.time)
        {
            return;
        }

        invunerabilityTimer = Time.time + invunerabilityTime;

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        controller.InterpretHealthLost(currentHealth / health, hurtLockStateTime, invunerabilityTime);
    }
}