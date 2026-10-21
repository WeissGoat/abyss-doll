// The game's own rolls: run seeds, monster intents and targets, counter shuffles, loot. Kept apart from
// UnityEngine.Random so presentation code drawing from that (idle phases, VFX angles) cannot shift what the player
// gets; the balance simulation and the Play Mode scenarios seed it for repeatable runs.
public static class GameRandom {
    private static System.Random _random = new System.Random();

    public static void Seed(int seed) {
        _random = new System.Random(seed);
    }

    // Same contract as UnityEngine.Random.Range(int, int): max exclusive, min when the range is empty.
    public static int Range(int minInclusive, int maxExclusive) {
        return maxExclusive <= minInclusive ? minInclusive : _random.Next(minInclusive, maxExclusive);
    }
}
