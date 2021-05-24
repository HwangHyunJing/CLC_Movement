using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]

public class OrbitCamera : MonoBehaviour
{
    // Orbit 카메라가 바라볼 대상
    [SerializeField]
    Transform focus = default;

    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    // 카메라와 플레이어 사이 어느 정도에 위치할지를 정함
    [SerializeField, Range(0f, 1f)]
    float focusCentering = .5f;

    Vector3 focusPoint;

    private void Awake()
    {
        // 받아온 focus 값을 활용하기 위해 다시 변수로 받음
        focusPoint = focus.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Update에서의 일들이 처리된 후 작동
    private void LateUpdate()
    {
        // Vector3 focusPoint = focus.position;
        UpdateFocusPoint();
        Vector3 lookDirection = transform.forward;
        transform.localPosition = focusPoint - lookDirection * distance;
    }

    void UpdateFocusPoint()
    {
        Vector3 targetPoint = focus.position;

        if(focusRadius > 0f)
        {
            // Camera와 Player Sphere 사이의 거리
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;

            // 카메라와 플레이어 사이에 거리가 존재 && focus Centering 보정이 필요
            if(distance > 0.01f && focusCentering > 0f)
            {
                // 월드 타임을 건드는 메소드에 영향을 받지 않도록
                // deltaTime 대신 unscaledDeltaTime을 사용
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }
            
            if (distance > focusRadius)
            {
                // 보간; 즉 고정된 거리에서 볼 생각이 없는 듯 하다
                // Lerp 대신에 아예 수식을 사용
                // focusPoint = Vector3.Lerp(targetPoint, focusPoint, focusRadius / distance);
                t = Mathf.Min(t, focusRadius / distance);
            }
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
                
        }
        else
        {
            focusPoint = targetPoint;
        }
        
    }
}
