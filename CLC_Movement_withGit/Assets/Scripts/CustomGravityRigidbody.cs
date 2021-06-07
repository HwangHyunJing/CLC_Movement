using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 기본적으로 rigidbody가 있어야 쓸모가 있는 컴포넌트이므로
[RequireComponent(typeof(Rigidbody))]

// Rigidbody 설정을 건드는 것 보다는, 코드를 통해 중력만 건드는 식
public class CustomGravityRigidbody : MonoBehaviour
{
    // float Delay의 적용 여부를 결정
    [SerializeField]
    bool floatToSleep = false;

    // 물체의 잠김을 파악하는 기준
    [SerializeField]
    float submergenceOffset = .5f;

    // 물체의 잠김을 판단하는 범위
    [SerializeField, Min(.1f)]
    float submergenceRange = 1f;

    // 부력
    [SerializeField, Min(0f)]
    float buoyancy = 1f;

    // 물체의 가볍고 무거운 부분을 임의로 정하는 위치
    [SerializeField]
    Vector3 buoyancyOffset = Vector3.zero;

    // 장력
    [SerializeField, Range(0f, 10f)]
    float waterDrag = 1f;

    // 물 layer mask
    [SerializeField]
    LayerMask waterMask = 0;

    


    Rigidbody body;
    // 물체가 공중에 떠 있는 상태에서 속도가 0이 되어서 그대로 고정되는 것을 방지
    // + 이전 예제에서 Layer Matrix 때문에 agent랑 detailed랑 충돌 안함. 그거 바꿔야 기능한다
    float floatDelay;

    // 물체가 잠긴 정도
    float submergence;
    // 
    Vector3 gravity;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
    }

    private void FixedUpdate()
    {
        if(floatToSleep)
        {
            // 해당 물체의 rigidbody가 sleep 상태인 경우
            if (body.IsSleeping())
            {
                floatDelay = 0f;
                return;
            }

            // 혹은 sleep은 아닌데, 움직임이 거의 없는 경우
            if (body.velocity.sqrMagnitude < 0.0001f)
            {
                floatDelay += Time.deltaTime;
                if (floatDelay >= 1f)
                {
                    return;
                }
            }
            else
            {
                floatDelay = 0f;
            }
        }

        gravity = CustomGravity.GetGravity(body.position);
        if(submergence > 0f)
        {
            // 속도를 저해할 실제 장력을 구함
            float drag = Mathf.Max(0f, 1f - waterDrag * submergence * Time.deltaTime);
            // Vector3가 아니라 float이므로 속도를 곱해서 저해하는 것이 맞음 (부력 아님)
            body.velocity *= drag;
            // 지속적으로 물체가 회전하는 것을 막기 위해 angular 속도도 조절
            body.angularVelocity *= drag;
            // body.AddForce (gravity * -(buoyancy * submergence), ForceMode.Acceleration);
            body.AddForceAtPosition(gravity * -(buoyancy * submergence),
                transform.TransformPoint(buoyancyOffset),
                ForceMode.Acceleration);

            // 잠긴 정도의 값을 리셋 (...왜?)
            submergence = 0f;
        }

        // 물체에 중력의 방향으로 가속되도록 함 = 중력이 작용
        body.AddForce(gravity, ForceMode.Acceleration);
    }

    private void OnTriggerEnter(Collider other)
    {
        if((waterMask & (1 << other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(!body.IsSleeping() && (waterMask & (1 << other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence();
        }
    }

    void EvaluateSubmergence ()
    {
        Vector3 upAxis = -gravity.normalized;
        if(Physics.Raycast(
            body.position + upAxis * submergenceOffset,
            -upAxis, out RaycastHit hit, submergenceRange + 1f,
            waterMask, QueryTriggerInteraction.Collide))
        {
            submergence = 1f - hit.distance / submergenceRange;
        }
        else
        {
            submergence = 1f;
        }
    }
}
