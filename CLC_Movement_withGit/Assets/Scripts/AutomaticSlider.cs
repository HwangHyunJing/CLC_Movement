using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AutomaticSlider : MonoBehaviour
{
    // 플랫폼이 몇 초에 걸쳐서 이동할 것인지 정의
    // 이동하는 from to 위치는 Position Interpolator에서 정의
    [SerializeField, Min(0.01f)]
    float duration = 1f;

    // Unity는 제너릭 형을 직렬화하지 못하기 때문에, 별도의 클래스를 생성
    [System.Serializable]
    public class OnVAlueChangedEvent : UnityEvent<float> { }

    [SerializeField]
    OnVAlueChangedEvent onValueChanged = default;

    // 플랫폼이 끝에 다다랐을 때 역 방향으로 움직일지 여부
    [SerializeField]
    bool autoReverse = false;
    // bool reversed;
    public bool Reversed { get; set; }
    public bool AutoReverse
    {
        get => autoReverse;
        set => autoReverse = value;
    }

    // 플랫폼의 시작과 끝 움직임에서 곡선을 줄지 여부
    [SerializeField]
    bool smoothstep = false;

    // 그냥 3차함수 그래프 식입니다
    float SmoothedValue => 3f * value * value - 2f * value * value * value;

    

    // 보간을 위한 값. 크기는 0~1로 제한된다
    float value;

    private void FixedUpdate()
    {
        float delta = Time.deltaTime / duration;

        // 역방향 이동
        if(Reversed)
        {
            value -= delta;
            if(value <= 0f)
            {
                if(autoReverse)
                {
                    // 단순히 값에 Clamp되는 대신에 역 방향 위치를 지원해서 부드러운 움직임을 만듦
                    value = Mathf.Min(1f, -value);
                    Reversed = false;
                }
                else
                {
                    value = 0f;
                    enabled = false;
                }
            }
        }

        // 역방향 이동이 아님
        else
        {
            // 값을 증가시켜주고, 최대에 다다른 경우 auto reverse 검사
            value += delta;
            if(value >= 1f)
            {
                if(autoReverse)
                {
                    value = Mathf.Max(0f, 2f -value);
                    Reversed = true;
                }
                else
                {
                    value = 1f;
                    enabled = false;
                }
            }
        }

        // 변화된 값으로 Event에 넘김
        onValueChanged.Invoke(smoothstep ? SmoothedValue : value);
    }
}
