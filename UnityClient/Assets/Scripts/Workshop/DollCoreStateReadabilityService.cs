using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum DollCoreEmotionState {
    Unknown,
    Energetic,
    Calm,
    Tired,
    Depressed,
    Panic,
    Broken
}

public class DollCoreStateReadabilitySnapshot {
    public bool Success;
    public string Reason;
    public string Header;
    public string DollID;
    public string Name;
    public int HPCurrent;
    public int HPMax;
    public int SANCurrent;
    public int SANMax;
    public float HPPercent;
    public float SANPercent;
    public float WearAndTear;
    public float Corruption;
    public int BondLevel;
    public int HiddenTrust;
    public DollCoreEmotionState EmotionState;
    public string HPText;
    public string SANText;
    public string SanStateText;
    public string EmotionText;
    public string BondStageText;
    public string MaintenanceText;
    public string ChassisText;
    public string StatsText;
    public string CombinedText;
    public List<string> WarningLines = new List<string>();
    public List<string> TraitLines = new List<string>();
    public List<DollCoreProstheticLine> ProstheticLines = new List<DollCoreProstheticLine>();
}

public class DollCoreProstheticLine {
    public string ProstheticID;
    public string Name;
    public string SlotType;
    public string Level;
    public bool ConfigFound;
    public string StatusText;
    public string DetailText;
}

public static class DollCoreStateReadabilityService {
    public static DollCoreStateReadabilitySnapshot BuildSnapshot(PlayerProfile player) {
        if (player == null) {
            return BuildFailureSnapshot("Player profile is missing.");
        }

        if (player.ActiveDoll == null) {
            return BuildFailureSnapshot("Active doll is missing.");
        }

        return BuildSnapshot(player.ActiveDoll);
    }

    public static DollCoreStateReadabilitySnapshot BuildSnapshot(DollEntity doll) {
        if (doll == null) {
            return BuildFailureSnapshot("Doll entity is missing.");
        }

        DollCoreStateReadabilitySnapshot snapshot = new DollCoreStateReadabilitySnapshot {
            Success = true,
            Header = "人偶状态",
            DollID = doll.DollID,
            Name = string.IsNullOrEmpty(doll.Name) ? doll.DollID : doll.Name,
            HPCurrent = doll.Status != null ? doll.Status.HP_Current : 0,
            HPMax = doll.Status != null ? doll.Status.HP_Max : 0,
            SANCurrent = doll.Status != null ? doll.Status.SAN_Current : 0,
            SANMax = doll.Status != null ? doll.Status.SAN_Max : 0,
            WearAndTear = doll.Status != null ? doll.Status.WearAndTear : 0f,
            Corruption = doll.Status != null ? doll.Status.Corruption : 0f,
            BondLevel = doll.Bond != null ? Mathf.Max(0, doll.Bond.AffectionLevel) : 0,
            HiddenTrust = doll.Bond != null ? Mathf.Max(0, doll.Bond.HiddenTrust) : 0
        };

        snapshot.HPPercent = BuildRatio(snapshot.HPCurrent, snapshot.HPMax);
        snapshot.SANPercent = BuildRatio(snapshot.SANCurrent, snapshot.SANMax);
        snapshot.EmotionState = DeriveEmotion(snapshot.SANCurrent, snapshot.SANMax, snapshot.BondLevel);
        snapshot.HPText = $"HP {Mathf.Max(0, snapshot.HPCurrent)}/{Mathf.Max(0, snapshot.HPMax)} ({FormatPercent(snapshot.HPPercent)})";
        snapshot.SANText = $"SAN {Mathf.Max(0, snapshot.SANCurrent)}/{Mathf.Max(0, snapshot.SANMax)} ({FormatPercent(snapshot.SANPercent)})";
        snapshot.SanStateText = FormatSanState(snapshot.SANCurrent, snapshot.SANMax);
        snapshot.EmotionText = FormatEmotion(snapshot.EmotionState);
        snapshot.BondStageText = FormatBondStage(snapshot.BondLevel, snapshot.HiddenTrust);
        snapshot.MaintenanceText = BuildMaintenanceText(snapshot);
        snapshot.ChassisText = BuildChassisText(doll.Chassis);
        snapshot.StatsText = BuildStatsText(doll.Stats);

        AppendTraitLines(snapshot, doll);
        AppendProstheticLines(snapshot, doll);
        snapshot.CombinedText = BuildCombinedText(snapshot);
        return snapshot;
    }

    private static DollCoreStateReadabilitySnapshot BuildFailureSnapshot(string reason) {
        DollCoreStateReadabilitySnapshot snapshot = new DollCoreStateReadabilitySnapshot {
            Success = false,
            Reason = reason,
            Header = "人偶状态",
            HPText = "人偶数据不可用",
            SANText = reason,
            SanStateText = "未知",
            EmotionText = "未知",
            BondStageText = "未知",
            MaintenanceText = reason,
            ChassisText = "底盘未知",
            StatsText = "属性未知"
        };
        snapshot.CombinedText = BuildCombinedText(snapshot);
        return snapshot;
    }

