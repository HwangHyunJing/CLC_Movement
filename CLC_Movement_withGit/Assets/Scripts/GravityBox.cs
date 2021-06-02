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

    // 상자의 '외부' 중력을 설정
    [SerializeField, Min(0f)]
    float outerDistance = 0f, outerFalloffDistance = 0f;

    // 거리에 따른 중력 크기 차감
    float innerFalloffFactor, outerFalloffFactor;

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

        // outer 값에 대해서도 최대, 최소 조정
        outerFalloffDistance = Mathf.Max(outerFalloffDistance, outerDistance);

        innerFalloffFactor = 1f / (innerFalloffDistance - innerDistance);
        outerFalloffFactor = 1f / (outerFalloffDistance - outerDistance);
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

        // 각각 노란색, 파란색으로 칠한다
        if(outerDistance > 0f)
        {
            Gizmos.color = Color.yellow;
            DrawGizmosOuterCube(outerDistance);
        }
        if(outerFalloffDistance > outerDistance)
        {
            Gizmos.color = Color.cyan;
            DrawGizmosOuterCube(outerFalloffDistance);
        }
    }

    // 라이브러리 아님!
    // 4개의 점을 잇는 직사각형을 그림
    // 상자의 각 면에 대해한 matrix4x4를 직접 접근할 수 없으므로
    void DrawGizmosRect(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    void DrawGizmosOuterCube (float distance)
    {
        Vector3 a, b, c, d;

        // boundary distance의 단순한 연장선
        a.y = b.y = boundaryDistance.y;
        d.y = c.y = -boundaryDistance.y;
        b.z = c.z = boundaryDistance.z;
        d.z = a.z = -boundaryDistance.z;
        a.x = b.x = c.x = d.x = boundaryDistance.x + distance;

        // 일단 하나를 그리고
        DrawGizmosRect(a, b, c, d);

        // x좌표에 대해서 반전
        a.x = b.x = c.x = d.x = -a.x;
        DrawGizmosRect(a, b, c, d);

        a.x = d.x = boundaryDistance.x;
        b.x = c.x = -boundaryDistance.x;
        a.z = b.z = boundaryDistance.z;
        c.z = d.z = -boundaryDistance.z;
        a.y = b.y = c.y = d.y = boundaryDistance.y + distance;
        DrawGizmosRect(a, b, c, d);
        a.y = b.y = c.y = d.y = -a.y;
        DrawGizmosRect(a, b, c, d);

        a.x = d.x = boundaryDistance.x;
        b.x = c.x = -boundaryDistance.x;
        a.y = b.y = boundaryDistance.y;
        c.y = d.y = -boundaryDistance.y;
        a.z = b.z = c.z = d.z = boundaryDistance.z + distance;
        DrawGizmosRect(a, b, c, d);
        a.z = b.z = c.z = d.z = -a.z;
        DrawGizmosRect(a, b, c, d);

        distance *= 0.57f; // 0.57은 3의 제곱근 근사값입니다
        Vector3 size = boundaryDistance;
        size.x = 2f * (size.x + distance);
        size.y = 2f * (size.y + distance);
        size.z = 2f * (size.z + distance);
        Gizmos.DrawWireCube(Vector3.zero, size);

    }

    // coordinate는 position의 '상대적인 위치'
    // 평면은 한 방향, 구체는 한 목적지를 향하지만 박스는 180에서 -180으로 순식간에 변동
    float GetGravityComponent(float coordinate, float distance)
    {
        float g = gravity;

        // 플레이어가 충분히 떨어져있는 경우 거리에 따라 중력이 차감
        if(distance > innerDistance)
        {
            // distance == innerDistance인 경우, g의 크기는 1
            // 반대로 둘의 차이가 inner와 innerFalloff 차이와 같으면, g의 크기는 0
            g *= 1 - (distance - innerDistance) * innerFalloffFactor;
        }

        // 그냥 g를 리턴하게 되면, + 방향만 중력이 적용되고 -는 적용되지 X
        // return g로 바꾸고 테스트하면 coordinate의 역할을 알 수 있다
        return coordinate > 0f ? -g : g;
    }

    // 다시 말하지만, position은 플레이어의 위치
    public override Vector3 GetGravity (Vector3 position)
    {
        // box gravity는 중심에서 '어느 방향에 있는가'에 따라 중력의 방향이 달라진다
        // 그래서 player의 position 정보를 '상대적인 위치'로 판단하고 계산

        // 설명으로는 크기 성분을 무시한다고 했는데, 뭔 소리인지는 영영 모르겠음
        // position -= transform.position;
        position = transform.InverseTransformDirection(position - transform.position);
        Vector3 vector = Vector3.zero;

        int outside = 0;
        // 중심부보다 멀어서 경계도 벗어나는 경우
        if(position.x > boundaryDistance.x)
        {
            vector.x = boundaryDistance.x - position.x;
            outside += 1;
        }
        // 반대쪽 끝
        else if (position.x < -boundaryDistance.x)
        {          
            vector.x = -boundaryDistance.x - position.x;
            outside += 1;
        }

        if(position.y > boundaryDistance.y)
        {
            vector.y = boundaryDistance.y - position.y;
            outside += 1;
        }
        else if(position.y < -boundaryDistance.y)
        {
            vector.y = -boundaryDistance.y - position.y;
            outside += 1;
        }

        if (position.z > boundaryDistance.z)
        {
            vector.z = boundaryDistance.z - position.z;
            outside += 1;
        }
        else if (position.z < -boundaryDistance.z)
        {
            vector.z = -boundaryDistance.z - position.z;
            outside += 1;
        }


        // 구체가 박스 밖에 있는 경우
        if(outside > 0)
        {
            float distance = (outside==1 ? Mathf.Abs(vector.x + vector.y + vector.z) : vector.magnitude);
            if(distance > outerFalloffDistance)
            {
                return Vector3.zero;
            }

            float g = gravity / distance;
            if(distance > outerDistance)
            {
                g *= 1f - (distance - outerDistance) * outerFalloffFactor;
            }

            // 원본은 여기서 +g를 곱하는데, 방향 이슈가 생겨서 -g로 했습니다
            return transform.TransformDirection(-g * vector);
        }

        // 중심부 기준에서의 거리(크기)를 구함
        Vector3 distances;
        distances.x = boundaryDistance.x - Mathf.Abs(position.x);
        distances.y = boundaryDistance.y - Mathf.Abs(position.y);
        distances.z = boundaryDistance.z - Mathf.Abs(position.z);

        // 중심을 기준으로 distnaces(땅에서 '부터의' 거리)가 가장 짤은 면의 중력을 받음
        // 그림 그려보면 이해 감
        if(distances.x < distances.y)
        {
            if(distances.x < distances.z)
            {
                vector.x = GetGravityComponent(position.x, distances.x);
            }
            else 
            {
                // distances.x >= distances.z
                vector.z = GetGravityComponent(position.z, distances.z);
            }
        }
        else if (distances.y < distances.z)
        {
            vector.y = GetGravityComponent(position.y, distances.y);
        }
        else
        {
            vector.z = GetGravityComponent(position.z, distances.z);
        }

        // 해당 메소드 초반에 InverseTransformDirection 했기 때문에 반대로 돌리는 것
        // return vector;

        //Debug.Log("return inner gravity");

        return transform.TransformDirection(vector);
    }
}
