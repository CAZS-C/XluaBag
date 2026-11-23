using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 通用按钮点击反馈：按下时缩小，抬起/移出时恢复。
/// 不改变 Button 自身的 ColorTint，只是叠加一个很轻的缩放，
/// 让包里原有按钮和新增的背包/菜单按钮都“点得动”。
/// </summary>
public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private Selectable selectable;
    private bool pressed;

    private void Awake()
    {
        originalScale = transform.localScale;
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        pressed = true;
        transform.localScale = originalScale * 0.94f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Restore();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 按住后移出按钮，也要恢复，避免一直保持缩小状态
        if (pressed) Restore();
    }

    private void OnDisable()
    {
        Restore();
    }

    private void Restore()
    {
        if (!pressed) return;
        pressed = false;
        transform.localScale = originalScale;
    }
}
