using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityPlane : GravitySource
{
    // 중력의 크기
    [SerializeField]
    float gravity = 9.81f;

    // 중력의 범위
    [SerializeField]
    float range = 1f;

    // Plane 방식의 중력을 리턴라도록 오버라이딩
    // 여기서 position은 플레이어의 rigidbody의 위치를 의미
    public override Vector3 GetGravity (Vector3 position)
    {
        Vector3 up = transform.up;
        float distance = Vector3.Dot(up, position - transform.position);
        
        // 구체와 plane 사이 거리(distance)가 range보다 멀다면 무중력 처리
        if(distance > range)
        {
            return Vector3.zero;
        }
        return -gravity * up;
    }

    private void OnDrawGizmos()
    {
        // 만약 ProBuilder를 쓴다면, 물체의 pivot(중심)을 가운데로 설정해야 한다

        Vector3 scale = transform.localScale;
        scale.y = range;
        // plane의 transform 성분을 건내주는 식
        // Gizmos의 차원 성분을 정해준다고 생각하면 편합니다
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);

        Vector3 size = new Vector3(1f, 0f, 1f);
        Gizmos.color = Color.yellow;
        // 인자는 각각 중심, 사이즈        
        Gizmos.DrawWireCube(Vector3.zero, size);

        if(range > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.up, size);
        }
    }
}
