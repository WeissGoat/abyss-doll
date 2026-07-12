using System;
using System.Collections.Generic;

namespace P3.Validation {
public static class ArtPersistAdapterRegistry {
 static readonly Dictionary<string,IArtPersistAdapter> Items=new Dictionary<string,IArtPersistAdapter>(StringComparer.OrdinalIgnoreCase);
 public static void Register(IArtPersistAdapter adapter){if(adapter==null||string.IsNullOrWhiteSpace(adapter.TargetId))throw new ArgumentException("invalid adapter");Items[adapter.TargetId]=adapter;}
 public static IArtPersistAdapter Get(string targetId){IArtPersistAdapter a;if(!Items.TryGetValue(targetId,out a))throw new InvalidOperationException("art_blocked:persist_adapter_missing");return a;}
 public static void ClearForTests()=>Items.Clear();
}
}
