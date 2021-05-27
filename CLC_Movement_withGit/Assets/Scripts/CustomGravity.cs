using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomGravity
{
    // 월드 좌표에 따른 중력을 리턴
    public static Vector3 GetGravity(Vector3 position)
    {
        // return Physics.gravity;
        return position.normalized * Physics.gravity.y;
    }

    // 구체와 orbit 카메라에 따라서 upAxis를 리턴
    public static Vector3 GetUpAxis(Vector3 position)
    {
        // 여러가지 중력을 적용할 수 있도록 함
        Vector3 up = position.normalized;

        // return -Physics.gravity.normalized;
        // return position.normalized;
        return Physics.gravity.y < 0f ? up : -up;
    }

    // out 파라미터를 이용해 upAxis까지 리턴하는 GetGravity 오버로딩형
    public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis)
    {
        // 기본적인 중력인 Physics.gravity 대신에 position 기반으로 진행
        Vector3 up = position.normalized;

        // upAxis = -Physics.gravity.normalized;
        upAxis = Physics.gravity.y < 0f ? up : -up;
        return up * Physics.gravity.y;
    }
}
