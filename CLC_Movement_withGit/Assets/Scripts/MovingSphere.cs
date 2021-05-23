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

    // desired 속도도 전역으로 처리
    Vector3 velocity, desiredVelocity;

    // 점프의 가능 여부를 판명
    bool desiredJump;

    // 땅 위에 있는가 판명 >> 아래의 기능에 포함되므로 대체
    // bool onGround;
    int groundContactCount;
    bool OnGround => groundContactCount > 0;

    // 공중에서 점프를 몇 번 했는가 체크
    int jumpPhase;

    // 땅으로 판정해주는 최소 값: 별도로 정의해줘야 한다
    float minGroundDotProduct;

    // 접촉면의 지면 각도를 판별
    Vector3 contactNormal;

    // 마지막으로 땅에 닿은 후 지난 physics step
    int stepsSinceLastGrounded;

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
        // onGround = false;
        ClearState();
    }

    // 기본적인 상태값을을 업데이트
    void UpdateState()
    {
        stepsSinceLastGrounded += 1;

        // 앞에 저장했던 속도를 그대로 사용해서 판단
        velocity = body.velocity;

        if(OnGround || SnapToGround())
        {
            stepsSinceLastGrounded = 0;
            jumpPhase = 0;
            if(groundContactCount > 1)
            {
                contactNormal.Normalize();
            }            
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }

    // 복합적인 기능이 필요하므로, 별도의 메소드를 추가
    void ClearState()
    {
        // 
        // onGround = false;
        groundContactCount = 0;
        // Evaluate Collision에서 Contact Normal이 단순 할당이 아니라 '축적'방식으로 변경.
        // 때문에 이를 초기화하는 코드가 필요
        
        contactNormal = Vector3.zero;
    }

    void Jump()
    {
        if(OnGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);

            // contact normal 방향에 맞게 alignedSpeed (점프 속도) 설정
            float alignedSpeed = Vector3.Dot(velocity, contactNormal);

            // jumpSpeed 대신 slignedSpeed를 사용
            if(alignedSpeed > 0f)
            {
                // 점프 속도가 기존의 속도를 초과하지 못하도록 함
                // velocity.y 대신에 alignedSpeed를 사용
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }

            // 지면으로 판별되는 곳의 윗 벡터 * 점프 속력
            velocity += contactNormal * jumpSpeed;
        }
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        // onGround = true;
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        // onGround = true;
        EvaluateCollision(collision);
    }

    // 충돌 시 법선을 판단
    void EvaluateCollision (Collision collision)
    {
        for(int i=0; i<collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            // onGround |= normal.y >= minGroundDotProduct;
            if(normal.y >= minGroundDotProduct)
            {
                // OnGround = true;
                groundContactCount += 1;
                // 모든 닿아있는 지면에 대해서 판단
                contactNormal += normal;
                
            }
        }
    }

    // player 구체가 경사의 '지면을 따라서' 이동하는 방향 (구하려는 것)
    Vector3 ProjectOnContactPlane (Vector3 vector)
    {
        // 그냥 차벡터로 방향을 바꾼 것
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    // contact Plane에 따라 점프 속도를 조정해주는 메소드
    void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    // 필요한 경우 땅에 붙도록 하는 메소드 (기능이 동작한 여부를 리턴)
    bool SnapToGround()
    {
        // 각 조건이 너무 길어서 각각 한건가....?
        // else보다는 의미의 명확성을 위해? (어차피 if 들어가면 return으로 나감)

        // 지면에서 떨어진 후 충분한(=1) physics step이 경과했는가?
        if (stepsSinceLastGrounded > 1)
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
        if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, probeDistance))
        {
            // 땅이 없음
            return false;
        }

        // 이 지면은 가파른가?
        // minGroundDotProduct는 값이 클수록 지면이 가파르다
        // 직접 그려서 normal.y랑 비교하면 알 수 있다
        if(hit.normal.y < minGroundDotProduct)
        {
            // 가파르기에 땅으로 판단할 수 없음
            return false;
        }

        // 이 모든 조건을 패스 = 지면에 Snap 해야하는 상태가 맞다
        groundContactCount = 1;
        // contact normal은 이미 속도/가속도 관련해서 쓰는 중
        contactNormal = hit.normal;
        // float speed = velocity.magnitude;
        float dot = Vector3.Dot(velocity, hit.normal);
        if(dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }       
        return true;
    }
}
