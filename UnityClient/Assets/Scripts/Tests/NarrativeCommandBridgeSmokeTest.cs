using System.Collections.Generic;
using UnityEngine;

public static class NarrativeCommandBridgeSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Narrative Command Bridge Smoke Test ===");

        NarrativeConfigDatabase database = BuildDatabase();
        NarrativeStateStore state = new NarrativeStateStore();
        FakeNarrativeCommandSink sink = new FakeNarrativeCommandSink();
        NarrativeCommandBridge bridge = new NarrativeCommandBridge(database, state, sink);

        bool allowedCommands = Execute(bridge, "play_visual", "prologue", "cg_t0_01a_black_wake").Success
            && Execute(bridge, "show_character", "workshop", "no0", "weak", "sitting").Success
            && Execute(bridge, "show_prologue_action", "prologue", "start_doll", "prologue.t0_01a.button.start_doll").Success
            && Execute(bridge, "show_status_card", "workshop", "no0_first_status", "before_wipe").Success
            && Execute(bridge, "unlock_ui", "workshop", "first_dive_gate", "primary").Success
            && Execute(bridge, "set_flag", "prologue", "No0Started").Success
            && Execute(bridge, "open_layer_confirm", "layer_confirm", "1").Success;

        bool sinkRequests = sink.VisualCount == 1
            && sink.CharacterCount == 1
            && sink.PrologueActionCount == 1
            && sink.StatusCardCount == 1
            && sink.UnlockUiCount == 1
            && sink.LayerConfirmCount == 1
            && sink.LastLayerID == 1
            && !sink.StartRunAtLayerCalled
            && state.GetFlag("No0Started");

        PlayerProfile player = BuildPlayerSnapshot();
        int moneyBefore = player.Money;
        int stashBefore = player.StashInventory.Count;
        int hpBefore = player.ActiveDoll.Status.HP_Current;
        int sanBefore = player.ActiveDoll.Status.SAN_Current;

        bool illegalCommandsRejected = !Execute(bridge, "add_money", "prologue", "999").Success
            && !Execute(bridge, "set_hp", "workshop", "1").Success
            && !Execute(bridge, "grant_drop", "layer_confirm", "boss_loot").Success
            && !Execute(bridge, "unlock_ui", "combat", "first_dive_gate").Success
            && !Execute(bridge, "set_flag", "prologue", "MissingFlag").Success
            && !Execute(bridge, "open_layer_confirm", "layer_confirm", "2").Success;

        bool gameplaySnapshotUnchanged = player.Money == moneyBefore
            && player.StashInventory.Count == stashBefore
            && player.ActiveDoll.Status.HP_Current == hpBefore
            && player.ActiveDoll.Status.SAN_Current == sanBefore
            && sink.LayerConfirmCount == 1
            && !sink.StartRunAtLayerCalled;

        bool playbackBridge = TestPlaybackBridge(database);

        if (allowedCommands && sinkRequests && illegalCommandsRejected && gameplaySnapshotUnchanged && playbackBridge) {
            Debug.Log("Narrative Command Bridge Smoke PASSED.");
        } else {
            Debug.LogError(
                "Narrative Command Bridge Smoke FAILED. "
                + $"Allowed={allowedCommands}, Sink={sinkRequests}, Illegal={illegalCommandsRejected}, "
                + $"GameplayUnchanged={gameplaySnapshotUnchanged}, Playback={playbackBridge}, "
                + $"LayerRequests={sink.LayerConfirmCount}, StartRun={sink.StartRunAtLayerCalled}");
        }

        Debug.Log("=== Narrative Command Bridge Smoke Test Finished ===");
    }

    private static bool TestPlaybackBridge(NarrativeConfigDatabase database) {
        NarrativeStateStore playbackState = new NarrativeStateStore();
        FakeNarrativeCommandSink playbackSink = new FakeNarrativeCommandSink();
        NarrativeScheduler scheduler = new NarrativeScheduler(playbackState);
        NarrativeCommandBridge bridge = new NarrativeCommandBridge(database, playbackState, playbackSink);
        NarrativePlaybackService playback = new NarrativePlaybackService(
            database,
            new P3YarnLikeNarrativeRuntime(),
            playbackState,
            scheduler,
            bridge);

        NarrativeRequest request = new NarrativeRequest {
            TriggerID = "t0_01a_layer1_confirm",
            YarnNode = "T0_01A_Layer1Confirm",
            BlockingMode = "modal_light",
            IfBusyPolicy = "drop",
            Priority = 95,
            Context = "layer_confirm",
            TriggerConfig = new NarrativeTriggerConfig {
                TriggerID = "t0_01a_layer1_confirm",
                YarnNode = "T0_01A_Layer1Confirm",
                BlockingMode = "modal_light",
                IfBusyPolicy = "drop",
                Priority = 95,
                Context = "layer_confirm",
                SetFlagsOnComplete = new List<string> { "Layer1ConfirmOpened" }
            }
        };

        bool started = scheduler.Submit(request) == NarrativeScheduleResult.Started;
        NarrativePlaybackResult result = playback.PlayActive();
        bool validPlayback = started
            && result.Success
            && playbackState.GetFlag("Layer1ConfirmOpened")
            && playbackState.GetFlag("No0Started")
            && playbackState.GetTriggerSeenCount("t0_01a_layer1_confirm") == 1
            && playbackSink.LayerConfirmCount == 1
            && playbackSink.LastLayerID == 1
            && !playbackSink.StartRunAtLayerCalled;

        NarrativeRequest illegalRequest = new NarrativeRequest {
            TriggerID = "illegal_trigger",
            YarnNode = "Illegal_Command_Node",
            BlockingMode = "modal",
            IfBusyPolicy = "drop",
            Priority = 100,
            Context = "prologue",
            TriggerConfig = new NarrativeTriggerConfig {
                TriggerID = "illegal_trigger",
                YarnNode = "Illegal_Command_Node",
                BlockingMode = "modal",
                IfBusyPolicy = "drop",
                Priority = 100,
                Context = "prologue"
            }
        };

        NarrativePlaybackResult illegalResult = playback.Play(illegalRequest);
        bool illegalPlaybackRejected = !illegalResult.Success
            && playbackState.GetTriggerSeenCount("illegal_trigger") == 0;

        return validPlayback && illegalPlaybackRejected;
    }

    private static NarrativeCommandBridgeResult Execute(
        NarrativeCommandBridge bridge,
        string commandName,
        string context,
        params string[] args) {
        return bridge.Execute(
            new NarrativeCommandOutput {
                CommandName = commandName,
                Arguments = new List<string>(args),
                RawCommand = commandName
            },
            new NarrativeCommandExecutionContext {
                Context = context,
                SourceNode = "SmokeNode",
                TriggerID = "SmokeTrigger"
            });
    }

    private static NarrativeConfigDatabase BuildDatabase() {
        NarrativeConfigDatabase database = new NarrativeConfigDatabase {
            RootPath = Application.dataPath,
            Flags = {
                ["No0Started"] = new NarrativeFlagConfig { FlagID = "No0Started", Scope = "Save" },
                ["Layer1ConfirmOpened"] = new NarrativeFlagConfig { FlagID = "Layer1ConfirmOpened", Scope = "Save" }
            },
            ScriptSources = {
                ["scripts/command_bridge_test.yarn"] = BuildScript()
            },
            ScriptNodes = {
                ["scripts/command_bridge_test.yarn"] = new HashSet<string> {
                    "T0_01A_Layer1Confirm",
                    "Illegal_Command_Node"
                }
            }
        };

        AddCommand(database, "play_visual", 1, 4, "prologue", "workshop", "layer_confirm");
        AddCommand(database, "show_character", 1, 6, "prologue", "workshop", "layer_confirm");
        AddCommand(database, "show_prologue_action", 2, 4, "prologue", "workshop");
        AddCommand(database, "show_status_card", 1, 2, "prologue", "workshop");
        AddCommand(database, "unlock_ui", 1, 2, "workshop");
        AddCommand(database, "open_layer_confirm", 1, 1, "layer_confirm");
        AddCommand(database, "set_flag", 1, 1, "prologue", "workshop", "layer_confirm");

        database.Nodes["T0_01A_Layer1Confirm"] = new NarrativeNodeConfig {
            NodeID = "T0_01A_Layer1Confirm",
            ScriptFile = "scripts/command_bridge_test.yarn",
            Context = "layer_confirm"
        };
        database.Nodes["Illegal_Command_Node"] = new NarrativeNodeConfig {
            NodeID = "Illegal_Command_Node",
            ScriptFile = "scripts/command_bridge_test.yarn",
            Context = "prologue"
        };

        return database;
    }

    private static void AddCommand(
        NarrativeConfigDatabase database,
        string commandName,
        int minArgs,
        int maxArgs,
        params string[] contexts) {
        database.Commands[commandName] = new NarrativeCommandConfig {
            CommandName = commandName,
            MinArgs = minArgs,
            MaxArgs = maxArgs,
            AllowedContexts = new List<string>(contexts)
        };
    }

    private static string BuildScript() {
        return @"title: T0_01A_Layer1Confirm
---
protagonist: go#line:line.layer_confirm
<<set_flag No0Started>>
<<open_layer_confirm 1>>
===
title: Illegal_Command_Node
---
protagonist: illegal#line:line.illegal
<<set_hp 1>>
===";
    }

    private static PlayerProfile BuildPlayerSnapshot() {
        return new PlayerProfile {
            Money = 123,
            StashInventory = new List<ItemEntity> {
                new ItemEntity { ConfigID = "loot_test_scrap" }
            },
            ActiveDoll = new DollEntity {
                Status = new DollStatusComponent {
                    HP_Current = 7,
                    HP_Max = 10,
                    SAN_Current = 8,
                    SAN_Max = 10
                }
            }
        };
    }

    private sealed class FakeNarrativeCommandSink : INarrativeCommandSink {
        public int VisualCount;
        public int CharacterCount;
        public int PrologueActionCount;
        public int StatusCardCount;
        public int UnlockUiCount;
        public int LayerConfirmCount;
        public int LastLayerID;
        public bool StartRunAtLayerCalled;

        public void PlayVisual(NarrativeVisualCommandRequest request) {
            VisualCount++;
        }

        public void ShowCharacter(NarrativeCharacterCommandRequest request) {
            CharacterCount++;
        }

        public void ShowPrologueAction(NarrativePrologueActionCommandRequest request) {
            PrologueActionCount++;
        }

        public void ShowStatusCard(NarrativeStatusCardCommandRequest request) {
            StatusCardCount++;
        }

        public void UnlockUi(NarrativeUnlockUiCommandRequest request) {
            UnlockUiCount++;
        }

        public void RequestLayerConfirm(NarrativeLayerConfirmCommandRequest request) {
            LayerConfirmCount++;
            LastLayerID = request.LayerID;
        }
    }
}
