using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private ParticleSystem arrowPS;

    private Rigidbody2D rb;
    private bool inactive = false;
    public float Velocity { get; set; }
    public float Damage { get; set; }

    // Update is called once per frame
    void Start()
    {
        TryGetComponent(out rb);

        rb.velocity = transform.up * Velocity * -1;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Target") && !collision.CompareTag("Ground") || inactive)
        {
            return;
        }

        if (collision.TryGetComponent(out IHittable hittable))
        {
            hittable.ReceiveHit(Damage, AttackType.range);
        }

        inactive = true;
        arrowPS.Stop();
        rb.velocity = Vector2.zero;

        Destroy(gameObject, 1);
    }
}
