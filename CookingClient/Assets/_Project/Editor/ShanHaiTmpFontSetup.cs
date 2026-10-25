using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Cooking.EditorTools
{
    /// <summary>
    /// 把山海集古宋刻做成 TMP 动态中文字体，并登记为项目默认字体和全局回退字体。
    /// 动态多图集才能覆盖简繁汉字；静态图集装不下整套字库。
    /// </summary>
    [InitializeOnLoad]
    public static class ShanHaiTmpFontSetup
    {
        const string FontPath = "Assets/_Project/Fonts/ShanHaiJiGuSongKe-JianFan-2.ttf";
        const string AssetPath = "Assets/_Project/Fonts/ShanHaiJiGuSongKe-JianFan-2 SDF.asset";
        const int SamplingPointSize = 48;
        const int Padding = 5;
        const int AtlasSize = 4096;
        const string SessionKey = "Cooking.ShanHaiTmpFontSetup.Done";

        static ShanHaiTmpFontSetup()
        {
            EditorApplication.delayCall += EnsureWhenReady;
        }

        static void EnsureWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureWhenReady;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (SessionState.GetBool(SessionKey, false))
                return;

            try
            {
                Ensure();
                SessionState.SetBool(SessionKey, true);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                File.WriteAllText(LogPath(), exception.ToString());
            }
        }

        [MenuItem("Tools/Cooking/配置山海 TMP 中文字体")]
        public static void EnsureFromMenu()
        {
            Ensure();
            SessionState.SetBool(SessionKey, true);
        }

        public static void Ensure()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            bool created = false;
            if (!IsValid(fontAsset))
            {
                if (fontAsset != null)
                    AssetDatabase.DeleteAsset(AssetPath);

                fontAsset = Create();
                created = true;
            }

            bool assigned = AssignProjectFont(fontAsset);
            if (!created && !assigned)
                return;

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();

            string message = "山海 TMP 中文字体已配置: " + AssetPath
                + " 采样 " + fontAsset.atlasWidth + "x" + fontAsset.atlasHeight
                + " 字号 " + fontAsset.faceInfo.pointSize
                + " 动态=" + (fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                + " 多图集=" + fontAsset.isMultiAtlasTexturesEnabled;
            Debug.Log(message, fontAsset);
            File.WriteAllText(LogPath(), message);
        }

        static bool IsValid(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null || fontAsset.material == null || fontAsset.atlasTexture == null)
                return false;

            if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                return false;

            if (!fontAsset.isMultiAtlasTexturesEnabled)
                return false;

            if (fontAsset.atlasWidth != AtlasSize || fontAsset.atlasHeight != AtlasSize)
                return false;

            string expectedGuid = AssetDatabase.AssetPathToGUID(FontPath);
            return fontAsset.creationSettings.sourceFontFileGUID == expectedGuid;
        }

        static TMP_FontAsset Create()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
                throw new FileNotFoundException("找不到字体文件: " + FontPath);

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                SamplingPointSize,
                Padding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
                throw new System.InvalidOperationException("TMP 无法读取字体。请确认字体导入设置里勾选了 Include Font Data。");

            fontAsset.name = "ShanHaiJiGuSongKe-JianFan-2 SDF";
            AssetDatabase.CreateAsset(fontAsset, AssetPath);

            Texture2D atlas = fontAsset.atlasTextures[0];
            atlas.name = "ShanHaiJiGuSongKe-JianFan-2 Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);

            Material material = fontAsset.material;
            material.name = "ShanHaiJiGuSongKe-JianFan-2 Atlas Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);

            string guid = AssetDatabase.AssetPathToGUID(FontPath);
            SerializedObject serializedFont = new SerializedObject(fontAsset);
            serializedFont.FindProperty("m_SourceFontFile").objectReferenceValue = font;
            serializedFont.FindProperty("m_SourceFontFileGUID").stringValue = guid;
            serializedFont.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Dynamic;
            serializedFont.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = true;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();

            string seed = SeedCharacters();
            fontAsset.creationSettings = new FontAssetCreationSettings
            {
                sourceFontFileName = Path.GetFileName(FontPath),
                sourceFontFileGUID = guid,
                faceIndex = 0,
                pointSizeSamplingMode = 1,
                pointSize = SamplingPointSize,
                padding = Padding,
                paddingMode = 2,
                packingMode = 0,
                atlasWidth = AtlasSize,
                atlasHeight = AtlasSize,
                characterSetSelectionMode = 7,
                characterSequence = seed,
                renderMode = (int)GlyphRenderMode.SDFAA,
                includeFontFeatures = false
            };

            if (!fontAsset.TryAddCharacters(seed, out string missing))
                Debug.LogWarning("山海字体有部分字符未能写入图集: " + missing, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        static bool AssignProjectFont(TMP_FontAsset fontAsset)
        {
            bool changed = TMP_Settings.defaultFontAsset != fontAsset;
            TMP_Settings.defaultFontAsset = fontAsset;

            List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks == null)
                fallbacks = new List<TMP_FontAsset>();

            if (fallbacks.Count == 0 || fallbacks[0] != fontAsset || fallbacks.Contains(null))
            {
                fallbacks.RemoveAll(asset => asset == null || asset == fontAsset);
                fallbacks.Insert(0, fontAsset);
                TMP_Settings.fallbackFontAssets = fallbacks;
                changed = true;
            }

            if (changed)
                EditorUtility.SetDirty(TMP_Settings.instance);

            return changed;
        }

        static string SeedCharacters()
        {
            StringBuilder builder = new StringBuilder(512);
            for (int code = 32; code <= 126; code++)
                builder.Append((char)code);

            builder.Append("，。！？、；：「」『』【】（）《》〈〉“”‘’…—～·");
            builder.Append("第天夜晚白日开始游戏设置退出确认取消睡觉留下采集烹饪食材库存美味");
            builder.Append("山海集古宋刻简繁体中文");
            return builder.ToString();
        }

        static string LogPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/ShanHaiTmpFontSetup.log"));
        }
    }
}
