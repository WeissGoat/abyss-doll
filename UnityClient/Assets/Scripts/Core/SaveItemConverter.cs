using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Items in a save are the config ID plus what belongs to this one item: where it sits, who owns it, wear, corruption,
// binding, and tags or a price a monster stamped on it (a junk item's "Junk" tag and 0 value). Everything else (name,
// shape, AP cost, damage, effects, price) is rebuilt from the current config on load, so retuned numbers reach items
// already in a save. An item whose config ID no longer exists is dropped with a warning.
// Version 1 saves wrote whole items; they read through the same path (their template fields are ignored, and tags
// beyond the template's count as stamped, with the value that came with them).
public sealed class SaveItemConverter : JsonConverter<ItemEntity> {
    public int DroppedCount { get; private set; }

    public override void WriteJson(JsonWriter writer, ItemEntity item, JsonSerializer serializer) {
        if (item == null) {
            writer.WriteNull();
            return;
        }

        ConfigManager.Items.TryGetValue(item.ConfigID ?? string.Empty, out ItemEntity template);
        JObject record = new JObject {
            ["InstanceID"] = item.InstanceID,
            ["ConfigID"] = item.ConfigID,
            ["OwnerScope"] = item.OwnerScope.ToString(),
            ["ContainerType"] = item.ContainerType.ToString(),
            ["Durability"] = item.Durability,
            ["CorruptionLevel"] = item.CorruptionLevel,
            ["IsBinding"] = item.IsBinding
        };

        if (item.Grid != null) {
            record["Grid"] = new JObject {
                ["Rotation"] = item.Grid.Rotation,
                ["CurrentPos"] = item.Grid.CurrentPos != null ? new JArray(item.Grid.CurrentPos) : null
            };
        }

        if (item.DynamicTags != null && item.DynamicTags.Count > 0) {
            record["DynamicTags"] = new JArray(item.DynamicTags);
        }

        List<string> stamped = StampedTags(item.Tags, template?.Tags);
        if (stamped.Count > 0) {
            record["StampedTags"] = new JArray(stamped);
        }

        if (template == null || item.BaseValue != template.BaseValue) {
            record["StampedValue"] = item.BaseValue;
        }

        record.WriteTo(writer);
    }

    public override ItemEntity ReadJson(JsonReader reader, Type objectType, ItemEntity existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            return null;
        }

        JObject record = JObject.Load(reader);
        string configID = (string)record["ConfigID"];
        if (string.IsNullOrEmpty(configID) || !ConfigManager.Items.TryGetValue(configID, out ItemEntity template)) {
            DroppedCount++;
            Debug.LogWarning($"[SaveService] Item [{configID}] is no longer in the config; dropped from the save.");
            return null;
        }

        ItemEntity item = ConfigManager.CreateItem(configID);
        item.InstanceID = (string)record["InstanceID"] ?? item.InstanceID;
        item.OwnerScope = ReadEnum(record["OwnerScope"], item.OwnerScope);
        item.ContainerType = ReadEnum(record["ContainerType"], item.ContainerType);
        item.Durability = record["Durability"]?.Value<float>() ?? item.Durability;
        item.CorruptionLevel = record["CorruptionLevel"]?.Value<int>() ?? 0;
        item.IsBinding = record["IsBinding"]?.Value<bool>() ?? false;
        item.DynamicTags = record["DynamicTags"]?.ToObject<List<string>>() ?? new List<string>();

        if (item.Grid != null && record["Grid"] is JObject grid) {
            item.Grid.Rotation = grid["Rotation"]?.Value<int>() ?? 0;
            int[] pos = grid["CurrentPos"]?.Type == JTokenType.Array ? grid["CurrentPos"].ToObject<int[]>() : null;
            if (pos != null && pos.Length >= 2) {
                item.Grid.CurrentPos = pos;
            }
        }

        bool versionOne = record["StampedTags"] == null && record["Tags"] != null;
        List<string> stamped = versionOne
            ? StampedTags(record["Tags"].ToObject<List<string>>(), template.Tags)
            : record["StampedTags"]?.ToObject<List<string>>() ?? new List<string>();
        foreach (string tag in stamped) {
            item.Tags.Add(tag);
        }

        if (record["StampedValue"] != null) {
            item.BaseValue = record["StampedValue"].Value<int>();
        } else if (versionOne && stamped.Count > 0 && record["BaseValue"] != null) {
            item.BaseValue = record["BaseValue"].Value<int>();
        }

        return item;
    }

    private static List<string> StampedTags(List<string> tags, List<string> templateTags) {
        List<string> stamped = new List<string>();
        if (tags == null) {
            return stamped;
        }

        foreach (string tag in tags) {
            if (!string.IsNullOrEmpty(tag) && (templateTags == null || !templateTags.Contains(tag)) && !stamped.Contains(tag)) {
                stamped.Add(tag);
            }
        }

        return stamped;
    }

    private static T ReadEnum<T>(JToken token, T fallback) where T : struct {
        if (token == null) {
            return fallback;
        }

        if (token.Type == JTokenType.Integer) {
            return (T)Enum.ToObject(typeof(T), token.Value<int>());
        }

        return Enum.TryParse((string)token, true, out T value) ? value : fallback;
    }
}
