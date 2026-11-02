using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class ButtonHoverScale : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [SerializeField] private bool selectOnEnable;
    [SerializeField] private Vector2 normalSize = new Vector2(550, 120);
    [SerializeField] private Vector2 selectedSize = new Vector2(620, 120);

    [SerializeField] private float duration = 0.15f;
    [SerializeField] private Ease ease = Ease.OutBack;
    private Tween sizeTween;

    private RectTransform rect;
    private Selectable selectable;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        selectable = GetComponent<Selectable>();
    }

    private void OnEnable()
    {
        SetSizeInstant(normalSize);
        if (selectOnEnable) StartCoroutine(SelectNextFrame());
    }

    private void OnDisable()
    {
        sizeTween?.Kill();
    }

    private IEnumerator SelectNextFrame()
    {
        yield return null;
        if (EventSystem.current == null) yield break;

        EventSystem.current.SetSelectedGameObject(null);
        ButtonSfx.mute = true;
        EventSystem.current.SetSelectedGameObject(gameObject);
        ButtonSfx.mute = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable.interactable) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetSize(selectedSize);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!isActiveAndEnabled)
        {
            SetSizeInstant(normalSize);
            return;
        }

        StartCoroutine(CheckDeselectNextFrame());
    }

    private IEnumerator CheckDeselectNextFrame()
    {
        yield return null;

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
        else
        {
            SetSize(normalSize);
        }
    }

    private void SetSize(Vector2 size)
    {
        sizeTween?.Kill();

        sizeTween = rect.DOSizeDelta(size, duration).SetEase(ease).SetUpdate(true).SetLink(gameObject);
    }

    private void SetSizeInstant(Vector2 size)
    {
        sizeTween?.Kill();
        rect.sizeDelta = size;
    }
}