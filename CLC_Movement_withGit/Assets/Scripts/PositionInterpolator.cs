using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionInterpolator : MonoBehaviour
{
    [SerializeField]
    Rigidbody body = default;

    [SerializeField]
    Vector3 from = default, to = default;

    // from에서 to까지 t값에 따라서 보간해주는 메소드
    public void Interpolate (float t)
    {
        body.MovePosition(Vector3.LerpUnclamped(from, to, t));
    }
}
