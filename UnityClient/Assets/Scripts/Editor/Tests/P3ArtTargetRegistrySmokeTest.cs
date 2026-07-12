using System;
using P3.Validation;
using UnityEngine;

public static class P3ArtTargetRegistrySmokeTest {
 public static void Run(){AssertRoot("workshop_main","WorkshopPanel");AssertRoot("dungeon_map","DungeonMapPanel");AssertRoot("dialogue_overlay","P3DialogueOverlay_Runtime");AssertRoot("t0_prologue","P3DialogueOverlay_Runtime");bool unsafeRejected=false,unknownRejected=false;try{ArtTargetRegistry.Get("../raw");}catch{unsafeRejected=true;}try{ArtTargetRegistry.Get("unknown");}catch{unknownRejected=true;}if(!unsafeRejected||!unknownRejected)throw new Exception("unsafe target accepted");if(ProfileRegistry.GetArt("art_regression").Mode!="regression")throw new Exception("regression profile missing");Debug.Log("P3 Art Target Registry PASSED");}
 static void AssertRoot(string id,string expected){var t=ArtTargetRegistry.Get(id);if(t.ExpectedRoots.Count==0||t.ExpectedRoots[0]!=expected)throw new Exception("target root mismatch: "+id);}
}
