using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace P3.Validation {
public static class ProgramSmokeRegistry {
    sealed class Root { public Dictionary<string,List<string>> smoke_sets; }
    public static IReadOnlyList<string> Get(string id){
        var path=Path.Combine(Application.dataPath,"Editor/P3Validation/program_validation_profiles.json");
        var root=JsonConvert.DeserializeObject<Root>(File.ReadAllText(path));
        List<string> tests;
        if(string.IsNullOrWhiteSpace(id)||root?.smoke_sets==null||!root.smoke_sets.TryGetValue(id,out tests))throw new ArgumentException("Unknown registered program smoke set: "+id);
        return tests;
    }
}
}
