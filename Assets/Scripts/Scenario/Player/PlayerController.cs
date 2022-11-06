using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerAnimation))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerController : MonoBehaviour
{
    private event Action<float> onPlayerHit;
    private event Action<float> onPlayerDash;

    private PlayerInputs playerInputs;

    [SerializeField] private float horizontalInputDeadZone = .5f;
    [SerializeField] private float verticalInputDeadZone = .5f;

    public bool LockInputs = true;
    public bool IsAlive { get; private set; } = true;

    private Rigidbody2D rigidbody2D;
    private BoxCollider2D boxCollider2D;
    private Animator animator;
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimation;
    private PlayerAttack playerAttack;
    private PlayerHealth playerHealth;

    #region GetSetFunctions

    public BoxCollider2D BoxCollider2D
    {
        get => boxCollider2D;
        private set => boxCollider2D = value;
    }

    public Rigidbody2D Rigidbody2D
    {
        get => rigidbody2D;
        private set => rigidbody2D = value;
    }

    public Animator Animator
    {
        get => animator;
        private set => animator = value;
    }

    public PlayerMovement PlayerMovement
    {
        get => playerMovement;
        private set => playerMovement = value;
    }

    public PlayerAnimation PlayerAnimation
    {
        get => playerAnimation;
        private set => playerAnimation = value;
    }

    public PlayerAttack PlayerAttack
    {
        get => playerAttack;
        private set => playerAttack = value;
    }

    public PlayerHealth PlayerHealth
    {
        get => playerHealth;
        private set => playerHealth = value;
    }

    #endregion

    public Vector2 MovementBuffer { get; private set; }

    private void Awake()
    {
        //Salva os componentes do personagens, para futuro acesso por outras classes.
        TryGetComponent(out rigidbody2D);
        TryGetComponent(out boxCollider2D);
        TryGetComponent(out animator);

        TryGetComponent(out playerMovement);
        playerMovement.controller = this;
        TryGetComponent(out playerAnimation);
        playerAnimation.controller = this;
        TryGetComponent(out playerAttack);
        playerAttack.controller = this;
        TryGetComponent(out playerHealth);
        playerHealth.controller = this;

        //Cria a classe de PlayerInputs que registrará os inputs do jogador e informará em forma de chamadas de eventos.
        playerInputs = new PlayerInputs();

        playerInputs.Player.Horizontal.performed += ctx => BufferMovementInput(ctx.ReadValue<Vector2>());
        playerInputs.Player.Horizontal.canceled += _ => BufferMovementInput(Vector2.zero);

        playerInputs.Player.Jump.performed += _ => ReceiveInput(NotifyJumpInput(true));
        playerInputs.Player.Jump.canceled += _ => ReceiveInput(NotifyJumpInput(false));

        playerInputs.Player.Dash.performed += _ => ReceiveInput(NotifyDashInput(true));
        playerInputs.Player.Attack.performed += _ => ReceiveInput(NotifyAttackInput(true));
        playerInputs.Player.Ranged.performed += _ => ReceiveInput(NotifyRangedInput(true));
    }

    private void OnEnable()
    {
        playerInputs.Enable();

        onPlayerHit += GameController.Instance.PlayerHitted;
        onPlayerDash += GameController.Instance.ReceiveDashConfirmation;

        GameController.Instance.onPauseResume += SetPlayerLock;
        GameController.Instance.onStageStart += UnlockPlayer;
    }
    private void OnDisable()
    {
        playerInputs.Disable();
        onPlayerHit -= GameController.Instance.PlayerHitted;
        onPlayerDash -= GameController.Instance.ReceiveDashConfirmation;

        GameController.Instance.onPauseResume -= SetPlayerLock;
        GameController.Instance.onStageStart -= UnlockPlayer;
    }

    private void BufferMovementInput(Vector2 direction)
    {
        Vector2 directionalValues = new Vector2(Mathf.Abs(direction.x) > horizontalInputDeadZone ? (direction.x > 0 ? 1 : -1) : 0,
                                                Mathf.Abs(direction.y) > verticalInputDeadZone ? (direction.y > 0 ? 1 : -1) : 0);
        MovementBuffer = directionalValues;

        if (!LockInputs)
        {
            GameController.Instance.PlayerInputCallback();
        }
    }

    private void Update()
    {
        ReceiveInput(NotifyHorizontalInput(MovementBuffer));
    }

    private void ReceiveInput(IEnumerator inputAction)
    {
        if (LockInputs)
        {
            return;
        }
        StartCoroutine(inputAction);
    }

    private IEnumerator NotifyHorizontalInput(Vector2 direction)
    {
        //Passa a informação da direção horizontal para o componente de movimentação
        playerMovement.ReceiveDirectionalInput(direction);
        yield break;
    }

    private IEnumerator NotifyJumpInput(bool callbackContext)
    {
        //Passa a informação do pulo para o componente de movimentação
        playerMovement.ReceiveJumpInput(callbackContext);
        GameController.Instance.PlayerInputCallback();
        yield break;
    }

    private IEnumerator NotifyDashInput(bool callbackContext)
    {
        //Passa a informação do dash para o componente de movimentação
        playerMovement.ReceiveDashInput();
        GameController.Instance.PlayerInputCallback();
        yield break;
    }

    private IEnumerator NotifyAttackInput(bool callbackContext)
    {
        //Reseta e trava a movimentação do jogador para dar lugar ao evento de ataque
        LockPlayer();
        AudioManager.instance.PlaySFX("Player_Attack");
        playerAnimation.PlayIndependAnimation("Player_Melee");
        GameController.Instance.PlayerInputCallback();
        yield break;
    }

    private IEnumerator NotifyRangedInput(bool callbackContext)
    {
        LockPlayer();
        AudioManager.instance.PlaySFX("Arrow_Shot");
        playerAnimation.PlayIndependAnimation("Player_Range");
        GameController.Instance.PlayerInputCallback();
        yield break;
    }

    public void ActivateDashEffect(float timeToRecharge, float timeToEffect)
    {
        onPlayerDash?.Invoke(timeToRecharge);
        playerAnimation.StartDashEffect(timeToEffect);
    }

    public void InterpretHealthLost(float healthRatio, float lockStateTime, float blinkingStateTime)
    {
        if (!IsAlive)
        {
            return;
        }

        onPlayerHit?.Invoke(healthRatio);
        AudioManager.instance.PlaySFX("Player_Damage");
        playerAnimation.PlayIndependAnimation("Player_Hurt");
        playerAnimation.StartBlinkSprite(blinkingStateTime);
        StartCoroutine(StartDeathSequence(lockStateTime));

        /*
        if (healthRatio == 0)
        {
            StartCoroutine(StartDeathSequence(lockStateTime));
            return;
        }
        StartCoroutine(SetLockUnlockStateOverTime(lockStateTime));
        */
    }

    private IEnumerator SetLockUnlockStateOverTime(float duration)
    {
        LockPlayer();
        yield return new WaitForSeconds(duration);
        UnlockPlayer();
    }

    private IEnumerator StartDeathSequence(float duration)
    {
        IsAlive = false;
        LockPlayer();
        yield return new WaitForSeconds(duration);

        AudioManager.instance.PlaySFX("Player_Death");

        playerAnimation.InstantiateDeathParticleEffect();
        playerAnimation.StopAllCoroutines();

        gameObject.TryGetComponent(out SpriteRenderer renderer);
        renderer.enabled = false;

        yield return new WaitForSeconds(.3f);
        GameController.Instance.RestartStage();
    }

    private void LockPlayer()
    {
        LockInputs = true;
        playerMovement.Lock = true;
        rigidbody2D.velocity = Vector2.zero;
        playerMovement.ClearActions();
        rigidbody2D.isKinematic = true;
    }

    //Animation Call
    private void UnlockPlayer()
    {
        playerMovement.Lock = false;
        PlayerAnimation.SetCommonAnimationCycle();
        LockInputs = false;
        rigidbody2D.isKinematic = false;
    }

    public void SetPlayerLock(bool value)
    {
        LockInputs = value;
    }
}
