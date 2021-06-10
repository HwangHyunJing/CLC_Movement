using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionInterpolator : MonoBehaviour
{
    [SerializeField]
    Rigidbody body = default;

    [SerializeField]
    Vector3 from = default, to = default;

    // 로컬 포지션으로 설정하기 위한 값
    [SerializeField]
    Transform relativeTo = default;


    // from에서 to까지 t값에 따라서 보간해주는 메소드
    public void Interpolate (float t)
    {
        Vector3 p;

        // 로컬 포지션에 대해 설정했을 경우
        if(relativeTo)
        {
            p = Vector3.LerpUnclamped(
                relativeTo.TransformPoint(from), relativeTo.TransformPoint(to), t
                );
        }
        // 월드 포지션에 대해 설정했을 경우
        else
        {
            p = Vector3.LerpUnclamped(from, to, t);
        }

        body.MovePosition(p);
    }
}
