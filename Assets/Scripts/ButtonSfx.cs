using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSfx : MonoBehaviour, ISelectHandler
{
    [SerializeField] private SfxType[] clickSfx;
    [SerializeField] private SfxType[] hoverSfx;

    private Button button;
    public static bool mute;

    void Start()
    {
        button = GetComponent<Button>();

        foreach (SfxType type in clickSfx)
        {
            button.onClick.AddListener(() => AudioManager.Instance.PlaySfx(type));
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (mute) return;

        foreach (SfxType type in hoverSfx)
        {
            AudioManager.Instance.PlaySfx(type);
        }
    }
}