using UnityEngine;

// 동공(Iris)이 마우스 커서를 따라가고, 눈 파츠도 살짝 함께 움직이게 합니다.
// 표정 오브젝트(Face Main_1, Face Main_Boring, Face Main_Blink)마다 하나씩 붙입니다.
public class EyeCursorFollow : MonoBehaviour
{
    [Header("# 대상 ====")]
    [Tooltip("커서를 따라갈 동공입니다. 비워두면 눈 파츠만 움직입니다 (깜빡임 표정용)")]
    [SerializeField] private Transform iris;

    [Tooltip("동공보다 적게 움직일 눈 파츠입니다 (흰자, 눈 외곽선 등)")]
    [SerializeField] private Transform[] eyeParts = new Transform[0];

    [Header("# 기준점 ====")]
    [Tooltip("이 오브젝트 기준 두 눈의 중심 위치입니다 (로컬 좌표)")]
    [SerializeField] private Vector2 eyeCenter = new Vector2(-3.33f, 1.64f);

    [Tooltip("커서가 눈 중심에서 이 거리 이상 떨어지면 최대로 움직입니다 (월드 단위)")]
    [SerializeField] private float reachDistance = 4f;

    [Header("# 동공 이동 범위 (월드 단위) ====")]
    [Tooltip("왼쪽/아래쪽 최대 이동량입니다 (음수)")]
    [SerializeField] private Vector2 irisMin = new Vector2(-0.07f, -0.04f);

    [Tooltip("오른쪽/위쪽 최대 이동량입니다 (양수). 오른쪽 눈은 외곽선까지 여유가 약 10px입니다")]
    [SerializeField] private Vector2 irisMax = new Vector2(0.07f, 0.04f);

    [Header("# 눈 파츠 이동 범위 (월드 단위) ====")]
    [SerializeField] private Vector2 eyeMin = new Vector2(-0.03f, -0.02f);
    [SerializeField] private Vector2 eyeMax = new Vector2(0.03f, 0.02f);

    [Header("# 부드러움 ====")]
    [Tooltip("따라가는 부드러움입니다 (클수록 느리게 따라옵니다)")]
    [SerializeField, Range(0.01f, 1f)] private float smoothTime = 0.08f;

    [Header("# 깜빡임 찌그러짐 ====")]
    [Tooltip("세로로 눌릴 때 가로로 늘어나는 비율입니다 (0이면 가로 변화 없음)")]
    [SerializeField, Range(0f, 1f)] private float squashWiden = 0.05f;

    // 깜빡임 연출용 세로 배율입니다 (1 = 기본). FaceExpressionController가 조절합니다.
    public float LidSquash { get; set; } = 1f;

    private Vector3 irisBase; // 동공 기본 위치
    private Vector3[] eyeBase; // 눈 파츠 기본 위치
    private Vector3[] eyeScaleBase; // 눈 파츠 기본 크기
    private Vector2 look; // 현재 시선 (-1 ~ 1 원 범위)
    private Vector2 velocity; // SmoothDamp용 속도값
    private Camera cam;
    private bool initialized;

    private void Awake()
    {
        cam = Camera.main;
        if (iris != null) irisBase = iris.localPosition;

        eyeBase = new Vector3[eyeParts.Length];
        eyeScaleBase = new Vector3[eyeParts.Length];
        for (int i = 0; i < eyeParts.Length; i++)
        {
            if (eyeParts[i] == null) continue;
            eyeBase[i] = eyeParts[i].localPosition;
            eyeScaleBase[i] = eyeParts[i].localScale;
        }
        initialized = true;
        SnapToCursor();
    }

    // 표정이 바뀌어 켜질 때 현재 시선으로 바로 맞춰서 동공이 튀지 않게 합니다.
    private void OnEnable()
    {
        if (initialized) SnapToCursor();
    }

    private void LateUpdate()
    {
        // 일시정지(timeScale 0) 중에도 동작하도록 unscaledDeltaTime을 사용합니다.
        look = Vector2.SmoothDamp(look, GetTargetLook(), ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        Apply();
    }

    private void SnapToCursor()
    {
        look = GetTargetLook();
        velocity = Vector2.zero;
        Apply();
    }

    // 현재 시선 값을 동공과 눈 파츠 위치에 반영합니다.
    private void Apply()
    {
        if (iris != null) iris.localPosition = irisBase + Remap(look, irisMin, irisMax);

        Vector3 eyeOffset = Remap(look, eyeMin, eyeMax);

        // 깜빡임 찌그러짐: 눈 중심을 기준으로 세로는 줄이고 가로는 살짝 늘립니다.
        float sy = LidSquash;
        float sx = 1f + (1f - sy) * squashWiden;

        for (int i = 0; i < eyeParts.Length; i++)
        {
            if (eyeParts[i] == null) continue;

            // 파츠의 피벗이 눈 중심이 아니므로, 눈 중심 기준으로 위치를 보정합니다.
            Vector3 pos = eyeBase[i] + eyeOffset;
            pos.x = eyeCenter.x + (pos.x - eyeCenter.x) * sx;
            pos.y = eyeCenter.y + (pos.y - eyeCenter.y) * sy;

            eyeParts[i].localPosition = pos;
            eyeParts[i].localScale = new Vector3(eyeScaleBase[i].x * sx, eyeScaleBase[i].y * sy, eyeScaleBase[i].z);
        }
    }

    // -1 ~ 1 시선 값을 방향별 최대 이동량으로 변환합니다. (음수면 min, 양수면 max 사용)
    private static Vector3 Remap(Vector2 v, Vector2 min, Vector2 max)
    {
        float x = v.x < 0f ? -v.x * min.x : v.x * max.x;
        float y = v.y < 0f ? -v.y * min.y : v.y * max.y;
        return new Vector3(x, y, 0f);
    }

    // 눈 중심에서 커서까지의 방향을 -1 ~ 1 원 범위로 반환합니다.
    private Vector2 GetTargetLook()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || !TryGetMouse(out Vector2 mouse)) return Vector2.zero;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0f));
        Vector3 center = transform.TransformPoint(eyeCenter);
        Vector2 dir = new Vector2(world.x - center.x, world.y - center.y) / Mathf.Max(0.01f, reachDistance);

        // 범위 밖이면 경계로 자릅니다.
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        return dir;
    }

    // 마우스 화면 좌표를 가져옵니다. 창 밖이거나 포커스가 없으면 false를 반환합니다 (정면을 봅니다).
    private static bool TryGetMouse(out Vector2 mouse)
    {
        mouse = Vector2.zero;
        if (!Application.isFocused) return false;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var m = UnityEngine.InputSystem.Mouse.current;
        if (m == null) return false;
        mouse = m.position.ReadValue();
#else
        mouse = Input.mousePosition;
#endif

        return mouse.x >= 0f && mouse.y >= 0f && mouse.x <= Screen.width && mouse.y <= Screen.height;
    }

    // 씬 뷰에서 눈 중심 위치와 반응 거리를 확인할 수 있게 표시합니다.
    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.TransformPoint(eyeCenter);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, 0.1f);
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(center, reachDistance);
    }
}
