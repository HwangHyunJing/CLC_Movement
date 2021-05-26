using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;

    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;

    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;

    [SerializeField, Min(0f)]
    float probeDistance = 1f;

    // 지면으로 판단하는 최대 각도
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;

    // 계단으로 판단하는 최대 각도
    [SerializeField, Range(0, 90)]
    float maxStairAngle = 50f;

    [SerializeField]
    LayerMask probeMask = -1, stairsMask = -1;

    [SerializeField]
    Transform playerInputSpace = default;

    // desired 속도도 전역으로 처리
    Vector3 velocity, desiredVelocity;

    // 점프의 가능 여부를 판명
    bool desiredJump;

    // 땅 위에 있는가 판명 >> 아래의 기능에 포함되므로 대체
    int groundContactCount;
    bool OnGround => groundContactCount > 0;

    // groundContactCount에 대응해서 생성
    int steepContactCount;
    bool OnSteep => steepContactCount > 0;

    // 공중에서 점프를 몇 번 했는가 체크
    int jumpPhase;

    // 땅으로 판정해주는 최소 값: 별도로 정의해줘야 한다
    float minGroundDotProduct;
    // 계단으로 판정해주는 최소 값
    float minStairsDotProdut;

    // 접촉면의 지면 각도를 판별
    Vector3 contactNormal;
    // 기울기가 크지만 천장 정도는 아닌 지면에 대한 법선
    Vector3 steepNormal;

    // 마지막으로 땅에 닿은 후 지난 physics step
    int stepsSinceLastGrounded;
    // 점프의 횟수가 아니라, 마지막 점프 후 지난 physics step
    int stepsSinceLastJump;

    // 환경에 따라 지정되는 '위' 방향
    Vector3 upAxis;
    // upAxis의 변경에 따라, right, forward의 방향도 달라진다
    Vector3 rightAxis, forwardAxis;

    Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }

    private void OnValidate()
    {
        // 땅으로 판단하는 최소 각도를 정해준다
        // Vector3.up과 표면 법선 벡터와의 Dot 연산 결과와도 동일하다 (물론 라디안은 곱해줘야)
        minGroundDotProduct = Mathf.Cos(maxGroundAngle) * Mathf.Deg2Rad;
        minStairsDotProdut = Mathf.Cos(maxStairAngle) * Mathf.Deg2Rad;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // 프레임마다 이동과 관련된 입력을 처리
    void Update()
    {

        // Get Player Input
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        // 플레이어가 카메라(playerInputSpace)에 맞게 돌아가는 것
        if (playerInputSpace)
        {
            /*
            // 순수하게 카메라 Input의 앞/옆 성분만 가져오는 과정
            Vector3 forward = playerInputSpace.forward;
            forward.y = 0f; // 수직 성분 제거
            forward.Normalize();

            Vector3 right = playerInputSpace.right;
            right.y = 0f; // 수직 성분 제거
            right.Normalize();

            desiredVelocity = (forward * playerInput.y + right * playerInput.x) * maxSpeed;
            */
            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);           
        }

        // 코드 통일에 따라 이동
        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        // 점프에 대한 입력을 받았는가?
        // |=를 쓰면 비 입력 상태가 점프 하려는 상태를 덮어쓰는 일이 없어진다
        desiredJump |= Input.GetButtonDown("Jump");

        // 확인을 위한 색상 변경
        GetComponent<Renderer>().material.SetColor("_Color", OnGround ? Color.black : Color.white);
    }

    // 값에 대한 계산들을 처리, 판단
    private void FixedUpdate()
    {
        // 값의 계산이니 Fixed Update에, 중력이기 때문에 가장 우선
        upAxis = -Physics.gravity.normalized;

        UpdateState();
        AdjustVelocity();


        // 점프 여부를 판단, 실행
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        // rigidbody가 추가되면서 필요 없어진 요소들은 전부 제거
        body.velocity = velocity;

        // 점프 판단 후에는 다시 onGround를 디폴트 값인 false로 바꿔준다
        ClearState();
    }

    // 기본적인 상태값을을 업데이트
    void UpdateState()
    {
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;


        // 앞에 저장했던 속도를 그대로 사용해서 판단
        velocity = body.velocity;

        if(OnGround || SnapToGround() || CheckSteepContacts())
        {
            stepsSinceLastGrounded = 0;

            // 물론 땅에 닿으면 초기화되는 게 맞기는 하지만
            // 점프가 입력된 직후(Update) 바로 jumpPhase가 여기서 0으로 초기화(FixedUpdate)되면 X
            // 때문에 어느 정도의 스텝(1)이 지난 뒤에 jumpPhase가 초기화 될 가능성을 열어두는 것

            // 즉 2부터 초기화되는 게 아니라, 1 이하에서 초기화되는 걸 막는다는 개념
            if (stepsSinceLastJump > 1)
            {
                jumpPhase = 0;
            }
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }            
        }
        else
        {
            // '위'에 대한 모든 정의가 바뀜; Vector3.up -> upAxis;
            // contactNormal = Vector3.up;
            contactNormal = upAxis;
        }
    }

    // 복합적인 기능이 필요하므로, 별도의 메소드를 추가
    void ClearState()
    {        
        groundContactCount = steepContactCount = 0;
        // Evaluate Collision에서 Contact Normal이 단순 할당이 아니라 '축적'방식으로 변경.
        // 때문에 이를 초기화하는 코드가 필요
        
        contactNormal = steepNormal = Vector3.zero;
    }

    void Jump()
    {

        Vector3 jumpDirection;

        // jumpDirection의 값 할당

        // 땅 위에 있는 경우
        if (OnGround)
        {
            jumpDirection = contactNormal;
        }
        // 땅은 아니지만, 가파른 지형에 닿아있는 경우
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
            jumpPhase = 0;
        }
        // 아예 닿아있는 곳은 없지만 공중 점프가 가능한 경우
        else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
        {
            // 그대로 절벽에 떨어진 후에 점프키를 입력
            // 이 경우 Air 이전의 일반 점프는 스킵

            // 다만 이 직후에 만약 Air Jump 자체를 막아두었다면, 점프를 방지할 방법이 없으므로
            // 그냥 if문에서 maxAirJumps > 0으로 사전에 막아두는 방식
            if(jumpPhase == 0)
            {
                jumpPhase = 1;
            }

            // Update State에서, 닿아있는 땅이 없으면 기본적으로 Vector3.up을 할당한다
            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }            

        // 점프가 막 실행되었으므로 해당 값 초기화
        stepsSinceLastJump = 0;

        jumpPhase += 1;
        // 중력의 성분 중 일부만 사용
        float jumpSpeed = Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);

        // 벽 점프 시, 윗 방향으로 편향되도록 조정
        // 벡터를 내리는 게 아니므로, 그냥 합벡터하고 방향을 뽑자 (정규화)
        jumpDirection = (jumpDirection + upAxis).normalized;
        // 일반 점프, 공중 점프의 Dir은 어차피 이미 Vector3.up을 사용하고 있어서 변화X

        // contact normal 방향에 맞게 alignedSpeed (점프 속도) 설정
        // contact Normal대신, 다양한 방향으로 점프가 가능하도록 jump Direction을 사용
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);

        // jumpSpeed 대신 slignedSpeed를 사용
        if (alignedSpeed > 0f)
        {
            // 점프 속도가 기존의 속도를 초과하지 못하도록 함
            // velocity.y 대신에 alignedSpeed를 사용
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }

        // 지면으로 판별되는 곳의 윗 벡터 * 점프 속력
        velocity += jumpDirection * jumpSpeed;


    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    // 충돌 시 법선을 판단
    void EvaluateCollision (Collision collision)
    {
        // 지금 collide한 대상이 지면인가 계단인가
        float minDot = GetMinDot(collision.gameObject.layer);
        for(int i=0; i<collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            // SnapToGround(), CheckSteepContacts()처럼 upAxis를 기존의 normal 성분에 내적
            float upDot = Vector3.Dot(upAxis, normal);

            // normal.y -> upDot;
            if(upDot >= minGroundDotProduct)
            {
                // OnGround = true;
                groundContactCount += 1;
                // 모든 닿아있는 지면에 대해서 판단
                contactNormal += normal;
                
            }
            else if (upDot > -0.01f)
            {
                // (벽보다 완만한) 모든 가파른 면에 대해 판단한다는 의미
                // 원래는 minStairsDotProduct가 맞는데, 조금 더 유연하게 적용
                steepContactCount += 1;
                steepNormal += normal;
            }
        }
    }

    /*
    // player 구체가 경사의 '지면을 따라서' 이동하는 방향 (구하려는 것)
    Vector3 ProjectOnContactPlane (Vector3 vector)
    {
        // 그냥 차벡터로 방향을 바꾼 것
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }
    */

    // normal의 방향이 임의로 바뀔 수 있어서, 아예 이를 인자로 넘김
    // 기존에 contactNormal을 인자로 넘기기도 하지만, 평지 상에서 right, forward를 구하기 위해 upAxis를 넘기기도 한다
    Vector3 ProjectDirectionOnPlane (Vector3 direction, Vector3 normal)
    {
        return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }


    // contact Plane에 따라 점프 속도를 조정해주는 메소드
    void AdjustVelocity()
    {
        /*
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;
        */

        Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
        Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    // 필요한 경우 땅에 붙도록 하는 메소드 (기능이 동작한 여부를 리턴)
    // 급격한 지면 변화의 경우에 작동한다
    bool SnapToGround()
    {
        

        // 각 조건이 너무 길어서 각각 한건가....?
        // else보다는 의미의 명확성을 위해? (어차피 if 들어가면 return으로 나감)

        // 지면에서 떨어진 후 충분한(=1) physics step이 경과했는가?
        // 점프 스텝은, 점프 직후에 바로 Snap되는 걸 막기 위함
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
        {
            // 땅에 붙어있지 않다
            return false;
        }

        // 구체의 속력이 Snap을 허용할 상한을 넘는가?
        float speed = velocity.magnitude;
        if(speed > maxSnapSpeed)
        {
            // Snap하기에 너무 빠름
            return false;
        }

        // 하단으로 쏜 Ray와 충돌하는 것이 없는가?
        if (!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask))
        {
            // 땅이 없음
            return false;
        }

        // '위'의 정의가 변경 > 충돌 지점 기준으로, hit의 '위'도 변경
        float upDot = Vector3.Dot(upAxis, hit.normal);

        // 이 지면은 가파른가?
        // minGroundDotProduct는 값이 클수록 지면이 가파르다
        // 직접 그려서 normal.y랑 비교하면 알 수 있다

        // hit,normal.y -> upDot
        if (upDot < minGroundDotProduct)
        {
            // 가파르기에 땅으로 판단할 수 없음
            return false;
        }

        // 이 모든 조건을 패스 = 지면에 Snap 해야하는 상태가 맞다
        groundContactCount = 1;
        // contact normal은 이미 속도/가속도 관련해서 쓰는 중
        contactNormal = hit.normal;
        float dot = Vector3.Dot(velocity, hit.normal);
        if(dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }       
        return true;
    }

    float GetMinDot (int layer)
    {
        // &는 bit AND 연산자입니다
        return (stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProdut;
    }

    // SnapToGround 검사에서, 땅에 떨어져있기는 한데 자격이 있는 지면이 없는 경우 호출
    bool CheckSteepContacts()
    {
        if(steepContactCount > 1)
        {
            steepNormal.Normalize();

            // 중력 변경으로 인한 추가
            float upDot = Vector3.Dot(upAxis, steepNormal);

            // steepNormal.y -> upDot
            if(upDot >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }


        return false;
    }
}
