using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class NodeFactory {
    private static Dictionary<string, Type> _nodeTypes;

    public static void Initialize() {
        if (_nodeTypes != null) return;
        
        _nodeTypes = new Dictionary<string, Type>();
        
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(NodeBase)) && !t.IsAbstract);

        foreach (var type in types) {
            _nodeTypes[type.Name] = type;
            
            if (type.Name.EndsWith("Node")) {
                string shortName = type.Name.Substring(0, type.Name.Length - 4);
                _nodeTypes[shortName] = type;
            }
        }
        
        Debug.Log($"[NodeFactory] Initialized with {_nodeTypes.Count} node types mapped via Reflection.");
    }

    public static NodeBase CreateNode(string nodeTypeStr) {
        if (string.IsNullOrEmpty(nodeTypeStr)) return null;

        if (_nodeTypes == null) {
            Initialize();
        }

        if (_nodeTypes.TryGetValue(nodeTypeStr, out Type nodeType)) {
            NodeBase node = (NodeBase)Activator.CreateInstance(nodeType);
            return node;
        }

        Debug.LogWarning($"[NodeFactory] Unknown NodeType: {nodeTypeStr}. Could not find a matching class.");
        return null;
    }

    public static bool IsNodeTypeRegistered(string nodeTypeStr) {
        if (string.IsNullOrEmpty(nodeTypeStr)) {
            return false;
        }

        if (_nodeTypes == null) {
            Initialize();
        }

        return _nodeTypes.ContainsKey(nodeTypeStr);
    }
}
