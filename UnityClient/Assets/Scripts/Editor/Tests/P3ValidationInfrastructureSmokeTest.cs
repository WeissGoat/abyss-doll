using System;
using System.Linq;
using P3.Validation;
using UnityEngine;
public static class P3ValidationInfrastructureSmokeTest { public static void Run(){var s=EditorSnapshot.Capture();if(s==null)throw new Exception("snapshot");var id="program_infra_"+Guid.NewGuid().ToString("N");var instance="instance_"+Guid.NewGuid().ToString("N");string active;if(!InstanceLock.TryAcquire(instance,id,out active))throw new Exception("lock");if(InstanceLock.TryAcquire(instance,id+"b",out active))throw new Exception("collision");InstanceLock.Release(instance,id);var marker=ConsoleTracker.Begin(id);var token="p3-infra-"+id;Debug.LogWarning(token);var path=ConsoleTracker.Complete(marker,ValidationDomain.Program);if(!System.IO.File.ReadAllText(path).Contains(token))throw new Exception("console");Debug.Log("P3 Validation Infrastructure PASSED");}}
