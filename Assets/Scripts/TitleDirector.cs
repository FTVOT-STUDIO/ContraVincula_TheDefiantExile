using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class TitleDirector : MonoBehaviour
{
    public Volume volume;
    public float impactTime = 2.8f;

    public Text typingText;
    public float typingDuration = 1.8f;
    public string cursor = "_";
    public float cursorBlink = 0.35f;

    public RawImage noise;
    public Vector2 noiseTiling = new Vector2(4f, 2.25f);
    public float noiseBase = 0.06f;
    public float burstAlpha = 0.3f;
    public Vector2 burstInterval = new Vector2(0.8f, 2.5f);
    public Vector2 burstLength = new Vector2(0.08f, 0.2f);
    public float impactBurstAlpha = 0.65f;
    public float impactBurstLength = 0.35f;

    public RectTransform[] jitterTargets;
    public float jitterAmount = 5f;

    public float preStaticDuration = 0.15f;
    public float preStaticAlpha = 0.95f;
    public float preStaticJitter = 18f;
    public float hitStop = 0.07f;

    public Transform[] punchTargets;
    public float punchScale = 1.25f;
    public float punchDuration = 0.45f;

    public float bloomPeak = 4f;

    public float blackoutStart = 4f;
    public float blackoutDuration = 2f;

    public Graphic quote;
    public float quoteFadeDuration = 1f;
    public float quoteRise = 12f;

    public float pushScale = 1.05f;
    public float pushDuration = 2.5f;

    Bloom bloom;
    float bloomRest;
    FilmGrain grain;
    float grainRest;
    bool showCursor = true;
    string lastSuffix = "";
    float burstUntil;
    float nextBurst;
    float burstPeak;
    float currentJitter;
    Vector2[] jitterBase;
    Vector3[] punchBase;

    void Start()
    {
        if (volume != null && volume.profile.TryGet(out bloom)) bloomRest = bloom.intensity.value;
        if (volume != null && volume.profile.TryGet(out grain)) grainRest = grain.intensity.value;
        jitterBase = new Vector2[jitterTargets.Length];
        for (int i = 0; i < jitterTargets.Length; i++) if (jitterTargets[i] != null) jitterBase[i] = jitterTargets[i].anchoredPosition;
        punchBase = new Vector3[punchTargets.Length];
        for (int i = 0; i < punchTargets.Length; i++) if (punchTargets[i] != null) punchBase[i] = punchTargets[i].localScale;
        currentJitter = jitterAmount;
        nextBurst = Random.Range(burstInterval.x, burstInterval.y) * 0.5f;
        StartCoroutine(Run());
    }

    void Update()
    {
        float t = Time.timeSinceLevelLoad;

        if (t >= nextBurst)
        {
            burstUntil = t + Random.Range(burstLength.x, burstLength.y);
            burstPeak = burstAlpha;
            currentJitter = jitterAmount;
            nextBurst = burstUntil + Random.Range(burstInterval.x, burstInterval.y);
        }

        float life = 1f - Mathf.Clamp01((t - blackoutStart) / blackoutDuration);
        bool bursting = (t < burstUntil || Time.timeScale == 0f) && life >= 1f;

        if (grain != null) grain.intensity.value = grainRest * life;

        if (noise != null)
        {
            noise.uvRect = new Rect(Random.value, Random.value, noiseTiling.x, noiseTiling.y);
            Color c = noise.color;
            c.a = (bursting ? burstPeak * Random.Range(0.6f, 1f) : noiseBase) * life;
            noise.color = c;
        }

        for (int i = 0; i < jitterTargets.Length; i++)
        {
            if (jitterTargets[i] == null) continue;
            Vector2 offset = bursting ? new Vector2(Random.Range(-currentJitter, currentJitter), 0f) : Vector2.zero;
            jitterTargets[i].anchoredPosition = jitterBase[i] + offset;
        }
    }

    void LateUpdate()
    {
        if (typingText == null) return;

        string s = typingText.text;
        if (lastSuffix.Length > 0 && s.EndsWith(lastSuffix)) s = s.Substring(0, s.Length - lastSuffix.Length);

        float t = Time.timeSinceLevelLoad;
        bool on = showCursor && (t < typingDuration || Mathf.Repeat(t - typingDuration, cursorBlink * 2f) < cursorBlink);

        Color c = typingText.color;
        if (!on) c.a = 0f;
        lastSuffix = "<color=#" + ColorUtility.ToHtmlStringRGBA(c) + ">" + cursor + "</color>";
        typingText.text = s + lastSuffix;
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(impactTime - preStaticDuration);

        showCursor = false;
        float t = Time.timeSinceLevelLoad;
        burstUntil = t + preStaticDuration;
        burstPeak = preStaticAlpha;
        currentJitter = preStaticJitter;

        yield return new WaitForSeconds(preStaticDuration);

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStop);
        Time.timeScale = 1f;

        t = Time.timeSinceLevelLoad;
        burstUntil = t + impactBurstLength;
        burstPeak = impactBurstAlpha;
        currentJitter = jitterAmount;
        nextBurst = burstUntil + Random.Range(burstInterval.x, burstInterval.y);

        for (int i = 0; i < punchTargets.Length; i++)
        {
            if (punchTargets[i] == null) continue;
            punchTargets[i].DOKill();
            punchTargets[i].localScale = punchBase[i] * punchScale;
            punchTargets[i].DOScale(punchBase[i], punchDuration).SetEase(Ease.OutExpo);
        }

        if (bloom != null)
        {
            bloom.intensity.value = bloomPeak;
            DOTween.To(() => bloom.intensity.value, x => bloom.intensity.value = x, bloomRest, 0.9f).SetEase(Ease.OutQuad);
        }

        if (quote != null)
        {
            var rt = quote.rectTransform;
            Vector2 end = rt.anchoredPosition;
            rt.anchoredPosition = end - new Vector2(0f, quoteRise);
            quote.color = new Color(quote.color.r, quote.color.g, quote.color.b, 0f);
            quote.DOFade(1f, quoteFadeDuration).SetEase(Ease.OutSine);
            rt.DOAnchorPos(end, quoteFadeDuration).SetEase(Ease.OutSine);
        }

        yield return new WaitForSeconds(punchDuration);

        for (int i = 0; i < punchTargets.Length; i++)
        {
            if (punchTargets[i] != null) punchTargets[i].DOScale(punchBase[i] * pushScale, pushDuration).SetEase(Ease.OutSine);
        }
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
