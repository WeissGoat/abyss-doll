using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArtAcceptanceRunner 的验收专用 payload 构造工厂。
/// 包含解锁层级、怪物 ID 解析、构造战利品/结算预览数据等验收辅助逻辑。
/// 这些方法将在后续阶段被存档快照注入（SaveState Snapshot）方案逐步替换。
/// </summary>
public partial class ArtAcceptanceRunner {

    private static readonly string[] FormalV1RuntimePanelRootNames = {
        "MaintenancePanel_Runtime",
        "DailyBillReportPanel_Runtime",
        "ShopStagingPanel_Runtime",
        "OrderBoardPanel_Runtime",
        "RumorBoardPanel_Runtime",
        "BusinessSettlementPanel_Runtime",
        "ChassisUpgradePanel_Runtime",
        "DollInteractionPanel_Runtime",
        "DollRoomPanel_Runtime",
        "FactionShopPanel_Runtime",
        "ScenarioEventPanel_Runtime"
    };

    private IEnumerator PrepareForCaptureStep(string stepName) {
        CloseWorkshopAcceptanceOverlays();
        HideAllWorkshopFormalV1Panels();
        DestroyLooseFormalV1PanelRoots();
        HideTransientRuntimePanels();
        ItemUseService.ClearPendingTargetSelection();
        VisualQueue.Clear();

        yield return WaitFrames(2);
        Canvas.ForceUpdateCanvases();
        Debug.Log($"[ArtAcceptance] Capture pre-clean completed for {stepName}.");
    }

    private int ResolveAcceptanceLayerID() {
        if (GameRoot.Core?.CurrentPlayer == null || ConfigManager.Dungeons == null) {
            return 0;
        }

        int highestUnlocked = Mathf.Max(1, GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer);
        int selected = 0;
        foreach (var kvp in ConfigManager.Dungeons) {
            if (kvp.Key <= highestUnlocked && (selected == 0 || kvp.Key < selected)) {
                selected = kvp.Key;
            }
        }

        return selected;
    }

    private void CloseWorkshopAcceptanceOverlays(WorkshopUIController workshopController = null) {
        WorkshopUIController controller = workshopController != null
            ? workshopController
            : FindObjectOfType<WorkshopUIController>();
        if (controller == null) {
            return;
        }

        controller.CloseSellPanel();
        controller.CloseProstheticPanel();
        controller.CloseDungeonStartLayerPanel();
        controller.CloseFormalV1Panel();
    }

    private void HideAllWorkshopFormalV1Panels() {
        WorkshopFormalV1PanelController[] controllers = FindObjectsOfType<WorkshopFormalV1PanelController>();
        foreach (WorkshopFormalV1PanelController controller in controllers) {
            if (controller != null) {
                controller.Hide();
            }
        }
    }

