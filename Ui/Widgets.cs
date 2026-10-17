// Forsworn Director by soybean_alien
// Licensed under the MIT License.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForswornDirector.Ui;

// Thin helpers over uGUI. All layout is absolute, measured from the top-left of the parent,
// in 1920x1080 reference pixels (the canvas scaler handles other resolutions).
internal static class Widgets
{
    private const int UiLayer = 5;

    internal static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name) { layer = UiLayer };
        var rect = go.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    internal static RectTransform Place(this RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    internal static RectTransform Fill(this RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        return rect;
    }

    internal static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
    {
        var image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    internal static TextMeshProUGUI Text(Transform parent, string name, string value, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft, float spacing = 0f)
    {
        var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.characterSpacing = spacing;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    // A transparent hit area that brightens on hover and runs onClick.
    internal static Button Clickable(RectTransform rect, Action onClick, float hoverAlpha = 0.05f, Sprite sprite = null)
    {
        var hit = rect.gameObject.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        if (hit.sprite == null)
        {
            hit.sprite = sprite ?? Theme.Card;
            hit.type = UnityEngine.UI.Image.Type.Sliced;
        }

        hit.color = Color.white;
        hit.raycastTarget = true;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, hoverAlpha);
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        colors.pressedColor = new Color(1f, 1f, 1f, hoverAlpha * 1.8f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(new Action(() =>
        {
            try
            {
                onClick();
            }
            catch (Exception exception)
            {
                Plugin.Instance.Log.LogError($"Director UI action failed: {exception}");
            }
        }));
        return button;
    }

    // Thin gold divider that fades out at both ends.
    internal static void Divider(Transform parent, float x, float y, float width, float alpha = 0.55f)
    {
        Image(parent, "Divider", Theme.FadeLine, Theme.WithAlpha(Theme.Gold, alpha)).rectTransform.Place(x, y, width, 1.5f);
    }
}

// Capsule on/off switch whose knob eases toward its target each frame.
internal sealed class Switch
{
    private const float Width = 48f;
    private const float Height = 24f;
    private const float Knob = 18f;

    private readonly Image _track;
    private readonly Image _ring;
    private readonly RectTransform _knob;
    private readonly Image _knobImage;
    private float _position;

    internal Switch(Transform parent, float x, float y, bool initial)
    {
        var root = Widgets.Rect("Switch", parent).Place(x, y, Width, Height);
        _track = Widgets.Image(root, "Track", Theme.Pill, Theme.TrackOff);
        _track.rectTransform.Fill();
        _ring = Widgets.Image(root, "Ring", Theme.PillRing, Theme.WithAlpha(Theme.Gold, 0.35f));
        _ring.rectTransform.Fill();
        _knobImage = Widgets.Image(root, "Knob", Theme.Dot, Theme.Text);
        _knob = _knobImage.rectTransform;
        _knob.anchorMin = _knob.anchorMax = new Vector2(0f, 0.5f);
        _knob.pivot = new Vector2(0.5f, 0.5f);
        _knob.sizeDelta = new Vector2(Knob, Knob);
        _position = initial ? 1f : 0f;
        Set(initial, true);
    }

    internal bool On { get; private set; }

    internal void Set(bool on, bool snap = false)
    {
        On = on;
        if (snap)
            _position = on ? 1f : 0f;
        Paint();
    }

    internal void Tick(float deltaTime)
    {
        var target = On ? 1f : 0f;
        if (Mathf.Approximately(_position, target))
            return;

        _position = Mathf.MoveTowards(_position, target, deltaTime * 7f);
        Paint();
    }

    private void Paint()
    {
        var eased = _position * _position * (3f - 2f * _position);
        var inset = (Height - Knob) / 2f + Knob / 2f;
        _knob.anchoredPosition = new Vector2(Mathf.Lerp(inset, Width - inset, eased), 0f);
        _track.color = Color.Lerp(Theme.TrackOff, Theme.WithAlpha(Theme.Gold, 0.92f), eased);
        _ring.color = Theme.WithAlpha(Theme.Gold, Mathf.Lerp(0.35f, 0f, eased));
        _knobImage.color = Color.Lerp(Theme.TextDim, Theme.Backdrop, eased);
    }
}
