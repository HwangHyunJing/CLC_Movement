﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f, maxClimbSpeed = 2f;

    [SerializeField, Range(0f, 100f)]
    float 
            maxAcceleration = 10f,
            maxAirAcceleration = 1f,
            maxClimbAcceleration = 20f;

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

    // 지면, 벽과는 별개로, 오를 수 있는 최대 각도
    [SerializeField, Range(90, 180)]
    float maxClimbAngle = 140f;

    [SerializeField]
    LayerMask probeMask = -1, stairsMask = -1, climbMask = -1;

    [SerializeField]
    Transform playerInputSpace = default;

    [SerializeField]
    Material normalMaterial = default, jumpingMaterial = default, climbingMaterial = default;


    // desired 속도도 전역으로 처리
    // Vector3 velocity, desiredVelocity;
    // 연결된 플랫폼의 속도를 저장하는 변수 (필수는 아니지만 있으면 편함)
    // Vector3 connectionVelocity;

    // desiredVelocity 제거
    Vector3 velocity, connectionVelocity;

    // 움직이는 플랫폼의 위치
    Vector3 connectionWorldPosition;
    // 움직이는 플랫폼의 로컬 위치 (회전하는 물체는 world 위치가 그대로이므로)
    Vector3 connectionLocalPosition;

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

    //  지면이 가팔라질수록 법선의 각도는 작아지는 게 맞으므로 min이다
    // 땅으로 판정해주는 최소 값: 별도로 정의해줘야 한다
    float minGroundDotProduct;
    // 계단으로 판정해주는 최소값
    float minStairsDotProdut;
    // 오를 수 있는 벽으로 판정해주는 최소값
    float minClimbDotProduct;

    // 접촉면의 지면 각도를 판별
    Vector3 contactNormal;
    // 기울기가 크지만 천장 정도는 아닌 지면에 대한 법선
    Vector3 steepNormal;

    // 벽을 오르기 위한 감지 변수
    Vector3 climbNormal;
    int climbContactCount;

    bool Climbing => climbContactCount > 0;

    // 마지막으로 땅에 닿은 후 지난 physics step
    int stepsSinceLastGrounded;
    // 점프의 횟수가 아니라, 마지막 점프 후 지난 physics step
    int stepsSinceLastJump;

    // Update에서 쓰던 변수를 전역으로 돌림
    Vector2 playerInput;
    // 환경에 따라 지정되는 '위' 방향
    Vector3 upAxis;
    // upAxis의 변경에 따라, right, forward의 방향도 달라진다
    Vector3 rightAxis, forwardAxis;

    Rigidbody body;
    // 플레이어와 연결되어 있는 일부 움직이는 플랫폼을 트래킹하기 위한 변수
    Rigidbody connectedBody, previouslyConnectedBody;

    

    MeshRenderer meshRenderer;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        // rigidbody에서 지원하는 중력을 제거
        body.useGravity = false;
        meshRenderer = GetComponent<MeshRenderer>();
        OnValidate();
    }

    private void OnValidate()
    {
        // 땅으로 판단하는 최소 각도를 정해준다
        // Vector3.up과 표면 법선 벡터와의 Dot 연산 결과와도 동일하다 (물론 라디안은 곱해줘야)
        minGroundDotProduct = Mathf.Cos(maxGroundAngle) * Mathf.Deg2Rad;
        minStairsDotProdut = Mathf.Cos(maxStairAngle) * Mathf.Deg2Rad;
        minClimbDotProduct = Mathf.Cos(maxClimbAngle) * Mathf.Deg2Rad;
    }

    // 프레임마다 이동과 관련된 입력을 처리
    void Update()
    {

        // Get Player Input
        // Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        // 플레이어가 카메라(playerInputSpace)에 맞게 돌아가는 것
        if (playerInputSpace)
        {
            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);           
        }

        // 코드 통일에 따라 이동
        // desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        // 점프에 대한 입력을 받았는가?
        // |=를 쓰면 비 입력 상태가 점프 하려는 상태를 덮어쓰는 일이 없어진다
        desiredJump |= Input.GetButtonDown("Jump");

        // 확인을 위한 색상 변경
        // 원래 점프 용도로 확인하던게 있어서, 본문에서 하나 더 추가했다
        meshRenderer.material = OnGround ? normalMaterial : (Climbing ? climbingMaterial : jumpingMaterial);
    }

    // 값에 대한 계산들을 처리, 판단
    private void FixedUpdate()
    {
        // 값의 계산이니 Fixed Update에, 중력이기 때문에 가장 우선
        // 여기도 Physics.gravity 대신에 CustomGravity의 정적 메소드를 사용
        // upAxis = -Physics.gravity.normalized;
        Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
        // 어차피 out 파라미터 덕분에 upAxis값을 사용할 수 있다

        UpdateState();
        AdjustVelocity();


        // 점프 여부를 판단, 실행
        if (desiredJump)
        {
            desiredJump = false;
            // Jump();
            Jump(gravity);
        }

        if(!Climbing)
        {
            velocity += gravity * Time.deltaTime;
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

        // if(OnGround || SnapToGround() || CheckSteepContacts() || CheckClimbing())
        if (CheckClimbing() || OnGround || SnapToGround() || CheckSteepContacts())
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

        // 모든 판단을 마친 후
        if(connectedBody)
        {
            // 플랫폼 외 여타 물체의 움직임까지 따라가지 않기 위함
            // layer를 왜 안하나 했는데, animated 된 물체는 velocity가 없다고 함
            if(connectedBody.isKinematic || connectedBody.mass >= body.mass)
            {
                UpdateConnectionState();
            }
        }
    }

    // (Update State 에서 호출) 별도로 연결된 물체에 대한 처리
    void UpdateConnectionState()
    {
        if(connectedBody == previouslyConnectedBody)
        {
            Vector3 connectionMovement = connectedBody.transform.TransformPoint(connectionLocalPosition) - connectionWorldPosition;
            // 속도 = 거리 / 시간이라는 간단한 공식
            connectionVelocity = connectionMovement / Time.deltaTime;
        }

        // 연결된 물체에 대한 위치를 넘김
        // World는 플레이어의 위치를 저장?
        connectionWorldPosition = body.position;
        connectionLocalPosition = connectedBody.transform.InverseTransformPoint(connectionWorldPosition);
    }

    // 복합적인 기능이 필요하므로, 별도의 메소드를 추가
    void ClearState()
    {        
        groundContactCount = steepContactCount = climbContactCount = 0;
        // Evaluate Collision에서 Contact Normal이 단순 할당이 아니라 '축적'방식으로 변경.
        // 때문에 이를 초기화하는 코드가 필요
        
        contactNormal = steepNormal = connectionVelocity = Vector3.zero;
        connectionVelocity = Vector3.zero;
        previouslyConnectedBody = connectedBody;
        connectedBody = null;
    }

    void Jump(Vector3 gravity)
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
        float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);

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
        int layer = collision.gameObject.layer;
        float minDot = GetMinDot(layer);
        for(int i=0; i<collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            // SnapToGround(), CheckSteepContacts()처럼 upAxis를 기존의 normal 성분에 내적
            float upDot = Vector3.Dot(upAxis, normal);

            if(upDot >= minGroundDotProduct)
            {
                // OnGround = true;
                groundContactCount += 1;
                // 모든 닿아있는 지면에 대해서 판단
                contactNormal += normal;

                // 지금 닿아있는 지면에 대한 rigidbody를 넘김
                connectedBody = collision.rigidbody;
            }
            // else if (upDot > -0.01f)
            else
            {
                // (벽보다 완만한) 모든 가파른 면에 대해 판단한다는 의미
                if (upDot > -0.01f)
                {
                    // 원래는 minStairsDotProduct가 맞는데, 조금 더 유연하게 적용
                    steepContactCount += 1;
                    steepNormal += normal;

                    // 땅이 아니더라도 일단은 connected인 무언가이므로 저장
                    if (groundContactCount == 0)
                    {
                        connectedBody = collision.rigidbody;
                    }
                }

                // 오를 수 있는 벽인지도 같이 판단 (이전 if문에 의해 땅은 걸러짐)
                if (upDot >= minClimbDotProduct && (climbMask & (1 << layer)) != 0)
                {
                    climbContactCount += 1;
                    climbNormal += normal;
                    connectedBody = collision.rigidbody;
                }
            }
        }
    }

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
        Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
        Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);
        */

        float acceleration, speed;

        Vector3 xAxis, zAxis;

        if(Climbing)
        {
            acceleration = maxClimbAcceleration;
            speed = maxClimbSpeed;
            xAxis = Vector3.Cross(contactNormal, upAxis);
            zAxis = upAxis;
        }
        else
        {
            acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
            speed = maxSpeed;
            xAxis = rightAxis;
            zAxis = forwardAxis;
        }

        xAxis = ProjectDirectionOnPlane(xAxis, contactNormal);
        zAxis = ProjectDirectionOnPlane(zAxis, contactNormal);

        // 아예 일괄로 적용을 해버리네...
        Vector3 relativeVelocity = velocity - connectionVelocity;

        // velocity -> relative velocity
        float currentX = Vector3.Dot(relativeVelocity, xAxis);
        float currentZ = Vector3.Dot(relativeVelocity, zAxis);
        // 이래서 움직이는 플랫폼 위에서는 관성도 있다

        // float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        // desiredVelocity -> playerInput(전역)
        float newX = Mathf.MoveTowards(currentX, playerInput.x * speed, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, playerInput.y * speed, maxSpeedChange);

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

        // slope의 경우에도 Snap이 가능한 경우라면 그냥 다 받으려는 속셈인 듯
        connectedBody = hit.rigidbody;
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

            if(upDot >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }

    // 벽을 오르는 움직임
    bool CheckClimbing()
    {
        if(Climbing)
        {
            // 결국 모든 지면과 관련된 움직임을 groundContactCount와 conotactNormal로 관리
            // 이 경향이 CheckSteepContacts에서도 그대로 드러났었음

            groundContactCount = climbContactCount;
            contactNormal = climbNormal;
            return true;
        }

        return false;
    }
}