    private void DestroyLooseFormalV1PanelRoots() {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases) {
            if (canvas == null) {
                continue;
            }

            for (int i = canvas.transform.childCount - 1; i >= 0; i--) {
                Transform child = canvas.transform.GetChild(i);
                if (child == null || !IsFormalV1RuntimePanelRoot(child.name)) {
                    continue;
                }

                DestroyAcceptanceRuntimeObject(child.gameObject);
            }
        }
    }

    private bool IsFormalV1RuntimePanelRoot(string objectName) {
        if (string.IsNullOrEmpty(objectName)) {
            return false;
        }

        for (int i = 0; i < FormalV1RuntimePanelRootNames.Length; i++) {
            if (objectName == FormalV1RuntimePanelRootNames[i]) {
                return true;
            }
        }

        return false;
    }

    private void HideTransientRuntimePanels() {
        GameFlowController flow = GameFlowController.Instance;
        if (flow == null) {
            return;
        }

        SetInactiveIfPresent(flow.combatLootPanel);
        SetInactiveIfPresent(flow.dungeonNodeResultPanel);
        SetInactiveIfPresent(flow.settlementPanel);
    }

    private void SetInactiveIfPresent(GameObject obj) {
        if (obj != null) {
            obj.SetActive(false);
        }
    }

    private void DestroyAcceptanceRuntimeObject(GameObject obj) {
        if (obj == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(obj);
        } else {
            DestroyImmediate(obj);
        }
    }

    private void UnlockConfiguredLayersForAcceptance() {
        if (GameRoot.Core?.CurrentPlayer == null || ConfigManager.Dungeons == null || ConfigManager.Dungeons.Count == 0) {
            return;
        }

        int highestConfiguredLayer = 1;
        foreach (int layerID in ConfigManager.Dungeons.Keys) {
            if (layerID > highestConfiguredLayer) {
                highestConfiguredLayer = layerID;
            }
        }

        GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer = Mathf.Max(
            GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer,
            highestConfiguredLayer);
    }

    private void EnsureAcceptanceDungeonLayer() {
        if (GameRoot.Core?.Dungeon == null || GameRoot.Core.Dungeon.CurrentLayer != null) {
            return;
        }

        int layerID = ResolveAcceptanceLayerID();
        if (layerID > 0) {
            GameRoot.Core.Dungeon.StartRunAtLayer(layerID);
        }
    }

    private List<string> ResolveAcceptanceMonsterIDs() {
        List<string> monsterIDs = new List<string>();
        DungeonLayer layer = GameRoot.Core?.Dungeon?.CurrentLayer;
        if (layer?.RootNode != null) {
            NodeBase node = layer.RootNode;
            while (node != null) {
                CombatNode combatNode = node as CombatNode;
                if (combatNode != null && combatNode.MonsterIDs != null && combatNode.MonsterIDs.Count > 0) {
                    monsterIDs.AddRange(combatNode.MonsterIDs);
                    return monsterIDs;
                }

                node = node.NextNodes != null && node.NextNodes.Count > 0 ? node.NextNodes[0] : null;
            }
        }

        if (ConfigManager.Monsters == null) {
            return monsterIDs;
        }

        foreach (var kvp in ConfigManager.Monsters) {
            if (kvp.Value != null && !string.IsNullOrEmpty(kvp.Key)) {
                monsterIDs.Add(kvp.Key);
                break;
            }
        }

        return monsterIDs;
    }

    private CombatNode FindBestAcceptanceCombatNode() {
        DungeonLayer layer = GameRoot.Core?.Dungeon?.CurrentLayer;
        if (layer?.NodeRows == null) {
            return null;
        }

        CombatNode fallback = null;
        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                CombatNode combatNode = node as CombatNode;
                if (combatNode == null || combatNode.MonsterIDs == null || combatNode.MonsterIDs.Count == 0) {
                    continue;
                }

                if (fallback == null) {
                    fallback = combatNode;
                }

                if (HasRenderableMonsterVisual(combatNode.MonsterIDs)) {
                    return combatNode;
                }
            }
        }

        return fallback;
    }

    private bool HasRenderableMonsterVisual(List<string> monsterIDs) {
        if (monsterIDs == null || monsterIDs.Count == 0 || ConfigManager.Monsters == null) {
            return false;
        }

        VisualAssetRegistry registry = Resources.Load<VisualAssetRegistry>("VisualAssetRegistry");
        if (registry == null) {
            return false;
        }

        registry.RebuildLookup();
        foreach (string monsterID in monsterIDs) {
            if (string.IsNullOrEmpty(monsterID) || !ConfigManager.Monsters.TryGetValue(monsterID, out MonsterEntity monster)) {
                continue;
            }

            string visualID = VisualAssetService.ResolveMonsterCombatVisualID(monster);
            if (!string.IsNullOrEmpty(visualID) &&
                registry.TryGetEntry(visualID, out VisualAssetEntry entry) &&
                entry != null &&
                entry.Sprite != null) {
                return true;
            }
        }

        return false;
    }

    private List<string> ResolveRenderableAcceptanceMonsterIDs() {
        List<string> monsterIDs = new List<string>();
        if (ConfigManager.Monsters == null || ConfigManager.Monsters.Count == 0) {
            return monsterIDs;
        }

        VisualAssetRegistry registry = Resources.Load<VisualAssetRegistry>("VisualAssetRegistry");
        if (registry != null) {
            registry.RebuildLookup();
        }

        foreach (var kvp in ConfigManager.Monsters) {
            MonsterEntity monster = kvp.Value;
            if (monster == null || string.IsNullOrEmpty(kvp.Key)) {
                continue;
            }

            string visualID = VisualAssetService.ResolveMonsterCombatVisualID(monster);
            if (registry != null &&
                !string.IsNullOrEmpty(visualID) &&
                registry.TryGetEntry(visualID, out VisualAssetEntry entry) &&
                entry != null &&
                entry.Sprite != null) {
                monsterIDs.Add(kvp.Key);
                return monsterIDs;
            }
        }

        return ResolveAcceptanceMonsterIDs();
    }

    // ──────────────────────────────────────────
    // 验收选品策略
    // ──────────────────────────────────────────

    /// <summary>
    /// 验收专用固定选品列表。按 Rarity × ItemType 覆盖视觉差异最大的组合。
    /// 优先级：Epic > Rare > Uncommon > Common，每个 Rarity 至少选一种不同 ItemType。
    /// 如果固定 ID 不存在，降级到按 Rarity 分桶自动选取。
    /// </summary>
    private static readonly string[] PreferredLootIDs = {
        "mat_core_tier1",           // Epic / QuestItem — 大体积 2x2，高价值
        "gear_chainsaw_sword",      // Rare / Weapon — 高价值武器
        "loot_toxic_filter",        // Rare / Loot — 高价值战利品
        "gear_iron_armor",          // Uncommon / Armor — 防具
        "con_repair_kit",           // Common / Consumable — 消耗品
        "loot_rusty_coil",          // Uncommon / Loot — 中价值战利品
    };

    /// <summary>
    /// 从配置中按选品策略构建战利品集合，最多 maxCount 件。
    /// 先尝试固定列表，再按 Rarity 分桶补齐。
    /// </summary>
    private List<ItemEntity> SelectCuratedItems(int maxCount) {
        List<ItemEntity> selected = new List<ItemEntity>();
        HashSet<string> usedIDs = new HashSet<string>();

        if (ConfigManager.Items == null || ConfigManager.Items.Count == 0) {
            return selected;
        }

        // 第一轮：从固定列表选取
        foreach (string configID in PreferredLootIDs) {
            if (selected.Count >= maxCount) {
                break;
            }

            if (ConfigManager.Items.ContainsKey(configID)) {
                ItemEntity item = ConfigManager.CreateItem(configID);
                if (item != null) {
                    selected.Add(item);
                    usedIDs.Add(configID);
                }
            }
        }

        // 第二轮：按 Rarity 分桶补齐（覆盖固定列表以外的稀有度）
        if (selected.Count < maxCount) {
            string[] rarityOrder = { "Epic", "Rare", "Uncommon", "Common" };
            foreach (string rarity in rarityOrder) {
                if (selected.Count >= maxCount) {
                    break;
                }

                foreach (var kvp in ConfigManager.Items) {
                    if (selected.Count >= maxCount) {
                        break;
                    }

                    if (usedIDs.Contains(kvp.Key)) {
                        continue;
                    }

                    if (kvp.Value.Rarity == rarity) {
                        ItemEntity item = ConfigManager.CreateItem(kvp.Key);
                        if (item != null) {
                            selected.Add(item);
                            usedIDs.Add(kvp.Key);
                        }
                    }
                }
            }
        }

        return selected;
    }

    private CombatLootPickupResult BuildAcceptanceLootResult() {
        CombatLootPickupResult result = new CombatLootPickupResult {
            NodeID = "art_acceptance_loot_preview"
        };

        List<ItemEntity> items = SelectCuratedItems(5);
        foreach (ItemEntity item in items) {
            result.OfferedItems.Add(item);
            result.TotalEstimatedValue += item.BaseValue;
        }

        // 添加怪物来源 ID，使 UI 显示更真实
        if (ConfigManager.Monsters != null) {
            int monsterCount = 0;
            foreach (var kvp in ConfigManager.Monsters) {
                if (monsterCount >= 2) {
                    break;
                }

                result.SourceMonsterIDs.Add(kvp.Key);
                monsterCount++;
            }
        }

        Debug.Log($"[ArtAcceptance] Loot payload: {result.OfferedItems.Count} items, " +
                  $"total value={result.TotalEstimatedValue}, " +
                  $"monsters={string.Join(",", result.SourceMonsterIDs)}");

        return result;
    }

    private DungeonSettlementResult BuildAcceptanceSettlementResult() {
        DungeonSettlementResult result = new DungeonSettlementResult {
            IsVictory = true,
            StashCountAfterSettlement = GameRoot.Core?.CurrentPlayer?.StashInventory?.Count ?? 0
        };

        List<ItemEntity> items = SelectCuratedItems(5);

        for (int i = 0; i < items.Count; i++) {
            ItemEntity item = items[i];

            // 全部计入 PickedUp
            result.PickedUpCount++;
            result.PickedUpEstimatedValue += item.BaseValue;
            result.PickedUpNames.Add(item.Name);

            if (i < 3) {
                // 前 3 件成功带出
                result.BroughtOutCount++;
                result.BroughtOutEstimatedValue += item.BaseValue;
                result.BroughtOutNames.Add(item.Name);
                result.LootTransferredCount++;
                result.LootEstimatedValue += item.BaseValue;
                result.LootNames.Add(item.Name);
            } else {
                // 后面的丢失（演示丢失区域的视觉）
                result.LostCount++;
                result.LostEstimatedValue += item.BaseValue;
                result.LostNames.Add(item.Name);
            }
        }

        Debug.Log($"[ArtAcceptance] Settlement payload: victory={result.IsVictory}, " +
                  $"pickedUp={result.PickedUpCount}, broughtOut={result.BroughtOutCount}, lost={result.LostCount}");

        return result;
    }
}
