using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour
{
    // 가상 메소드
    public virtual Vector3 GetGravity (Vector3 position)
    {
        return Physics.gravity;
    }

    // 스스로를 추가 및 해제하는 메소드
    // onEnable, onDisable은 물체의 active/inactive에 따라 자동으로 호출
    private void OnEnable()
    {
        CustomGravity.Register(this);
    }

    private void OnDisable()
    {
        CustomGravity.Unregister(this);
    }
}
