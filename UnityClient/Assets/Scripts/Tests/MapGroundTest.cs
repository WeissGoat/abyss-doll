using System.Collections.Generic;
using UnityEngine;

// Route nodes stand on the terrain's ground (MapGround): nodes off the ground step onto it without piling up, and the
// ground's span ignores thin slivers at its edges.
public static class MapGroundTest {
    public static void Run() {
        Debug.Log("=== Running Map Ground Test ===");

        TestNodesStepOntoTheGround();
        TestSpanIgnoresSlivers();

        Debug.Log("=== Map Ground Test Finished ===");
    }

    // A 100x50 mask whose ground is a band in the lower half (rows 5-20), world 1000x500.
    private static MapGround Band(int sliverColumn = -1) {
        const int width = 100;
        const int height = 50;
        bool[] cells = new bool[width * height];
        for (int y = 5; y <= 20; y++) {
            for (int x = 10; x < 90; x++) {
                cells[y * width + x] = true;
            }
        }

        if (sliverColumn >= 0) {
            cells[30 * width + sliverColumn] = true;
        }

        return new MapGround(cells, width, height);
    }

    private static void TestNodesStepOntoTheGround() {
        MapGround ground = Band();
        Vector2 world = new Vector2(1000f, 500f);
        Dictionary<string, Vector2> positions = new Dictionary<string, Vector2> {
            ["on"] = new Vector2(0f, -100f),
            ["above_a"] = new Vector2(-200f, 150f),
            ["above_b"] = new Vector2(-200f, 140f),
            ["edge"] = new Vector2(480f, -100f)
        };

        ground.Settle(positions, world, 120f, 60f, 20f);
        bool allOnGround = true;
        foreach (Vector2 p in positions.Values) {
            allOnGround &= ground.IsGround(p.x / world.x + 0.5f, p.y / world.y + 0.5f);
        }

        bool apart = (positions["above_a"] - positions["above_b"]).magnitude >= 60f;
        bool kept = positions["on"] == new Vector2(0f, -100f);
        bool stayedInColumn = Mathf.Abs(positions["above_a"].x + 200f) <= 120f && Mathf.Abs(positions["above_b"].x + 200f) <= 120f;
        if (allOnGround && apart && kept && stayedInColumn) {
            Debug.Log("Map Ground Nodes Step Onto Ground PASSED.");
        } else {
            Debug.LogError($"Map Ground Nodes Step Onto Ground FAILED. OnGround={allOnGround}, Apart={apart}, Kept={kept}, Column={stayedInColumn}, a={positions["above_a"]}, b={positions["above_b"]}, edge={positions["edge"]}");
        }
    }

    private static void TestSpanIgnoresSlivers() {
        MapGround ground = Band(sliverColumn: 2);
        bool span = Mathf.Abs(ground.MinU - 0.105f) < 0.01f && Mathf.Abs(ground.MaxU - 0.895f) < 0.01f;
        bool center = Mathf.Abs(ground.CenterV(0.5f) - 0.26f) < 0.02f;
        if (span && center) {
            Debug.Log("Map Ground Span PASSED.");
        } else {
            Debug.LogError($"Map Ground Span FAILED. MinU={ground.MinU}, MaxU={ground.MaxU}, CenterV={ground.CenterV(0.5f)}");
        }
    }
}
