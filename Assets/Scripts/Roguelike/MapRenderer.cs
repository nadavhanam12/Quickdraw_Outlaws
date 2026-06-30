using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapRenderer : MonoBehaviour
{
    public RectTransform mapContainer;

    const float COL_SPACING   = 185f;
    const float ROW_SPACING   = 78f;
    const float NODE_SIZE     = 46f;
    const float LINE_W        = 3.5f;
    const float LABEL_DY      = -40f;

    static readonly Color C_WEAK   = new Color(0.20f, 0.45f, 0.82f);
    static readonly Color C_NORMAL = new Color(0.88f, 0.48f, 0.10f);
    static readonly Color C_STRONG = new Color(0.80f, 0.18f, 0.18f);
    static readonly Color C_ELITE  = new Color(0.62f, 0.08f, 0.52f);

    static readonly Color LINE_GOLD   = new Color(1f, 0.82f, 0.18f, 1f);
    static readonly Color LINE_ACTIVE = new Color(0.72f, 0.72f, 0.72f, 0.72f);
    static readonly Color LINE_DIM    = new Color(0.30f, 0.30f, 0.30f, 0.28f);

    public event Action<int> OnColumnChosen;

    float ColX(int col) => (col - 1) * COL_SPACING;

    float RowY(int row, int total) => row * ROW_SPACING - (total - 1) * ROW_SPACING * 0.5f;

    public void DrawMap(MapData data, int currentFloor)
    {
        if (mapContainer == null) return;

        foreach (Transform child in mapContainer)
            Destroy(child.gameObject);

        int total = data.TotalRows;

        // ── Lines (behind nodes) ──────────────────────────────────────────────
        for (int row = 0; row < total - 1; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                Vector2 from = new Vector2(ColX(col), RowY(row, total));
                foreach (int nextCol in data.ConnectionsFrom(row, col))
                {
                    Vector2 to = new Vector2(ColX(nextCol), RowY(row + 1, total));
                    SpawnLine(from, to, LineColor(data, row, col, nextCol, currentFloor));
                }
            }
        }

        // ── Nodes (on top) ───────────────────────────────────────────────────
        for (int row = 0; row < total; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                Vector2 pos     = new Vector2(ColX(col), RowY(row, total));
                bool isClickable = row == currentFloor && data.CanChoose(currentFloor, col);
                SpawnNode(data.rows[row][col], pos, row, col, currentFloor, data, isClickable);
            }
        }
    }

    // ── Colour helpers ────────────────────────────────────────────────────────

    Color LineColor(MapData data, int row, int fromCol, int toCol, int currentFloor)
    {
        bool fromChosen = data.chosenColumns[row] == fromCol;
        bool toChosen   = row + 1 < data.TotalRows && data.chosenColumns[row + 1] == toCol;

        if (fromChosen && toChosen && row < currentFloor - 1)
            return LINE_GOLD;   // fully walked section

        if (fromChosen && row == currentFloor - 1)
            return LINE_GOLD;   // last step leading into current floor

        if (row < currentFloor && !fromChosen)
            return LINE_DIM;    // past, not taken

        if (row == currentFloor)
            return data.CanChoose(currentFloor, fromCol) ? LINE_ACTIVE : LINE_DIM;

        return LINE_DIM;        // future
    }

    static Color TierColor(PathData.EnemyTier t)
    {
        switch (t)
        {
            case PathData.EnemyTier.Weak:   return C_WEAK;
            case PathData.EnemyTier.Normal: return C_NORMAL;
            case PathData.EnemyTier.Strong: return C_STRONG;
            case PathData.EnemyTier.Elite:  return C_ELITE;
            default:                        return Color.gray;
        }
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    void SpawnLine(Vector2 from, Vector2 to, Color color)
    {
        var go  = new GameObject("Line");
        go.transform.SetParent(mapContainer, false);
        var rt  = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();

        Vector2 mid   = (from + to) * 0.5f;
        float   len   = Vector2.Distance(from, to);
        float   angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = mid;
        rt.sizeDelta        = new Vector2(len, LINE_W);
        rt.localRotation    = Quaternion.Euler(0, 0, angle);
        img.color           = color;
    }

    void SpawnNode(PathData path, Vector2 pos, int row, int col,
                   int currentFloor, MapData data, bool clickable)
    {
        bool isPast    = row < currentFloor;
        bool isFuture  = row > currentFloor;
        bool isChosen  = isPast && data.chosenColumns[row] == col;

        Color base_ = TierColor(path.enemyTier);
        Color fill;

        if (isChosen)
            fill = WithAlpha(base_ * 0.95f, 1f);
        else if (isPast)
            fill = WithAlpha(base_ * 0.22f, 0.45f);
        else if (clickable)
            fill = WithAlpha(base_ * 1.25f, 1f);
        else if (!isFuture)  // current floor but not reachable
            fill = WithAlpha(base_ * 0.28f, 0.4f);
        else                  // future
            fill = WithAlpha(base_ * 0.42f, 0.62f);

        // Diamond node
        var nodeGO = new GameObject($"N_{row}_{col}");
        nodeGO.transform.SetParent(mapContainer, false);
        var nRT = nodeGO.AddComponent<RectTransform>();
        nRT.anchorMin = nRT.anchorMax = new Vector2(0.5f, 0.5f);
        nRT.anchoredPosition = pos;
        nRT.sizeDelta = new Vector2(NODE_SIZE, NODE_SIZE);
        nRT.localRotation = Quaternion.Euler(0, 0, 45f);
        nodeGO.AddComponent<Image>().color = fill;

        // Glow outline
        if (clickable || isChosen)
        {
            var ol = nodeGO.AddComponent<Outline>();
            ol.effectColor    = isChosen
                ? new Color(1f, 0.82f, 0.18f, 0.9f)
                : new Color(1f, 1f, 0.55f, 1f);
            ol.effectDistance = new Vector2(3f, 3f);
        }

        // Label (un-rotated — parented to container directly)
        Color labelCol = (clickable || isChosen)
            ? Color.white
            : new Color(0.45f, 0.45f, 0.45f, isPast ? 0.45f : 0.6f);

        SpawnLabel(PathData.EnemyLabel(path.enemyTier),
                   new Vector2(pos.x, pos.y + LABEL_DY), labelCol, 13f);

        // Current floor: add transparent button overlay
        if (clickable)
        {
            var btnGO = new GameObject($"Btn_{col}");
            btnGO.transform.SetParent(mapContainer, false);
            var bRT = btnGO.AddComponent<RectTransform>();
            bRT.anchorMin = bRT.anchorMax = new Vector2(0.5f, 0.5f);
            bRT.anchoredPosition = pos;
            bRT.sizeDelta = new Vector2(NODE_SIZE * 2f, NODE_SIZE * 2f);
            btnGO.AddComponent<Image>().color = Color.clear;
            var btn = btnGO.AddComponent<Button>();
            int cap = col;
            btn.onClick.AddListener(() => OnColumnChosen?.Invoke(cap));
        }
    }

    void SpawnLabel(string text, Vector2 pos, Color color, float fontSize)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(mapContainer, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(130f, 26f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    static Color WithAlpha(Color c, float a) { c.a = a; return c; }
}
