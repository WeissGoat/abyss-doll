using System;
using MCPForUnity.Editor.Helpers;

public static class P3ValidationJobStore {
    private static string Key(string runId)=>"p3_validation_"+runId;
    public static P3ValidationJobState Load(string runId)=>McpJobStateStore.LoadState<P3ValidationJobState>(Key(runId));
    public static void Save(P3ValidationJobState state){ state.UpdatedAt=DateTime.UtcNow.ToString("o"); McpJobStateStore.SaveState(Key(state.RunId),state); }
    public static void Clear(string runId)=>McpJobStateStore.ClearState(Key(runId));
}

public static class P3ValidationInstanceLock {
    private sealed class LockState { public string instanceId, runId, updatedAt; }
    private const string Key="p3_validation_instance_lock";
    public static bool TryAcquire(string instanceId,string runId,out string activeRunId) {
        var current=McpJobStateStore.LoadState<LockState>(Key); activeRunId=current?.runId;
        if(current!=null && current.instanceId==instanceId && current.runId!=runId) {
            DateTime updated; var stale=!DateTime.TryParse(current.updatedAt,out updated)||DateTime.UtcNow-updated.ToUniversalTime()>TimeSpan.FromMinutes(30);
            var job=P3ValidationJobStore.Load(current.runId); var terminal=job!=null&&(job.Status=="Complete"||job.Status=="Failed"||job.Status=="Cancelled");
            if(!stale&&!terminal) return false;
        }
        McpJobStateStore.SaveState(Key,new LockState{instanceId=instanceId,runId=runId,updatedAt=DateTime.UtcNow.ToString("o")}); activeRunId=runId; return true;
    }
    public static void Release(string instanceId,string runId){ var c=McpJobStateStore.LoadState<LockState>(Key); if(c!=null&&c.instanceId==instanceId&&c.runId==runId) McpJobStateStore.ClearState(Key); }
}
