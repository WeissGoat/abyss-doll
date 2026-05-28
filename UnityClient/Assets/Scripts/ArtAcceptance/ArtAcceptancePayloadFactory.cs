using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ArtAcceptanceRunner 的验收专用 payload 构造工厂。
/// 包含解锁层级、怪物 ID 解析、构造战利品/结算预览数据等验收辅助逻辑。
/// 这些方法将在后续阶段被存档快照注入（SaveState Snapshot）方案逐步替换。
/// </summary>
public partial class ArtAcceptanceRunner {

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

    private CombatLootPickupResult BuildAcceptanceLootResult() {
        CombatLootPickupResult result = new CombatLootPickupResult {
            NodeID = "art_acceptance_loot_preview"
        };

        if (ConfigManager.Items == null) {
            return result;
        }

        int count = 0;
        foreach (var kvp in ConfigManager.Items) {
            if (count >= 4) {
                break;
            }

            ItemEntity item = ConfigManager.CreateItem(kvp.Key);
            if (item == null) {
                continue;
            }

            result.OfferedItems.Add(item);
            result.TotalEstimatedValue += item.BaseValue;
            count++;
        }

        return result;
    }

    private DungeonSettlementResult BuildAcceptanceSettlementResult() {
        DungeonSettlementResult result = new DungeonSettlementResult {
            IsVictory = true,
            StashCountAfterSettlement = GameRoot.Core?.CurrentPlayer?.StashInventory?.Count ?? 0
        };

        if (ConfigManager.Items == null) {
            return result;
        }

        int count = 0;
        foreach (var kvp in ConfigManager.Items) {
            if (count >= 3) {
                break;
            }

            ItemEntity item = ConfigManager.CreateItem(kvp.Key);
            if (item == null) {
                continue;
            }

            result.PickedUpCount++;
            result.PickedUpEstimatedValue += item.BaseValue;
            result.PickedUpNames.Add(item.Name);

            if (count < 2) {
                result.BroughtOutCount++;
                result.BroughtOutEstimatedValue += item.BaseValue;
                result.BroughtOutNames.Add(item.Name);
                result.LootTransferredCount++;
                result.LootEstimatedValue += item.BaseValue;
                result.LootNames.Add(item.Name);
            } else {
                result.LostCount++;
                result.LostEstimatedValue += item.BaseValue;
                result.LostNames.Add(item.Name);
            }

            count++;
        }

        return result;
    }
}
