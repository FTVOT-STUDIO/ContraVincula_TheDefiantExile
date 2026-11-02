using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Global Volume의 Bloom Scatter 값을 부드럽게 왕복시킵니다
[RequireComponent(typeof(Volume))]
public class BloomScatterPulse : MonoBehaviour
{
    public enum Mode
    {
        Sine, // 일정한 주기로 숨쉬듯 왕복합니다
        Noise // 불규칙하게 일렁입니다
    }

    [SerializeField] Mode mode = Mode.Sine;
    [SerializeField, Range(0f, 1f)] float min = 0f;
    [SerializeField, Range(0f, 1f)] float max = 1f;
    [Tooltip("한 번 왕복하는 데 걸리는 시간 (초)")]
    [SerializeField, Min(0.1f)] float period = 4f;
    [Tooltip("일시정지(timeScale 0) 중에도 계속 움직입니다")]
    [SerializeField] bool useUnscaledTime = true;

    Bloom bloom;
    float seed;

    void Start()
    {
        var volume = GetComponent<Volume>();
        // profile에 접근하면 런타임 복사본이 만들어지므로 원본 에셋은 바뀌지 않습니다
        if (!volume.profile.TryGet(out bloom))
        {
            Debug.LogWarning("BloomScatterPulse: Volume Profile에 Bloom이 없습니다.", this);
            enabled = false;
            return;
        }
        bloom.scatter.overrideState = true;
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (bloom == null) return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float k;

        if (mode == Mode.Sine)
        {
            // 코사인 곡선: 0 → 1 → 0, 양 끝에서 자연스럽게 감속합니다
            k = 0.5f - 0.5f * Mathf.Cos(t / period * Mathf.PI * 2f);
        }
        else
        {
            // 펄린 노이즈는 대략 0.2~0.8 사이라서 0~1로 늘려줍니다
            float n = Mathf.PerlinNoise(seed, t / period);
            k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.8f, n));
        }

        bloom.scatter.value = Mathf.Lerp(min, max, k);
    }
}
