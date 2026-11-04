using System.Collections.Generic;

public class SanTaxLine {
    public string Reason;
    public int Amount;
}

// The road tax on top of a layer's SAN per step (GDD_02 §3.5): the deeper layers charge more while Zero is badly
// hurt, the backpack is overloaded or her corruption runs high. The rules are the layer's SANTax; lifts and the
// stairs cost nothing, so they are never taxed.
public static class DungeonSanTaxService {
    public static List<SanTaxLine> Resolve(DungeonConfig layer, DollEntity doll, BackpackGrid grid) {
        List<SanTaxLine> lines = new List<SanTaxLine>();
        DungeonSanTaxRules rules = layer?.SANTax;
        if (rules == null || doll?.Status == null) {
            return lines;
        }

        if (rules.LowHPExtra > 0 && doll.Status.HP_Max > 0 && doll.Status.HP_Current < doll.Status.HP_Max * rules.LowHPRatio) {
            lines.Add(new SanTaxLine { Reason = "重伤", Amount = rules.LowHPExtra });
        }

        if (rules.OverloadExtra > 0 && grid != null) {
            int usable = grid.UsableCellCount();
            if (usable > 0 && grid.FilledCellCount() >= usable * rules.OverloadRatio) {
                lines.Add(new SanTaxLine { Reason = "过载", Amount = rules.OverloadExtra });
            }
        }

        if (rules.CorruptionExtra > 0 && doll.Status.Corruption >= rules.CorruptionThreshold) {
            lines.Add(new SanTaxLine { Reason = "侵蚀", Amount = rules.CorruptionExtra });
        }

        return lines;
    }

    public static int Total(List<SanTaxLine> lines) {
        int total = 0;
        foreach (SanTaxLine line in lines) {
            total += line.Amount;
        }

        return total;
    }

    // "过载 +1、重伤 +2" for the map's status line.
    public static string Describe(List<SanTaxLine> lines) {
        List<string> parts = new List<string>();
        foreach (SanTaxLine line in lines) {
            parts.Add($"{line.Reason} +{line.Amount}");
        }

        return string.Join("、", parts);
    }
}
