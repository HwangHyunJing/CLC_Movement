using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]

public class OrbitCamera : MonoBehaviour
{
    // Orbit 카메라가 바라볼 대상
    [SerializeField]
    Transform focus = default;

    // focus point에서 카메라까지의 거리
    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    // 물체가 최소 어느 정도 움직일 때 부터 카메라가 이를 따르는가?
    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    // 카메라가 센터링되는 시기의 정도 (1로 하면 카메라가 delay 없이 그대로 따라감)
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

    // box cast에 사용될 layer
    [SerializeField]
    LayerMask obstructionMask = -1;

    [SerializeField, Min(0f)]
    float upAlignmentSpeed = 360f;


    // 위/아래로 까딱거리는 각도 (0도가 수평, 90도가 수직으로 아래)
    // Z rotation 성분은 필요 없으므로 그냥 Vector2 사용했다
    Vector2 orbitAngles = new Vector2(45f, 0f);

    // 카메라가 실제로 바라보는 위치
    Vector3 focusPoint;
    // 자동 정렬에서 카메라가 바라볼 위치
    Vector3 previousFocusPoint;

    // 일정 시간동안 Rotation을 입력하지 않으면, 카메라가 자동으로 align 된다
    float lastManualRotationTime;

    // (뭔가의 이유로 인해) 자기 자신의 Camera 컴포넌트를 가져와야 한다
    // (이 스크립트를 지닌 오브젝트는 카메라 자체가 아닙니다)
    Camera regularCamera;

    // 바뀐 중력에 따른 카메라의 정렬
    // Quaternion.identity는 Vector3.zero랑 비슷한 역할인듯
    Quaternion gravityAlignment = Quaternion.identity;

