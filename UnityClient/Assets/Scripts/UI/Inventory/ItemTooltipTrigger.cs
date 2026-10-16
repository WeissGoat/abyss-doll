using UnityEngine;
using UnityEngine.EventSystems;

// Hover card for item rows that are not draggable backpack items (pre-dive stash, shop offers).
public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public ItemEntity Item;

    public static ItemTooltipTrigger Attach(GameObject target, ItemEntity item) {
        if (target == null || item == null) {
            return null;
        }

        ItemTooltipTrigger trigger = target.GetComponent<ItemTooltipTrigger>();
        if (trigger == null) {
            trigger = target.AddComponent<ItemTooltipTrigger>();
        }

        trigger.Item = item;
        return trigger;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (eventData != null && eventData.dragging) {
            return;
        }

        ItemTooltip.Show(Item, (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData) {
        ItemTooltip.HideFor((RectTransform)transform);
    }

    private void OnDisable() {
        ItemTooltip.HideFor((RectTransform)transform);
    }
}
