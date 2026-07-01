using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapRenderer : MonoBehaviour
{
    public RectTransform mapContainer;
    public ScrollRect    scrollRect;

    [Header("Node Sprites")]
    public Sprite[] tierSprites; // 0=Weak, 1=Normal, 2=Strong, 3=Elite

    [Header("Map Background")]
    public Sprite mapBackgroundSprite;

    const float COL_SPACING = 240f;
    const float ROW_SPACING = 165f;
    const float NODE_SIZE   = 92f;
    const float LINE_W      = 5f;
    const float LABEL_DY    = -72f;

    // Dot pattern for future lines
    const float DOT_LEN  = 8f;
    const float DOT_GAP  = 10f;

    static readonly Color LINE_GOLD   = new Color(1f,    0.82f, 0.18f, 1f);
    static readonly Color LINE_ACTIVE = new Color(0.72f, 0.72f, 0.72f, 0.72f);
    static readonly Color LINE_DIM    = new Color(0.30f, 0.30f, 0.30f, 0.28f);

    public event Action<int> OnColumnChosen;

    float ColX(int col) => (col - 1) * COL_SPACING;
    float RowY(int row, int total) => row * ROW_SPACING - (total - 1) * ROW_SPACING * 0.5f;

    public void DrawMap(MapData data, int currentFloor)
    {
        if (mapContainer == null) return;
        foreach (Transform child in mapContainer) Destroy(child.gameObject);

        int total = data.TotalRows;

        // ── Background (behind everything, scrolls with content) ──────────────
        if (mapBackgroundSprite != null)
        {
            var bgGO = new GameObject("MapBackground");
            bgGO.transform.SetParent(mapContainer, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite        = mapBackgroundSprite;
            bgImg.type          = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;
        }

        // ── Lines (behind nodes) ──────────────────────────────────────────────
        for (int row = 0; row < total - 1; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (data.rows[row][col] == null) continue;
                Vector2 from = new Vector2(ColX(col), RowY(row, total));
                foreach (int nextCol in data.ConnectionsFrom(row, col))
                {
                    if (data.rows[row + 1][nextCol] == null) continue;
                    Vector2 to    = new Vector2(ColX(nextCol), RowY(row + 1, total));
                    Color   color = LineColor(data, row, col, nextCol, currentFloor);
                    SpawnLine(from, to, color, true);
                }
            }
        }

        // Auto-scroll so current floor is visible
        if (scrollRect != null && total > 1)
            scrollRect.verticalNormalizedPosition = (float)currentFloor / (total - 1);

        // ── Nodes (on top) ───────────────────────────────────────────────────
        for (int row = 0; row < total; row++)
            for (int col = 0; col < 3; col++)
            {
                if (data.rows[row][col] == null) continue;
                Vector2 pos         = new Vector2(ColX(col), RowY(row, total));
                bool    isClickable = row == currentFloor && data.CanChoose(currentFloor, col);
                SpawnNode(data.rows[row][col], pos, row, col, currentFloor, data, isClickable);
            }
    }

    // ── Colour helpers ────────────────────────────────────────────────────────

    Color LineColor(MapData data, int row, int fromCol, int toCol, int currentFloor)
    {
        bool fromChosen = data.chosenColumns[row] == fromCol;
        bool toChosen   = row + 1 < data.TotalRows && data.chosenColumns[row + 1] == toCol;

        if (fromChosen && toChosen && row < currentFloor - 1) return LINE_GOLD;
        if (fromChosen && row == currentFloor - 1)            return LINE_GOLD;
        if (row < currentFloor && !fromChosen)                return LINE_DIM;
        if (row == currentFloor)
            return data.CanChoose(currentFloor, fromCol) ? LINE_ACTIVE : LINE_DIM;
        return LINE_DIM;
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    void SpawnLine(Vector2 from, Vector2 to, Color color, bool dotted)
    {
        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        float len   = Vector2.Distance(from, to);

        if (!dotted)
        {
            // Solid line
            SpawnLineSegment((from + to) * 0.5f, new Vector2(len, LINE_W), angle, color);
            return;
        }

        // Dotted line — walk segments along the direction
        Vector2 dir    = (to - from).normalized;
        float   travel = 0f;
        float   step   = DOT_LEN + DOT_GAP;

        while (travel + DOT_LEN * 0.5f < len)
        {
            float segLen = Mathf.Min(DOT_LEN, len - travel);
            Vector2 centre = from + dir * (travel + segLen * 0.5f);
            SpawnLineSegment(centre, new Vector2(segLen, LINE_W), angle, color);
            travel += step;
        }
    }

    void SpawnLineSegment(Vector2 pos, Vector2 size, float angle, Color color)
    {
        var go  = new GameObject("LineSeg");
        go.transform.SetParent(mapContainer, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        rt.localRotation    = Quaternion.Euler(0, 0, angle);
        go.AddComponent<Image>().color = color;
    }

    void SpawnNode(PathData path, Vector2 pos, int row, int col,
                   int currentFloor, MapData data, bool clickable)
    {
        bool isPast   = row < currentFloor;
        bool isFuture = row > currentFloor;
        bool isChosen = isPast && data.chosenColumns[row] == col;

        // Alpha: chosen/clickable = full, past-not-chosen = dim, future = semi-dim
        float alpha = isChosen   ? 1.0f
                    : clickable  ? 1.0f
                    : isPast     ? 0.25f
                    : isFuture   ? 0.45f
                    : 0.30f; // current floor but unreachable

        // Node image
        var nodeGO = new GameObject($"N_{row}_{col}");
        nodeGO.transform.SetParent(mapContainer, false);
        var nRT = nodeGO.AddComponent<RectTransform>();
        nRT.anchorMin        = nRT.anchorMax = new Vector2(0.5f, 0.5f);
        nRT.anchoredPosition = pos;
        nRT.sizeDelta        = new Vector2(NODE_SIZE, NODE_SIZE);

        var img = nodeGO.AddComponent<Image>();
        img.preserveAspect = true;

        int sprIdx = (int)path.enemyTier;
        if (tierSprites != null && sprIdx < tierSprites.Length && tierSprites[sprIdx] != null)
        {
            img.sprite = tierSprites[sprIdx];
            img.color  = new Color(1f, 1f, 1f, alpha);
        }
        else
        {
            // Fallback: coloured diamond
            nRT.localRotation = Quaternion.Euler(0, 0, 45f);
            img.color = WithAlpha(TierColor(path.enemyTier), alpha);
        }

        // Glow outline for chosen / clickable
        if (clickable || isChosen)
        {
            var ol = nodeGO.AddComponent<Outline>();
            ol.effectColor    = isChosen
                ? new Color(1f, 0.82f, 0.18f, 0.9f)
                : new Color(1f, 1f, 0.55f, 1f);
            ol.effectDistance = new Vector2(4f, 4f);
        }

        // Label below node
        Color labelCol = (clickable || isChosen)
            ? Color.white
            : new Color(0.45f, 0.45f, 0.45f, isPast ? 0.45f : 0.6f);
        SpawnLabel(PathData.EnemyLabel(path.enemyTier),
                   new Vector2(pos.x, pos.y + LABEL_DY), labelCol, 17f);

        // Button overlay for clickable nodes
        if (clickable)
        {
            var btnGO = new GameObject($"Btn_{col}");
            btnGO.transform.SetParent(mapContainer, false);
            var bRT = btnGO.AddComponent<RectTransform>();
            bRT.anchorMin        = bRT.anchorMax = new Vector2(0.5f, 0.5f);
            bRT.anchoredPosition = pos;
            bRT.sizeDelta        = new Vector2(NODE_SIZE * 1.5f, NODE_SIZE * 1.5f);
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
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(180f, 32f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    static readonly Color C_WEAK   = new Color(0.20f, 0.45f, 0.82f);
    static readonly Color C_NORMAL = new Color(0.88f, 0.48f, 0.10f);
    static readonly Color C_STRONG = new Color(0.80f, 0.18f, 0.18f);
    static readonly Color C_ELITE  = new Color(0.62f, 0.08f, 0.52f);

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

    static Color WithAlpha(Color c, float a) { c.a = a; return c; }
}
