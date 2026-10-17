// Forsworn Director by soybean_alien
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForswornDirector.Ui;

// The in-game Director panel: a uGUI overlay styled after Forsworn's own HUD (Cinzel headings,
// Cormorant body text, dark slate with gold trim). Built lazily on first open.
internal sealed class DirectorPanel : MonoBehaviour
{
    private const float Width = 620f;
    private const float Pad = 28f;
    private const float Inner = Width - Pad * 2f;
    private const float HeaderHeight = 112f;
    private const float TabsTop = 124f;
    private const float ContentTop = 176f;
    private const float FooterHeight = 52f;
    private const float RefreshInterval = 0.25f;

    private GameObject _canvas;
    private RectTransform _panel;
    private RectTransform _content;
    private RectTransform _footer;
    private Image _statusDot;
    private TextMeshProUGUI _statusText;
    private Switch _master;
    private readonly List<(Image fill, Image ring, TextMeshProUGUI label, Image badge, int page)> _tabs = new();
    private readonly List<(Switch view, ConfigEntry<bool> entry)> _bindings = new();
    private readonly List<(Image fill, Image ring, TextMeshProUGUI label, int value)> _chips = new();
    private TextMeshProUGUI _xpDescription;
    private TextMeshProUGUI _readout;

    private bool _visible;
    private int _page;
    private float _nextRefresh;
    private bool _dragging;
    private Vector2 _dragOffset;

    public DirectorPanel(IntPtr pointer) : base(pointer) { }

    private static ClassSettings[] ClassPages => Plugin.Classes.Values.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();

