using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screens")]
    public GameObject mapScreen;
    public GameObject combatScreen;
    public GameObject lootScreen;
    public GameObject gameOverScreen;

    [Header("Map UI")]
    public MapRenderer mapRenderer;
    public TextMeshProUGUI mapGoldText;
    public TextMeshProUGUI mapFloorText;

    [Header("Combat UI")]
    public TextMeshProUGUI playerActionText;
    public TextMeshProUGUI enemyActionText;
    public Image           playerActionIcon;
    public Image           enemyActionIcon;
    public Sprite[]        actionSprites; // 0=Fire, 1=Defend, 2=Reload (matches CombatAction enum)
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI playerBulletsText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI enemyBulletsText;
    public TextMeshProUGUI selectedActionText;
    public Button fireButton;
    public Button defendButton;
    public Button reloadButton;
    public TextMeshProUGUI blockUsesText;
    public TextMeshProUGUI combatLogText;
    public BattleVisuals battleVisuals;

    [Header("Combat Animation")]
    public Image     battleBackground;
    public Sprite[]  battleBackgroundSprites;
    public Image shieldOverlay;
    public Image enemyShieldOverlay;
    public Transform playerAmmoDisplay;
    public Transform enemyAmmoDisplay;

    [Header("Transitions")]
    public Image transitionOverlay;

    [Header("Aim Icons")]
    public Sprite[] aimRankSprites; // 0 = bad (0-1), 1 = medium (2-3), 2 = good (4-5)

    [Header("Bullet FX")]
    public Image         playerBulletImage;
    public Image         enemyBulletImage;
    public RectTransform playerGunTipMarker;
    public RectTransform enemyGunTipMarker;
    public RectTransform playerHitMarker;
    public RectTransform enemyHitMarker;

    [Header("Loot UI")]
    public TextMeshProUGUI lootTitleText;
    public TextMeshProUGUI lootInfoText;
    public Button[] upgradeButtons;
    public TextMeshProUGUI[] upgradeButtonTexts;
    public Sprite[] upgradeIcons; // index = upgrade id - 1 (id 1 → [0], id 27 → [26])

    [Header("Game Over UI")]
    public TextMeshProUGUI gameOverText;
    public Button restartButton;

    private List<string> logLines = new List<string>();
    private bool battleEnded;
    private bool _animating;

    void Awake() => Instance = this;

    void Start()
    {
        if (mapRenderer != null)
            mapRenderer.OnColumnChosen += col => GameManager.Instance.StartBattle(col);

        fireButton.onClick.AddListener(() => OnActionButton(CombatAction.Fire));
        defendButton.onClick.AddListener(() => OnActionButton(CombatAction.Defend));
        reloadButton.onClick.AddListener(() => OnActionButton(CombatAction.Reload));

        restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
    }

    // ── Screens ───────────────────────────────────────────────────────────────

    public void ShowMap()
    {
        UnsubscribeCombat();
        SetScreen(mapScreen);
        var gm = GameManager.Instance;
        if (mapGoldText  != null) mapGoldText.text  = $"Gold: {gm.Player.gold}";
        if (mapFloorText != null) mapFloorText.text = $"Battle {gm.CurrentFloor + 1} / {GameManager.MAP_ROWS}";
        if (mapRenderer  != null) mapRenderer.DrawMap(gm.CurrentMap, gm.CurrentFloor);
    }

    public void ShowCombat()
    {
        SetScreen(combatScreen);
        battleEnded = false;
        _animating  = false;
        logLines.Clear();

        if (combatLogText      != null) combatLogText.text      = "";
        if (selectedActionText != null) selectedActionText.text = "Select an action";
        if (playerActionText   != null) { playerActionText.text = ""; playerActionText.transform.localScale = Vector3.one; }
        if (enemyActionText    != null) { enemyActionText.text  = ""; enemyActionText.transform.localScale  = Vector3.one; }
        if (playerActionIcon   != null) { playerActionIcon.sprite = null; playerActionIcon.enabled = false; playerActionIcon.transform.localScale = Vector3.one; }
        if (enemyActionIcon    != null) { enemyActionIcon.sprite  = null; enemyActionIcon.enabled  = false; enemyActionIcon.transform.localScale  = Vector3.one; }
        if (battleBackground != null && battleBackgroundSprites != null && battleBackgroundSprites.Length > 0)
            battleBackground.sprite = battleBackgroundSprites[Random.Range(0, battleBackgroundSprites.Length)];

        // Reset sprite alphas that may have been zeroed by the end-battle fade
        if (battleVisuals?.playerSprite != null) { var c = battleVisuals.playerSprite.color; c.a = 1f; battleVisuals.playerSprite.color = c; }
        if (battleVisuals?.enemySprite  != null) { var c = battleVisuals.enemySprite.color;  c.a = 1f; battleVisuals.enemySprite.color  = c; }
        // Reset action text color in case it was tinted by the victory/defeat card
        if (playerActionText != null) playerActionText.color = Color.white;

        if (shieldOverlay       != null) shieldOverlay.gameObject.SetActive(false);
        if (enemyShieldOverlay  != null) enemyShieldOverlay.gameObject.SetActive(false);
        if (playerBulletImage  != null) playerBulletImage.gameObject.SetActive(false);
        if (enemyBulletImage   != null) enemyBulletImage.gameObject.SetActive(false);

        SetActionButtonsInteractable(true);

        if (battleVisuals != null && GameManager.Instance.LastPath != null)
            battleVisuals.SetEnemyTier(GameManager.Instance.LastPath.enemyTier);

        var cm = CombatManager.Instance;
        cm.OnCombatLog    += AddLog;
        cm.OnTurnResolved += RefreshCombatUI;
        cm.OnBattleEnd    += OnBattleEndUI;

        RefreshCombatUI();
    }

    public void ShowLoot()
    {
        UnsubscribeCombat();
        SetScreen(lootScreen);
        var gm = GameManager.Instance;
        string tierName = PathData.EnemyLabel(gm.LastPath.enemyTier);
        if (lootTitleText != null) lootTitleText.text = $"LOOT  —  {tierName} Enemy";
        if (lootInfoText  != null) lootInfoText.text  = $"+{gm.LastLootHp} HP  →  {gm.Player.hp}/{gm.Player.maxHp}    +{gm.LastLootGold} Gold  (Total: {gm.Player.gold})";
        var upgrades = gm.CurrentUpgrades;
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (i < upgrades.Length)
            {
                var u = upgrades[i];
                upgradeButtonTexts[i].text = $"<b>{u.name}</b>\n{u.description}";
                upgradeButtons[i].gameObject.SetActive(true);
                int id = u.id;
                upgradeButtons[i].onClick.RemoveAllListeners();
                upgradeButtons[i].onClick.AddListener(() => GameManager.Instance.ApplyUpgrade(id));
                if (upgradeIcons != null)
                {
                    int iconIdx = u.id - 1;
                    var iconImg = GetOrCreateButtonIcon(upgradeButtons[i]);
                    iconImg.sprite  = (iconIdx < upgradeIcons.Length) ? upgradeIcons[iconIdx] : null;
                    iconImg.enabled = iconImg.sprite != null;
                }
            }
            else upgradeButtons[i].gameObject.SetActive(false);
        }
    }

    public void ShowGameOver()
    {
        UnsubscribeCombat();
        SetScreen(gameOverScreen);
        var gm = GameManager.Instance;
        bool won = gm.CurrentFloor >= GameManager.MAP_ROWS;
        gameOverText.text = won ? "YOU WON!\nThe Outlaw rides into the sunset" : "YOU DIED\nBetter luck next time";
    }

    // ── Combat helpers ────────────────────────────────────────────────────────

    void OnActionButton(CombatAction action)
    {
        if (battleEnded || _animating) return;
        SetActionButtonsInteractable(false);
        StartCoroutine(TurnSequence(action));
    }

    void OnBattleEndUI(bool playerWon)
    {
        battleEnded = true;
        SetActionButtonsInteractable(false);
        StartCoroutine(BattleEndSequence(playerWon));
    }

    IEnumerator BattleEndSequence(bool playerWon)
    {
        Image loserSpr  = playerWon ? battleVisuals?.enemySprite  : battleVisuals?.playerSprite;
        Image winnerSpr = playerWon ? battleVisuals?.playerSprite : battleVisuals?.enemySprite;

        // 1 — big impact burst on the loser
        if (loserSpr != null)
        {
            Vector2 loserPos = loserSpr.rectTransform.anchoredPosition;
            StartCoroutine(ImpactBurst(loserPos, new Color(1f, 0.75f, 0.2f)));
            StartCoroutine(ImpactBurst(loserPos + new Vector2(0f, -40f), new Color(0.6f, 0.6f, 0.6f))); // dust
        }

        yield return new WaitForSeconds(0.15f);

        // 2 — white flash
        var flashGo = new GameObject("EndFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        flashGo.transform.SetParent(combatScreen.transform, false);
        var flashRt  = flashGo.GetComponent<RectTransform>();
        flashRt.anchorMin = Vector2.zero; flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = flashRt.offsetMax = Vector2.zero;
        var flashImg = flashGo.GetComponent<Image>();
        flashImg.color = new Color(1f, 1f, 1f, 0.85f);
        yield return null;
        flashImg.color = Color.clear;
        Destroy(flashGo);

        // 3 — brief freeze
        yield return new WaitForSeconds(0.35f);

        // 4 — loser shakes then fades out
        if (loserSpr != null)
        {
            StartCoroutine(Shake(loserSpr.rectTransform, 0.45f, 18f));
            yield return new WaitForSeconds(0.45f);
            float fadeDur = 0.4f;
            Color c = loserSpr.color;
            for (float f = 0; f < fadeDur; f += Time.deltaTime)
            {
                c.a = 1f - f / fadeDur;
                loserSpr.color = c;
                yield return null;
            }
            c.a = 0f; loserSpr.color = c;
        }

        // 5 — winner bounces
        if (winnerSpr != null)
            StartCoroutine(BounceScale(winnerSpr.transform, 0.4f));

        yield return new WaitForSeconds(0.25f);

        // 6 — WIN / DEFEAT card punches in
        if (playerActionText != null)
        {
            playerActionText.text = playerWon ? "VICTORY!" : "DEFEAT";
            playerActionText.color = playerWon ? new Color(1f, 0.88f, 0.2f) : new Color(1f, 0.25f, 0.2f);
            playerActionText.transform.localScale = Vector3.zero;
            StartCoroutine(ScalePunch(playerActionText.transform, 0.4f));
        }

        yield return new WaitForSeconds(1.2f);
        // GameManager.HandleBattleEnd is already subscribed to OnBattleEnd and will transition screens
    }

    void RefreshCombatUI()
    {
        if (_animating) return;
        var cm = CombatManager.Instance;
        var p  = cm.Player;
        var e  = cm.Enemy;
        if (p == null || e == null) return;

        if (playerHpText      != null) playerHpText.text      = $"{p.hp} / {p.maxHp}";
        if (playerBulletsText != null) playerBulletsText.text = $"Bullets: {p.bullets} / {p.maxBullets}";
        if (enemyHpText       != null) enemyHpText.text       = $"{e.hp} / {e.maxHp}";
        if (enemyBulletsText  != null) enemyBulletsText.text  = $"Bullets: {e.bullets} / {e.maxBullets}";
        if (blockUsesText     != null) blockUsesText.text     = $"Shield: {p.blockUses} / {p.maxBlockUses}";

        fireButton.interactable   = !battleEnded;
        defendButton.interactable = !battleEnded && p.blockUses > 0;
        battleVisuals?.Refresh(p, e);
    }

    void AddLog(string msg)
    {
        logLines.Add(msg);
        if (logLines.Count > 12) logLines.RemoveAt(0);
        if (combatLogText != null) combatLogText.text = string.Join("\n", logLines);
    }

    void SetActionButtonsInteractable(bool value)
    {
        fireButton.interactable   = value;
        reloadButton.interactable = value;
        if (!value) defendButton.interactable = false;
    }

    Image GetOrCreateButtonIcon(Button btn)
    {
        var t = btn.transform.Find("UpgradeIcon");
        if (t != null) return t.GetComponent<Image>();

        var go  = new GameObject("UpgradeIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(btn.transform, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0.5f);
        rt.anchorMax        = new Vector2(0f, 0.5f);
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(8f, 0f);
        rt.sizeDelta        = new Vector2(48f, 48f);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    void SetScreen(GameObject screen) => StartCoroutine(SlideTransitionTo(screen));

    IEnumerator TransitionTo(GameObject screen)
    {
        if (transitionOverlay == null)
        {
            SwitchScreen(screen);
            yield break;
        }

        // 1 — white flash (1 frame)
        transitionOverlay.color = new Color(1f, 1f, 1f, 0.9f);
        yield return null;

        // 2 — fade to black (0.2s)
        float fadeIn = 0.2f;
        for (float f = 0; f < fadeIn; f += Time.deltaTime)
        {
            float t = f / fadeIn;
            transitionOverlay.color = new Color(Mathf.Lerp(1f, 0f, t), Mathf.Lerp(1f, 0f, t), Mathf.Lerp(1f, 0f, t), 1f);
            yield return null;
        }
        transitionOverlay.color = Color.black;

        // 3 — swap screen at peak black
        SwitchScreen(screen);

        // 4 — fade back in (0.25s)
        float fadeOut = 0.25f;
        for (float f = 0; f < fadeOut; f += Time.deltaTime)
        {
            transitionOverlay.color = new Color(0f, 0f, 0f, 1f - f / fadeOut);
            yield return null;
        }
        transitionOverlay.color = new Color(0f, 0f, 0f, 0f);
    }

    IEnumerator SlideTransitionTo(GameObject nextScreen)
    {
        const float width = 1280f;
        const float dur   = 0.3f;

        // Find currently visible screen
        GameObject current = null;
        foreach (var s in new[] { mapScreen, combatScreen, lootScreen, gameOverScreen })
            if (s != null && s.activeSelf && s != nextScreen) { current = s; break; }

        // Bring next screen in from the right, hidden at start
        nextScreen.SetActive(true);
        var nextRt    = nextScreen.GetComponent<RectTransform>();
        var currentRt = current?.GetComponent<RectTransform>();

        Vector2 nextStart    = new Vector2( width, 0f);
        Vector2 nextEnd      = Vector2.zero;
        Vector2 currentStart = Vector2.zero;
        Vector2 currentEnd   = new Vector2(-width, 0f);

        if (nextRt)    nextRt.anchoredPosition    = nextStart;
        if (currentRt) currentRt.anchoredPosition = currentStart;

        for (float f = 0; f < dur; f += Time.deltaTime)
        {
            float t = 1f - Mathf.Pow(1f - f / dur, 3f); // ease-out cubic
            if (nextRt)    nextRt.anchoredPosition    = Vector2.Lerp(nextStart,    nextEnd,    t);
            if (currentRt) currentRt.anchoredPosition = Vector2.Lerp(currentStart, currentEnd, t);
            yield return null;
        }

        if (nextRt)    nextRt.anchoredPosition    = nextEnd;
        if (currentRt) currentRt.anchoredPosition = currentStart; // reset before hiding

        if (current != null) current.SetActive(false);

        // Hide other screens too
        foreach (var s in new[] { mapScreen, combatScreen, lootScreen, gameOverScreen })
            if (s != null && s != nextScreen) s.SetActive(false);
    }

    IEnumerator IrisWipeTo(GameObject screen)
    {
        // Spawn a circle that expands to cover the screen
        var go = new GameObject("Iris", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transitionOverlay != null ? transitionOverlay.transform.parent : combatScreen.transform.parent, false);
        go.transform.SetAsLastSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = Vector2.one * 0.5f;
        rt.anchoredPosition = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        // Assign circle sprite if available, otherwise the square covers fine at full black
        var circleSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        if (circleSprite != null) img.sprite = circleSprite;

        float expandDur = 0.32f;
        float maxSize   = 1600f; // covers 1280x720 diagonally
        for (float f = 0; f < expandDur; f += Time.deltaTime)
        {
            float t = f / expandDur;
            float ease = t * t;
            rt.sizeDelta = Vector2.one * Mathf.Lerp(0f, maxSize, ease);
            yield return null;
        }
        rt.sizeDelta = Vector2.one * maxSize;

        SwitchScreen(screen);

        float shrinkDur = 0.35f;
        for (float f = 0; f < shrinkDur; f += Time.deltaTime)
        {
            float t = f / shrinkDur;
            float ease = 1f - (1f - t) * (1f - t);
            rt.sizeDelta = Vector2.one * Mathf.Lerp(maxSize, 0f, ease);
            yield return null;
        }

        Destroy(go);
    }

    void SwitchScreen(GameObject screen)
    {
        if (mapScreen)      mapScreen.SetActive(false);
        if (combatScreen)   combatScreen.SetActive(false);
        if (lootScreen)     lootScreen.SetActive(false);
        if (gameOverScreen) gameOverScreen.SetActive(false);
        screen?.SetActive(true);
    }

    void UnsubscribeCombat()
    {
        var cm = CombatManager.Instance;
        if (cm == null) return;
        cm.OnCombatLog    -= AddLog;
        cm.OnTurnResolved -= RefreshCombatUI;
        cm.OnBattleEnd    -= OnBattleEndUI;
    }

    void SetActionIcon(Image icon, CombatAction action)
    {
        if (icon == null || actionSprites == null) return;
        int idx = (int)action; // Fire=0, Defend=1, Reload=2
        if (idx >= actionSprites.Length) return;
        icon.sprite  = actionSprites[idx];
        icon.enabled = icon.sprite != null;
        icon.transform.localScale = icon.enabled ? Vector3.zero : Vector3.one;
    }

    static string ActionLabel(CombatAction action) => action switch
    {
        CombatAction.Fire   => "BANG!",
        CombatAction.Reload => "RELOADING...",
        CombatAction.Defend => "DEFENDING!",
        _                   => ""
    };

    // ── Turn sequence ─────────────────────────────────────────────────────────

    IEnumerator TurnSequence(CombatAction playerAction)
    {
        _animating = true;
        var cm = CombatManager.Instance;

        cm.DecideActions(playerAction);
        var pAction = cm.LastPlayerAction.Value;
        var eAction = cm.LastEnemyAction.Value;

        // ── Phase 1: Action icons/texts pop in ────────────────────────────
        if (playerActionText != null) { playerActionText.text = ActionLabel(pAction); playerActionText.transform.localScale = Vector3.zero; }
        if (enemyActionText  != null) { enemyActionText.text  = ActionLabel(eAction); enemyActionText.transform.localScale  = Vector3.zero; }
        if (selectedActionText != null) selectedActionText.text = $"► {ActionLabel(pAction)}";
        SetActionIcon(playerActionIcon, pAction);
        SetActionIcon(enemyActionIcon,  eAction);

        StartCoroutine(ScalePunch(playerActionText?.transform));
        StartCoroutine(ScalePunch(enemyActionText?.transform));
        StartCoroutine(ScalePunch(playerActionIcon?.transform));
        StartCoroutine(ScalePunch(enemyActionIcon?.transform));
        yield return new WaitForSeconds(0.45f);

        // ── Phase 2: Action animations ────────────────────────────────────
        var pSpr = battleVisuals?.playerSprite;
        var eSpr = battleVisuals?.enemySprite;

        bool pFires = pAction == CombatAction.Fire;
        bool eFires = eAction == CombatAction.Fire;

        // Fire: muzzle → bullet → impact/deflect + shake (sequenced sub-coroutine)
        if (pFires || eFires)
            StartCoroutine(FireFX(pFires, eFires, pSpr, eSpr,
                pAction == CombatAction.Defend, eAction == CombatAction.Defend));

        // Defend: shield overlay + blue flash
        if (pAction == CombatAction.Defend)
        {
            if (shieldOverlay != null) StartCoroutine(ShowShieldOverlay(shieldOverlay));
            if (pSpr != null) StartCoroutine(FlashColor(pSpr, new Color(0.35f, 0.65f, 1f), 0.55f));
        }
        if (eAction == CombatAction.Defend)
        {
            if (enemyShieldOverlay != null) StartCoroutine(ShowShieldOverlay(enemyShieldOverlay));
            if (eSpr != null) StartCoroutine(FlashColor(eSpr, new Color(0.35f, 0.65f, 1f), 0.55f));
        }

        // Reload: character shake + gold flash + bullet overshoot bounce
        if (pAction == CombatAction.Reload && battleVisuals?.playerBulletIcons != null)
        {
            if (pSpr != null) StartCoroutine(ReloadCharacterFX(pSpr));
            int idx  = Mathf.Clamp(cm.Player.bullets, 0, battleVisuals.playerBulletIcons.Length - 1);
            var icon = battleVisuals.playerBulletIcons[idx];
            icon.gameObject.SetActive(true);
            StartCoroutine(ReloadBulletFX(icon, battleVisuals.bulletFilledSprite));
        }
        if (eAction == CombatAction.Reload && battleVisuals?.enemyBulletIcons != null)
        {
            if (eSpr != null) StartCoroutine(ReloadCharacterFX(eSpr));
            int idx  = Mathf.Clamp(cm.Enemy.bullets, 0, battleVisuals.enemyBulletIcons.Length - 1);
            var icon = battleVisuals.enemyBulletIcons[idx];
            icon.gameObject.SetActive(true);
            StartCoroutine(ReloadBulletFX(icon, battleVisuals.bulletFilledSprite));
        }

        // Wait just long enough for the active animations to finish
        float phase2Wait = (pFires || eFires) ? 2.0f : 0.65f;
        yield return new WaitForSeconds(phase2Wait);

        // ── Phase 3: Apply + show results ─────────────────────────────────
        int prePlayerHp = cm.Player.hp;
        int preEnemyHp  = cm.Enemy.hp;

        cm.ApplyTurn();

        int playerDmgTaken = Mathf.Max(0, prePlayerHp - cm.Player.hp);
        int enemyDmgTaken  = Mathf.Max(0, preEnemyHp  - cm.Enemy.hp);

        if (playerDmgTaken > 0 && battleVisuals?.playerSprite != null)
            StartCoroutine(FloatDamageText($"-{playerDmgTaken}", battleVisuals.playerSprite.rectTransform.anchoredPosition, new Color(1f, 0.25f, 0.2f)));
        if (enemyDmgTaken > 0 && battleVisuals?.enemySprite != null)
            StartCoroutine(FloatDamageText($"-{enemyDmgTaken}", battleVisuals.enemySprite.rectTransform.anchoredPosition, new Color(1f, 0.25f, 0.2f)));

        if (playerHpText  != null) playerHpText.text  = $"{cm.Player.hp} / {cm.Player.maxHp}";
        if (enemyHpText   != null) enemyHpText.text   = $"{cm.Enemy.hp} / {cm.Enemy.maxHp}";
        if (blockUsesText != null) blockUsesText.text = $"Shield: {cm.Player.blockUses} / {cm.Player.maxBlockUses}";
        battleVisuals?.Refresh(cm.Player, cm.Enemy);

        yield return new WaitForSeconds(0.25f);

        _animating = false;
        if (!battleEnded)
        {
            SetActionButtonsInteractable(true);
            RefreshCombatUI(); // re-evaluates defend button against current blockUses
        }
    }

    // ── Fire FX sub-sequence ──────────────────────────────────────────────────

    IEnumerator FireFX(bool pFires, bool eFires, Image pSpr, Image eSpr, bool pDefends, bool eDefends)
    {
        // Gun tips: where shots originate
        Vector2 pTip = playerGunTipMarker != null ? playerGunTipMarker.anchoredPosition : new Vector2(-120f, -125f);
        Vector2 eTip = enemyGunTipMarker  != null ? enemyGunTipMarker.anchoredPosition  : new Vector2( 120f,  120f);

        // Hit markers: where bullets land on each target
        Vector2 pHit = playerHitMarker != null ? playerHitMarker.anchoredPosition : pTip;
        Vector2 eHit = enemyHitMarker  != null ? enemyHitMarker.anchoredPosition  : eTip;

        // Anticipation tilt: lean back, then snap forward on fire
        if (pFires && pSpr != null) StartCoroutine(FireAnticipation(pSpr.rectTransform,  28f, -5f, -70f));
        if (eFires && eSpr != null) StartCoroutine(FireAnticipation(eSpr.rectTransform,  28f,  5f,  70f));

        yield return new WaitForSeconds(1.0f); // lean-back phase

        // Snap moment: muzzle flash + bullet + aim popup all fire together
        var cm2 = CombatManager.Instance;
        if (pFires)
        {
            StartCoroutine(MuzzleFlash(pTip));
            if (pSpr) StartCoroutine(FlashColor(pSpr, new Color(1f, 0.55f, 0.15f)));
            StartCoroutine(BulletFly(playerBulletImage, pTip, eHit));
            StartCoroutine(AimBonusPopup(cm2.LastPlayerAimBonus, pTip));
        }
        if (eFires)
        {
            StartCoroutine(MuzzleFlash(eTip));
            if (eSpr) StartCoroutine(FlashColor(eSpr, new Color(1f, 0.55f, 0.15f)));
            StartCoroutine(BulletFly(enemyBulletImage, eTip, pHit));
            StartCoroutine(AimBonusPopup(cm2.LastEnemyAimBonus, eTip));
        }

        yield return new WaitForSeconds(0.3f);

        // Impact or deflect at the hit marker position
        var impactColor = new Color(1f, 0.75f, 0.2f);
        if (pFires)
        {
            if (eDefends) StartCoroutine(DeflectBurst(eHit, pTip - eHit, enemyShieldOverlay));
            else          { StartCoroutine(ImpactBurst(eHit, impactColor)); if (eSpr) StartCoroutine(Shake(eSpr.rectTransform)); }
        }
        if (eFires)
        {
            if (pDefends) StartCoroutine(DeflectBurst(pHit, eTip - pHit, shieldOverlay));
            else          { StartCoroutine(ImpactBurst(pHit, impactColor)); if (pSpr) StartCoroutine(Shake(pSpr.rectTransform)); }
        }

        yield return new WaitForSeconds(0.35f);
    }

    IEnumerator BulletFly(Image img, Vector2 from, Vector2 to, float duration = 0.28f)
    {
        if (img == null) yield break;
        img.gameObject.SetActive(true);
        var rt = img.rectTransform;

        Vector2 dir   = (to - from).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation    = Quaternion.Euler(0f, 0f, angle);
        rt.anchoredPosition = from;

        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            rt.anchoredPosition = Vector2.Lerp(from, to, f / duration);
            yield return null;
        }
        img.gameObject.SetActive(false);
    }

    IEnumerator MuzzleFlash(Vector2 pos)
    {
        var go  = new GameObject("MuzzleFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(combatScreen.transform, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = Vector2.one * 0.5f;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();

        float dur = 0.14f;
        for (float f = 0; f < dur; f += Time.deltaTime)
        {
            float t = f / dur;
            rt.sizeDelta = Vector2.one * Mathf.Lerp(18f, 55f, t);
            img.color    = new Color(1f, 0.9f, 0.3f, 1f - t);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator ImpactBurst(Vector2 pos, Color color)
    {
        int   count = 7;
        var   rts   = new RectTransform[count];
        var   imgs  = new Image[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("ImpactParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(combatScreen.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = Vector2.one * 0.5f;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = Vector2.one * (i == 0 ? 44f : 11f);
            imgs[i] = go.GetComponent<Image>();
            imgs[i].color = color;
            rts[i] = rt;
        }

        // Precompute outward directions for the ring particles
        var dirs = new Vector2[count - 1];
        for (int i = 0; i < dirs.Length; i++)
        {
            float a = i * (360f / dirs.Length) * Mathf.Deg2Rad;
            dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        float dur = 0.38f;
        for (float f = 0; f < dur; f += Time.deltaTime)
        {
            float t = f / dur;
            // Center flash: expand + fade
            rts[0].sizeDelta = Vector2.one * Mathf.Lerp(44f, 88f, t);
            imgs[0].color    = new Color(color.r, color.g, color.b, 1f - t);
            // Ring particles: fly outward + fade
            for (int i = 1; i < count; i++)
            {
                rts[i].anchoredPosition = pos + dirs[i - 1] * 95f * t;
                imgs[i].color = new Color(color.r, color.g * (1f - t * 0.5f), 0f, 1f - t);
            }
            yield return null;
        }

        for (int i = 0; i < count; i++) Destroy(rts[i].gameObject);
    }

    // ── Animation helpers ─────────────────────────────────────────────────────

    IEnumerator ScalePunch(Transform t, float duration = 0.35f)
    {
        if (t == null) yield break;
        float half = duration * 0.5f;
        for (float f = 0; f < half; f += Time.deltaTime) { t.localScale = Vector3.one * Mathf.Lerp(0f,    1.15f, f / half); yield return null; }
        for (float f = 0; f < half; f += Time.deltaTime) { t.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f,    f / half); yield return null; }
        t.localScale = Vector3.one;
    }

    IEnumerator FlashColor(Image img, Color flash, float duration = 0.2f)
    {
        if (img == null) yield break;
        Color orig = img.color;
        img.color = flash;
        yield return new WaitForSeconds(duration);
        img.color = orig;
    }

    IEnumerator Shake(RectTransform rt, float duration = 0.42f, float magnitude = 10f)
    {
        if (rt == null) yield break;
        Vector2 orig = rt.anchoredPosition;
        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            rt.anchoredPosition = orig + new Vector2(Random.Range(-1f, 1f) * magnitude, Random.Range(-0.3f, 0.3f) * magnitude);
            yield return null;
        }
        rt.anchoredPosition = orig;
    }

    IEnumerator ReloadCharacterFX(Image spr)
    {
        if (spr == null) yield break;
        // Quick nervous shake
        yield return StartCoroutine(Shake(spr.rectTransform, 0.25f, 5f));
    }

    IEnumerator ReloadBulletFX(Image icon, Sprite filledSprite)
    {
        if (icon == null) yield break;

        // Gold flash
        var origColor = icon.color;
        icon.color = new Color(1f, 0.95f, 0.2f, 1f);
        yield return null;
        icon.color = origColor;

        // Scale up to peak
        float dur  = 0.35f;
        float peak = 1.6f;
        float half = dur * 0.5f;
        for (float f = 0; f < half; f += Time.deltaTime)
        {
            icon.transform.localScale = Vector3.one * Mathf.Lerp(0f, peak, f / half);
            yield return null;
        }

        // Swap to filled sprite at peak
        if (filledSprite != null) icon.sprite = filledSprite;
        icon.color = origColor;

        // Scale back down
        for (float f = 0; f < half; f += Time.deltaTime)
        {
            icon.transform.localScale = Vector3.one * Mathf.Lerp(peak, 1f, f / half);
            yield return null;
        }
        icon.transform.localScale = Vector3.one;
    }

    IEnumerator FireAnticipation(RectTransform rt, float leanAngle, float snapAngle, float recoilPush)
    {
        if (rt == null) yield break;

        Vector2 origin = rt.anchoredPosition;

        // Phase 1: lean back with shake that starts slow and grows with the lean
        float leanDur  = 1.0f;
        float maxShake = 3f;
        for (float f = 0; f < leanDur; f += Time.deltaTime)
        {
            float t        = f / leanDur;
            float lean     = t * t;                    // ease-in — slow start, accelerates into lean
            float shakeMag = maxShake * lean;          // shake tied to lean progress, near-zero at start

            rt.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, leanAngle, lean));
            rt.anchoredPosition = origin + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shakeMag;
            yield return null;
        }

        // Phase 2: fire — snap forward then instantly kick back (recoil), all one frame
        float recoilAngle = leanAngle * 0.45f;
        rt.localRotation    = Quaternion.Euler(0f, 0f, recoilAngle);
        rt.anchoredPosition = origin + new Vector2(recoilPush, 0f);

        // Phase 3: ease-out settle back to exact origin position and zero rotation
        float settleDur   = 0.4f;
        Vector2 recoilPos = origin + new Vector2(recoilPush, 0f);
        for (float f = 0; f < settleDur; f += Time.deltaTime)
        {
            float t = 1f - Mathf.Pow(1f - f / settleDur, 3f); // ease-out cubic
            rt.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Lerp(recoilAngle, 0f, t));
            rt.anchoredPosition = Vector2.Lerp(recoilPos, origin, t);
            yield return null;
        }
        rt.localRotation    = Quaternion.identity;
        rt.anchoredPosition = origin;
    }

    IEnumerator BounceScale(Transform t, float duration = 0.32f)
    {
        if (t == null) yield break;
        float half = duration * 0.5f;
        for (float f = 0; f < half; f += Time.deltaTime) { t.localScale = Vector3.one * Mathf.Lerp(1f,    1.28f, f / half); yield return null; }
        for (float f = 0; f < half; f += Time.deltaTime) { t.localScale = Vector3.one * Mathf.Lerp(1.28f, 1f,    f / half); yield return null; }
        t.localScale = Vector3.one;
    }

    IEnumerator ShowShieldOverlay(Image overlay, float duration = 0.75f)
    {
        if (overlay == null) yield break;
        overlay.gameObject.SetActive(true);
        Color c = overlay.color;

        float fadeIn = duration * 0.35f;
        for (float f = 0; f < fadeIn; f += Time.deltaTime)  { c.a = f / fadeIn;       overlay.color = c; yield return null; }
        c.a = 1f; overlay.color = c;
        yield return new WaitForSeconds(duration * 0.3f);

        float fadeOut = duration * 0.35f;
        for (float f = 0; f < fadeOut; f += Time.deltaTime) { c.a = 1f - f / fadeOut; overlay.color = c; yield return null; }
        c.a = 0f; overlay.color = c;
        overlay.gameObject.SetActive(false);
    }

    IEnumerator AimBonusPopup(int bonus, Vector2 pos)
    {
        int rankIdx = bonus <= 1 ? 0 : bonus <= 3 ? 1 : 2;

        // ── Icon ──────────────────────────────────────────────────────────
        GameObject iconGo = null;
        RectTransform iconRt = null;
        Image iconImg = null;
        if (aimRankSprites != null && aimRankSprites.Length == 3 && aimRankSprites[rankIdx] != null)
        {
            iconGo = new GameObject("AimIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(combatScreen.transform, false);
            iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot     = Vector2.one * 0.5f;
            iconRt.anchoredPosition = pos + new Vector2(0f, 20f);
            iconRt.sizeDelta = new Vector2(84f, 84f);
            iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = aimRankSprites[rankIdx];
        }

        // ── Bonus text (always shown) ─────────────────────────────────────
        GameObject textGo = null;
        RectTransform textRt = null;
        TextMeshProUGUI tmp = null;
        {
            textGo = new GameObject("AimBonus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(combatScreen.transform, false);
            textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.pivot     = Vector2.one * 0.5f;
            textRt.anchoredPosition = pos + new Vector2(60f, 20f);
            textRt.sizeDelta = new Vector2(200f, 120f);
            tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text      = $"+{bonus}";
            tmp.color     = new Color(1f, 0.95f, 0.2f, 1f);
            tmp.fontSize  = 84f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ── Punch scale in ────────────────────────────────────────────────
        float punchDur = 0.15f;
        for (float f = 0; f < punchDur; f += Time.deltaTime)
        {
            float s = Mathf.Lerp(0f, 1.3f, f / punchDur);
            if (iconGo)  iconGo.transform.localScale  = Vector3.one * s;
            if (textGo)  textGo.transform.localScale  = Vector3.one * s;
            yield return null;
        }
        if (iconGo)  iconGo.transform.localScale  = Vector3.one;
        if (textGo)  textGo.transform.localScale  = Vector3.one;

        // ── Hold then float up + fade ─────────────────────────────────────
        yield return new WaitForSeconds(0.25f);

        float fadeDur  = 0.6f;
        Vector2 iconStart = iconRt  != null ? iconRt.anchoredPosition  : Vector2.zero;
        Vector2 textStart = textRt  != null ? textRt.anchoredPosition  : Vector2.zero;

        for (float f = 0; f < fadeDur; f += Time.deltaTime)
        {
            float t = f / fadeDur;
            float rise = 70f * t;
            float alpha = 1f - t;

            if (iconRt  != null) iconRt.anchoredPosition  = iconStart  + new Vector2(0f, rise);
            if (textRt  != null) textRt.anchoredPosition  = textStart  + new Vector2(0f, rise);
            if (iconImg != null) { Color c = iconImg.color; c.a = alpha; iconImg.color = c; }
            if (tmp     != null) { Color c = tmp.color;     c.a = alpha; tmp.color     = c; }
            yield return null;
        }

        if (iconGo) Destroy(iconGo);
        if (textGo) Destroy(textGo);
    }

    IEnumerator FloatDamageText(string text, Vector2 pos, Color color)
    {
        var go = new GameObject("DmgText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(combatScreen.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = Vector2.one * 0.5f;
        rt.anchoredPosition = pos + new Vector2(0f, 30f);
        rt.sizeDelta = new Vector2(200f, 100f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.color     = color;
        tmp.fontSize  = 72f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        float dur = 0.8f;
        Vector2 startPos = rt.anchoredPosition;
        for (float f = 0; f < dur; f += Time.deltaTime)
        {
            float t = f / dur;
            rt.anchoredPosition = startPos + new Vector2(0f, 60f * t);
            Color c = tmp.color; c.a = 1f - t * t; tmp.color = c;
            yield return null;
        }
        Destroy(go);
    }

    // Sparks fan out 120° in the reflect direction, plus a shield flash
    IEnumerator DeflectBurst(Vector2 pos, Vector2 incomingDir, Image shieldImg)
    {
        StartCoroutine(ShieldFlash(shieldImg));

        int   count    = 8;
        float spreadRad = 60f * Mathf.Deg2Rad; // ±60° = 120° fan
        float baseAngle = Mathf.Atan2(-incomingDir.y, -incomingDir.x); // reflect

        var rts  = new RectTransform[count];
        var imgs = new Image[count];
        var dirs = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            float a   = baseAngle + Mathf.Lerp(-spreadRad, spreadRad, (float)i / (count - 1));
            dirs[i]   = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

            var go = new GameObject("DeflectSpark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(combatScreen.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = Vector2.one * 0.5f;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(6f, 18f);
            rt.localRotation    = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            imgs[i] = go.GetComponent<Image>();
            imgs[i].color = new Color(0.5f, 0.85f, 1f, 1f); // blue-white sparks
            rts[i]  = rt;
        }

        float dur = 0.42f;
        for (float f = 0; f < dur; f += Time.deltaTime)
        {
            float t = f / dur;
            for (int i = 0; i < count; i++)
            {
                rts[i].anchoredPosition = pos + dirs[i] * 110f * t;
                float alpha = Mathf.Lerp(1f, 0f, t * t);
                imgs[i].color = new Color(0.5f + 0.5f * (1f - t), 0.85f, 1f, alpha);
            }
            yield return null;
        }

        for (int i = 0; i < count; i++) Destroy(rts[i].gameObject);
    }

    IEnumerator ShieldFlash(Image overlay)
    {
        if (overlay == null) yield break;
        overlay.gameObject.SetActive(true);
        Color c = overlay.color;

        // Instant full alpha, then quick fade out
        c.a = 1f; overlay.color = c;
        float dur = 0.3f;
        for (float f = 0; f < dur; f += Time.deltaTime)
        {
            c.a = 1f - f / dur;
            overlay.color = c;
            yield return null;
        }
        c.a = 0f; overlay.color = c;
        overlay.gameObject.SetActive(false);
    }
}
