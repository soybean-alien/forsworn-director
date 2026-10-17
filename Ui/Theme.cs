// Forsworn Director by soybean_alien
// Licensed under the MIT License.

using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ForswornDirector.Ui;

// Palette, fonts and procedurally generated sprites. Everything is built at runtime so the
// mod ships no game assets; fonts are the game's own Cinzel/Cormorant TMP assets, looked up by name.
internal static class Theme
{
    internal static readonly Color Backdrop = Rgba(0x0B0C10, 0.94f);
    internal static readonly Color Surface = Rgba(0x15171D, 0.96f);
    internal static readonly Color SurfaceRaised = Rgba(0x1D2028, 1f);
    internal static readonly Color Gold = Rgba(0xD8B46A, 1f);
    internal static readonly Color GoldBright = Rgba(0xF1D598, 1f);
    internal static readonly Color GoldMuted = Rgba(0x8E7444, 1f);
    internal static readonly Color Text = Rgba(0xE9E2D2, 1f);
    internal static readonly Color TextDim = Rgba(0x9C968A, 1f);
    internal static readonly Color TextFaint = Rgba(0x6B675F, 1f);
    internal static readonly Color Active = Rgba(0x8FD18A, 1f);
    internal static readonly Color Paused = Rgba(0xE0A24E, 1f);
    internal static readonly Color Off = Rgba(0x7C7A75, 1f);
    internal static readonly Color TrackOff = Rgba(0x2B2F38, 1f);

    internal static TMP_FontAsset Title;
    internal static TMP_FontAsset Heading;
    internal static TMP_FontAsset Body;
    internal static TMP_FontAsset BodyItalic;

    internal static Sprite Panel;      // rounded 12px fill, 9-sliced
    internal static Sprite PanelRing;  // rounded 12px 1.5px outline, 9-sliced
    internal static Sprite Card;       // rounded 8px fill
    internal static Sprite CardRing;   // rounded 8px outline
    internal static Sprite Pill;       // fully rounded 24px-high capsule
    internal static Sprite PillRing;
    internal static Sprite Dot;        // anti-aliased circle
    internal static Sprite FadeLine;   // horizontal line fading at both ends
    internal static Sprite Glow;       // vertical gradient, opaque at top

    private static bool _ready;

    internal static void EnsureLoaded()
    {
        if (_ready)
            return;

        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset Find(params string[] names)
        {
            foreach (var name in names)
            {
                var hit = fonts.FirstOrDefault(font => font != null && font.name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (hit != null)
                    return hit;
            }

            return null;
        }

        var fallback = TMP_Settings.defaultFontAsset ?? fonts.FirstOrDefault(font => font != null);
        Title = Find("Cinzel-Bold SDF", "CinzelDecorative-Bold SDF", "Cinzel-SemiBold SDF") ?? fallback;
        Heading = Find("Cinzel-SemiBold SDF", "Cinzel-Medium SDF", "Cinzel-Regular SDF") ?? Title;
        Body = Find("CormorantInfant-SemiBold SDF", "CormorantInfant-Medium SDF", "CormorantInfant-Regular SDF", "CormorantSC-Medium SDF") ?? fallback;
        BodyItalic = Find("CormorantInfant-MediumItalic SDF", "CormorantInfant-Italic SDF") ?? Body;
        Plugin.Instance.Log.LogInfo($"UI fonts: title={Title?.name}, heading={Heading?.name}, body={Body?.name}, italic={BodyItalic?.name}");

        Panel = Rounded(48, 12f, 0f);
        PanelRing = Rounded(48, 12f, 1.5f);
        Card = Rounded(32, 8f, 0f);
        CardRing = Rounded(32, 8f, 1.25f);
        Pill = Rounded(24, 12f, 0f);
        PillRing = Rounded(24, 12f, 1.25f);
        Dot = Rounded(32, 16f, 0f, sliced: false);
        FadeLine = Fade(256, 2, horizontal: true);
        Glow = Fade(4, 128, horizontal: false);
        _ready = true;
    }

    internal static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, alpha);

    private static Color Rgba(int hex, float alpha) =>
        new(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, alpha);

    // White rounded rectangle (or outline when thickness > 0) with signed-distance anti-aliasing.
    private static Sprite Rounded(int size, float radius, float thickness, bool sliced = true)
    {
        var texture = NewTexture(size, size);
        var half = size / 2f;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var px = Math.Abs(x + 0.5f - half) - (half - radius);
            var py = Math.Abs(y + 0.5f - half) - (half - radius);
            var outside = (float)Math.Sqrt(Math.Max(px, 0f) * Math.Max(px, 0f) + Math.Max(py, 0f) * Math.Max(py, 0f));
            var distance = outside + Math.Min(Math.Max(px, py), 0f) - radius;
            var alpha = Mathf.Clamp01(0.5f - distance);
            if (thickness > 0f)
                alpha *= Mathf.Clamp01(0.5f + distance + thickness);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }

        texture.Apply();
        var border = sliced ? Mathf.Ceil(radius) + 1f : 0f;
        return MakeSprite(texture, new Vector4(border, border, border, border));
    }

    private static Sprite Fade(int width, int height, bool horizontal)
    {
        var texture = NewTexture(width, height);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var alpha = horizontal
                ? Mathf.Sin((x + 0.5f) / width * Mathf.PI)
                : Mathf.Pow((y + 0.5f) / height, 2.2f);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }

        texture.Apply();
        return MakeSprite(texture, Vector4.zero);
    }

    private static Texture2D NewTexture(int width, int height)
    {
        return new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
    }

    private static Sprite MakeSprite(Texture2D texture, Vector4 border)
    {
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
