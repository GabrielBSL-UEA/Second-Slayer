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

        //Checa a lista de a????es a cada update, vendo se tem alguma a????o a ser realizada.
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

            //Realiza a a????o e a retira da lista de a????es;
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
        //Cria-se duas vari??veis que ser??o usadas na chamada de BoxCast para detec????o do ch??o
        //Origin: ponto de origem do quadrado.
        //Size: tamanho do quadrado.
        var boxBounds = controller.BoxCollider2D.bounds;
        Vector2 origin = new Vector2(boxBounds.center.x, boxBounds.center.y - boxBounds.extents.y - (distanceFromFoot / 2) + .1f);
        Vector2 size = new Vector2(boxBounds.size.x - .1f, distanceFromFoot / 2);

        //Chama a fun????o BoxCast da classe DrawingRaycast2D que, al??m de gerar a caixa, tamb??m a desenha no editor do Unity para f??cil vizualiza????o
        var playerOnGround = DrawingRaycast2D.OverlapBox(origin, size, groundLayer, Color.green, Color.red);

        //Verifica se o jogador estava em contato o ch??o e deixou de estar, importante para o coyote time do pulo do abismo.
        if (onGround.condition && !playerOnGround)
        {
            StartCoroutine(SetOnGoingOffGroundByTime());
        }

        //Seta o onGround e, dependendo se o jogador est?? em contato com o ch??o ou n??o, muda o valor de pulos no ar.
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
        //Recebe o valor do direcional da esquerda e da direita, traduzindo-os em velocidade horizontal para o personagem em forma de for??a
        //Verifica a diferen??a entre a velocidade desejada e a velocidade atual e multiplica o resultado na aplica????o da for??a.
        //Quandto mais perto da velocidade m??xima, menor ?? a for??a aplicada e quando mais longe, maior ?? a for??a

        //O resultado disso ?? um movimento mais natural do personagem
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
        //Verifica a velocidade horizontal do personagem, alterando seu valor dependendo se ele est?? caindo ou n??o
        controller.Rigidbody2D.gravityScale = controller.Rigidbody2D.velocity.y > 0 ? gravityForce :
            gravityForceOnFalling;

        //Controla a velocidade de queda do jogador para que ele n??o passe do limite estabelecido
        controller.Rigidbody2D.velocity = new Vector2(controller.Rigidbody2D.velocity.x, Mathf.Clamp(controller.Rigidbody2D.velocity.y, -maxFallingVelocity, maxUpwardVelocity));
    }

    //---------
    //InputReceivers
    //---------

    public void ReceiveDirectionalInput(Vector2 directionInput)
    {
        //Recebe o input horizontal do jogador
        currentHorizontalDirection = directionInput.x;

        //Guarda a dire????o em que o jogador est?? virado
        if (currentHorizontalDirection == 0)
        {
            return;
        }
        facingRight = currentHorizontalDirection > 0;
        facingValue = facingRight ? 1 : -1;
    }

    public void ReceiveJumpInput(bool inputContext)
    {
        //Salva a informa????o de que o jogador est?? segurando o bot??o de pular ou n??o
        jumpHolded.condition = inputContext;

        if (!inputContext)
        {
            return;
        }

        //Cria as vari??veis que ser??o usadas para determinar o tempo de vida da a????o e suas condi????es
        float actionTime;
        List<ConditionContext> conditions = new List<ConditionContext>();

        conditions.Add(CreateContext(onDash, true));

        //Verifica se o jogador est?? no ch??o ou se a condi????o de coyote time do abismo s??o verdadeiras 
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
                //Determina a condi????o de pular para quando o jogador encostar no ch??o e seu tempo de vida
                actionTime = reachingGroundJumpTime;
                conditions.Add(CreateContext(onGround));
            }
        }

        //Chama a fun????o que salva as a????es e condi????es por um certo per??odo de tempo
        StartCoroutine(AddActionToList(actionTime, Jump(), conditions));
    }

    public void ReceiveDashInput()
    {
        if (onDashDelay.condition)
        {
            return;
        }

        //Adiciona a????o de dash na lista de a????es
        StartCoroutine(AddActionToList(dashWaitingTime, Dash(), new List<ConditionContext>()));
    }

    //---------
    //Timers
    //---------

    private IEnumerator AddActionToList(float duration, IEnumerator action, List<ConditionContext> conditions)
    {
        //Adiciona a condi????o na lista de a????o e as condi????es no dicion??rio de condi????es
        actionList.Add(action);
        actionConditions[action] = conditions;

        //Seta o tempo de vida da a????o
        yield return new WaitForSeconds(duration);

        //Retira a a????o da lista caso ela ainda esteja l?? (ela n??o estar?? caso tenha sido acionada)
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

        //Inicia o loop que modifica a velocidade vertical do jogador pelo valor pr??-determinada pelo jogador
        while (timePassed < maxHoldTime)
        {
            //Caso o jogador n??o esteja pressionando o bot??o de pular, o loop ?? quebrado mais r??pido, fazendo ele pular uma altura menor
            if (!jumpHolded.condition && timePassed > minHoldTime)
            {
                break;
            }

            //Velocidade vertical ?? setada e o loop espera pelo pr??ximo update f??sico
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

        //Seta a dire????o do dash
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
        //Cria um novo contexto para ser usado como condi????o
        var onNewContext = new ConditionContext();
        onNewContext.value = condition;
        onNewContext.isNegative = isNegative;

        return onNewContext;
    }

    public void ClearActions()
    {
        //Limpa todas as a????es da lista e as a????es ocorrendo atualmente
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
