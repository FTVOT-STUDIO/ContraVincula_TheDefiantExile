using System.Collections;
using UnityEngine;

// 로비 캐릭터 표정을 관리합니다.
// 1) 일정 간격으로 눈을 깜빡입니다. (가끔 두 번 연속)
// 2) 입력이 없으면 지루한 표정으로 바뀌고, 입력이 생기면 기본 표정으로 돌아옵니다.
// 표정이 바뀔 때는 깜빡임을 거쳐서 자연스럽게 전환됩니다.
public class FaceExpressionController : MonoBehaviour
{
    [Header("# 표정 오브젝트 ====")]
    [SerializeField] private GameObject mainFace; // 기본 표정
    [SerializeField] private GameObject blinkFace; // 눈 감은 표정 (공통)
    [SerializeField] private GameObject boringFace; // 지루한 표정

    [Header("# 깜빡임 ====")]
    [Tooltip("다음 깜빡임까지의 최소/최대 대기 시간(초)입니다")]
    [SerializeField] private Vector2 blinkInterval = new Vector2(2.5f, 5.5f);

    [Tooltip("눈 감은 표정을 유지하는 시간(초)입니다")]
    [SerializeField] private float blinkHold = 0.06f;

    [Tooltip("두 번 연속으로 깜빡일 확률입니다")]
    [SerializeField, Range(0f, 1f)] private float doubleBlinkChance = 0.2f;

    [Tooltip("연속 깜빡임 사이 간격(초)입니다")]
    [SerializeField] private float doubleBlinkGap = 0.12f;

    [Header("# 깜빡임 연출 ====")]
    [Tooltip("눈을 감는 데 걸리는 시간(초)입니다. 세로로 눌리면서 감깁니다")]
    [SerializeField] private float closeTime = 0.05f;

    [Tooltip("눈 뜨는 데 걸리는 시간(초)입니다. 살짝 튕기면서 열립니다")]
    [SerializeField] private float openTime = 0.14f;

    [Tooltip("감기 직전/뜨기 시작할 때의 세로 배율입니다 (작을수록 많이 눌립니다)")]
    [SerializeField, Range(0.1f, 1f)] private float closedSquash = 0.8f;

    [Tooltip("뜰 때 튕기는 정도입니다 (0이면 튕김 없음)")]
    [SerializeField, Range(0f, 5f)] private float openBounce = 1.2f;

    [Header("# 지루한 표정 ====")]
    [Tooltip("입력이 없을 때 지루한 표정으로 바뀌기까지의 시간(초)입니다")]
    [SerializeField] private float boringDelay = 15f;

    private GameObject current; // 눈을 떴을 때 보여줄 표정
    private GameObject shown; // 지금 화면에 보이는 표정
    private bool isBlinking;
    private float idleTime;
    private Vector2 lastMouse;

    private void Awake()
    {
        current = mainFace;
        Show(current);
        lastMouse = ReadMouse();
    }

    private void OnEnable()
    {
        isBlinking = false;
        idleTime = 0f;
        ResetSquash();
        Show(current);
        StartCoroutine(BlinkLoop());
    }

    private void Update()
    {
        // 입력이 있으면 대기 시간을 초기화하고, 지루한 표정이면 기본 표정으로 돌아옵니다.
        if (HasInput())
        {
            idleTime = 0f;
            if (current == boringFace) SetExpression(mainFace);
            return;
        }

        idleTime += Time.unscaledDeltaTime;
        if (boringFace != null && current != boringFace && idleTime >= boringDelay) SetExpression(boringFace);
    }

    // 표정을 바꿉니다. 깜빡임 중이면 눈을 뜰 때 새 표정이 나오고, 아니면 깜빡임을 한 번 거칩니다.
    public void SetExpression(GameObject face)
    {
        if (face == null || face == current) return;
        current = face;
        if (!isBlinking) StartCoroutine(Blink());
    }

