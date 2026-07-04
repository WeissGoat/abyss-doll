using System.Collections.Generic;
using UnityEngine;

public static class NarrativeSchedulerSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Narrative Scheduler Smoke Test ===");

        NarrativeConfigDatabase database = BuildDatabase();
        NarrativeStateStore state = new NarrativeStateStore();
        NarrativeTriggerService triggerService = new NarrativeTriggerService(database, state);
        NarrativeScheduler scheduler = new NarrativeScheduler(state);
        NarrativePlaybackService playback = new NarrativePlaybackService(database, new P3YarnLikeNarrativeRuntime(), state, scheduler);

        List<NarrativeRequest> firstRequests = triggerService.Evaluate(NarrativeEventContext.Create("PrologueStarted", "prologue"));
        bool firstTriggered = firstRequests.Count == 1 && firstRequests[0].YarnNode == "T0_01A_Opening_CG";
        bool modalLocked = scheduler.Submit(firstRequests[0]) == NarrativeScheduleResult.Started
            && state.IsInputLocked
            && state.LockedContext == "prologue";
        NarrativePlaybackResult firstResult = playback.PlayActive();
        bool completedFlags = firstResult.Success
            && state.GetFlag("PrologueStarted")
            && state.GetFlag("LastCoreShardSeen")
            && state.GetTriggerSeenCount("t0_01a_new_game_opening") == 1
            && state.GetNodeSeenCount("T0_01A_Opening_CG") == 1
            && !state.IsInputLocked;

        bool onceRejected = triggerService.Evaluate(NarrativeEventContext.Create("PrologueStarted", "prologue")).Count == 0;
        bool requiredFlagPassed = triggerService.Evaluate(NarrativeEventContext.Create("NarrativeNodeCompleted", "prologue")).Count == 1;

        List<NarrativeRequest> illegalContext = triggerService.Evaluate(
            NarrativeEventContext.Create("PrologueActionClicked", "combat").WithValue("action", "start_doll"));
        bool illegalContextRejected = illegalContext.Count == 0;

        state.SetFlag("No0Found");
        List<NarrativeRequest> startRequests = triggerService.Evaluate(
            NarrativeEventContext.Create("PrologueActionClicked", "prologue").WithValue("action", "start_doll"));
        bool actionTriggered = startRequests.Count == 1 && startRequests[0].IfBusyPolicy == "drop";

        NarrativeRequest active = BuildRequest("active_modal", "T0_01A_FindNo0", "modal", "queue_once", 90, "prologue");
        NarrativeRequest dropped = BuildRequest("drop_when_busy", "T0_01A_StartDoll", "modal", "drop", 95, "prologue");
        NarrativeRequest queued = BuildRequest("queue_when_busy", "T0_01A_No0Wake", "modal_light", "queue_once", 80, "workshop");
        bool busyPolicies = scheduler.Submit(active) == NarrativeScheduleResult.Started
            && scheduler.Submit(dropped) == NarrativeScheduleResult.Dropped
            && scheduler.Submit(queued) == NarrativeScheduleResult.Queued
            && scheduler.Submit(queued) == NarrativeScheduleResult.Dropped
            && state.PendingCount == 1
            && state.IsInputLocked;

        scheduler.CompleteActive();
        bool queuedStarted = scheduler.IsBusy
            && scheduler.ActiveRequest.TriggerID == "queue_when_busy"
            && state.IsInputLocked
            && state.LockedContext == "workshop";
        scheduler.CompleteActive();

        if (firstTriggered
            && modalLocked
            && completedFlags
            && onceRejected
            && requiredFlagPassed
            && illegalContextRejected
            && actionTriggered
            && busyPolicies
            && queuedStarted) {
            Debug.Log("Narrative Scheduler Smoke PASSED.");
        } else {
            Debug.LogError(
                "Narrative Scheduler Smoke FAILED. "
                + $"First={firstTriggered}, Modal={modalLocked}, Complete={completedFlags}, Once={onceRejected}, "
                + $"Required={requiredFlagPassed}, Context={illegalContextRejected}, Action={actionTriggered}, "
                + $"Busy={busyPolicies}, Queued={queuedStarted}, Errors={string.Join("|", firstResult.Errors)}");
        }

        Debug.Log("=== Narrative Scheduler Smoke Test Finished ===");
    }

    private static NarrativeConfigDatabase BuildDatabase() {
        NarrativeConfigDatabase database = new NarrativeConfigDatabase {
            RootPath = Application.dataPath,
            Speakers = {
                ["protagonist"] = new NarrativeSpeakerConfig {
                    SpeakerID = "protagonist",
                    DisplayNameKey = "speaker.protagonist",
                    FallbackVisualID = "ui_dialogue_speaker_fallback"
                }
            },
            Flags = {
                ["PrologueStarted"] = new NarrativeFlagConfig { FlagID = "PrologueStarted", Scope = "Save" },
                ["LastCoreShardSeen"] = new NarrativeFlagConfig { FlagID = "LastCoreShardSeen", Scope = "Save" },
                ["No0Found"] = new NarrativeFlagConfig { FlagID = "No0Found", Scope = "Save" },
                ["No0Started"] = new NarrativeFlagConfig { FlagID = "No0Started", Scope = "Save" }
            },
            Commands = {
                ["set_flag"] = new NarrativeCommandConfig {
                    CommandName = "set_flag",
                    MinArgs = 1,
                    MaxArgs = 1,
                    AllowedContexts = new List<string> { "prologue", "workshop" }
                }
            },
            ScriptSources = {
                ["scripts/test.yarn"] = BuildScript()
            },
            ScriptNodes = {
                ["scripts/test.yarn"] = new HashSet<string> {
                    "T0_01A_Opening_CG",
                    "T0_01A_FindNo0",
                    "T0_01A_StartDoll",
                    "T0_01A_No0Wake"
                }
            }
        };

        AddNode(database, "T0_01A_Opening_CG", "prologue");
        AddNode(database, "T0_01A_FindNo0", "prologue");
        AddNode(database, "T0_01A_StartDoll", "prologue");
        AddNode(database, "T0_01A_No0Wake", "workshop");

        database.Triggers["t0_01a_new_game_opening"] = new NarrativeTriggerConfig {
            TriggerID = "t0_01a_new_game_opening",
            EventType = "PrologueStarted",
            YarnNode = "T0_01A_Opening_CG",
            Priority = 100,
            BlockingMode = "modal",
            IfBusyPolicy = "queue_once",
            Context = "prologue",
            Once = true,
            Chance = 1f,
            ForbiddenFlags = new List<string> { "PrologueStarted" },
            Conditions = new List<NarrativeConditionConfig> {
                new NarrativeConditionConfig { Type = "FlagNotSet", Key = "PrologueStarted" }
            },
            SetFlagsOnComplete = new List<string> { "PrologueStarted", "LastCoreShardSeen" }
        };

        database.Triggers["t0_01a_find_no0"] = new NarrativeTriggerConfig {
            TriggerID = "t0_01a_find_no0",
            EventType = "NarrativeNodeCompleted",
            YarnNode = "T0_01A_FindNo0",
            Priority = 95,
            BlockingMode = "modal",
            IfBusyPolicy = "queue_once",
            Context = "prologue",
            Once = true,
            Chance = 1f,
            RequiredFlags = new List<string> { "LastCoreShardSeen" },
            ForbiddenFlags = new List<string> { "No0Found" },
            Conditions = new List<NarrativeConditionConfig> {
                new NarrativeConditionConfig { Type = "FlagEquals", Key = "LastCoreShardSeen", Value = "true" }
            },
            SetFlagsOnComplete = new List<string> { "No0Found" }
        };

        database.Triggers["t0_01a_start_doll_button"] = new NarrativeTriggerConfig {
            TriggerID = "t0_01a_start_doll_button",
            EventType = "PrologueActionClicked",
            YarnNode = "T0_01A_StartDoll",
            Priority = 95,
            BlockingMode = "modal",
            IfBusyPolicy = "drop",
            Context = "prologue",
            Once = true,
            Chance = 1f,
            RequiredFlags = new List<string> { "No0Found" },
            ForbiddenFlags = new List<string> { "No0Started" },
            Conditions = new List<NarrativeConditionConfig> {
                new NarrativeConditionConfig { Type = "ActionEquals", Key = "action", Value = "start_doll" }
            },
            SetFlagsOnComplete = new List<string> { "No0Started" }
        };

        return database;
    }

    private static void AddNode(NarrativeConfigDatabase database, string nodeID, string context) {
        database.Nodes[nodeID] = new NarrativeNodeConfig {
            NodeID = nodeID,
            ScriptFile = "scripts/test.yarn",
            Context = context,
            ContentType = "modal",
            ImplementationState = "locked_for_implementation",
            SpeakerIDs = new List<string> { "protagonist" },
            LineKeys = new List<string> { "line." + nodeID },
            CommandNames = new List<string> { "set_flag" },
            VisualIntents = new List<NarrativeVisualIntentConfig> {
                new NarrativeVisualIntentConfig {
                    VisualID = "cg_test",
                    FallbackVisualID = "ui_black_screen",
                    Required = true
                }
            }
        };
    }

    private static NarrativeRequest BuildRequest(string triggerID, string nodeID, string blockingMode, string busyPolicy, int priority, string context) {
        return new NarrativeRequest {
            TriggerID = triggerID,
            YarnNode = nodeID,
            BlockingMode = blockingMode,
            IfBusyPolicy = busyPolicy,
            Priority = priority,
            Context = context,
            TriggerConfig = new NarrativeTriggerConfig {
                TriggerID = triggerID,
                YarnNode = nodeID,
                BlockingMode = blockingMode,
                IfBusyPolicy = busyPolicy,
                Priority = priority,
                Context = context
            }
        };
    }

    private static string BuildScript() {
        return @"title: T0_01A_Opening_CG
---
protagonist: opening#line:line.T0_01A_Opening_CG
<<set_flag PrologueStarted>>
===
title: T0_01A_FindNo0
---
protagonist: find#line:line.T0_01A_FindNo0
===
title: T0_01A_StartDoll
---
protagonist: start#line:line.T0_01A_StartDoll
===
title: T0_01A_No0Wake
---
protagonist: wake#line:line.T0_01A_No0Wake
===";
    }
}
