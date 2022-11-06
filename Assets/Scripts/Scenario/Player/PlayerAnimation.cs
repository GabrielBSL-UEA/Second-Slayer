using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public PlayerController controller { get; set; }

    [Header("Death")]
    [SerializeField] private GameObject deathParticleEffect;

    [Header("blink")]
    [SerializeField] private float blinkFrequence;

    [Header("Dash")]
    [SerializeField] private GameObject ghostReference;
    [SerializeField] private float spawnFrequence;
    [SerializeField] private float ghostDuration;
    [SerializeField] private int ghostsInitialAmount;

    [Header("SFX")]
    [SerializeField] private float walkSFXDelay = .5f;

    private SpriteRenderer spriteRenderer;

    public bool IsRight { get; private set; } = true;
    private bool canPlayCycleSound;

    private Coroutine currentAnimationCoroutine;
    private Coroutine sfxCycle;

    private Queue<GameObject> dashGhosts = new Queue<GameObject>();
    private Dictionary<GameObject, SpriteRenderer> ghostsSpriteRenderer = new Dictionary<GameObject, SpriteRenderer>();

    private enum AnimationCycle
    {
        walking,
        wallClimbing,
        none
    }

    private AnimationCycle currentAnimationCycle;

    private void Awake()
    {
        TryGetComponent(out spriteRenderer);

        for (int i = 0; i < ghostsInitialAmount; i++)
        {
            GameObject newGhost = Instantiate(ghostReference);
            newGhost.TryGetComponent(out SpriteRenderer ghostSpriteRenderer);

            ghostsSpriteRenderer[newGhost] = ghostSpriteRenderer;
            dashGhosts.Enqueue(newGhost);
            newGhost.SetActive(false);
        }
    }

    private void Start()
    {
        currentAnimationCoroutine = StartCoroutine(CommonAnimationCycle());
    }

    private void FixedUpdate()
    {
        RotateObject();
    }

    //Verifica a direção por onde o jogador está se movento e gira o personagem para tal direção
    private void RotateObject()
    {
        if (controller.MovementBuffer.x > .1f && !IsRight)
        {
            IsRight = true;
            transform.eulerAngles = Vector3.zero;
        }
        else if (controller.MovementBuffer.x < -.1f && IsRight)
        {
            IsRight = false;
            transform.eulerAngles = new Vector3(0, 180, 0);
        }
    }

    public void SetCommonAnimationCycle()
    {
        if (currentAnimationCycle == AnimationCycle.walking)
        {
            return;
        }

        StopCoroutine(currentAnimationCoroutine);
        StopCoroutine(sfxCycle);

        currentAnimationCoroutine = StartCoroutine(CommonAnimationCycle());
        currentAnimationCycle = AnimationCycle.walking;
    }


    //---------------
    //AnimationCycles
    //---------------

    //Verifica a velocidade horizontal e vertical para determinar animações como idle, run, jump e fall
    private IEnumerator CommonAnimationCycle()
    {
        sfxCycle = StartCoroutine(AnimationCycleSound("Player_Walk", walkSFXDelay));

        while (true)
        {
            yield return null;
            canPlayCycleSound = false;

            if (!controller.PlayerMovement.onGround.condition)
            {
                if (controller.Rigidbody2D.velocity.y > .1f)
                {
                    controller.Animator.Play("Player_Jump");
                    continue;
                }

                controller.Animator.Play("Player_Fall");
                continue;
            }

            if (Mathf.Abs(controller.Rigidbody2D.velocity.x) > .1f)
            {
                controller.Animator.Play("Player_Walk");
                canPlayCycleSound = true;
                continue;
            }

            controller.Animator.Play("Player_Idle");
        }
    }

    private IEnumerator AnimationCycleSound(string sfxName, float delay)
    {
        while (true)
        {
            if (!canPlayCycleSound)
            {
                yield return null;
                continue;
            }

            AudioManager.instance.PlaySFX(sfxName);
            yield return new WaitForSeconds(delay);
        }
    }

    public void PlayIndependAnimation(string animationName)
    {
        StopCoroutine(currentAnimationCoroutine);
        StopCoroutine(sfxCycle);
        currentAnimationCycle = AnimationCycle.none;

        controller.Animator.Play(animationName);
    }

    public void StartBlinkSprite(float duration)
    {
        StartCoroutine(BlinkState(duration));
    }

    private IEnumerator BlinkState(float duration)
    {
        float timePassed = 0;
        bool toEnable = false;

        while (timePassed < duration)
        {
            spriteRenderer.enabled = toEnable;

            yield return new WaitForSeconds(blinkFrequence);
            toEnable = !toEnable;
            timePassed += blinkFrequence;
        }

        spriteRenderer.enabled = true;
    }

    public void StartDashEffect(float duration)
    {
        StartCoroutine(dashEffector(duration));
    }

    private IEnumerator dashEffector(float duration)
    {
        float timePassed = 0;

        while (timePassed < duration)
        {
            if (dashGhosts.Count == 0)
            {
                GameObject newGhost = Instantiate(ghostReference);
                newGhost.TryGetComponent(out SpriteRenderer ghostSpriteRenderer);

                ghostsSpriteRenderer[newGhost] = ghostSpriteRenderer;
                dashGhosts.Enqueue(newGhost);
                newGhost.SetActive(false);
            }

            GameObject ghostToSpawn = dashGhosts.Dequeue();
            ghostToSpawn.transform.position = transform.position;
            ghostToSpawn.SetActive(true);

            LeanTween.value(1, 0, ghostDuration)
                .setOnUpdate((float value) =>
                {
                    ghostsSpriteRenderer[ghostToSpawn].color = new Color(value, Mathf.Max(value / 2, 0), Mathf.Max(value / 3, 0), value);
                    ghostsSpriteRenderer[ghostToSpawn].sprite = spriteRenderer.sprite;
                })
                .setOnComplete(() =>
                {
                    dashGhosts.Enqueue(ghostToSpawn);
                    ghostToSpawn.SetActive(false);
                });

            yield return new WaitForSeconds(spawnFrequence);
            timePassed += spawnFrequence;
        }
    }

    public void InstantiateDeathParticleEffect()
    {
        Instantiate(deathParticleEffect, transform.position, Quaternion.identity);
    }
}
