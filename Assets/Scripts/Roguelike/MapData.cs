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
        if (floor == 0) return true;
        int prev = chosenColumns[floor - 1];
        if (prev < 0) return false;
        return connections[floor - 1][prev].Contains(col);
    }

    // Outgoing connections from (row, col) → row+1
    public List<int> ConnectionsFrom(int row, int col) => connections[row][col];

    // ── Connection generation ─────────────────────────────────────────────────

    public static List<int>[] GenerateConnections(System.Random rng)
    {
        var result  = new List<int>[] { new List<int>(), new List<int>(), new List<int>() };
        var covered = new bool[3];

        // Step 1: one primary connection per fromCol, keeping toCols non-decreasing
        // (non-decreasing = no crossings when fromCols are already sorted 0,1,2)
        int to0 = rng.Next(0, 2);                                       // 0 or 1
        int to2 = rng.Next(1, 3);                                       // 1 or 2
        int lo  = Mathf.Max(0, to0), hi = Mathf.Min(2, to2);
        int to1 = lo <= hi ? rng.Next(lo, hi + 1) : lo;                // clamped between to0 and to2

        result[0].Add(to0); covered[to0] = true;
        result[1].Add(to1); covered[to1] = true;
        result[2].Add(to2); covered[to2] = true;

        // Step 2: ensure every toCol is covered by at least one connection
        for (int to = 0; to < 3; to++)
        {
            if (covered[to]) continue;

            // Collect valid fromCols (adjacent, no crossings with existing edges)
            var candidates = new List<int>();
            for (int f = Mathf.Max(0, to - 1); f <= Mathf.Min(2, to + 1); f++)
            {
                if (!result[f].Contains(to) && NoCrossing(result, f, to))
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
                for (int t = Mathf.Max(0, f - 1); t <= Mathf.Min(2, f + 1); t++)
                    if (!result[f].Contains(t) && NoCrossing(result, f, t))
                        extras.Add((f, t));

            if (extras.Count > 0)
            {
                var e = extras[rng.Next(extras.Count)];
                result[e.f].Add(e.t);
            }
        }

        return result;
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
