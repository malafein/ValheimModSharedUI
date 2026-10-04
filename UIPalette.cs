using System.Text.RegularExpressions;
using UnityEngine;

namespace malafein.Valheim.SharedUI
{
    // Text colours matched to the vanilla Compendium: body text near-white, section headers and
    // highlighted values orange, category labels yellow. The rich-text helpers need richText on
    // the TMP component (it's on by default).
    internal static class UIPalette
    {
        public static readonly Color BodyTextColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        public const string HeaderColorTag = "orange";
        public const string ValueColorTag  = "orange";
        public const string LabelColorTag  = "yellow";

        // Golden accent for Color-typed uses (selected list rows, detail topic). Matches the
        // vanilla window-title / Compendium-topic colour; HeaderColorTag handles inline markup.
        public static readonly Color AccentGold = new Color(1f, 0.718f, 0.36f, 1f);

        public static string Colored(string colorTag, string s) => $"<color={colorTag}>{s}</color>";

        // A bold orange section header.
        public static string Header(string s) => $"<b>{Colored(HeaderColorTag, s)}</b>";

        // A yellow category label (e.g. "Current effects:").
        public static string Label(string s) => Colored(LabelColorTag, s);

        // Highlight percentages in orange, like vanilla tooltips.
        public static string HighlightValues(string s)
            => Regex.Replace(s, @"\d+%", m => Colored(ValueColorTag, m.Value));
    }
}
