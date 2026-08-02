using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCPForUnity.Editor.Tools;
using MCPForUnity.Editor.Services;
using UnityEngine;

public static class P3McpToolSchemaSmokeTest
{
    private sealed class Contract
    {
        public Type ToolType;
        public string ToolName;
        public ParameterContract[] Parameters;
        public bool HasParameters;
    }

    private sealed class ParameterContract
    {
        public string Name;
        public Type Type;
        public bool Required;
    }

    private static ParameterContract Parameter(string name, Type type, bool required = true)
    {
        return new ParameterContract { Name = name, Type = type, Required = required };
    }

    public static void Run()
    {
        var contracts = new[]
        {
            new Contract { ToolType = typeof(P3ArtRunProfileTool), ToolName = "p3_art_run_profile", Parameters = new[] { Parameter("action", typeof(string), false), Parameter("run_id", typeof(string)), Parameter("profile_id", typeof(string), false), Parameter("target_id", typeof(string), false), Parameter("target_ids", typeof(string[]), false), Parameter("instance_id", typeof(string), false), Parameter("review", typeof(object), false) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ArtOpenTargetTool), ToolName = "p3_art_open_target", Parameters = new[] { Parameter("target_id", typeof(string)) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ArtInspectTargetTool), ToolName = "p3_art_inspect_target", Parameters = new[] { Parameter("run_id", typeof(string)), Parameter("target_id", typeof(string)), Parameter("max_nodes", typeof(int), false) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ArtPrepareCaptureTool), ToolName = "p3_art_prepare_capture", Parameters = new[] { Parameter("run_id", typeof(string)), Parameter("profile_id", typeof(string)), Parameter("target_id", typeof(string)), Parameter("capture_role", typeof(string)), Parameter("iteration_id", typeof(string), false) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ArtFinalizeCaptureTool), ToolName = "p3_art_finalize_capture", Parameters = new[] { Parameter("run_id", typeof(string)), Parameter("capture_ticket_id", typeof(string)) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ArtCompareIterationTool), ToolName = "p3_art_compare_iteration", Parameters = new[] { Parameter("action", typeof(string), false), Parameter("change", typeof(object)) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ArtRunRegressionTool), ToolName = "p3_art_run_regression", Parameters = new[] { Parameter("run_id", typeof(string)), Parameter("instance_id", typeof(string), false) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ProgramRunProfileTool), ToolName = "p3_program_run_profile", Parameters = new[] { Parameter("run_id", typeof(string)), Parameter("profile_id", typeof(string), false), Parameter("instance_id", typeof(string), false) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ProgramRunSmokeTool), ToolName = "p3_program_run_smoke", Parameters = new[] { Parameter("run_id", typeof(string)), Parameter("profile_id", typeof(string), false) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ValidationCollectConsoleTool), ToolName = "p3_validation_collect_console", Parameters = new[] { Parameter("run_id", typeof(string)), Parameter("validation_domain", typeof(string)) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ValidationCollectEvidenceTool), ToolName = "p3_validation_collect_evidence", Parameters = new[] { Parameter("run_id", typeof(string)), Parameter("validation_domain", typeof(string)) }, HasParameters = true },
            new Contract { ToolType = typeof(P3ValidationReadinessTool), Parameters = new ParameterContract[0], ToolName = "p3_validation_readiness", HasParameters = false }
        };

        foreach (var contract in contracts)
        {
            var attribute = contract.ToolType.GetCustomAttributes(typeof(McpForUnityToolAttribute), false).Cast<McpForUnityToolAttribute>().SingleOrDefault();
            if (attribute == null || attribute.Name != contract.ToolName)
                throw new Exception("MCP tool attribute mismatch: " + contract.ToolName);

            var parametersType = contract.ToolType.GetNestedType("Parameters", BindingFlags.Public | BindingFlags.NonPublic);
            if (!contract.HasParameters)
            {
                if (parametersType != null)
                    throw new Exception("Unexpected Parameters type: " + contract.ToolName);
                continue;
            }

            if (parametersType == null)
                throw new Exception("Missing Parameters type: " + contract.ToolName);

            var members = parametersType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.MemberType == MemberTypes.Property || x.MemberType == MemberTypes.Field)
                .ToDictionary(x => x.Name, StringComparer.Ordinal);
            foreach (var parameter in contract.Parameters)
            {
                MemberInfo member;
                if (!members.TryGetValue(parameter.Name, out member))
                    throw new Exception("Missing parameter " + parameter.Name + " on " + contract.ToolName);
                var property = member as PropertyInfo;
                if (property == null || property.PropertyType != parameter.Type)
                    throw new Exception("Wrong parameter type " + parameter.Name + " on " + contract.ToolName);
                var attributes = member.GetCustomAttributes(typeof(ToolParameterAttribute), false).Cast<ToolParameterAttribute>().ToArray();
                if (attributes.Length != 1)
                    throw new Exception("Missing ToolParameter attribute " + parameter.Name + " on " + contract.ToolName);
                if (attributes[0].Required != parameter.Required)
                    throw new Exception("Wrong Required flag " + parameter.Name + " on " + contract.ToolName);
            }
        }

        var discovery = new ToolDiscoveryService();
        foreach (var contract in contracts)
        {
            var metadata = discovery.GetToolMetadata(contract.ToolName);
            if (metadata == null)
                throw new Exception("Tool discovery missing: " + contract.ToolName);
            if (metadata.Parameters == null || metadata.Parameters.Count != contract.Parameters.Length)
                throw new Exception("Tool discovery parameter count mismatch: " + contract.ToolName);
            foreach (var parameter in contract.Parameters)
            {
                var discovered = metadata.Parameters.SingleOrDefault(x => x.Name == parameter.Name);
                if (discovered == null)
                    throw new Exception("Tool discovery missing parameter " + parameter.Name + " on " + contract.ToolName);
                var expectedType = parameter.Type == typeof(string[]) ? "array" : parameter.Type == typeof(int) ? "integer" : parameter.Type == typeof(object) ? "object" : "string";
                if (discovered.Type != expectedType || discovered.Required != parameter.Required)
                    throw new Exception("Tool discovery mismatch " + parameter.Name + " on " + contract.ToolName);
            }
        }

        Debug.Log("P3 MCP Tool Schema PASSED");
    }
}