    private static void AppendTraitLines(DollCoreStateReadabilitySnapshot snapshot, DollEntity doll) {
        if (snapshot == null || doll?.Traits == null || doll.Traits.Count == 0) {
            return;
        }

        foreach (string traitID in doll.Traits) {
            if (!string.IsNullOrWhiteSpace(traitID)) {
                snapshot.TraitLines.Add(traitID);
            }
        }
    }

    private static void AppendProstheticLines(DollCoreStateReadabilitySnapshot snapshot, DollEntity doll) {
        if (snapshot == null || doll?.EquippedProsthetics == null || doll.EquippedProsthetics.Count == 0) {
            return;
        }

        foreach (string prostheticID in doll.EquippedProsthetics) {
            if (string.IsNullOrWhiteSpace(prostheticID)) {
                snapshot.WarningLines.Add("义体引用为空。");
                snapshot.ProstheticLines.Add(new DollCoreProstheticLine {
                    ProstheticID = string.Empty,
                    Name = "空义体引用",
                    ConfigFound = false,
                    StatusText = "配置缺失",
                    DetailText = "已装备义体列表存在空 ID。"
                });
                continue;
            }

            ProstheticEntity prosthetic = null;
            bool found = ConfigManager.Prosthetics != null && ConfigManager.Prosthetics.TryGetValue(prostheticID, out prosthetic);
            if (!found || prosthetic == null) {
                snapshot.WarningLines.Add($"义体配置缺失: {prostheticID}");
                snapshot.ProstheticLines.Add(new DollCoreProstheticLine {
                    ProstheticID = prostheticID,
                    Name = prostheticID,
                    ConfigFound = false,
                    StatusText = "配置缺失",
                    DetailText = "该义体 ID 不存在于 Prosthetics 配置。"
                });
                continue;
            }

            snapshot.ProstheticLines.Add(new DollCoreProstheticLine {
                ProstheticID = prostheticID,
                Name = string.IsNullOrEmpty(prosthetic.Name) ? prostheticID : prosthetic.Name,
                SlotType = string.IsNullOrEmpty(prosthetic.SlotType) ? "未知槽位" : prosthetic.SlotType,
                Level = string.IsNullOrEmpty(prosthetic.Level) ? "未知等级" : prosthetic.Level,
                ConfigFound = true,
                StatusText = "已装备",
                DetailText = $"{prosthetic.SlotType} / {prosthetic.Level}"
            });
        }
    }

    private static string BuildMaintenanceText(DollCoreStateReadabilitySnapshot snapshot) {
        List<string> parts = new List<string>();

        if (snapshot.HPMax > 0 && snapshot.HPPercent <= 0.3f) {
            parts.Add("HP偏低");
        }

        if (snapshot.SANMax > 0 && snapshot.SANCurrent <= 0) {
            parts.Add("SAN崩溃");
        } else if (snapshot.SANMax > 0 && snapshot.SANPercent < 0.3f) {
            parts.Add("SAN偏低");
        }

        if (snapshot.WearAndTear >= DiveReadinessService.ExtremeWearThreshold) {
            parts.Add("磨损阻断");
        } else if (snapshot.WearAndTear >= DiveReadinessService.HeavyWearWarningThreshold) {
            parts.Add("高磨损");
        }

        if (snapshot.Corruption >= DiveReadinessService.ExtremeCorruptionThreshold) {
            parts.Add("侵蚀阻断");
        } else if (snapshot.Corruption >= DiveReadinessService.HighCorruptionWarningThreshold) {
            parts.Add("高侵蚀");
        }

        foreach (string part in parts) {
            snapshot.WarningLines.Add(part);
        }

        string status = parts.Count == 0 ? "正常" : string.Join(" / ", parts);
        return $"维护风险: {status} | 磨损 {snapshot.WearAndTear:0.#} / 侵蚀 {snapshot.Corruption:0.#}";
    }

    private static string BuildChassisText(ChassisComponent chassis) {
        if (chassis == null) {
            return "底盘: 未安装";
        }

        string id = string.IsNullOrEmpty(chassis.ChassisID) ? "未知底盘" : chassis.ChassisID;
        return $"底盘: {id} Lv.{chassis.Level} / {Mathf.Max(0, chassis.GridWidth)}x{Mathf.Max(0, chassis.GridHeight)}";
    }

    private static string BuildStatsText(DollStatsComponent stats) {
        if (stats == null) {
            return "属性: 未配置";
        }

        return $"属性: AP {stats.MaxAP}+{stats.APRegen}/回合 / 力量 {stats.Power} / 算力 {stats.Compute} / 魅力 {stats.Charm}";
    }

