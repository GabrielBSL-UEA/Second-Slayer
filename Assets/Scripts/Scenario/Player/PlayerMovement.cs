using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public PlayerController controller { get; set; }

    [Header("General")]
    [SerializeField] private float actionTimeLimit;

    [Header("Movement")]
    [SerializeField] private float maxHorizontalSpeed;
    [SerializeField] private float accelerationSpeed;
    [SerializeField] private float deaccelerationSpeed;
    [SerializeField] private float maxFallingVelocity;
    [SerializeField] private float maxUpwardVelocity;

    [Header("Jump")]
    [SerializeField] private float jumpVelocity;
    [SerializeField] private float minHoldTime;
    [SerializeField] private float maxHoldTime;
    [SerializeField] private float gravityForce;
    [SerializeField] private float gravityForceOnFalling;
    [SerializeField] private int airJumps;

    [Header("Dash")]
    [SerializeField] private float dashTime;
    [SerializeField] private float dashVelocity;
    [SerializeField] private float dashDelay;

    [Header("Detections")]
    [SerializeField] private float distanceFromFoot;
    [SerializeField] private LayerMask groundLayer;

    [Header("Coyote Time")]
    [SerializeField] private float dashWaitingTime;
    [SerializeField] private float noGroundJumpTime;
    [SerializeField] private float reachingGroundJumpTime;

    public class Condition
    {
        public bool condition;
    }
    private struct ConditionContext
    {
        public Condition value;
        public bool isNegative;
    }

    public Condition onGround { get; private set; } = new Condition();
    public Condition onGoingOffGround { get; private set; } = new Condition();
    public Condition jumpHolded { get; private set; } = new Condition();
    public Condition onDash { get; private set; } = new Condition();
    public Condition onDashDelay { get; private set; } = new Condition();
    public Condition onCanWalk { get; private set; } = new Condition();

    private int airJumpCounter;
    private float currentHorizontalDirection;

    private bool facingRight = true;
    private float facingValue = 1;

    private List<IEnumerator> actionList = new List<IEnumerator>();
    private Dictionary<IEnumerator, List<ConditionContext>> actionConditions = new Dictionary<IEnumerator, List<ConditionContext>>();

    public bool Lock { get; set; }

    private void Awake()
    {
        onCanWalk.condition = true;
    }

    private void Update()
    {
        if (Lock)
        {
            return;
        }

        //Checa a lista de ações a cada update, vendo se tem alguma ação a ser realizada.
        foreach (var action in actionList.ToList())
        {
            var canRealiseAction = true;

            foreach (var condition in actionConditions[action])
            {
                if (condition.value.condition == condition.isNegative)
                {
                    canRealiseAction = false;
                    break;
                }
            }

            if (!canRealiseAction)
            {
                continue;
            }

            //Realiza a ação e a retira da lista de ações;
            StartCoroutine(action);
            actionList.Remove(action);
        }
    }

    private void FixedUpdate()
    {
        if (Lock)
        {
            return;
        }

        CheckForGround();
        SetDesiredVelocity();
        SetGravityInfluenceOnPlayer();
    }

    //---------
    //Detectors
    //---------

    private void CheckForGround()
    {
        //Cria-se duas variáveis que serão usadas na chamada de BoxCast para detecção do chão
        //Origin: ponto de origem do quadrado.
        //Size: tamanho do quadrado.
        var boxBounds = controller.BoxCollider2D.bounds;
        Vector2 origin = new Vector2(boxBounds.center.x, boxBounds.center.y - boxBounds.extents.y - (distanceFromFoot / 2) + .1f);
        Vector2 size = new Vector2(boxBounds.size.x - .1f, distanceFromFoot / 2);

        //Chama a função BoxCast da classe DrawingRaycast2D que, além de gerar a caixa, também a desenha no editor do Unity para fácil vizualização
        var playerOnGround = DrawingRaycast2D.OverlapBox(origin, size, groundLayer, Color.green, Color.red);

        //Verifica se o jogador estava em contato o chão e deixou de estar, importante para o coyote time do pulo do abismo.
        if (onGround.condition && !playerOnGround)
        {
            StartCoroutine(SetOnGoingOffGroundByTime());
        }

        //Seta o onGround e, dependendo se o jogador está em contato com o chão ou não, muda o valor de pulos no ar.
        onGround.condition = playerOnGround;
        airJumpCounter = playerOnGround ? 0 : airJumpCounter;
    }

    //---------
    //ForceAppliers
    //---------

    private void SetDesiredVelocity()
    {
        ApplyHorizontalVelocity();
    }

    private void ApplyHorizontalVelocity()
    {
        //Recebe o valor do direcional da esquerda e da direita, traduzindo-os em velocidade horizontal para o personagem em forma de força
        //Verifica a diferença entre a velocidade desejada e a velocidade atual e multiplica o resultado na aplicação da força.
        //Quandto mais perto da velocidade máxima, menor é a força aplicada e quando mais longe, maior é a força

        //O resultado disso é um movimento mais natural do personagem
        float directionReference = onCanWalk.condition ? currentHorizontalDirection : 0;

        float targetSpeed = directionReference * maxHorizontalSpeed;
        float speedDifference = targetSpeed - controller.Rigidbody2D.velocity.x;
        float accelerationRate = Mathf.Sign(controller.Rigidbody2D.velocity.x) == MathF.Sign(targetSpeed) && targetSpeed != 0
            ? accelerationSpeed
            : deaccelerationSpeed;

        controller.Rigidbody2D.AddForce(Vector2.right * (accelerationRate * speedDifference));
    }

    private void SetGravityInfluenceOnPlayer()
    {
        //Verifica a velocidade horizontal do personagem, alterando seu valor dependendo se ele está caindo ou não
        controller.Rigidbody2D.gravityScale = controller.Rigidbody2D.velocity.y > 0 ? gravityForce :
            gravityForceOnFalling;

        //Controla a velocidade de queda do jogador para que ele não passe do limite estabelecido
        controller.Rigidbody2D.velocity = new Vector2(controller.Rigidbody2D.velocity.x, Mathf.Clamp(controller.Rigidbody2D.velocity.y, -maxFallingVelocity, maxUpwardVelocity));
    }

    //---------
    //InputReceivers
    //---------

    public void ReceiveDirectionalInput(Vector2 directionInput)
    {
        //Recebe o input horizontal do jogador
        currentHorizontalDirection = directionInput.x;

        //Guarda a direção em que o jogador está virado
        if (currentHorizontalDirection == 0)
        {
            return;
        }
        facingRight = currentHorizontalDirection > 0;
        facingValue = facingRight ? 1 : -1;
    }

    public void ReceiveJumpInput(bool inputContext)
    {
        //Salva a informação de que o jogador está segurando o botão de pular ou não
        jumpHolded.condition = inputContext;

        if (!inputContext)
        {
            return;
        }

        //Cria as variáveis que serão usadas para determinar o tempo de vida da ação e suas condições
        float actionTime;
        List<ConditionContext> conditions = new List<ConditionContext>();

        conditions.Add(CreateContext(onDash, true));

        //Verifica se o jogador está no chão ou se a condição de coyote time do abismo são verdadeiras 
        if (onGround.condition || onGoingOffGround.condition)
        {
            actionTime = actionTimeLimit;
        }
        else
        {
            //Verifica se o jogador ainda tem pulos no ar restantes
            if (airJumpCounter < airJumps)
            {
                actionTime = actionTimeLimit;
                airJumpCounter++;
            }
            else
            {
                //Determina a condição de pular para quando o jogador encostar no chão e seu tempo de vida
                actionTime = reachingGroundJumpTime;
                conditions.Add(CreateContext(onGround));
            }
        }

        //Chama a função que salva as ações e condições por um certo período de tempo
        StartCoroutine(AddActionToList(actionTime, Jump(), conditions));
    }

    public void ReceiveDashInput()
    {
        if (onDashDelay.condition)
        {
            return;
        }

        //Adiciona ação de dash na lista de ações
        StartCoroutine(AddActionToList(dashWaitingTime, Dash(), new List<ConditionContext>()));
    }

    //---------
    //Timers
    //---------

    private IEnumerator AddActionToList(float duration, IEnumerator action, List<ConditionContext> conditions)
    {
        //Adiciona a condição na lista de ação e as condições no dicionário de condições
        actionList.Add(action);
        actionConditions[action] = conditions;

        //Seta o tempo de vida da ação
        yield return new WaitForSeconds(duration);

        //Retira a ação da lista caso ela ainda esteja lá (ela não estará caso tenha sido acionada)
        if (actionList.Contains(action))
        {
            actionList.Remove(action);
        }
    }

    //Seta o booleanos de coyote time
    private IEnumerator SetOnGoingOffGroundByTime()
    {
        onGoingOffGround.condition = true;
        yield return new WaitForSeconds(noGroundJumpTime);
        onGoingOffGround.condition = false;
    }

    private IEnumerator SetOnDashDelay(float duration)
    {
        onDashDelay.condition = true;
        yield return new WaitForSeconds(duration);
        onDashDelay.condition = false;
    }
    private IEnumerator SetOnCanWalk(float duration)
    {
        onCanWalk.condition = false;
        yield return new WaitForSeconds(duration);
        onCanWalk.condition = true;
    }

    //---------
    //Actions
    //---------

    private IEnumerator Jump()
    {
        //Reseta o booleano do coyote time do abismo
        onGoingOffGround.condition = false;
        AudioManager.instance.PlaySFX("Player_Jump");
        float timePassed = 0;

        //Inicia o loop que modifica a velocidade vertical do jogador pelo valor pré-determinada pelo jogador
        while (timePassed < maxHoldTime)
        {
            //Caso o jogador não esteja pressionando o botão de pular, o loop é quebrado mais rápido, fazendo ele pular uma altura menor
            if (!jumpHolded.condition && timePassed > minHoldTime)
            {
                break;
            }

            //Velocidade vertical é setada e o loop espera pelo próximo update físico
            controller.Rigidbody2D.velocity = new Vector2(controller.Rigidbody2D.velocity.x, jumpVelocity);
            yield return new WaitForFixedUpdate();

            timePassed += Time.fixedDeltaTime;
        }
    }

    private IEnumerator Dash()
    {
        StartCoroutine(SetOnDashDelay(dashTime + dashWaitingTime));
        controller.ActivateDashEffect(dashTime + dashWaitingTime, dashTime);
        AudioManager.instance.PlaySFX("Player_Dash");

        //Seta a direção do dash
        onDash.condition = true;
        float directionReference = currentHorizontalDirection == 0 ? facingValue : currentHorizontalDirection;

        float dashDirection = directionReference;

        float timePassed = 0;
        while (timePassed < dashTime)
        {
            //Aplica o dash pelo tempo determinado pelo jogador
            controller.Rigidbody2D.velocity = new Vector2(dashDirection * dashVelocity, 0);

            yield return new WaitForFixedUpdate();
            timePassed += Time.fixedDeltaTime;
        }

        onDash.condition = false;
        yield return null;
    }

    //---------
    //Utils
    //---------

    private ConditionContext CreateContext(Condition condition, bool isNegative = false)
    {
        //Cria um novo contexto para ser usado como condição
        var onNewContext = new ConditionContext();
        onNewContext.value = condition;
        onNewContext.isNegative = isNegative;

        return onNewContext;
    }

    public void ClearActions()
    {
        //Limpa todas as ações da lista e as ações ocorrendo atualmente
        actionList.Clear();
        StopAllCoroutines();
        ResetValues();
    }

    private void ResetValues()
    {
        jumpHolded.condition = false;
        onGround.condition = false;
        onGoingOffGround.condition = false;
        jumpHolded.condition = false;
        onDash.condition = false;
        onDashDelay.condition = false;
        onCanWalk.condition = true;

        currentHorizontalDirection = 0;
    }
}
