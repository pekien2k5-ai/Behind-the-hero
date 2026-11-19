using BTH.Content;
using UnityEngine;

namespace BTH.UI
{
    public static class HudSpecPresenter
    {
        public static Color ParseHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var col))
            {
                return col;
            }
            return Color.white;
        }

        public static int PaletteCount => ArtAssetCatalog.GetHudColorPalette().Count;
    }
}
