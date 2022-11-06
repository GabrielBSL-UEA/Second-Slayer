using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public PlayerController controller { get; set; }

    [Header("Melee")]
    [SerializeField] private float meleeDamage;
    [SerializeField] private Transform hitBoxTransform;
    [SerializeField] private Vector2 boxSize;

    [Header("Range")]
    [SerializeField] private GameObject arrowGameObject;
    [SerializeField] private float arrowDamage;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private float arrowVelocity;

    [Header("Geral")]
    [SerializeField] private LayerMask enemyLayerMask;

    //Animation Call
    public void SpawnAttackHitBox()
    {
        var enemiesHitted = DrawingRaycast2D.OverlapBoxAll(hitBoxTransform.position, boxSize, enemyLayerMask, Color.green, Color.red, .5f);

        for (int i = 0; i < enemiesHitted.Length; i++)
        {
            if (enemiesHitted[i].TryGetComponent(out IHittable hittable))
            {
                hittable.ReceiveHit(meleeDamage, AttackType.melee);
            }
        }
    }

    //Animation Call
    public void SpawnArrow()
    {
        var newArrow = Instantiate(arrowGameObject, arrowSpawnPoint.position, Quaternion.identity);
        newArrow.TryGetComponent(out Arrow arrowComponent);

        arrowComponent.Velocity = arrowVelocity;
        arrowComponent.Damage = arrowDamage;

        newArrow.transform.eulerAngles += new Vector3(0, 0, 90 + (controller.PlayerAnimation.IsRight ? 0 : 180));
    }
}