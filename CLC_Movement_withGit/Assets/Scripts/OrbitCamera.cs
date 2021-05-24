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

    // 카메라가 돌아가는 정도
    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;

    [SerializeField, Min(0f)]
    float alignDelay = 5f;

    // 카메라가 회전할 수 있는 수직 최대/최소 각도
    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    // 위/아래로 까딱거리는 각도 (0도가 수평, 90도가 수직으로 아래)
    // Z rotation 성분은 필요 없으므로 그냥 Vector2 사용했다
    Vector2 orbitAngles = new Vector2(45f, 0f);

    Vector3 focusPoint;

    private void OnValidate()
    {
        // 실수로 max값을 min보다 작게 입력한 경우
        if(maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    private void Awake()
    {
        // 받아온 focus 값을 활용하기 위해 다시 변수로 받음
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
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
        ManualRotation();

        // 계산을 위해 오일러 식으로 변경
        // Quaternion lookRotation = Quaternion.Euler(orbitAngles);

        // 사용자 지정 입력이 있는지 여부에 따라 입력값과 디폴트로 나뉨
        Quaternion lookRotation;

        if(ManualRotation())
        {
            // 각도의 입력값 자체를 제한범위 안에 들어가도록 하기 (Clamp)
            ConstrainAngles();
            lookRotation = Quaternion.Euler(orbitAngles);
        }
        else
        {
            // 카메라의 회전이 입력되지 않는 경우에 해당 (키보드 입력일 수도 있잖아 ?)
            lookRotation = transform.localRotation;
            // 이를 활용하기 위해서는 localRotaion을 우선적으로 활성화해야 한다.
        }

        Vector3 lookDirection = transform.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;
        // transform.localPosition = focusPoint - lookDirection * distance;
        transform.SetPositionAndRotation(lookPosition, lookRotation);
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
                // 이렇게 역 등비수열로 하면, 서서히 줄어드는 속력을 만들 수 있다
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

    bool ManualRotation()
    {
        Vector2 input = new Vector2(
            Input.GetAxis("Vertical Camera"),
            Input.GetAxis("Horizontal Camera")
        );

        const float e = 0.001f;
        if (input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            return true;
        }

        return false;
    }

    // 설정한 값에 따라 각도를 제한
    void ConstrainAngles()
    {
        // orbitAngles: 최종적으로 정해지는 각도
        // Clamp가 min과 max 인자 사이에 값이 오도록 하는 메소드
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        // 각도의 결과가 0 ~ 360안에 들어오도록 강제
        if(orbitAngles.y < 0f)
        {
            orbitAngles.y += 360f;
        }
        else if(orbitAngles.y >= 360f)
        {
            orbitAngles.y -= 360f;
        }
    }
}
