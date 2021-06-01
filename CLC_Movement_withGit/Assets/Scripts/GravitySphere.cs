using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitySphere : GravitySource
{
    [SerializeField]
    float gravity = 9.81f;

    // outerRadius는 중력이 상수 최대로 작용하는 범위
    // outerFalloffRadius 외는, 중력이 더 이상 작용하지 않는 범위
    // 보통 play area는 outer Radius 내부로 잡는게 좋다
    [SerializeField, Min(0f)]
    float outerRadius = 10f, outerFalloffRadius = 15f;

    // 거리에 따라서 감소되는 중력의 정도
    // 이 값은 지정된 상수가 아니라 outerFalloff, outer 이 두개에 따라서 달라진다
    float outerFalloffFactor;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        outerFalloffRadius = Mathf.Max(outerFalloffRadius, outerRadius);
        outerFalloffFactor = 1f / (outerFalloffRadius - outerRadius);
    }

    private void OnDrawGizmos()
    {
        Vector3 p = transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, outerRadius);
        if(outerFalloffRadius > outerRadius)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(p, outerFalloffRadius);
        }
    }

    public override Vector3 GetGravity (Vector3 position)
    {
        // 플레이어 -> 중심으로 향하는 중력 방향의 벡터
        Vector3 vector = transform.position - position;
        float distance = vector.magnitude;
        if(distance > outerFalloffRadius)
        {
            return Vector3.zero;
        }

        // 이미 방향을 알기 때문에 normalize보다는 divide가 낫다
        // 즉, 크기*방향 대신에, 그냥 vector 값을 distance로 나누는 식
        // (방향 성분만 따로 뽑을 이유가 없어서 그럼)
        float g = gravity / distance;

        // 방향은 정해졌으니, 크기만 조정
        if(distance > outerRadius)
        {
            // 이것도 plane처럼, distance == outerRadius이면 중력의 손실이 없음
            // 반대로 distance가 outerFalloffRadius가 되면 g는 0이 됨 (OnValidate 참고)
            g *= 1f - (distance - outerRadius) * outerFalloffFactor;
        }

        return g * vector;
    }

}
