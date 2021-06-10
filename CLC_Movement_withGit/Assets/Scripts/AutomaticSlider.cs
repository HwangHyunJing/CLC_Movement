using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AutomaticSlider : MonoBehaviour
{
    // 
    [SerializeField, Min(0.01f)]
    float duration = 1f;

    // Unity는 제너릭 형을 직렬화하지 못하기 때문에, 별도의 클래스를 생성
    [System.Serializable]
    public class OnVAlueChangedEvent : UnityEvent<float> { }

    [SerializeField]
    OnVAlueChangedEvent onValueChanged = default;

    float value;

    private void FixedUpdate()
    {
        // 플랫폼의 위치에 대응되는 값을 변화
        value += Time.deltaTime / duration;
        if(value >= 1f)
        {
            value = 1f;
            enabled = false;
        }

        // 변화된 값으로 Event에 넘김
        onValueChanged.Invoke(value);
    }
}
