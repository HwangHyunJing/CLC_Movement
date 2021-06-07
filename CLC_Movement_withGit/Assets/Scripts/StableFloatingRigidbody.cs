using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 기본적으로 rigidbody가 있어야 쓸모가 있는 컴포넌트이므로
[RequireComponent(typeof(Rigidbody))]

// 기존에 CustomGravityRigidbody를 응용한 스크립트
public class StableFloatingRigidbody : MonoBehaviour
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

    // 물체의 가볍고 무거운 부분을 임의로 정하는 위치(들)
    [SerializeField]
    // Vector3 buoyancyOffset = Vector3.zero;
    Vector3[] buoyancyOffsets = default;

    // 장력
    [SerializeField, Range(0f, 10f)]
    float waterDrag = 1f;

    // 물 layer mask
    [SerializeField]
    LayerMask waterMask = 0;

    // 조금 더 정확한 floating을 원하는지 여부 (추가적인 계산이 들어감)
    [SerializeField]
    bool safeFloating = false;


    Rigidbody body;
    // 물체가 공중에 떠 있는 상태에서 속도가 0이 되어서 그대로 고정되는 것을 방지
    // + 이전 예제에서 Layer Matrix 때문에 agent랑 detailed랑 충돌 안함. 그거 바꿔야 기능한다
    float floatDelay;

    // 물체의 각 부분이 잠긴 정도
    float [] submergence;
    // 
    Vector3 gravity;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        // 아예 submergence를 버리지는 않습니다
        submergence = new float[buoyancyOffsets.Length];
    }

    private void FixedUpdate()
    {
        if (floatToSleep)
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

        // 전체 waterDrag와 buoyancy를 buoyancyOffsets의 원소 수 만큼 나눠버림
        float dragFactor = waterDrag * Time.deltaTime / buoyancyOffsets.Length;
        float buoyancyFactor = -buoyancy / buoyancyOffsets.Length;

        for(int i=0; i<buoyancyOffsets.Length; i++)
        {
            if (submergence[i] > 0f)
            {
                // 속도를 저해할 실제 장력을 구함
                float drag = Mathf.Max(0f, 1f - dragFactor * submergence[i]);
                // Vector3가 아니라 float이므로 속도를 곱해서 저해하는 것이 맞음 (부력 아님)
                body.velocity *= drag;
                // 지속적으로 물체가 회전하는 것을 막기 위해 angular 속도도 조절
                body.angularVelocity *= drag;
                // body.AddForce (gravity * -(buoyancy * submergence), ForceMode.Acceleration);
                body.AddForceAtPosition(gravity * (buoyancyFactor * submergence[i]),
                    transform.TransformPoint(buoyancyOffsets[i]),
                    ForceMode.Acceleration);

                // 잠긴 정도의 값을 리셋 (...왜?)
                submergence[i] = 0f;
            }
        }

        

        // 물체에 중력의 방향으로 가속되도록 함 = 중력이 작용
        body.AddForce(gravity, ForceMode.Acceleration);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((waterMask & (1 << other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!body.IsSleeping() && (waterMask & (1 << other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence();
        }
    }

    void EvaluateSubmergence()
    {
        Vector3 down = gravity.normalized;
        Vector3 offset = down * -submergenceOffset;

        for(int i=0; i < buoyancyOffsets.Length; i++)
        {
            // Vector3 upAxis = -gravity.normalized;
            Vector3 p = offset + transform.TransformPoint(buoyancyOffsets[i]);
            if (Physics.Raycast(
                p, down, out RaycastHit hit, submergenceRange + 1f,
                waterMask, QueryTriggerInteraction.Collide))
            {
                submergence[i] = 1f - hit.distance / submergenceRange;
            }
            // safeFloating이 true인 경우, 뒤에 CheckSphere가 발동
            else if (!safeFloating
                || Physics.CheckSphere(p, 0.01f, waterMask, QueryTriggerInteraction.Collide))
            {
                submergence[i] = 1f;
            }
        }

        
    }
}
