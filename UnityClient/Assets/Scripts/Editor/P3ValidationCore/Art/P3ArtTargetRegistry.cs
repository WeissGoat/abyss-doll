using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace P3.Validation {
public static class ArtTargetRegistry {
 sealed class Root { public List<ArtTargetDefinition> targets; }
 static List<ArtTargetDefinition> Load(){var path=Path.Combine(Application.dataPath,"Editor/P3Validation/art_validation_targets.json");var root=JsonConvert.DeserializeObject<Root>(File.ReadAllText(path));return root?.targets??new List<ArtTargetDefinition>();}
 public static ArtTargetDefinition Get(string id){if(string.IsNullOrWhiteSpace(id)||id.Contains("..")||id.IndexOfAny(new[]{'/','\\'})>=0)throw new ArgumentException("art_blocked:unknown_target");var target=Load().Find(x=>string.Equals(x.TargetId,id,StringComparison.OrdinalIgnoreCase));if(target==null)throw new ArgumentException("art_blocked:unknown_target");return target;}
 public static bool IsRegistered(string id){try{Get(id);return true;}catch{return false;}}
 public static GameObject ResolveVisibleRoot(string id){var target=Get(id);foreach(var name in target.ExpectedRoots){var go=GameObject.Find(name);if(go&&go.activeInHierarchy)return go;}throw new InvalidOperationException("art_blocked:target_screen_unreachable;handoff=program");}
}
}
