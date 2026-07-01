using System.Collections.Generic;
using UnityEngine;

public class MapData
{
    public readonly PathData[][] rows;          // [row][col]
    public readonly int[] chosenColumns;        // -1 = not yet chosen
    public readonly List<int>[][] connections;  // connections[row][col] → list of cols in row+1

    public int TotalRows => rows.Length;

    public MapData(PathData[][] rows, List<int>[][] connections)
    {
        this.rows        = rows;
        this.connections = connections;
        chosenColumns    = new int[rows.Length];
        for (int i = 0; i < rows.Length; i++) chosenColumns[i] = -1;
    }

    // Can the player choose `col` at `floor`?
    public bool CanChoose(int floor, int col)
    {
        if (rows[floor][col] == null) return false;
        if (floor == 0) return true;
        int prev = chosenColumns[floor - 1];
        if (prev < 0) return false;
        return connections[floor - 1][prev].Contains(col);
    }

    // Outgoing connections from (row, col) → row+1
    public List<int> ConnectionsFrom(int row, int col) => connections[row][col];

    // ── Connection generation ─────────────────────────────────────────────────

    // For a single starting node at col=1 → connects to 2-3 cols in next row
    public static List<int>[] GenerateSingleSourceConnections(System.Random rng)
    {
        var result = new List<int>[] { new List<int>(), new List<int>(), new List<int>() };
        // Always include center, plus randomly left and/or right
        result[1].Add(1);
        if (rng.Next(2) == 0) result[1].Add(0);
        if (rng.Next(2) == 0) result[1].Add(2);
        // Ensure at least 2 targets for meaningful choice
        if (result[1].Count < 2)
            result[1].Add(rng.Next(2) == 0 ? 0 : 2);
        return result;
    }

    public static List<int>[] GenerateConnections(System.Random rng,
                                                   PathData[] fromRow = null,
                                                   PathData[] toRow   = null)
    {
        var result  = new List<int>[] { new List<int>(), new List<int>(), new List<int>() };
        var covered = new bool[3];

        // Determine which cols are active (non-null)
        bool[] fromActive = new bool[3];
        bool[] toActive   = new bool[3];
        for (int i = 0; i < 3; i++)
        {
            fromActive[i] = fromRow == null || fromRow[i] != null;
            toActive[i]   = toRow   == null || toRow[i]   != null;
        }

        // Step 1: one primary connection per active fromCol → nearest active toCol
        int to0 = FirstActiveTowards(toActive, rng.Next(0, 2));
        int to2 = FirstActiveTowards(toActive, rng.Next(1, 3));
        int lo  = Mathf.Max(0, to0 >= 0 ? to0 : 0);
        int hi  = Mathf.Min(2, to2 >= 0 ? to2 : 2);
        int to1 = lo <= hi ? rng.Next(lo, hi + 1) : lo;
        to1 = FirstActiveTowards(toActive, to1);

        if (fromActive[0] && to0 >= 0) { result[0].Add(to0); covered[to0] = true; }
        if (fromActive[1] && to1 >= 0) { result[1].Add(to1); covered[to1] = true; }
        if (fromActive[2] && to2 >= 0) { result[2].Add(to2); covered[to2] = true; }

        // Step 2: ensure every active toCol is covered
        for (int to = 0; to < 3; to++)
        {
            if (!toActive[to] || covered[to]) continue;
            var candidates = new List<int>();
            for (int f = Mathf.Max(0, to - 1); f <= Mathf.Min(2, to + 1); f++)
            {
                if (fromActive[f] && !result[f].Contains(to) && NoCrossing(result, f, to))
                    candidates.Add(f);
            }
            if (candidates.Count > 0)
            {
                int from = candidates[rng.Next(candidates.Count)];
                result[from].Add(to);
                covered[to] = true;
            }
        }

        // Step 3: optionally add one extra branch (67% chance)
        if (rng.Next(3) != 0)
        {
            var extras = new List<(int f, int t)>();
            for (int f = 0; f < 3; f++)
                if (fromActive[f])
                    for (int t = Mathf.Max(0, f - 1); t <= Mathf.Min(2, f + 1); t++)
                        if (toActive[t] && !result[f].Contains(t) && NoCrossing(result, f, t))
                            extras.Add((f, t));
            if (extras.Count > 0)
            {
                var e = extras[rng.Next(extras.Count)];
                result[e.f].Add(e.t);
            }
        }

        return result;
    }

    static int FirstActiveTowards(bool[] active, int preferred)
    {
        preferred = Mathf.Clamp(preferred, 0, 2);
        if (active[preferred]) return preferred;
        for (int d = 1; d <= 2; d++)
        {
            if (preferred - d >= 0 && active[preferred - d]) return preferred - d;
            if (preferred + d <= 2 && active[preferred + d]) return preferred + d;
        }
        return -1; // no active toCol
    }

    static bool NoCrossing(List<int>[] existing, int newFrom, int newTo)
    {
        for (int f = 0; f < existing.Length; f++)
        {
            if (f == newFrom) continue;
            foreach (int t in existing[f])
            {
                if (f < newFrom && t > newTo) return false;
                if (f > newFrom && t < newTo) return false;
            }
        }
        return true;
    }
}
