using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 格子右键转发。
///
/// 为什么单独做一个组件：
///   UGUI 的 Button.onClick 只处理左键；右键需要实现 IPointerClickHandler。
///   组件由 BagItemPool 在运行时加到每个池化格子上，不改 BagItemCell 预制体，
///   也不影响现有左键 Button 的逻辑。
/// </summary>
public class ItemCellRightClick : MonoBehaviour, IPointerClickHandler
{
    /// <summary>由 Lua 的 ItemCellCtrl 绑定；点击右键时触发</summary>
    public Action onRightClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (onRightClick != null) onRightClick();
    }
}
