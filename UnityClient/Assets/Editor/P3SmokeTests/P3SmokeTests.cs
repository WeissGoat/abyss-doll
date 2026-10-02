using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

// Runs the project's static smoke tests through the Unity Test Framework, so the Unity MCP run_tests
// tool can start them. A case is every class whose name ends in "Test" and has a static Run() (the set
// AutoTestDaemon's RUN_ALL_TESTS uses), plus every entry of the registered smoke sets, whose IDs
// (foundation, t0_core, p0_core) become the case categories. The framework fails a case on any error,
// exception or assert log, the same rule AutoTestDaemon applies.
public class P3SmokeTests {
    private const string SmokeSetsPath = "Editor/P3Validation/program_validation_profiles.json";

    private static IEnumerable<TestCaseData> SmokeCases() {
        Dictionary<string, List<string>> setsByEntry = SmokeSetsByEntry();
        var entries = new SortedSet<string>(setsByEntry.Keys, StringComparer.Ordinal);
        foreach (Type type in GameAssemblies().SelectMany(LoadableTypes)) {
            if (type.IsClass && type.Name.EndsWith("Test") && !type.Name.Contains("<") && FindMethod(type, "Run") != null) {
                entries.Add(type.FullName + ".Run");
            }
        }
        foreach (string entry in entries) {
            TestCaseData data = new TestCaseData(entry).SetName(entry);
            if (setsByEntry.TryGetValue(entry, out List<string> sets)) {
                foreach (string set in sets) data.SetCategory(set);
            }
            yield return data;
        }
    }

    [TestCaseSource(nameof(SmokeCases))]
    public void Smoke(string entry) {
        int split = entry.LastIndexOf('.');
        Assert.Greater(split, 0, "Smoke entry must be Type.Method: " + entry);
        Type type = GameAssemblies()
            .Select(assembly => assembly.GetType(entry.Substring(0, split)))
            .FirstOrDefault(found => found != null);
        Assert.IsNotNull(type, "Smoke type not found: " + entry);
        MethodInfo run = FindMethod(type, entry.Substring(split + 1));
        Assert.IsNotNull(run, "Smoke method not found: " + entry);
        try {
            run.Invoke(null, null);
        } catch (TargetInvocationException e) {
            ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
        }
    }

    private static Dictionary<string, List<string>> SmokeSetsByEntry() {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        string path = Path.Combine(Application.dataPath, SmokeSetsPath);
        if (!File.Exists(path)) return result;
        if (!(JObject.Parse(File.ReadAllText(path))["smoke_sets"] is JObject sets)) return result;
        foreach (JProperty set in sets.Properties()) {
            foreach (JToken item in set.Value) {
                string entry = (string)item;
                if (!result.TryGetValue(entry, out List<string> names)) {
                    names = new List<string>();
                    result[entry] = names;
                }
                names.Add(set.Name);
            }
        }
        return result;
    }

    private static IEnumerable<Assembly> GameAssemblies() {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name.StartsWith("Assembly-CSharp", StringComparison.Ordinal));
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.Where(type => type != null);
        }
    }

    private static MethodInfo FindMethod(Type type, string name) {
        return type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
    }
}
