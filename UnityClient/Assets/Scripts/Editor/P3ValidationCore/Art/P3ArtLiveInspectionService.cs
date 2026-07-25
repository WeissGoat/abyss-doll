using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace P3.Validation {
public static class ArtLiveInspectionService {
    static string BuildPath(Transform t, Transform root) {
        if (t == root) return root.name;
        var path = t.name;
        while (t.parent && t.parent != root) {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return root.name + "/" + path;
    }

    static string NormalizeAssetPath(string path) {
        return (path ?? string.Empty).Replace('\\', '/');
    }

    static bool TryGetRegisteredSpriteIdentity(string visualId, out string assetPath, out string assetGuid) {
        assetPath = null;
        assetGuid = null;
        var registry = Resources.Load<VisualAssetRegistry>("VisualAssetRegistry");
        VisualAssetEntry entry;
        if (registry == null || !registry.TryGetEntry(visualId, out entry) || entry == null || entry.Sprite == null) {
            return false;
        }

        assetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(entry.Sprite));
        assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
        return assetPath.StartsWith("Assets/Art/Approved/", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(assetGuid);
    }

    public static ArtBindingResolution ResolveBindingEvidence(ArtTargetDefinition target, IList<ArtUiNodeSnapshot> nodes) {
        var result = new ArtBindingResolution();
        var required = target.RequiredVisualIds ?? new List<string>();
        if (required.Count == 0) {
            result.Status = "not_configured";
            result.Issues.Add(new ArtInspectionIssue {
                IssueId = "runtime_binding_contract_missing",
                TargetId = target.TargetId,
                Category = ArtIssueCategory.VisualId,
                Severity = "P0",
                Observation = "Runtime target has no required VisualID binding contract.",
                Blocking = true
            });
            return result;
        }

        result.Status = "passed";
        foreach (var visualId in required.Distinct(StringComparer.OrdinalIgnoreCase)) {
            var matches = (nodes ?? new List<ArtUiNodeSnapshot>())
                .Where(node => node != null && node.Active && string.Equals(node.VisualId, visualId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count != 1) {
                result.Status = "failed";
                result.Issues.Add(new ArtInspectionIssue {
                    IssueId = "runtime_binding_missing_" + visualId,
                    TargetId = target.TargetId,
                    Category = ArtIssueCategory.SpriteBinding,
                    Severity = "P0",
                    Observation = matches.Count == 0
                        ? "Required VisualID is not consumed by a live UGUI node."
                        : "Required VisualID is consumed by multiple live UGUI nodes.",
                    Blocking = true
                });
                continue;
            }

            var node = matches[0];
            var nodePath = NormalizeAssetPath(node.SpriteAssetPath);
            if (!nodePath.StartsWith("Assets/Art/Approved/", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(node.SpriteGuid)) {
                result.Status = "failed";
                result.Issues.Add(new ArtInspectionIssue {
                    IssueId = "runtime_binding_mismatch_" + visualId,
                    TargetId = target.TargetId,
                    Category = ArtIssueCategory.SpriteBinding,
                    Severity = "P0",
                    Observation = "Live VisualID does not resolve to a registered Approved asset.",
                    Blocking = true,
                    SuspectedComponents = { node.Path }
                });
                continue;
            }

            string registryPath;
            string registryGuid;
            if (!TryGetRegisteredSpriteIdentity(visualId, out registryPath, out registryGuid)) {
                result.Status = "failed";
                result.Issues.Add(new ArtInspectionIssue {
                    IssueId = "runtime_binding_registry_missing_" + visualId,
                    TargetId = target.TargetId,
                    Category = ArtIssueCategory.SpriteBinding,
                    Severity = "P0",
                    Observation = "VisualID is not available as a Sprite in the live VisualAssetRegistry.",
                    Blocking = true,
                    SuspectedComponents = { node.Path }
                });
                continue;
            }

            if (!string.Equals(nodePath, registryPath, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(node.SpriteGuid, registryGuid, StringComparison.OrdinalIgnoreCase)) {
                result.Status = "failed";
                result.Issues.Add(new ArtInspectionIssue {
                    IssueId = "runtime_binding_mismatch_" + visualId,
                    TargetId = target.TargetId,
                    Category = ArtIssueCategory.SpriteBinding,
                    Severity = "P0",
                    Observation = "Live Sprite path or GUID differs from the registered VisualAssetRegistry entry.",
                    Blocking = true,
                    SuspectedComponents = { node.Path }
                });
                continue;
            }

            result.Bindings.Add(new ArtBindingEvidence {
                TargetId = target.TargetId,
                VisualId = visualId,
                ComponentPath = node.Path,
                AssetPath = nodePath,
                AssetGuid = node.SpriteGuid,
                Status = "passed",
                Observation = "Live UGUI node consumes the Approved Sprite returned by VisualAssetRegistry."
            });
        }
        return result;
    }

    public static ArtInspectionResult Inspect(string targetId, int maxNodes = 200) {
        if (maxNodes < 1 || maxNodes > 500) throw new ArgumentOutOfRangeException("maxNodes");
        var def = ArtTargetRegistry.Get(targetId);
        var result = new ArtInspectionResult {
            TargetId = targetId,
            ScreenTag = def.ScreenTag,
            CapturedAt = DateTime.UtcNow.ToString("o")
        };
        GameObject root;
        try {
            root = ArtTargetRegistry.ResolveVisibleRoot(targetId);
            result.Reachable = true;
        } catch {
            result.Issues.Add(new ArtInspectionIssue {
                IssueId = "reachability",
                TargetId = targetId,
                Category = ArtIssueCategory.ProgramReachability,
                Severity = "P0",
                Observation = "Registered target root is not visible.",
                Blocking = true
            });
            return result;
        }

        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length && result.Nodes.Count < maxNodes; i++) {
            var go = transforms[i].gameObject;
            var rectTransform = go.GetComponent<RectTransform>();
            var graphic = go.GetComponent<Graphic>();
            var canvasGroup = go.GetComponent<CanvasGroup>();
            var image = go.GetComponent<Image>();
            var node = new ArtUiNodeSnapshot {
                Path = BuildPath(transforms[i], root.transform),
                Active = go.activeInHierarchy,
                HasRectTransform = rectTransform,
                HasGraphic = graphic,
                RaycastTarget = graphic && graphic.raycastTarget,
                Alpha = canvasGroup ? canvasGroup.alpha : (graphic ? graphic.color.a : 1),
                SpriteName = image && image.sprite ? image.sprite.name : null
            };
            if (image && image.sprite) {
                node.SpriteAssetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(image.sprite));
                node.SpriteGuid = AssetDatabase.AssetPathToGUID(node.SpriteAssetPath);
                if (node.SpriteAssetPath.StartsWith("Assets/Art/Approved/", StringComparison.OrdinalIgnoreCase)) {
                    node.VisualId = Path.GetFileNameWithoutExtension(node.SpriteAssetPath);
                }
            }
            if (rectTransform) {
                node.X = rectTransform.anchoredPosition.x;
                node.Y = rectTransform.anchoredPosition.y;
                node.Width = rectTransform.rect.width;
                node.Height = rectTransform.rect.height;
                if (float.IsNaN(node.X) || float.IsInfinity(node.X) || float.IsNaN(node.Width) || float.IsInfinity(node.Width)) {
                    result.Issues.Add(new ArtInspectionIssue {
                        IssueId = "invalid_rect_" + i,
                        TargetId = targetId,
                        Category = ArtIssueCategory.Layout,
                        Severity = "P0",
                        Observation = "RectTransform contains NaN or infinity.",
                        Blocking = true
                    });
                }
            }
            if (image && go.activeInHierarchy && !image.sprite) {
                result.Issues.Add(new ArtInspectionIssue {
                    IssueId = "missing_sprite_" + i,
                    TargetId = targetId,
                    Category = ArtIssueCategory.SpriteBinding,
                    Severity = "P1",
                    Observation = "Visible Image has no sprite.",
                    Blocking = true,
                    SuspectedComponents = { node.Path }
                });
            }
            result.Nodes.Add(node);
        }

        var binding = ResolveBindingEvidence(def, result.Nodes);
        result.BindingStatus = binding.Status;
        result.Bindings = binding.Bindings;
        result.Issues.AddRange(binding.Issues);
        var scaler = root.GetComponentInParent<Canvas>()?.GetComponent<CanvasScaler>();
        if (scaler && (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || scaler.referenceResolution != new Vector2(1920, 1080))) {
            result.Issues.Add(new ArtInspectionIssue {
                IssueId = "canvas_scaler",
                TargetId = targetId,
                Category = ArtIssueCategory.Layout,
                Severity = "P1",
                Observation = "CanvasScaler is not 1920x1080 ScaleWithScreenSize.",
                Blocking = true
            });
        }
        return result;
    }

    public static string Write(string run, ArtInspectionResult result) {
        var dir = Path.Combine(EvidencePaths.ForArtRun(run).Root, "targets", result.TargetId);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "components.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(result, Formatting.Indented));
        return path;
    }
}
}
