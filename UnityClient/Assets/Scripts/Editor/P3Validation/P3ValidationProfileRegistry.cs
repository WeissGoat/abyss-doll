using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class P3ValidationProfileRegistry {
    private sealed class Root { public List<P3ValidationProfile> profiles; }
    private static string PathName => Path.GetFullPath(Path.Combine(Application.dataPath,"Editor/P3Validation/p3_validation_profiles.json"));
    public static P3ValidationProfile GetRequired(string id) {
        var root=JsonConvert.DeserializeObject<Root>(File.ReadAllText(PathName));
        var found=root?.profiles?.Find(p=>p.Id==id);
        if(found==null) throw new ArgumentException("Unknown P3 validation profile: "+id);
        if(found.Version!="1" || found.EditorControl!="exclusive_restore") throw new InvalidDataException("Unsupported profile contract: "+id);
        return found;
    }
    public static IReadOnlyList<P3ValidationProfile> All() => JsonConvert.DeserializeObject<Root>(File.ReadAllText(PathName)).profiles;
}