    private static DollCoreEmotionState DeriveEmotion(int sanCurrent, int sanMax, int bondLevel) {
        if (sanMax <= 0) {
            return DollCoreEmotionState.Unknown;
        }

        if (sanCurrent <= 0) {
            return DollCoreEmotionState.Broken;
        }

        float ratio = BuildRatio(sanCurrent, sanMax);
        if (ratio < 0.1f) {
            return DollCoreEmotionState.Panic;
        }

        if (ratio < 0.3f) {
            return DollCoreEmotionState.Depressed;
        }

        if (ratio < 0.5f) {
            return DollCoreEmotionState.Tired;
        }

        if (ratio >= 0.8f && bondLevel >= 4) {
            return DollCoreEmotionState.Energetic;
        }

        return DollCoreEmotionState.Calm;
    }

    private static string FormatSanState(int sanCurrent, int sanMax) {
        if (sanMax <= 0) {
            return "SAN状态: 未配置";
        }

        if (sanCurrent <= 0) {
            return "SAN状态: 崩溃";
        }

        float ratio = BuildRatio(sanCurrent, sanMax);
        if (ratio < 0.1f) {
            return "SAN状态: 崩溃边缘";
        }

        if (ratio < 0.3f) {
            return "SAN状态: 压抑";
        }

        if (ratio < 0.5f) {
            return "SAN状态: 疲惫";
        }

        if (ratio < 0.8f) {
            return "SAN状态: 正常";
        }

        return "SAN状态: 稳定";
    }

    private static string FormatEmotion(DollCoreEmotionState state) {
        switch (state) {
            case DollCoreEmotionState.Energetic:
                return "情绪: 元气";
            case DollCoreEmotionState.Calm:
                return "情绪: 平静";
            case DollCoreEmotionState.Tired:
                return "情绪: 疲惫";
            case DollCoreEmotionState.Depressed:
                return "情绪: 压抑";
            case DollCoreEmotionState.Panic:
                return "情绪: 崩溃边缘";
            case DollCoreEmotionState.Broken:
                return "情绪: 崩溃";
            default:
                return "情绪: 未知";
        }
    }

    private static string FormatBondStage(int bondLevel, int hiddenTrust) {
        int safeLevel = Mathf.Clamp(bondLevel, 0, 10);
        string stageName;
        switch (safeLevel) {
            case 1:
                stageName = "认识";
                break;
            case 2:
                stageName = "熟悉";
                break;
            case 3:
                stageName = "信任";
                break;
            case 4:
                stageName = "依靠";
                break;
            case 5:
                stageName = "羁绊";
                break;
            case 6:
                stageName = "坚定";
                break;
            case 7:
                stageName = "亲密";
                break;
            case 8:
                stageName = "共鸣";
                break;
            case 9:
                stageName = "誓约";
                break;
            case 10:
                stageName = "灵魂连接";
                break;
            default:
                stageName = "冷启动";
                break;
        }

        return $"Bond: Lv.{safeLevel} {stageName} / 隐藏信任 {Mathf.Max(0, hiddenTrust)}";
    }

    private static string BuildCombinedText(DollCoreStateReadabilitySnapshot snapshot) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(snapshot.Header ?? "人偶状态");

        if (!string.IsNullOrEmpty(snapshot.Name) || !string.IsNullOrEmpty(snapshot.DollID)) {
            builder.AppendLine($"{snapshot.Name} ({snapshot.DollID})");
        }

        AppendLineIfNotEmpty(builder, snapshot.HPText);
        AppendLineIfNotEmpty(builder, snapshot.SANText);
        AppendLineIfNotEmpty(builder, snapshot.SanStateText);
        AppendLineIfNotEmpty(builder, snapshot.EmotionText);
        AppendLineIfNotEmpty(builder, snapshot.BondStageText);
        AppendLineIfNotEmpty(builder, snapshot.MaintenanceText);
        AppendLineIfNotEmpty(builder, snapshot.ChassisText);
        AppendLineIfNotEmpty(builder, snapshot.StatsText);

        if (snapshot.ProstheticLines.Count > 0) {
            builder.AppendLine("义体:");
            foreach (DollCoreProstheticLine line in snapshot.ProstheticLines) {
                if (line == null) {
                    continue;
                }

                builder.AppendLine($"- {line.StatusText} {line.Name} ({line.ProstheticID}) | {line.DetailText}");
            }
        }

        if (snapshot.TraitLines.Count > 0) {
            builder.AppendLine("特质:");
            foreach (string line in snapshot.TraitLines) {
                builder.AppendLine($"- {line}");
            }
        }

        if (snapshot.WarningLines.Count > 0) {
            builder.AppendLine("警告:");
            foreach (string line in snapshot.WarningLines) {
                builder.AppendLine($"- {line}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendLineIfNotEmpty(StringBuilder builder, string text) {
        if (!string.IsNullOrEmpty(text)) {
            builder.AppendLine(text);
        }
    }

    private static float BuildRatio(int current, int max) {
        if (max <= 0) {
            return 0f;
        }

        return Mathf.Clamp01((float)Mathf.Max(0, current) / max);
    }

    private static string FormatPercent(float ratio) {
        return $"{Mathf.RoundToInt(Mathf.Clamp01(ratio) * 100f)}%";
    }
}