    // 랜덤 간격으로 깜빡임을 반복합니다.
    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(blinkInterval.x, blinkInterval.y));
            if (isBlinking) continue;

            yield return Blink();
            if (Random.value < doubleBlinkChance)
            {
                yield return new WaitForSecondsRealtime(doubleBlinkGap);
                yield return Blink();
            }
        }
    }

    // 눈을 감았다가 현재 표정으로 다시 뜹니다.
    // 1) 보이는 표정을 세로로 눌러 감기 → 2) 감은 표정 유지 → 3) 눌린 상태에서 튕기며 뜨기
    private IEnumerator Blink()
    {
        if (blinkFace == null) { Show(current); yield break; }

        isBlinking = true;

        GameObject closing = shown;
        yield return Squash(closing, 1f, closedSquash, closeTime, false);
        SetSquash(closing, 1f);

        Show(blinkFace);
        yield return new WaitForSecondsRealtime(blinkHold);

        // 깜빡이는 동안 표정이 바뀌었으면 새 표정으로 뜹니다.
        GameObject opening = current;
        SetSquash(opening, closedSquash);
        Show(opening);
        yield return Squash(opening, closedSquash, 1f, openTime, true);

        isBlinking = false;
    }

    // 표정의 세로 배율을 from → to로 바꿉니다. bounce면 끝에서 살짝 튕깁니다.
    private IEnumerator Squash(GameObject face, float from, float to, float duration, bool bounce)
    {
        EyeCursorFollow eyes = face != null ? face.GetComponent<EyeCursorFollow>() : null;
        if (eyes == null || duration <= 0f) yield break;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            k = bounce ? EaseOutBack(k, openBounce) : k * k; // 감을 때는 가속, 뜰 때는 튕김
            eyes.LidSquash = Mathf.LerpUnclamped(from, to, k);
            yield return null;
        }
        eyes.LidSquash = to;
    }

    private static void SetSquash(GameObject face, float value)
    {
        EyeCursorFollow eyes = face != null ? face.GetComponent<EyeCursorFollow>() : null;
        if (eyes != null) eyes.LidSquash = value;
    }

    // 중간에 끊겼을 때 눌린 상태로 남지 않도록 모든 표정의 배율을 되돌립니다.
    private void ResetSquash()
    {
        SetSquash(mainFace, 1f);
        SetSquash(blinkFace, 1f);
        SetSquash(boringFace, 1f);
    }

    // 끝에서 목표값을 살짝 넘었다가 돌아오는 이징입니다.
    private static float EaseOutBack(float x, float overshoot)
    {
        float c3 = overshoot + 1f;
        float a = x - 1f;
        return 1f + c3 * a * a * a + overshoot * a * a;
    }

    // 지정한 표정 하나만 켭니다.
    private void Show(GameObject face)
    {
        shown = face;
        if (mainFace != null) mainFace.SetActive(face == mainFace);
        if (blinkFace != null) blinkFace.SetActive(face == blinkFace);
        if (boringFace != null) boringFace.SetActive(face == boringFace);
    }

    // 마우스 이동, 키보드, 마우스 버튼 입력이 있었는지 확인합니다.
    private bool HasInput()
    {
        Vector2 mouse = ReadMouse();
        bool moved = (mouse - lastMouse).sqrMagnitude > 4f; // 2px 이상 움직였을 때만 인정합니다
        lastMouse = mouse;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var k = UnityEngine.InputSystem.Keyboard.current;
        var m = UnityEngine.InputSystem.Mouse.current;
        var g = UnityEngine.InputSystem.Gamepad.current;
        bool pressed = (k != null && k.anyKey.isPressed) || (m != null && (m.leftButton.isPressed || m.rightButton.isPressed)) || (g != null && (g.buttonSouth.isPressed || g.buttonEast.isPressed || g.startButton.isPressed || g.leftStick.ReadValue().sqrMagnitude > 0.04f || g.dpad.ReadValue().sqrMagnitude > 0.04f));
#else
        bool pressed = Input.anyKey;
#endif
        return moved || pressed;
    }

    private static Vector2 ReadMouse()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var m = UnityEngine.InputSystem.Mouse.current;
        return m != null ? m.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
