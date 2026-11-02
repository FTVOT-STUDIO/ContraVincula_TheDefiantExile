using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ButtonPressTween : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Scale")]
    [SerializeField] private float pressedScale = 0.9f;

    [Header("Duration")]
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.12f;

    [Header("Ease")]
    [SerializeField] private Ease pressEase = Ease.OutQuad;
    [SerializeField] private Ease releaseEase = Ease.OutBack;

    private Vector3 originScale;
    private Tween scaleTween;
    private Selectable button;
    private bool initialized;

    private void Awake()
    {
        if (target == null)
            target = transform;

        originScale = target.localScale;
        initialized = true;
        button = GetComponent<Selectable>();
        if (button != null)
        {
            //   button.transition = Selectable.Transition.None;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        Press();
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData) => Release();

    public void Press()
    {
        if (!initialized || !isActiveAndEnabled || (button != null && !button.IsInteractable())) return;
        scaleTween?.Kill();

        scaleTween = target.DOScale(originScale * pressedScale, pressDuration).SetEase(pressEase).SetUpdate(true);
    }

    public void Release()
    {
        if (!initialized || !isActiveAndEnabled) return;
        scaleTween?.Kill();

        scaleTween = target.DOScale(originScale, releaseDuration).SetEase(releaseEase).SetUpdate(true);
    }

    private void OnDisable()
    {
        scaleTween?.Kill();

        if (initialized && target != null)
            target.localScale = originScale;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!initialized || !isActiveAndEnabled || (button != null && !button.IsInteractable())) return;
        scaleTween?.Kill();

        scaleTween = DOTween.Sequence().Append(target.DOScale(originScale * pressedScale, pressDuration).SetEase(pressEase)).Append(target.DOScale(originScale, releaseDuration).SetEase(releaseEase)).SetUpdate(true);
    }
}
