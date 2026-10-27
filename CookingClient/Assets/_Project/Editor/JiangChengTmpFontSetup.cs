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
    /// 把江城律动圆、江城律动黑做成 TMP 动态中文字体。
    /// 动态多图集才能覆盖简体汉字；静态图集装不下整套字库。
    /// 不改项目默认字体，现有界面仍用山海集古宋刻。
    /// </summary>
    [InitializeOnLoad]
    public static class JiangChengTmpFontSetup
    {
        const int SamplingPointSize = 48;
        const int Padding = 5;
        const int AtlasSize = 4096;
        const string SessionKey = "Cooking.JiangChengTmpFontSetup.Done";

        static readonly FontSpec[] Fonts =
        {
            new FontSpec(
                "Assets/_Project/Fonts/江城律动圆.ttf",
                "Assets/_Project/Fonts/江城律动圆 SDF.asset",
                "江城律动圆"),
            new FontSpec(
                "Assets/_Project/Fonts/江城律动黑.ttf",
                "Assets/_Project/Fonts/江城律动黑 SDF.asset",
                "江城律动黑"),
        };

        static JiangChengTmpFontSetup()
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

        [MenuItem("Tools/Cooking/配置江城 TMP 中文字体")]
        public static void EnsureFromMenu()
        {
            Ensure();
            SessionState.SetBool(SessionKey, true);
        }

        public static void Ensure()
        {
            var notes = new StringBuilder();
            bool anyChange = false;
            foreach (FontSpec spec in Fonts)
            {
                bool created = EnsureFont(spec, out TMP_FontAsset fontAsset);
                anyChange |= created;
                notes.Append(spec.displayName);
                notes.Append(" 采样 ");
                notes.Append(fontAsset.atlasWidth);
                notes.Append("x");
                notes.Append(fontAsset.atlasHeight);
                notes.Append(" 字号 ");
                notes.Append(fontAsset.faceInfo.pointSize);
                notes.Append(" 动态=");
                notes.Append(fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic);
                notes.Append(" 多图集=");
                notes.Append(fontAsset.isMultiAtlasTexturesEnabled);
                notes.AppendLine();
            }

            if (!anyChange)
                return;

            AssetDatabase.SaveAssets();
            string message = "江城 TMP 中文字体已配置:\n" + notes;
            Debug.Log(message);
            File.WriteAllText(LogPath(), message);
        }

        static bool EnsureFont(FontSpec spec, out TMP_FontAsset fontAsset)
        {
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(spec.assetPath);
            if (IsValid(fontAsset, spec.fontPath))
                return false;

            if (fontAsset != null)
                AssetDatabase.DeleteAsset(spec.assetPath);

            fontAsset = Create(spec);
            return true;
        }

        static bool IsValid(TMP_FontAsset fontAsset, string fontPath)
        {
            if (fontAsset == null || fontAsset.material == null || fontAsset.atlasTexture == null)
                return false;

            if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                return false;

            if (!fontAsset.isMultiAtlasTexturesEnabled)
                return false;

            if (fontAsset.atlasWidth != AtlasSize || fontAsset.atlasHeight != AtlasSize)
                return false;

            string expectedGuid = AssetDatabase.AssetPathToGUID(fontPath);
            return fontAsset.creationSettings.sourceFontFileGUID == expectedGuid;
        }

        static TMP_FontAsset Create(FontSpec spec)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(spec.fontPath);
            if (font == null)
                throw new FileNotFoundException("找不到字体文件: " + spec.fontPath);

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
                throw new System.InvalidOperationException("TMP 无法读取字体: " + spec.fontPath);

            fontAsset.name = spec.displayName + " SDF";
            AssetDatabase.CreateAsset(fontAsset, spec.assetPath);

            Texture2D atlas = fontAsset.atlasTextures[0];
            atlas.name = spec.displayName + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);

            Material material = fontAsset.material;
            material.name = spec.displayName + " Atlas Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);

            string guid = AssetDatabase.AssetPathToGUID(spec.fontPath);
            SerializedObject serializedFont = new SerializedObject(fontAsset);
            serializedFont.FindProperty("m_SourceFontFile").objectReferenceValue = font;
            serializedFont.FindProperty("m_SourceFontFileGUID").stringValue = guid;
            serializedFont.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Dynamic;
            serializedFont.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = true;
            SerializedProperty clearDynamic = serializedFont.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearDynamic != null)
                clearDynamic.boolValue = true;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();

            string seed = SeedCharacters(spec.displayName);
            fontAsset.creationSettings = new FontAssetCreationSettings
            {
                sourceFontFileName = Path.GetFileName(spec.fontPath),
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
                Debug.LogWarning(spec.displayName + " 有部分字符未能写入图集: " + missing, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        static string SeedCharacters(string fontName)
        {
            StringBuilder builder = new StringBuilder(512);
            for (int code = 32; code <= 126; code++)
                builder.Append((char)code);

            builder.Append("，。！？、；：「」『』【】（）《》〈〉“”‘’…—～·");
            builder.Append("第天夜晚白日开始游戏设置退出确认取消睡觉留下采集烹饪食材库存美味");
            builder.Append(fontName);
            return builder.ToString();
        }

        static string LogPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/JiangChengTmpFontSetup.log"));
        }

        readonly struct FontSpec
        {
            public readonly string fontPath;
            public readonly string assetPath;
            public readonly string displayName;

            public FontSpec(string fontPath, string assetPath, string displayName)
            {
                this.fontPath = fontPath;
                this.assetPath = assetPath;
                this.displayName = displayName;
            }
        }
    }
}
