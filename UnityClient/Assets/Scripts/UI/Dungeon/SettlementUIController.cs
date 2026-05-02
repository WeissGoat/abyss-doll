using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SettlementUIController : MonoBehaviour {
    public Text titleText;
    public Text summaryText;
    public Text lootText;
    public Button continueBtn;

    public void Present(DungeonSettlementResult result, Action onContinue) {
        if (result == null) return;

        if (titleText != null) {
            titleText.text = result.IsVictory ? "撤离结算" : "战败结算";
        }

        if (summaryText != null) {
            if (result.IsVictory) {
                summaryText.text =
                    $"本次已回收 {result.LootTransferredCount} 件物资\n" +
                    $"估值合计: {result.LootEstimatedValue}G\n" +
                    $"当前仓库库存: {result.StashCountAfterSettlement} 件";
            } else {
                summaryText.text =
                    "本次深入失败，背包内物资已丢失\n" +
                    $"当前仓库库存: {result.StashCountAfterSettlement} 件";
            }
        }

        if (lootText != null) {
            if (result.LootNames != null && result.LootNames.Count > 0) {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("回收清单:");
                for (int i = 0; i < result.LootNames.Count; i++) {
                    builder.Append("- ");
                    builder.AppendLine(result.LootNames[i]);
                }
                lootText.text = builder.ToString().TrimEnd();
            } else {
                lootText.text = result.IsVictory ? "本次未带出任何物资" : "未保住任何局内物资";
            }
        }

        if (continueBtn != null) {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() => onContinue?.Invoke());
        }
    }
}

public class CombatLootUIController : MonoBehaviour {
    public Text titleText;
    public Text summaryText;
    public Transform lootParent;
    public Button continueBtn;

    private static readonly Vector2[] BaseSpawnOffsets = {
        new Vector2(-540f, 180f),
        new Vector2(-640f, 20f),
        new Vector2(-540f, -140f),
        new Vector2(540f, 180f),
        new Vector2(640f, 20f),
        new Vector2(540f, -140f),
        new Vector2(-180f, 340f),
        new Vector2(180f, 340f),
        new Vector2(-180f, -320f),
        new Vector2(180f, -320f)
    };

    public void Present(CombatLootPickupResult result, GameObject itemPrefab, Action onContinue) {
        if (result == null) {
            return;
        }

        ClearUnclaimedLootVisuals();
        PrepareOverlayForPickup();

        if (titleText != null) {
            titleText.text = "战利品拾取";
            titleText.raycastTarget = false;
        }

        if (summaryText != null) {
            summaryText.text =
                $"本场共掉落 {result.OfferedItems.Count} 件物资\n" +
                $"估值合计: {result.TotalEstimatedValue}G\n" +
                "将战利品拖入背包后点击继续，未拿取的物品将被丢弃";
            summaryText.raycastTarget = false;
        }

        if (lootParent != null && itemPrefab != null) {
            for (int i = 0; i < result.OfferedItems.Count; i++) {
                var item = result.OfferedItems[i];
                if (item == null) {
                    continue;
                }

                GameObject itemGo = Instantiate(itemPrefab, lootParent);
                DraggableItemUI itemUI = itemGo.GetComponent<DraggableItemUI>();
                if (itemUI != null) {
                    itemUI.SetupData(item);
                }

                PositionLootItem(itemGo, i, result.OfferedItems.Count);
            }
        }

        if (continueBtn != null) {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() => {
                ClearUnclaimedLootVisuals();
                onContinue?.Invoke();
            });
        }
    }

    private void PrepareOverlayForPickup() {
        Image panelImage = GetComponent<Image>();
        if (panelImage != null) {
            panelImage.raycastTarget = false;
        }

        if (lootParent == null) {
            return;
        }

        LayoutGroup layoutGroup = lootParent.GetComponent<LayoutGroup>();
        if (layoutGroup != null) {
            layoutGroup.enabled = false;
        }

        RectTransform lootRect = lootParent as RectTransform;
        if (lootRect != null) {
            lootRect.anchorMin = Vector2.zero;
            lootRect.anchorMax = Vector2.one;
            lootRect.pivot = new Vector2(0.5f, 0.5f);
            lootRect.anchoredPosition = Vector2.zero;
            lootRect.sizeDelta = Vector2.zero;
        }
    }

    private void PositionLootItem(GameObject itemGo, int index, int totalCount) {
        if (itemGo == null) {
            return;
        }

        RectTransform itemRect = itemGo.GetComponent<RectTransform>();
        if (itemRect == null) {
            return;
        }

        Vector2 spawnOffset = GetSpawnOffset(index, totalCount);
        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = spawnOffset;
        itemRect.localRotation = Quaternion.identity;
        itemRect.localScale = Vector3.one;
        itemGo.transform.SetAsLastSibling();
    }

    private Vector2 GetSpawnOffset(int index, int totalCount) {
        if (index < BaseSpawnOffsets.Length) {
            return BaseSpawnOffsets[index];
        }

        int overflowIndex = index - BaseSpawnOffsets.Length;
        int column = overflowIndex % 2;
        int row = overflowIndex / 2;
        float x = column == 0 ? -720f : 720f;
        float y = 220f - (row * 140f);
        return new Vector2(x, y);
    }

    private void ClearUnclaimedLootVisuals() {
        if (lootParent == null) {
            return;
        }

        for (int i = lootParent.childCount - 1; i >= 0; i--) {
            Destroy(lootParent.GetChild(i).gameObject);
        }
    }
}
