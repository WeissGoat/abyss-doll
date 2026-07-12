using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;

namespace P3.Validation {
public static class ProgramValidationOrchestrator {
    static StepResult Write(string run,Profile profile,string step,StepStatus status,string code){
        var result=new StepResult{ValidationDomain=ValidationDomain.Program,RunId=run,ProfileId=profile.Id,StepId=step,Required=profile.RequiredSteps.Contains(step),Status=status,Code=code};
        var dir=Path.Combine(EvidencePaths.ForProgramRun(run).Steps,step);Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"result.json"),JsonConvert.SerializeObject(result,Formatting.Indented));return result;
    }
    public static StepResult RunSmoke(string run,string profileId){
        var profile=ProfileRegistry.GetProgram(profileId);var marker=ConsoleTracker.Begin(run);
        try{var result=P3SmokeExecutionService.Execute(ProgramSmokeRegistry.Get(profile.SmokeSet));return Write(run,profile,"smoke",result.Passed?StepStatus.Passed:StepStatus.Failed,result.Passed?"program_passed:smoke":"program_failed:smoke");}
        finally{ConsoleTracker.Complete(marker,ValidationDomain.Program);}
    }
    public static object RunProfile(string run,string profileId,string instanceId){
        var profile=ProfileRegistry.GetProgram(profileId);string active;
        if(!InstanceLock.TryAcquire(instanceId,run,out active))throw new InvalidOperationException("infra_blocked:instance_locked:"+active);
        var job=new JobState{RunId=run,Domain="program",ProfileId=profile.Id,InstanceId=instanceId,Status="Running",StartedAt=DateTime.UtcNow.ToString("o"),OriginalPlayMode=EditorApplication.isPlaying,OriginalPaused=EditorApplication.isPaused};
        try{
            job.CurrentStep="Preflight";job.Progress=.1f;JobStore.Save(job);
            var snapshot=EditorSnapshot.Capture();if(snapshot.HasDirtyScenes||snapshot.PrefabStageDirty)throw new InvalidOperationException("infra_blocked:dirty_editor_state");
            job.CurrentStep="Compile";job.Progress=.3f;JobStore.Save(job);Write(run,profile,"compile_gate",EditorApplication.isCompiling?StepStatus.Limited:StepStatus.Passed,EditorApplication.isCompiling?"infra_limited:compiling":"program_passed:compile_gate");
            job.CurrentStep="Smoke";job.Progress=.6f;JobStore.Save(job);var smoke=RunSmoke(run,profile.Id);
            job.CurrentStep="Console";job.Progress=.9f;JobStore.Save(job);Write(run,profile,"console_delta",StepStatus.Passed,"program_passed:console_collected");
            job.CurrentStep="Complete";job.Progress=1;job.Status=smoke.Status==StepStatus.Passed?"Passed":"Failed";JobStore.Save(job);return new{run_id=run,profile_id=profile.Id,status=job.Status,evidence_root=EvidencePaths.ForProgramRun(run).Root};
        }catch(Exception e){job.Status="Failed";job.LastError=e.Message;JobStore.Save(job);throw;}finally{InstanceLock.Release(instanceId,run);}
    }
}
}
