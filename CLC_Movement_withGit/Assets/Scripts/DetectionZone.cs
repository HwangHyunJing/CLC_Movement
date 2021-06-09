using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DetectionZone : MonoBehaviour
{
    // onEnter, onExit은 그냥 이름 붙인겁니다. 라이브러리 아니에요
    [SerializeField]
    UnityEvent onFirstEnter = default, onLastExit = default;

    // 구역 안에 있는 요소들을 감지, 관리하기 위한 리스트
    List<Collider> colliders = new List<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        // 아무것도 없는 경우 on First Enter를 발동
        if(colliders.Count == 0)
        {
            onFirstEnter.Invoke();
        }
        // 구역 안에 있는 것들의 목록에 추가
        colliders.Add(other);

        // 해당 구역에 들어왔을 때의 Event를 발생
        onFirstEnter.Invoke();
    }

    // Stay는 딱히 의미가 없어서 안 쓴건가?
    private void OnTriggerExit(Collider other)
    {
        // 일단 나간 물체를 목록에서 제거, 그 다음에 판단
        // 나갈 물체가 없는 경우에는 false를 리턴
        if(colliders.Remove(other) && colliders.Count == 0)
        {
            // 해당 구역에서 나갔을 때의 Event를 발생
            onLastExit.Invoke();
        }
    }
}
