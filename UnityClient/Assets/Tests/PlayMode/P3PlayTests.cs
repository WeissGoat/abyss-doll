using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

// Play Mode scenarios that need real frames (coroutines, timing, input order). The scenarios live in the game
// assembly as `public static IEnumerator Run()` on classes named *PlayScenario (Scripts/Tests/PlayMode), the same
// convention as the EditMode smoke tests; this runner finds them by reflection because an asmdef cannot reference
// Assembly-CSharp. A scenario fails by throwing or by logging an error.
public class P3PlayTests {
    [UnityTest]
    [Timeout(180000)]
    public IEnumerator Play([ValueSource(nameof(ScenarioNames))] string scenario) {
        Type type = GameTypes().FirstOrDefault(t => t.FullName == scenario);
        Assert.IsNotNull(type, "Scenario not found: " + scenario);
        return (IEnumerator)RunMethod(type).Invoke(null, null);
    }

    private static IEnumerable<string> ScenarioNames() {
        return GameTypes()
            .Where(type => type.IsClass && type.Name.EndsWith("PlayScenario") && RunMethod(type) != null)
            .Select(type => type.FullName)
            .OrderBy(name => name);
    }

    private static IEnumerable<Type> GameTypes() {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name.StartsWith("Assembly-CSharp", StringComparison.Ordinal))
            .SelectMany(assembly => {
                try {
                    return assembly.GetTypes();
                } catch (ReflectionTypeLoadException e) {
                    return e.Types.Where(type => type != null).ToArray();
                }
            });
    }

    private static MethodInfo RunMethod(Type type) {
        MethodInfo run = type.GetMethod("Run", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);
        return run != null && run.ReturnType == typeof(IEnumerator) ? run : null;
    }
}
