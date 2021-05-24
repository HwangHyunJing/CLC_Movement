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

    // 자동 정렬 시 속도의 제한
    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;

    // 위/아래로 까딱거리는 각도 (0도가 수평, 90도가 수직으로 아래)
    // Z rotation 성분은 필요 없으므로 그냥 Vector2 사용했다
    Vector2 orbitAngles = new Vector2(45f, 0f);

    // 카메라가 실제로 바라보는 위치
    Vector3 focusPoint;
    // 자동 정렬에서 카메라가 바라볼 위치
    Vector3 previousFocusPoint;

    // 일정 시간동안 Rotation을 입력하지 않으면, 카메라가 자동으로 align 된다
    float lastManualRotationTime;

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

        // 카메라 회전 입력이 있는지 확인 > 없으면 자동 정렬 여부를 확인
        if(ManualRotation() || AutomaticRotation())
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

    // 카메라가 '실제로' 바라볼 Focus Point들을 정의하는 곳
    void UpdateFocusPoint()
    {
        // 이전 프레임의 focus Point를 저장
        previousFocusPoint = focusPoint;
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
            // Orbit Angles는 '변화량'이 아니라 '이동해야 하는 값'이 맞습니다
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;

            // 회전에 대한 입력이 수행되었을 경우, 그 순간의 시간을 받음
            lastManualRotationTime = Time.unscaledTime;
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

    // 시간을 비교해서 자동정렬을 할 것인지 여부를 리턴하는 메소드
    bool AutomaticRotation()
    {
        // 지금 시각 - 마지막으로 회전한 시각 < 기준 딜레이
        if(Time.unscaledTime - lastManualRotationTime < alignDelay)
        {
            // 아직 delay 만큼의 시간이 경과되지 않았으므로 자동 정렬 실행하지 X
            return false;
        }

        // if 충분한 딜레이 이상의 시간동안 매뉴얼 rotation이 없었다면
        // 움직임 자체가 있었는지 확인?

        // 차이 = 움직여야 하는 정도
        Vector2 movement = new Vector2(
            focusPoint.x - previousFocusPoint.x,
            focusPoint.z - previousFocusPoint.z
        );

        // 벡터의 정확한 크기 값 자체를 원하는 게 아니면, 그냥 제곱된 크기를 가져오는게 좋다
        float movementDeltaSqr = movement.sqrMagnitude;

        // 이전 프레임에서 조금의 차이라도 존재하지 않는다면
        if(movementDeltaSqr < 0.000001f)
        {
            // 굳이 자동으로 돌릴 이유가 없다
            return false;
        }

        // 카메라의 수평 성분에 대해서 자동 정렬을 실행합니다
        // Get Angle의 인자는, mvoement의 방향 벡터. movementDeltaSqr이 뭔지 잘 생각해보자
        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));

        // 지금 각도 대비 정렬하려는 각도와의 델타값에 대한 절대값(Abs)
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        // float rotationChange = rotationSpeed * Time.unscaledDeltaTime;
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);

        // 변화량이 일정 기준보다 크지 않다면
        if(deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if(180f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }

        // 이 만큼 돌아가라...
        orbitAngles.y
            = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    // 
    static float GetAngle (Vector2 direction)
    {
        // 아주 평범한 아코싸인
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;

        // 각도가 음수인 경우 무조건 양수 결과가 나오도록 함
        return direction.x < 0f ? 360f - angle : angle;
    }
}
