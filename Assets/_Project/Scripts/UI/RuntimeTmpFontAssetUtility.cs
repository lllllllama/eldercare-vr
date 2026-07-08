using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PicoElderCare.UI
{
    public static class RuntimeTmpFontAssetUtility
    {
        public static TMP_FontAsset PrepareDynamicFont(
            TMP_FontAsset sourceFont,
            string requiredCharacters,
            ref TMP_FontAsset runtimeFont,
            ref TMP_FontAsset runtimeFontSource)
        {
            if (sourceFont == null) return null;

            if (sourceFont.sourceFontFile == null)
            {
                DestroyRuntimeFont(ref runtimeFont, ref runtimeFontSource);
                return sourceFont;
            }

            if (runtimeFont == null || runtimeFontSource != sourceFont)
            {
                DestroyRuntimeFont(ref runtimeFont, ref runtimeFontSource);
                runtimeFontSource = sourceFont;
                runtimeFont = TMP_FontAsset.CreateFontAsset(
                    sourceFont.sourceFontFile,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true);
                runtimeFont.name = sourceFont.name;
                runtimeFont.hideFlags = HideFlags.DontSave;
                runtimeFont.fallbackFontAssetTable = sourceFont.fallbackFontAssetTable;
                HideGeneratedFontObjects(runtimeFont);
            }

            if (!string.IsNullOrEmpty(requiredCharacters))
            {
                runtimeFont.TryAddCharacters(requiredCharacters);
                HideGeneratedFontObjects(runtimeFont);
            }

            return runtimeFont;
        }

        public static TMP_FontAsset ResolveSourceFont(TMP_FontAsset uiFont, TMP_FontAsset runtimeFont, TMP_FontAsset runtimeFontSource)
        {
            return uiFont == runtimeFont ? runtimeFontSource : uiFont;
        }

        public static void DestroyRuntimeFont(ref TMP_FontAsset runtimeFont, ref TMP_FontAsset runtimeFontSource)
        {
            if (runtimeFont == null)
            {
                runtimeFontSource = null;
                return;
            }

            DestroyObject(runtimeFont);
            runtimeFont = null;
            runtimeFontSource = null;
        }

        private static void HideGeneratedFontObjects(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            if (fontAsset.material != null)
            {
                fontAsset.material.hideFlags = HideFlags.DontSave;
            }

            var atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures == null) return;

            for (var i = 0; i < atlasTextures.Length; i++)
            {
                if (atlasTextures[i] != null)
                {
                    atlasTextures[i].hideFlags = HideFlags.DontSave;
                }
            }
        }

        private static void DestroyObject(Object asset)
        {
            if (asset == null) return;

            if (Application.isPlaying)
            {
                Object.Destroy(asset);
            }
            else
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
