using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccelerationZone : MonoBehaviour
{
    // 어느 속력의 물체에 대해서까지만 가속을 가할 것인가
    [SerializeField, Min(0f)]
    float acceleration = 10f, speed = 10f;

    private void OnTriggerEnter(Collider other)
    {
        // 들어간 대상의 rigidbody를 가져옴
        Rigidbody body = other.attachedRigidbody;
        if(body)
        {
            Accelerate(body);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if(body)
        {
            Accelerate(body);
        }
    }

    void Accelerate (Rigidbody body)
    {
        // Vector3 velocity = body.velocity;
        Vector3 velocity = transform.InverseTransformDirection(body.velocity);
        // 여기 velocity는 객체의 변수가 아니라 Rigidbody의 변수입니다

        // speed 이상의 물체에 대해서는 힘을 가하지 않는다
        if(velocity.y >= speed)
        {
            return;
        }

        // 순식간에 힘을 주는 게 아니라 정말 가속을 부여하는 것
        if(acceleration > 0f)
        {
            velocity.y = Mathf.MoveTowards(
                velocity.y, speed, acceleration * Time.deltaTime);
        }
        // 물론 따로 가속도 넣은 생각아 아니면 그냥 속력 집어넣으면 된다
        else
        {
            // 시작은 y방향 가속(말이 가속이지 그냥 힘 가하는 거)
            velocity.y = speed;
        }

        body.velocity = transform.TransformDirection(velocity);

        // 다만, 직접 normal을 검사하지 않기 때문에 모든 면에 대한 반사가 아니라 물체의 y방향 벡터에 대한 튕김이다

        // 태그 검사하지 말고, 이걸로 검사하자
        if(body.TryGetComponent(out MovingSphere sphere))
        {
            sphere.PreventSnapToGround();
        }
    }
}
