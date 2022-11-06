using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour, IHittable
{
    [SerializeField] private List<AttackType> vulnerability;

    private void Start()
    {
        GameController.Instance.AddTarget();
    }

    public void ReceiveHit(float damage, AttackType attackType)
    {
        if (!vulnerability.Contains(attackType))
        {
            return;
        }

        GameController.Instance.RemoveTarget();
        Destroy(gameObject);
    }
}
