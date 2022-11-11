using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour, IHittable
{
    [SerializeField] private List<AttackType> vulnerability;
    [SerializeField] private GameObject destroyPS;

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
        var destroyEffect = Instantiate(destroyPS, transform.position, Quaternion.identity);
        destroyEffect.SetActive(true);

        Destroy(gameObject);
    }
}
