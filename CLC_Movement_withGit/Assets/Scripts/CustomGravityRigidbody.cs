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

    Rigidbody body;
    // 물체가 공중에 떠 있는 상태에서 속도가 0이 되어서 그대로 고정되는 것을 방지
    // + 이전 예제에서 Layer Matrix 때문에 agent랑 detailed랑 충돌 안함. 그거 바꿔야 기능한다
    float floatDelay;

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

        // 물체에 중력의 방향으로 가속되도록 함 = 중력이 작용
        body.AddForce(CustomGravity.GetGravity(body.position), ForceMode.Acceleration);
    }
}
