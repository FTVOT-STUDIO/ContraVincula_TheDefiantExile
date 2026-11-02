using UnityEngine;

// 마우스 위치에 따라 오브젝트를 살짝 움직이는 패럴랙스 효과입니다
// 멀리 있는 레이어는 strength를 작게, 가까운 레이어는 크게 주면 깊이감이 생깁니다
public class MouseParallaxLayer : MonoBehaviour
{
    [Tooltip("마우스가 화면 끝에 있을 때 최대 이동 거리 (월드 단위)")]
    public Vector2 strength = new Vector2(0.3f, 0.2f);

    [Tooltip("켜면 마우스 반대 방향으로 움직입니다 (시점이 이동하는 느낌)")]
    public bool invert = true;

    [Tooltip("따라가는 부드러움 (클수록 느리게 따라옵니다)")]
    [Range(0.01f, 1f)] public float smoothTime = 0.15f;

    [Tooltip("마우스가 창 밖에 있거나 포커스를 잃으면 원위치로 돌아갑니다")]
    public bool resetWhenUnfocused = true;

    Vector3 basePos; // 시작 위치
    Vector2 offset; // 현재 적용 중인 오프셋
    Vector2 velocity; // SmoothDamp용 속도값

    void Awake()
    {
        basePos = transform.localPosition;
    }

    void OnDisable()
    {
        // 비활성화될 때 원위치로 되돌립니다
        transform.localPosition = basePos;
        offset = Vector2.zero;
        velocity = Vector2.zero;
    }

    void LateUpdate()
    {
        Vector2 target = GetMouseNormalized(); // -1 ~ 1
        if (invert) target = -target;
        target = Vector2.Scale(target, strength);

        // 일시정지(timeScale 0) 중에도 동작하도록 unscaledDeltaTime을 사용합니다
        offset = Vector2.SmoothDamp(offset, target, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        transform.localPosition = basePos + new Vector3(offset.x, offset.y, 0f);
    }

    // 화면 중앙 기준 마우스 위치를 -1 ~ 1 범위로 반환합니다
    Vector2 GetMouseNormalized()
    {
        if (resetWhenUnfocused && !Application.isFocused) return Vector2.zero;

        Vector2 mouse;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var m = UnityEngine.InputSystem.Mouse.current;
        if (m == null) return Vector2.zero;
        mouse = m.position.ReadValue();
#else
        mouse = Input.mousePosition;
#endif
        if (Screen.width <= 0 || Screen.height <= 0) return Vector2.zero;

        // 창 밖으로 나간 경우 처리
        if (resetWhenUnfocused && (mouse.x < 0f || mouse.y < 0f || mouse.x > Screen.width || mouse.y > Screen.height)) return Vector2.zero;

        float x = mouse.x / Screen.width * 2f - 1f;
        float y = mouse.y / Screen.height * 2f - 1f;
        return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
    }
}
