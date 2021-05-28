using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomGravity
{
    // 복수개의 중력을 다루기 위한 List
    static List<GravitySource> sources = new List<GravitySource>();

    // 월드 좌표에 따른 중력을 리턴
    public static Vector3 GetGravity(Vector3 position)
    {
        // List의 중력원들을 하나씩 더함
        Vector3 g = Vector3.zero;
        for(int i=0; i < sources.Count; i++)
        {
            
            g += sources[i].GetGravity(position);
        }

        // return position.normalized * Physics.gravity.y;
        // 리턴값은 방향 성분만이 아니라 크기도 있으므로, 정규화 안하는게 맞다
        return g;
    }

    // 구체와 orbit 카메라에 따라서 upAxis를 리턴
    public static Vector3 GetUpAxis(Vector3 position)
    {
        // position 대신 source 기반
        Vector3 g = Vector3.zero;
        for(int i=0; i < sources.Count; i++)
        {
            g += sources[i].GetGravity(position);
        }

        // up Axis 즉 방향을 리턴
        return -g.normalized;
    }

    // out 파라미터를 이용해 upAxis까지 리턴하는 GetGravity 오버로딩형
    public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis)
    {
        // position 기반 대신에 sources List 기반으로 진행
        Vector3 g = Vector3.zero;
        for(int i=0; i < sources.Count; i++)
        {
            g += sources[i].GetGravity(position);
        }

        upAxis = -g.normalized;
        return g;

        /*
        Vector3 up = position.normalized;

        // upAxis = -Physics.gravity.normalized;
        upAxis = Physics.gravity.y < 0f ? up : -up;
        return up * Physics.gravity.y;
        */
    }

    // 중력원을 추가
    public static void Register(GravitySource source)
    {
        // 이미 있는 중력원을 또 추가하려 하면 에러 메시지를 띄움
        // assert의 bool 조건은, '되기를 바라는, 예상하는' 조건입니다
        Debug.Assert(
            !sources.Contains(source),
            "Duplicated registration of gravity source!", source);

        sources.Add(source);
    }

    // 중력원을 제거
    public static void Unregister(GravitySource source)
    {
        Debug.Assert(
            sources.Contains(source),
            "Unregisteration of unkown  gravity source!", source);

        sources.Remove(source);
    }
}
