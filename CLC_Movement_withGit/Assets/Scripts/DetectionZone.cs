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

    private void Awake()
    {
        enabled = false;
    }

    private void FixedUpdate()
    {
        // 배열 내에서 넣어둔 물체들이 여전히 유효한지 트래킹
        // (Destroy와 같은 소멸 상황을 On Trigger Exit이 포착하지 못하기 때문)
        for(int i=0; i < colliders.Count; i++)
        {
            Collider collider = colliders[i];

            // 콜라이더가 사라졌거나 or 콜라이더를 지닌 게임 오브젝트가 사라졌거나
            if(!collider || !collider.gameObject.activeInHierarchy)
            {
                colliders.RemoveAt(i--);

                // on Trigger Exit이 감지하지 못해서 처리하지 않은 일을 대신 해준다
                if (colliders.Count == 0)
                {
                    onLastExit.Invoke();
                    enabled = false;
                }
                    
            }
        }
    }

    // 구역 자체가 파괴되었을 경우
    private void OnDisable()
    {

        // 유니티의 hot reload는 오브젝트의 OnDisable을 수반하기 때문에, 이를 막기 위한 조치
#if UNITY_EDITOR
        if(enabled && gameObject.activeInHierarchy)
        {
            return;
        }
#endif

        if(colliders.Count > 0)
        {
            // 배열 정리하고, onLastExit 이벤트 호출한 뒤에 파괴
            colliders.Clear();
            onLastExit.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 아무것도 없는 경우 on First Enter를 발동
        if(colliders.Count == 0)
        {
            onFirstEnter.Invoke();
            enabled = true;
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
            enabled = false;
        }
    }
}
