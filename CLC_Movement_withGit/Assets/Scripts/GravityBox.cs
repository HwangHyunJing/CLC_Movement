using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sphere랑 전혀 반대 방향, 같은 원리
// constant 벡터 - 거리 감산 - zero 이렇게 3단계

public class GravityBox : GravitySource
{
    [SerializeField]
    float gravity = 9.81f;

    // 이 값은 inspector에서 x y z 개별로 설정해줘야 한다
    [SerializeField]
    Vector3 boundaryDistance = Vector3.one;

    // 이 distance는 '평면에서부터의 거리'가 된다
    // 즉 innerFalloff가 더 작은 박스이지만, 해당 값은 더 크다
    [SerializeField, Min(0f)]
    float innerDistance = 0f, innerFalloffDistance = 0f;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        boundaryDistance = Vector3.Max(boundaryDistance, Vector3.zero);

        // 지금 우리는 총 3개의 성분을 사용하는 중
        // boundary >= inner >= innerFalloff 순서의 크기가 되어야 하므로
        // boundary의 x y z중 가장 작은 값을 max Inner로 두어서
        // 두 inner 거리 성분의 값이 max Inner를 넘지 못하도록 제한
        float maxInner = Mathf.Min(Mathf.Min(boundaryDistance.z, boundaryDistance.y), boundaryDistance.z);
        innerDistance = Mathf.Min(innerDistance, maxInner);
        innerFalloffDistance = Mathf.Max(Mathf.Min(innerFalloffDistance, maxInner), innerDistance);
    }

    private void OnDrawGizmos()
    {
        // 그냥 차원 성분 가져오는 하나의 간단한 식
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Vector3 size;
        if(innerFalloffDistance > innerDistance)
        {
            Gizmos.color = Color.cyan;
            size.x = 2f * (boundaryDistance.x - innerFalloffDistance);
            size.y = 2f * (boundaryDistance.y - innerFalloffDistance);
            size.z = 2f * (boundaryDistance.z - innerFalloffDistance);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
        if (innerDistance > 0f)
        {
            Gizmos.color = Color.yellow;
            size.x = 2f * (boundaryDistance.x - innerDistance);
            size.y = 2f * (boundaryDistance.y - innerDistance);
            size.z = 2f * (boundaryDistance.z - innerDistance);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, 2f * boundaryDistance);
    }
}
