using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class NarrativeConfigValidationSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Narrative Config Validation Smoke Test ===");

        ConfigManager.LoadAllConfigs();
        ConfigValidationReport loadedReport = ConfigValidator.ValidateLoadedConfigs(false);
        bool loadedConfigValid = loadedReport.IsValid
            && ConfigManager.Narrative.Nodes.ContainsKey("T0_01A_Opening_CG")
            && ConfigManager.Narrative.Triggers.ContainsKey("t0_01a_new_game_opening")
            && ConfigManager.Narrative.Speakers.ContainsKey("no0")
            && ConfigManager.Narrative.Flags.ContainsKey("FirstDiveUnlocked")
            && ConfigManager.Narrative.Commands.ContainsKey("set_flag")
            && ConfigManager.Narrative.HasYarnNode("scripts/t0_01a_prologue.yarn", "T0_01A_Layer1Confirm");

        bool malformedConfigRejected = MalformedMissingNodeIsRejected()
            && MalformedMissingTriggerIsRejected()
            && MalformedIllegalCommandIsRejected()
            && MalformedMissingSpeakerIsRejected()
            && MalformedMissingVisualFallbackIsRejected();

        if (loadedConfigValid && malformedConfigRejected) {
            Debug.Log("Narrative Config Validation PASSED.");
        } else {
            loadedReport.LogSummary();
            Debug.LogError($"Narrative Config Validation FAILED. Loaded={loadedConfigValid}, MalformedRejected={malformedConfigRejected}");
        }

        Debug.Log("=== Narrative Config Validation Smoke Test Finished ===");
    }

    private static bool MalformedMissingNodeIsRejected() {
        NarrativeConfigDatabase database = BuildValidDatabase();
        database.Triggers["trigger"].YarnNode = "Missing_Node";
        return HasError(database, "references missing YarnNode [Missing_Node]");
    }

    private static bool MalformedMissingTriggerIsRejected() {
        NarrativeConfigDatabase database = BuildValidDatabase();
        database.Triggers.Clear();
        return HasError(database, "has no narrative trigger");
    }

    private static bool MalformedIllegalCommandIsRejected() {
        NarrativeConfigDatabase database = BuildValidDatabase("<<add_money 100>>");
        database.Nodes["T0_01A_Test"].CommandNames.Add("add_money");
        return HasError(database, "uses illegal command [add_money]");
    }

    private static bool MalformedMissingSpeakerIsRejected() {
        NarrativeConfigDatabase database = BuildValidDatabase("unknown_speaker: bad line #line:prologue.t0_01a.test.line_01");
        database.Nodes["T0_01A_Test"].SpeakerIDs.Add("unknown_speaker");
        return HasError(database, "references missing speaker [unknown_speaker]")
            && HasError(database, "Yarn line references missing speaker [unknown_speaker]");
    }

    private static bool MalformedMissingVisualFallbackIsRejected() {
        NarrativeConfigDatabase database = BuildValidDatabase();
        database.Nodes["T0_01A_Test"].VisualIntents[0].FallbackVisualID = string.Empty;
        return HasError(database, "has no fallback");
    }

    private static bool HasError(NarrativeConfigDatabase database, string expected) {
        ConfigValidationReport report = NarrativeConfigValidator.ValidateStandalone(database);
        foreach (string error in report.Errors) {
            if (error.Contains(expected)) {
                return true;
            }
        }

        return false;
    }

    private static NarrativeConfigDatabase BuildValidDatabase(string extraBodyLine = null) {
        string bodyLine = string.IsNullOrEmpty(extraBodyLine)
            ? "protagonist: test line #line:prologue.t0_01a.test.line_01"
            : extraBodyLine;

        return new NarrativeConfigDatabase {
            RootPath = Application.dataPath,
            Nodes = {
                ["T0_01A_Test"] = new NarrativeNodeConfig {
                    NodeID = "T0_01A_Test",
                    ScriptFile = "scripts/test.yarn",
                    SourceTable = "source_tables/test.dialogue.csv",
                    Context = "prologue",
                    ContentType = "modal",
                    ImplementationState = "locked_for_implementation",
                    SpeakerIDs = new List<string> { "protagonist" },
                    LineKeys = new List<string> { "prologue.t0_01a.test.line_01" },
                    CommandNames = new List<string> { "play_visual", "set_flag" },
                    VisualIntents = new List<NarrativeVisualIntentConfig> {
                        new NarrativeVisualIntentConfig {
                            VisualID = "cg_test",
                            FallbackVisualID = "ui_black_screen",
                            Required = true
                        }
                    }
                }
            },
            Triggers = {
                ["trigger"] = new NarrativeTriggerConfig {
                    TriggerID = "trigger",
                    EventType = "PrologueStarted",
                    YarnNode = "T0_01A_Test",
                    Priority = 100,
                    BlockingMode = "modal",
                    IfBusyPolicy = "queue_once",
                    Context = "prologue",
                    Once = true,
                    Chance = 1f,
                    ForbiddenFlags = new List<string> { "PrologueStarted" },
                    SetFlagsOnComplete = new List<string> { "PrologueStarted" }
                }
            },
            Speakers = {
                ["protagonist"] = new NarrativeSpeakerConfig {
                    SpeakerID = "protagonist",
                    DisplayNameKey = "speaker.protagonist",
                    FallbackVisualID = "ui_dialogue_speaker_fallback"
                }
            },
            Flags = {
                ["PrologueStarted"] = new NarrativeFlagConfig {
                    FlagID = "PrologueStarted",
                    Scope = "Save"
                }
            },
            Commands = {
                ["play_visual"] = new NarrativeCommandConfig {
                    CommandName = "play_visual",
                    MinArgs = 1,
                    MaxArgs = 4,
                    AllowedContexts = new List<string> { "prologue" }
                },
                ["set_flag"] = new NarrativeCommandConfig {
                    CommandName = "set_flag",
                    MinArgs = 1,
                    MaxArgs = 1,
                    AllowedContexts = new List<string> { "prologue" }
                }
            },
            ScriptSources = {
                ["scripts/test.yarn"] = "title: T0_01A_Test\n---\n<<play_visual cg_test>>\n" + bodyLine + "\n<<set_flag PrologueStarted>>\n==="
            },
            ScriptNodes = {
                ["scripts/test.yarn"] = new HashSet<string> { "T0_01A_Test" }
            },
            SourceTableLineKeys = {
                ["source_tables/test.dialogue.csv"] = new HashSet<string> { "prologue.t0_01a.test.line_01" }
            }
        };
    }
}
