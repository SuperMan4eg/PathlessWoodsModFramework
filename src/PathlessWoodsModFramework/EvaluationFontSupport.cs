using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
namespace PathlessWoodsModFramework;
// Native evaluation animation expects one mesh page. Use the game's own font source for a small, single atlas.
internal static class EvaluationFontSupport
{
    private static TMP_FontAsset? evaluationFont;
    private static bool attempted;
    private const string Glyphs="АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя…—–«»";
    internal static void Apply(TMP_Text text)
    {
        if(!attempted)
        {
            attempted=true;
            var source=text.font?.sourceFontFile;
            if(source is null){Localization.Log?.LogWarning("Evaluation font source unavailable; kept native font");return;}
            var font=TMP_FontAsset.CreateFontAsset(source,48,5,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,false);
            if(font is null || !font.TryAddCharacters(Glyphs,out var missing))
            {Localization.Log?.LogWarning("Evaluation single-atlas coverage failed; kept native font");return;}
            foreach(var glyph in font.glyphTable)if(glyph.atlasIndex!=0){Localization.Log?.LogWarning("Evaluation atlas overflow; kept native font");return;}
            font.name="PathlessWoodsEvaluationCyrillic";
            UnityEngine.Object.DontDestroyOnLoad(font);
            evaluationFont=font;
            Localization.Log?.LogInfo("Evaluation single-atlas Cyrillic coverage PASS; all glyphs on page0");
        }
        if(evaluationFont is null)return;
        if(text.font!=evaluationFont){text.font=evaluationFont;text.fontSharedMaterial=evaluationFont.material;}
    }
}