    private void Update()
    {
        if (Input.GetKeyDown(Plugin.ToggleKey.Value))
            SetVisible(!_visible);

        if (!_visible || _panel == null)
            return;

        HandleDrag();
        foreach (var binding in _bindings)
            binding.view.Tick(Time.unscaledDeltaTime);
        _master.Tick(Time.unscaledDeltaTime);

        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + RefreshInterval;
            Refresh();
        }
    }

    private void SetVisible(bool visible)
    {
        try
        {
            if (visible && _canvas == null)
                Build();

            _visible = visible;
            if (_canvas != null)
                _canvas.SetActive(visible);
            if (visible)
            {
                EnsureEventSystem();
                Plugin.ApplyLoadedHeroes();
                Refresh();
            }
        }
        catch (Exception exception)
        {
            _visible = false;
            Plugin.Instance.Log.LogError($"Could not open the Director panel: {exception}");
        }
    }

    // ---------------------------------------------------------------- construction

    private void Build()
    {
        Theme.EnsureLoaded();

        var canvasRect = Widgets.Rect("ForswornDirectorCanvas", null);
        _canvas = canvasRect.gameObject;
        DontDestroyOnLoad(_canvas);
        var canvas = _canvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = _canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        _canvas.AddComponent<GraphicRaycaster>();

        _panel = Widgets.Rect("Panel", canvasRect).Place(Plugin.PanelX.Value, Plugin.PanelY.Value, Width, 600f);

        // Soft drop shadow, body, a faint gold glow under the title, then a double gold rule.
        var shadow = Widgets.Image(_panel, "Shadow", Theme.Panel, new Color(0f, 0f, 0f, 0.45f));
        shadow.rectTransform.Fill(-10f);
        shadow.rectTransform.anchoredPosition = new Vector2(4f, -8f);
        Widgets.Image(_panel, "Body", Theme.Panel, Theme.Backdrop, raycast: true).rectTransform.Fill();
        var glow = Widgets.Image(_panel, "Glow", Theme.Glow, Theme.WithAlpha(Theme.Gold, 0.09f));
        glow.rectTransform.Place(2f, 2f, Width - 4f, 140f);
        Widgets.Image(_panel, "Rim", Theme.PanelRing, Theme.WithAlpha(Theme.GoldMuted, 0.95f)).rectTransform.Fill();
        Widgets.Image(_panel, "InnerRim", Theme.PanelRing, Theme.WithAlpha(Theme.Gold, 0.12f)).rectTransform.Fill(5f);

        BuildHeader();
        BuildTabs();
        _content = Widgets.Rect("Content", _panel).Place(Pad, ContentTop, Inner, 10f);
        BuildFooter();
        ShowPage(0);
    }

    private void BuildHeader()
    {
        Widgets.Text(_panel, "Eyebrow", "SOLO  TOOLKIT", Theme.Heading, 12f, Theme.GoldMuted, spacing: 9f)
            .rectTransform.Place(Pad, 24f, 300f, 16f);
        Widgets.Text(_panel, "Title", "Forsworn Director", Theme.Title, 32f, Theme.Gold, spacing: 1.5f)
            .rectTransform.Place(Pad, 40f, 420f, 40f);

        _statusDot = Widgets.Image(_panel, "StatusDot", Theme.Dot, Theme.Active);
        _statusDot.rectTransform.Place(Pad + 1f, 89f, 9f, 9f);
        _statusText = Widgets.Text(_panel, "Status", "", Theme.Body, 18f, Theme.TextDim);
        _statusText.rectTransform.Place(Pad + 18f, 82f, 380f, 22f);

        var close = Widgets.Rect("Close", _panel).Place(Width - 54f, 18f, 34f, 34f);
        Widgets.Clickable(close, () => SetVisible(false), 0.08f, Theme.Pill);
        Widgets.Text(close, "X", "X", Theme.Heading, 18f, Theme.TextDim, TextAlignmentOptions.Center).rectTransform.Fill();

        Widgets.Text(_panel, "MasterLabel", "DIRECTOR", Theme.Heading, 12f, Theme.TextDim, TextAlignmentOptions.MidlineRight, 4f)
            .rectTransform.Place(Width - Pad - 48f - 132f, 81f, 122f, 24f);
        var masterHit = Widgets.Rect("MasterHit", _panel).Place(Width - Pad - 48f - 4f, 77f, 56f, 32f);
        Widgets.Clickable(masterHit, () => Flip(Plugin.MasterEnabled), 0.06f, Theme.Pill);
        _master = new Switch(_panel, Width - Pad - 48f, 81f, Plugin.MasterEnabled.Value);
    }

    private void BuildTabs()
    {
        Widgets.Divider(_panel, Pad - 8f, HeaderHeight, Inner + 16f);
        var names = new List<string> { "Party" };
        names.AddRange(ClassPages.Select(item => item.Name));
        const float gap = 4f;
        var width = (Inner - gap * (names.Count - 1)) / names.Count;
        for (var index = 0; index < names.Count; index++)
        {
            var page = index;
            var tab = Widgets.Rect("Tab" + names[index], _panel).Place(Pad + index * (width + gap), TabsTop, width, 34f);
            var fill = Widgets.Image(tab, "Fill", Theme.Pill, Theme.WithAlpha(Theme.Gold, 0.14f));
            fill.rectTransform.Fill();
            var ring = Widgets.Image(tab, "Ring", Theme.PillRing, Theme.WithAlpha(Theme.Gold, 0.7f));
            ring.rectTransform.Fill();
            Widgets.Clickable(tab, () => ShowPage(page), 0.06f, Theme.Pill);
            var label = Widgets.Text(tab, "Label", names[index].ToUpperInvariant(), Theme.Heading, 12.5f, Theme.TextDim, TextAlignmentOptions.Center, 1f);
            label.rectTransform.Fill();
            var badge = Widgets.Image(tab, "Badge", Theme.Dot, Theme.Gold);
            badge.rectTransform.Place(width - 11f, 4f, 6f, 6f);
            _tabs.Add((fill, ring, label, badge, page));
        }
    }

    private void BuildFooter()
    {
        _footer = Widgets.Rect("Footer", _panel).Place(0f, 0f, Width, FooterHeight);
        Widgets.Divider(_footer, Pad - 8f, 0f, Inner + 16f, 0.3f);
        Widgets.Text(_footer, "Hint", $"{Plugin.ToggleKey.Value} to hide   ·   drag the title to move   ·   pauses itself in co-op",
                Theme.BodyItalic, 16f, Theme.TextFaint, TextAlignmentOptions.Center)
            .rectTransform.Place(0f, 10f, Width, 32f);
    }

    private void ShowPage(int page)
    {
        _page = page;
        _bindings.Clear();
        _chips.Clear();
        _xpDescription = null;
        _readout = null;
        for (var index = _content.childCount - 1; index >= 0; index--)
            Destroy(_content.GetChild(index).gameObject);

        var height = page == 0 ? BuildPartyPage() : BuildClassPage(ClassPages[page - 1]);
        _content.sizeDelta = new Vector2(Inner, height);
        _footer.anchoredPosition = new Vector2(0f, -(ContentTop + height + 10f));
        _panel.sizeDelta = new Vector2(Width, ContentTop + height + 10f + FooterHeight);
        ClampToScreen();
        Refresh();
    }

    private float BuildPartyPage()
    {
        var y = 0f;
        y = Section(y, "Treasury");
        y = Row(y, "Unlimited Gold", "Every purse in the party stays at 999,999.", Plugin.InfiniteGold);
        y = Row(y, "Unlimited Soul Shards", "The run's Soul Shard reserve stays at 999,999.", Plugin.InfiniteSoulShards);

        y = Section(y + 8f, "Progression");
        y = Row(y, "Unlimited Talent Points", "Buy any talent node. Rank limits still apply.", Plugin.UnlimitedTalentPoints);
        y = Row(y, "Mega Battle XP", "", Plugin.MegaBattleXP);
        _xpDescription = _content.Find("Row:Mega Battle XP/Description").GetComponent<TextMeshProUGUI>();
        y = XpChips(y + 2f);

        y = Section(y + 12f, "Whole party");
        const float gap = 12f;
        var half = (Inner - gap) / 2f;
        ActionButton(0f, y, half, "Empower every hero", Theme.Gold, () => SetAllClasses(true));
        ActionButton(half + gap, y, half, "Restore normal rules", Theme.TextDim, () => SetAllClasses(false));
        return y + 44f;
    }

    private float BuildClassPage(ClassSettings settings)
    {
        var card = Widgets.Rect("HeroCard", _content).Place(0f, 0f, Inner, 74f);
        Widgets.Image(card, "Fill", Theme.Card, Theme.Surface).rectTransform.Fill();
        Widgets.Image(card, "Ring", Theme.CardRing, Theme.WithAlpha(Theme.Gold, 0.18f)).rectTransform.Fill();
        Widgets.Text(card, "Name", settings.Name, Theme.Title, 24f, Theme.Gold, spacing: 1f).rectTransform.Place(18f, 10f, 300f, 30f);
        _readout = Widgets.Text(card, "Readout", "", Theme.Body, 17f, Theme.TextDim);
        _readout.rectTransform.Place(18f, 42f, Inner - 200f, 22f);

        SmallButton(card, Inner - 162f, 21f, "All on", () => SetClass(settings, true));
        SmallButton(card, Inner - 84f, 21f, "All off", () => SetClass(settings, false));

        var y = 90f;
        y = Row(y, "God Mode", "Damage is ignored and this hero cannot fall.", settings.GodMode);
        y = Row(y, "Health Always Full", "Health refills to the class maximum after every hit.", settings.MaxHealth);
        y = Row(y, "Endless Action Points", "Action points stay at the class maximum.", settings.MaxActionPoints);
        y = Row(y, "Endless Movement", "Movement stays at the class maximum.", settings.MaxMovement);
        y = Row(y, "Ultimate Always Ready", "Ultimate charge never drains.", settings.MaxUltimate);
        return y;
    }

    private float Section(float y, string title)
    {
        Widgets.Text(_content, "Section:" + title, title.ToUpperInvariant(), Theme.Heading, 12.5f, Theme.GoldMuted, spacing: 5f)
            .rectTransform.Place(0f, y + 6f, Inner, 18f);
        Widgets.Divider(_content, 0f, y + 28f, Inner, 0.22f);
        return y + 34f;
    }

    private float Row(float y, string label, string description, ConfigEntry<bool> entry)
    {
        const float height = 58f;
        var row = Widgets.Rect("Row:" + label, _content).Place(-10f, y, Inner + 20f, height);
        Widgets.Clickable(row, () => Flip(entry));
        Widgets.Text(row, "Label", label, Theme.Heading, 17f, Theme.Text, spacing: 0.5f).rectTransform.Place(10f, 8f, Inner - 80f, 24f);
        Widgets.Text(row, "Description", description, Theme.Body, 17f, Theme.TextDim).rectTransform.Place(10f, 31f, Inner - 80f, 22f);
        _bindings.Add((new Switch(row, Inner + 10f - 48f, 17f, entry.Value), entry));
        return y + height;
    }

    private float XpChips(float y)
    {
        const float gap = 8f;
        var width = (Inner - gap * (Plugin.XpPresets.Length - 1)) / Plugin.XpPresets.Length;
        for (var index = 0; index < Plugin.XpPresets.Length; index++)
        {
            var value = Plugin.XpPresets[index];
            var chip = Widgets.Rect("Chip" + value, _content).Place(index * (width + gap), y, width, 30f);
            var fill = Widgets.Image(chip, "Fill", Theme.Pill, Theme.WithAlpha(Theme.Gold, 0.9f));
            fill.rectTransform.Fill();
            var ring = Widgets.Image(chip, "Ring", Theme.PillRing, Theme.WithAlpha(Theme.Gold, 0.35f));
            ring.rectTransform.Fill();
            Widgets.Clickable(chip, () => SetXp(value), 0.08f, Theme.Pill);
            var label = Widgets.Text(chip, "Label", "x" + value.ToString("N0"), Theme.Heading, 14f, Theme.Text, TextAlignmentOptions.Center);
            label.rectTransform.Fill();
            _chips.Add((fill, ring, label, value));
        }

        return y + 40f;
    }

    private void ActionButton(float x, float y, float width, string caption, Color accent, Action onClick)
    {
        var button = Widgets.Rect("Action:" + caption, _content).Place(x, y, width, 40f);
        Widgets.Image(button, "Fill", Theme.Card, Theme.WithAlpha(accent, 0.08f)).rectTransform.Fill();
        Widgets.Image(button, "Ring", Theme.CardRing, Theme.WithAlpha(accent, 0.55f)).rectTransform.Fill();
        Widgets.Clickable(button, onClick, 0.07f);
        Widgets.Text(button, "Label", caption.ToUpperInvariant(), Theme.Heading, 13.5f, accent, TextAlignmentOptions.Center, 2f).rectTransform.Fill();
    }

    private void SmallButton(Transform parent, float x, float y, string caption, Action onClick)
    {
        var button = Widgets.Rect("Small:" + caption, parent).Place(x, y, 70f, 32f);
        Widgets.Image(button, "Ring", Theme.PillRing, Theme.WithAlpha(Theme.Gold, 0.45f)).rectTransform.Fill();
        Widgets.Clickable(button, onClick, 0.08f, Theme.Pill);
        Widgets.Text(button, "Label", caption.ToUpperInvariant(), Theme.Heading, 11.5f, Theme.Gold, TextAlignmentOptions.Center, 1.5f).rectTransform.Fill();
    }

    // ---------------------------------------------------------------- actions

    private void Flip(ConfigEntry<bool> entry)
    {
        entry.Value = !entry.Value;
        Plugin.SaveConfig();
        Refresh();
    }

    private void SetXp(int value)
    {
        Plugin.BattleXPMultiplier.Value = value;
        Plugin.MegaBattleXP.Value = true;
        Plugin.SaveConfig();
        Refresh();
    }

    private void SetClass(ClassSettings settings, bool on)
    {
        foreach (var entry in settings.All)
            entry.Value = on;
        Plugin.SaveConfig();
        Refresh();
    }

    private void SetAllClasses(bool on)
    {
        foreach (var settings in Plugin.Classes.Values)
        foreach (var entry in settings.All)
            entry.Value = on;
        Plugin.SaveConfig();
        Refresh();
    }

    // ---------------------------------------------------------------- live state

    private void Refresh()
    {
        if (_panel == null)
            return;

        var solo = Plugin.TryGetSoloSession(out var session);
        if (!Plugin.MasterEnabled.Value)
            SetStatus(Theme.Off, "Director switched off");
        else if (solo)
            SetStatus(Theme.Active, $"{session}  ·  effects active");
        else
            SetStatus(Theme.Paused, $"Paused  ·  {session}");
        _master.Set(Plugin.MasterEnabled.Value);

        foreach (var binding in _bindings)
            binding.view.Set(binding.entry.Value);

        var classes = ClassPages;
        foreach (var tab in _tabs)
        {
            var selected = tab.page == _page;
            tab.fill.enabled = selected;
            tab.ring.color = Theme.WithAlpha(Theme.Gold, selected ? 0.75f : 0.16f);
            tab.label.color = selected ? Theme.GoldBright : Theme.TextDim;
            tab.badge.enabled = tab.page > 0 && classes[tab.page - 1].ActiveCount > 0;
        }

        var multiplier = Plugin.XpMultiplier;
        foreach (var chip in _chips)
        {
            var selected = chip.value == multiplier;
            chip.fill.enabled = selected;
            chip.ring.color = Theme.WithAlpha(Theme.Gold, selected ? 0f : 0.3f);
            chip.label.color = selected ? Theme.Backdrop : Theme.TextDim;
        }

        if (_xpDescription != null)
            _xpDescription.text = $"Battle XP is multiplied x{multiplier:N0} when awarded. Level cap still applies.";

        if (_readout != null)
            _readout.text = Readout(classes[_page - 1].Name);
    }

    private void SetStatus(Color color, string text)
    {
        _statusDot.color = color;
        _statusText.text = text;
        _statusText.color = Color.Lerp(color, Theme.Text, 0.35f);
    }

    private static string Readout(string className)
    {
        var hero = Plugin.FindHero(className);
        var stats = hero != null ? hero.mechanicStats : null;
        if (stats == null)
            return "Not on the field right now.";

        return $"HP {stats.currentHP}/{stats.maxHP}   ·   AP {stats.remainingActionPoints}/{stats.maxActionPoints}   ·   "
            + $"Move {stats.remainingMoveDistance:0.#}/{stats.maxMoveDistance:0.#}   ·   Ult {stats.currentUltimateCharge}/{stats.maxUltimateCharge}";
    }

    // ---------------------------------------------------------------- window handling

    private void HandleDrag()
    {
        var mouse = (Vector2)Input.mousePosition;
        if (Input.GetMouseButtonDown(0) && OverHeader(mouse))
        {
            _dragging = true;
            _dragOffset = _panel.anchoredPosition - mouse / ScaleFactor();
        }

        if (_dragging && Input.GetMouseButton(0))
        {
            _panel.anchoredPosition = _dragOffset + mouse / ScaleFactor();
            ClampToScreen();
        }

        if (_dragging && Input.GetMouseButtonUp(0))
        {
            _dragging = false;
            Plugin.PanelX.Value = Mathf.Round(_panel.anchoredPosition.x);
            Plugin.PanelY.Value = Mathf.Round(-_panel.anchoredPosition.y);
            Plugin.Instance.Config.Save();
        }
    }

    private bool OverHeader(Vector2 mouse)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_panel, mouse, null, out var local))
            return false;

        // Local origin is the panel's top-left pivot; the title strip is draggable, the header controls are not.
        return local.x >= 0f && local.x <= Width - 200f && local.y <= 0f && local.y >= -HeaderHeight;
    }

    private float ScaleFactor()
    {
        var canvas = _canvas.GetComponent<Canvas>();
        return canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
    }

    private void ClampToScreen()
    {
        var scale = ScaleFactor();
        var screenWidth = Screen.width / scale;
        var screenHeight = Screen.height / scale;
        var size = _panel.sizeDelta;
        var position = _panel.anchoredPosition;
        position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, screenWidth - size.x));
        position.y = Mathf.Clamp(position.y, -Mathf.Max(0f, screenHeight - Mathf.Min(size.y, screenHeight)), 0f);
        _panel.anchoredPosition = position;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var go = new GameObject("ForswornDirectorEventSystem");
        DontDestroyOnLoad(go);
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        Plugin.Instance.Log.LogInfo("No EventSystem in scene; created one for the Director panel.");
    }
}
