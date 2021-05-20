using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f;

    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;

    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;

    // desired 속도도 전역으로 처리
    Vector3 velocity, desiredVelocity;

    // 점프의 가능 여부를 판명
    bool desiredJump;

    // 땅 위에 있는가 판명
    bool onGround;

    // 공중에서 점프를 몇 번 했는가 체크
    int jumpPhase;

    Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
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
    }

    // 값에 대한 계산들을 처리, 판단
    private void FixedUpdate()
    {
        
        UpdateState();

        float maxSpeedChange = maxAcceleration * Time.deltaTime;

        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        /*
        Vector3 displacement = velocity * Time.deltaTime;

        // local Position에 바로 접근해도 되지만, 변수를 한 번 거쳐서 판단
        Vector3 newPosition = transform.localPosition + displacement;

        transform.localPosition = newPosition;
        */

        // 점프 여부를 판단, 실행
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        // rigidbody가 추가되면서 필요 없어진 요소들은 전부 제거
        body.velocity = velocity;

        

        // 점프 판단 후에는 다시 onGround를 디폴트 값인 false로 바꿔준다
        onGround = false;
    }

    // 기본적인 상태값을을 업데이트
    void UpdateState()
    {
        // 앞에 저장했던 속도를 그대로 사용해서 판단
        velocity = body.velocity;

        if(onGround)
        {
            jumpPhase = 0;
        }
    }

    void Jump()
    {
        if(onGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            velocity.y += Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
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
            onGround |= normal.y >= 0.9f;
        }
    }
}
