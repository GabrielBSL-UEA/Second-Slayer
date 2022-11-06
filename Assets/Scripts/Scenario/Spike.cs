using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spike : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }

        collision.TryGetComponent(out IHittable hittable);
        hittable.ReceiveHit(1, AttackType.melee);
    }
}
