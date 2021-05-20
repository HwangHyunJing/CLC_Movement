using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f;

    // ground의 이동 바운더리 설정
    [SerializeField]
    Rect allowedArea = new Rect(-5f, -5f, 10f, 10f);

    Vector3 velocity;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // Get Player Input
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        // playerInput.Normalize();
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        // 값 저장을 위해 velocity를  전역변수화
        // Vector3 velocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        Vector3 desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        float maxSpeedChange = maxAcceleration * Time.deltaTime;

        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        Vector3 displacement = velocity * Time.deltaTime;

        // local Position에 바로 접근해도 되지만, 변수를 한 번 거쳐서 판단
        Vector3 newPosition = transform.localPosition + displacement;
        // 오버로딩 형을 맞추기 위해, 인자는 Vector2로
        if(!allowedArea.Contains(new Vector2(newPosition.x, newPosition.z)))
        {
            newPosition = transform.localPosition;
        }

        transform.localPosition = newPosition;
    }
}
