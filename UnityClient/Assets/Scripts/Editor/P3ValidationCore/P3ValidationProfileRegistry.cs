using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace P3.Validation {
public static class ProfileRegistry {
    private sealed class Root { public List<Profile> profiles; }
    private static Profile Get(string file,string id,ValidationDomain domain){var path=Path.Combine(Application.dataPath,"Editor/P3Validation",file);var root=JsonConvert.DeserializeObject<Root>(File.ReadAllText(path));var profile=root?.profiles?.Find(p=>p.Id==id);if(profile==null)throw new ArgumentException("Unknown "+domain+" profile: "+id);if(profile.ValidationDomain!=domain)throw new InvalidDataException("Profile domain mismatch: "+id);return profile;}
    public static Profile GetProgram(string id)=>Get("program_validation_profiles.json",id,ValidationDomain.Program);
    public static Profile GetArt(string id)=>Get("art_validation_profiles.json",id,ValidationDomain.Art);
    public static Profile GetRelease(string id)=>Get("release_validation_profiles.json",id,ValidationDomain.Release);
}
}
