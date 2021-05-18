using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f;

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
        transform.localPosition += displacement;
    }
}