    Quaternion orbitRotation;

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
        regularCamera = GetComponent<Camera>();
        // 받아온 focus 값을 활용하기 위해 다시 변수로 받음
        focusPoint = focus.position;
        transform.localRotation = orbitRotation = Quaternion.Euler(orbitAngles);
    }

    // Update에서의 일들이 처리된 후 작동
    private void LateUpdate()
    {
        // 기준이 되는 값은 가장 먼저 설정한다
        // gravityAlignment = Quaternion.FromToRotation(gravityAlignment * Vector3.up, CustomGravity.GetUpAxis(focusPoint)) * gravityAlignment;
        // 기존의 기능을 넘기면서 동시에 추가
        UpdateGravityAlignment();

        // Vector3 focusPoint = focus.position;
        UpdateFocusPoint();

        // 사용자 지정 입력이 있는지 여부에 따라 입력값과 디폴트로 나뉨
        // 기존에 lookRotation은 중력의 방향이 아래인 경우만 커버할 수 있으므로 패기

        // 카메라 회전 입력이 있는지 확인 || 없으면 자동 정렬 여부를 확인
        if(ManualRotation() || AutomaticRotation())
        {
            // 각도의 입력값 자체를 제한범위 안에 들어가도록 하기 (Clamp)
            ConstrainAngles();
            // 즉 입력에 따라 orbit angle가 정해지는 방식
            orbitRotation = Quaternion.Euler(orbitAngles);
        }

        Quaternion lookRotation = gravityAlignment * orbitRotation;

        // 기존에 정면을 바라보고있던 카메라를 look Rotation만큼 돌리는 과정
        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;

        Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
        Vector3 rectPosition = lookPosition + rectOffset;
        Vector3 castFrom = focus.position;
        Vector3 castLine = rectPosition - castFrom;
        float castDistance = castLine.magnitude;
        Vector3 castDirection = castLine / castDistance;

        // 인자를 잘 보면, focus Point에서 box형태로 cast하는 중
        // halfExtends: 상자 크기를 정의. 해당 값은 그 크기의 절반
        if (Physics.BoxCast(castFrom, CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            // near Clip Plane 더한건 최소한의 카메라 시야 확보를 위함
            lookPosition = focusPoint - lookDirection * (hit.distance + regularCamera.nearClipPlane);
        }

        // 카메라를 look Rotation만큼 돌리고, 그 만큼의 look Position에 위치
        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    // 카메라가 '실제로' 바라볼 Focus Point들을 정의하는 곳
    // focus.position(실제값) > targetPoint(업데이팅) > (focusRadius 조건에 따라) focus Point
    void UpdateFocusPoint()
    {
        // 이전 프레임의 focus Point를 저장
        previousFocusPoint = focusPoint;
        Vector3 targetPoint = focus.position;

        // focus Radius 만큼 이동했을 때부터 카메라가 그 움직임을 트레킹
        if(focusRadius > 0f)
        {
            // Camera와 Player Sphere 사이의 거리
            // target Point는 지속적으로 Update되고, focus Point는 그 결과에 따라 값이 할당
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;

            // 최소한의 이동이 존재 && 카메라가 focus Point에 붙어있지 않음
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
            // 그냥 카메라가 모든 움직임을 즉각적으로 캐치하겠다고 설정한 경우
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

    // 현 프레임에서 벡터의 움직임을 계산하는 메소드
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

        // 
        Vector3 alignedDelta = Quaternion.Inverse(gravityAlignment) * (focusPoint - previousFocusPoint);

        // 
        Vector2 movement = new Vector2(alignedDelta.x, alignedDelta.z);

        // movement 크기의 제곱
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

    // 장애물 인식을 위한 Box Cast의 상자 크기를 정의 (정확히는 그 상자의 절반 크기)
    Vector3 CameraHalfExtends
    {
        get
        {
            Vector3 halfExtends;
            // 높이는 그냥 near clip plane 기준으로 box casting하는 듯
            halfExtends.y = regularCamera.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            // Camera.aspect: 너비/높이 = 쉽게 말해서 tan
            // 별 거 없고, 그냥 카메라의 화면비에 맞춰서 y값이 줄어든 만큼 x값을 얻는 방식
            halfExtends.x = halfExtends.y * regularCamera.aspect;
            halfExtends.z = 0f;
            return halfExtends;
        }
    }

    // 기존에 gravity alignment를, 인게임에서 변하는 중력에 맞춰서 바로 변환하는 메소드
    void UpdateGravityAlignment()
    { 
        // 기존에 From To Rotation의 각 인자값
        Vector3 fromUp = gravityAlignment * Vector3.up;
        Vector3 toUp = CustomGravity.GetUpAxis(focusPoint);

        // 중력이 바뀔때, 카메라는 '서서히 돌아가도록' 하기 위한 값
        // (이거 없으면 그냥 순식간에 중력에 맞춰서 전환)

        // float dot = Vector3.Dot(fromUp, toUp);
        // Acos에는 -1 ~ 1의 값만 들어가야 하지만, 오차로 인해 이를 벗어나면 오류가 뜬다
        // 이를 막기 위해서 단순히 Dot의 결과를 할당하지 않고 Clamp까지 해주는 것
        float dot = Mathf.Clamp(Vector3.Dot(fromUp, toUp), -1f, 1f);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
        float maxAngle = upAlignmentSpeed * Time.deltaTime;

        // 카메라의 윗 방향을, (중력의 영향을 받은) 플레이어의 윗 방향으로 돌리는 쿼터니언을 리턴
        // 그래서 리턴값을 다시 gravity Alignment에 곱하는 것
        Quaternion newAlignment 
            = Quaternion.FromToRotation(fromUp, toUp) * gravityAlignment;
        
        // 현재 각도 차이가 alignment의 최대 각도를 넘어서지 않는다면 즉시 전환
        if(angle <= maxAngle)
        {
            gravityAlignment = newAlignment;
        }
        else
        {
            // 이를 넘어선다면 서서히 전환
            gravityAlignment 
                = Quaternion.SlerpUnclamped(gravityAlignment, newAlignment, maxAngle / angle);
            // Slerp Unclamped를 쓰는게 interpolate 옵션에서 오차를 줄이는 방법
        }
    }
}
