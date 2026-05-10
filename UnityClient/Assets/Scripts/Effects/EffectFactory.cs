using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class EffectFactory {
    private static Dictionary<string, Type> _effectTypes;

    // Called once to scan all classes that inherit from EffectBase
    public static void Initialize() {
        if (_effectTypes != null) return;
        
        _effectTypes = new Dictionary<string, Type>();
        
        // Find all non-abstract subclasses of EffectBase
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(EffectBase)) && !t.IsAbstract);

        foreach (var type in types) {
            // Assume the class name matches the EffectID.
            // Example: If class is "DamageMultiplierEffect", the JSON ID can be "DamageMultiplierEffect" or "DamageMultiplier"
            _effectTypes[type.Name] = type;
            
            if (type.Name.EndsWith("Effect")) {
                string shortName = type.Name.Substring(0, type.Name.Length - 6);
                _effectTypes[shortName] = type;
            }
        }
        
        Debug.Log($"[EffectFactory] Initialized with {_effectTypes.Count} effect types mapped via Reflection.");
    }

    public static EffectBase CreateEffect(EffectData data) {
        if (data == null || string.IsNullOrEmpty(data.EffectID)) return null;

        if (_effectTypes == null) {
            Initialize();
        }

        if (_effectTypes.TryGetValue(data.EffectID, out Type effectType)) {
            // Instantiate dynamically without long switch/case blocks
            EffectBase effect = (EffectBase)Activator.CreateInstance(effectType);
            effect.Init(data);
            return effect;
        }

        Debug.LogWarning($"[EffectFactory] Unknown EffectID: {data.EffectID}. Could not find a matching class.");
        return null;
    }

    public static bool IsEffectRegistered(string effectID) {
        if (string.IsNullOrEmpty(effectID)) {
            return false;
        }

        if (_effectTypes == null) {
            Initialize();
        }

        return _effectTypes.ContainsKey(effectID);
    }
}
