using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class AdeConsoleWindow : EditorWindow
{
    const string AdeDataInfoPath = "Assets/Ade_Framework/Resources/ScriptableObject/AdeDataInfo.asset";
    const string AdsDataPath = "Assets/Ade_Framework/Resources/ScriptableObject/AdsData.asset";
    const string EntryScriptPath = "Assets/Ade_Framework/Scriptes/1_Enter/Entry.cs";
    const string ADManagerScriptPath = "Assets/Ade_Framework/ADManager.cs";
    const string DebugAdScriptPath = "Assets/Ade_Framework/Scriptes/Debug/DebugAd.cs";
    const string ResourceFolderPath = "Assets/Ade_Framework/Resources/ScriptableObject";
    const string FeedLaunchModePlayerPrefsKey = "Ade.Editor.FeedLaunchMode";
    const string BgdtPackagePath = @"E:\UnityTools\Editor\TTtool\com.bytedance.bgdt-cp-3.0.271.unitypackage";
    const string MinigamePackagePath = @"E:\UnityTools\Editor\TTtool\minigame.202601131148.unitypackage";

    static readonly string[] KnownPlatformSymbols =
    {
        "Ade_TT",
        "Ade_WX",
        "Ade_KS",
        "Ade_BiliBili",
        "Ade_Bilibili",
        "UNITY_EDITOR",
    };

    readonly PlatformPreset[] presets =
    {
        new PlatformPreset("编辑器/无平台宏", string.Empty, "清理平台宏，保留其他自定义宏"),
        new PlatformPreset("抖音", "Ade_TT", "启用抖音小游戏宏"),
        new PlatformPreset("微信", "Ade_WX", "启用微信小游戏宏"),
        new PlatformPreset("快手", "Ade_KS", "启用快手小游戏宏"),
        new PlatformPreset("哔哩哔哩", "Ade_BiliBili", "启用哔哩哔哩小游戏宏"),
    };

    Vector2 scrollPosition;
    BuildTargetGroup cachedCustomGroup = BuildTargetGroup.Unknown;
    string cachedDefineString = string.Empty;
    bool customSymbolsDirty;
    bool launchModeDirty;
    int selectedPlatformPresetIndex;
    FeedLaunchMode selectedFeedLaunchMode;
    FeedLaunchMode cachedFeedLaunchMode;
    readonly List<string> editableCustomSymbols = new();
    ReorderableList customSymbolList;
    GUIStyle sectionTitleStyle;
    GUIStyle sectionNoteStyle;
    GUIStyle summaryLabelStyle;
    GUIStyle summaryValueStyle;
    GUIStyle pathLabelStyle;

    [MenuItem("Ade_Tools/Ade 控制台")]
    public static void OpenWindow()
    {
        GetWindow<AdeConsoleWindow>("Ade 控制台");
    }

    void OnEnable()
    {
        minSize = new Vector2(720f, 520f);
        LoadFeedLaunchMode();
        BuildStyles();
        customSymbolList = new ReorderableList(editableCustomSymbols, typeof(string), true, true, true, true);
        customSymbolList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Scripting Define Symbols");
        };
        customSymbolList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            rect.y += 1;
            rect.height = EditorGUIUtility.singleLineHeight;

            if (index < 0 || index >= editableCustomSymbols.Count)
            {
                return;
            }

            rect.x += 6f;
            rect.width -= 6f;
            string updatedSymbol = EditorGUI.TextField(rect, editableCustomSymbols[index]);
            if (updatedSymbol != editableCustomSymbols[index])
            {
                editableCustomSymbols[index] = updatedSymbol;
                customSymbolsDirty = true;
            }
        };
        customSymbolList.onAddCallback = _ =>
        {
            editableCustomSymbols.Add(string.Empty);
            customSymbolsDirty = true;
        };
        customSymbolList.onRemoveCallback = list =>
        {
            if (list.index < 0 || list.index >= editableCustomSymbols.Count)
            {
                return;
            }

            editableCustomSymbols.RemoveAt(list.index);
            customSymbolsDirty = true;

            if (editableCustomSymbols.Count == 0)
            {
                list.index = -1;
            }
            else
            {
                list.index = Mathf.Clamp(list.index - 1, 0, editableCustomSymbols.Count - 1);
            }
        };
        customSymbolList.footerHeight = 22f;
        customSymbolList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
    }

    void OnGUI()
    {
        BuildStyles();
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        List<string> symbols = GetSymbols(group);

        try
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader(group, symbols);
            EditorGUILayout.Space(12);

            DrawDefineSection(group, symbols);
            EditorGUILayout.Space(12);

            DrawResourceSection();
            EditorGUILayout.Space(12);

            DrawPackageCleanupSection();
            EditorGUILayout.Space(12);

            DrawShortcutSection();
            EditorGUILayout.Space(12);

            DrawPlayModeSection();
        }
        catch (Exception exception)
        {
            EditorGUILayout.HelpBox($"Ade 控制台渲染异常:\n{exception.GetType().Name}: {exception.Message}", MessageType.Error);
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    void DrawHeader(BuildTargetGroup group, List<string> symbols)
    {
        BeginSectionCard("概览", "平台、宏与启动状态");
        EditorGUILayout.HelpBox("宏定义、普通宏、推荐流启动模式都集中在下方维护。", MessageType.Info);
        DrawSummaryRow("当前平台组", group.ToString());
        DrawSummaryRow("当前平台宏", GetCurrentPlatformSymbol(symbols));
        DrawSummaryRow("当前宏列表", symbols.Count > 0 ? string.Join("; ", symbols) : "无");
        EndSectionCard();
    }

    void DrawDefineSection(BuildTargetGroup group, List<string> symbols)
    {
        SyncCustomSymbols(group, symbols);
        SyncFeedLaunchMode();

        BeginSectionCard("宏与启动", "平台预设、推荐流模拟、宏列表");

        string[] presetLabels = GetPlatformPresetLabels();
        selectedPlatformPresetIndex = GetCurrentPresetIndex(symbols);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("平台预设", summaryLabelStyle, GUILayout.Width(90f));
            int nextPresetIndex = EditorGUILayout.Popup(selectedPlatformPresetIndex, presetLabels);
            if (nextPresetIndex != selectedPlatformPresetIndex)
            {
                selectedPlatformPresetIndex = nextPresetIndex;
                if (selectedPlatformPresetIndex < presets.Length)
                {
                    ApplyPlatformPresetToEditor(symbols, presets[selectedPlatformPresetIndex].Symbol);
                }
            }
        }
        string presetDescription = selectedPlatformPresetIndex < presets.Length
            ? presets[selectedPlatformPresetIndex].Description
            : "当前宏不是内置平台预设，可直接在下方列表继续编辑。";
        EditorGUILayout.LabelField(presetDescription, sectionNoteStyle);

        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("推荐流模式", summaryLabelStyle, GUILayout.Width(90f));
            string[] launchModeLabels = { "正常启动", "复访启动", "获客启动" };
            FeedLaunchMode[] launchModeValues =
            {
                FeedLaunchMode.None,
                FeedLaunchMode.FeedDirectPlay,
                FeedLaunchMode.FeedAcquisition,
            };

            int currentLaunchIndex = GetFeedLaunchModeIndex(selectedFeedLaunchMode, launchModeValues);
            int nextLaunchIndex = EditorGUILayout.Popup(currentLaunchIndex, launchModeLabels);
            FeedLaunchMode nextLaunchMode = launchModeValues[nextLaunchIndex];
            if (nextLaunchMode != selectedFeedLaunchMode)
            {
                selectedFeedLaunchMode = nextLaunchMode;
                launchModeDirty = selectedFeedLaunchMode != cachedFeedLaunchMode;
            }
        }
        EditorGUILayout.LabelField($"当前模拟模式: {GetFeedLaunchModeLabel(selectedFeedLaunchMode)}", sectionNoteStyle);

        EditorGUILayout.Space(8f);
        customSymbolList.DoLayoutList();

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("复制宏", GUILayout.Height(24)))
            {
                EditorGUIUtility.systemCopyBuffer = BuildDefineString(GetSanitizedCustomSymbols(false));
                Debug.Log("AdeConsole: 已复制当前宏定义。");
            }

            if (GUILayout.Button("还原", GUILayout.Height(24)))
            {
                LoadCustomSymbols(group, symbols);
                LoadFeedLaunchMode();
            }

            GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isUpdating && (customSymbolsDirty || launchModeDirty);
            if (GUILayout.Button("应用", GUILayout.Height(24)))
            {
                SaveCustomSymbols(group);
                SaveFeedLaunchMode();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space(6f);
        DrawSummaryRow("预览结果", BuildDefineString(GetSanitizedCustomSymbols(false)));
        EndSectionCard();
    }

    void DrawResourceSection()
    {
        BeginSectionCard("配置资源", "资源路径与基础配置");

        UnityEngine.Object adeDataInfo = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdeDataInfoPath);
        DrawAssetBlock(
            "AdeDataInfo",
            AdeDataInfoPath,
            adeDataInfo,
            FindOtherAssetPath("AdeDataInfo", AdeDataInfoPath),
            CreateAdeDataInfoAsset);

        if (adeDataInfo != null)
        {
            DrawAdeDataInfoEditor(adeDataInfo);
        }

        EditorGUILayout.Space(8);

        UnityEngine.Object adsData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdsDataPath);
        DrawAssetBlock(
            "AdsData",
            AdsDataPath,
            adsData,
            FindOtherAssetPath("AdsData", AdsDataPath),
            CreateAdsDataAsset);

        if (adsData != null)
        {
            DrawAdsDataEditor(adsData);
        }

        EndSectionCard();
    }

    void DrawShortcutSection()
    {
        BeginSectionCard("快捷脚本", "常用入口定位");

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawScriptPingButton("Entry", EntryScriptPath);
            DrawScriptPingButton("ADManager", ADManagerScriptPath);
            DrawScriptPingButton("DebugAd", DebugAdScriptPath);
        }

        EndSectionCard();
    }

    void DrawPlayModeSection()
    {
        BeginSectionCard("运行时操作", "Play Mode 下可用");

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play Mode 后，这里会显示常用测试按钮。", MessageType.None);
            EndSectionCard();
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("激励广告"))
            {
                InvokeSingletonMethod(
                    "ADManager",
                    "ShowRewardAD",
                    (Action)(() => Debug.Log("AdeConsole: 奖励广告成功")),
                    (Action)(() => Debug.LogWarning("AdeConsole: 奖励广告失败")));
            }

            if (GUILayout.Button("插屏广告"))
            {
                InvokeSingletonMethod("ADManager", "ShowWhiteAd");
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("显示 Banner"))
            {
                InvokeSingletonMethod("ADManager", "ShowBanner");
            }

            if (GUILayout.Button("隐藏 Banner"))
            {
                InvokeSingletonMethod("ADManager", "HideBanner");
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("打开侧边栏面板"))
            {
                InvokeSingletonMethod("SidebarPlane", "Show", null);
            }

            if (GUILayout.Button("打开推荐流面板"))
            {
                InvokeSingletonMethod("FeedPlane", "Show", null);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("分享"))
            {
                TriggerShareFromConsole();
            }

            if (GUILayout.Button("订阅"))
            {
                TriggerSubscribeFromConsole();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("排行榜"))
            {
                TriggerRankListFromConsole();
            }

            if (GUILayout.Button("推荐流订阅"))
            {
                TriggerFeedFromConsole();
            }
        }

        EndSectionCard();
    }

    void DrawAssetBlock(string label, string expectedPath, UnityEngine.Object fixedPathAsset, string otherAssetPath, Action createAction)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(expectedPath, pathLabelStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(fixedPathAsset, typeof(UnityEngine.Object), false);
                }

                if (fixedPathAsset == null && GUILayout.Button("创建", GUILayout.Width(72f)))
                {
                    createAction?.Invoke();
                }
            }

            if (fixedPathAsset == null && !string.IsNullOrEmpty(otherAssetPath))
            {
                EditorGUILayout.HelpBox($"已找到同类型资源，但不在固定路径下:\n{otherAssetPath}", MessageType.Warning);
            }
            else if (fixedPathAsset == null)
            {
                EditorGUILayout.HelpBox("固定路径下未找到资源。", MessageType.Warning);
            }
        }
    }

    void DrawScriptPingButton(string label, string assetPath)
    {
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
        if (GUILayout.Button($"定位 {label}", GUILayout.Height(24)))
        {
            if (script == null)
            {
                Debug.LogWarning($"AdeConsole: 未找到脚本 {assetPath}");
                return;
            }

            EditorGUIUtility.PingObject(script);
            Selection.activeObject = script;
        }
    }

    void DrawPackageCleanupSection()
    {
        BeginSectionCard("包清理", "按包文件名识别清理目标");
        EditorGUILayout.HelpBox("按包文件名识别清理目标。ByteGame 相关包会直接清理整个 Assets/Plugins/ByteGame。", MessageType.Warning);

        DrawPackageCleanupButton(BgdtPackagePath);
        DrawPackageCleanupButton(MinigamePackagePath);
        EndSectionCard();
    }

    void DrawPackageCleanupButton(string packagePath)
    {
        string[] importedAssets = GetImportedAssetsByPackageFileName(packagePath);
        string buttonLabel = GetCleanupButtonLabel(packagePath);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isUpdating;
            if (GUILayout.Button(buttonLabel, GUILayout.Height(24)))
            {
                DeleteImportedPackageAssets(packagePath, importedAssets);
            }
            GUI.enabled = true;

            EditorGUILayout.LabelField(
                AnyAssetExists(importedAssets) ? "已检测到可删除内容" : "当前工程里未检测到这些导入内容",
                EditorStyles.miniLabel);
        }
    }

    string[] GetImportedAssetsByPackageFileName(string packagePath)
    {
        string fileName = System.IO.Path.GetFileName(packagePath).ToLowerInvariant();
        if (fileName.Contains("bgdt") || fileName.Contains("bytedance"))
        {
            return new[]
            {
                "Assets/Plugins/ByteGame",
            };
        }

        if (fileName.Contains("minigame"))
        {
            return new[]
            {
                "Assets/WX-WASM-SDK-V2",
                "Assets/WebGLTemplates/WXTemplate",
                "Assets/WebGLTemplates/WXTemplate2020",
                "Assets/WebGLTemplates/WXTemplate2022",
                "Assets/WebGLTemplates/WXTemplate2022TJ",
            };
        }

        return Array.Empty<string>();
    }

    string GetCleanupButtonLabel(string packagePath)
    {
        string fileName = System.IO.Path.GetFileName(packagePath).ToLowerInvariant();
        if (fileName.Contains("bgdt") || fileName.Contains("bytedance"))
        {
            return "清理 ByteGame 文件夹";
        }

        if (fileName.Contains("minigame"))
        {
            return "清理 minigame 导入内容";
        }

        return $"清理 {System.IO.Path.GetFileNameWithoutExtension(packagePath)}";
    }

    void DrawAdeDataInfoEditor(UnityEngine.Object adeDataInfo)
    {
        EnsureListField(adeDataInfo, "SubscribeTmplIds");
        EnsureListField(adeDataInfo, "FeedContentIDs");

        SerializedObject serializedObject = new SerializedObject(adeDataInfo);
        serializedObject.Update();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("内容配置", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShareId"), new GUIContent("ShareId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SubscribeTmplIds"), new GUIContent("订阅模板"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FeedContentIDs"), new GUIContent("推荐流内容"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FeedRepeatContentID"), new GUIContent("复访流内容"), true);
            if (EditorGUI.EndChangeCheck())
            {
                SaveSerializedChanges(serializedObject, adeDataInfo);
            }
        }
    }

    void DrawAdsDataEditor(UnityEngine.Object adsData)
    {
        EnsureAdsDataStructure(adsData);

        SerializedObject serializedObject = new SerializedObject(adsData);
        serializedObject.Update();
        SerializedProperty adDataProperty = serializedObject.FindProperty("AdData");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("广告配置", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(adDataProperty.FindPropertyRelative("InterstitialID"), new GUIContent("插屏广告"), true);
            EditorGUILayout.PropertyField(adDataProperty.FindPropertyRelative("BannerID"), new GUIContent("Banner 广告"), true);
            EditorGUILayout.PropertyField(adDataProperty.FindPropertyRelative("RewardID"), new GUIContent("激励广告"), true);
            if (EditorGUI.EndChangeCheck())
            {
                SaveSerializedChanges(serializedObject, adsData);
            }
        }
    }

    void BuildStyles()
    {
        if (sectionTitleStyle != null)
        {
            return;
        }

        sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
        };

        sectionNoteStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
        };

        summaryLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
        };

        summaryValueStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
        };

        pathLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
            richText = false,
        };
    }

    void BeginSectionCard(string title, string subtitle)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, sectionTitleStyle);
        if (!string.IsNullOrEmpty(subtitle))
        {
            EditorGUILayout.LabelField(subtitle, sectionNoteStyle);
        }
        EditorGUILayout.Space(6f);
    }

    void EndSectionCard()
    {
        EditorGUILayout.EndVertical();
    }

    void DrawSummaryRow(string label, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, summaryLabelStyle, GUILayout.Width(90f));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(value) ? "无" : value, summaryValueStyle);
        }
    }

    void SaveSerializedChanges(SerializedObject serializedObject, UnityEngine.Object asset)
    {
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
    }

    void ApplyPlatformPresetToEditor(List<string> symbols, string symbol)
    {
        List<string> newSymbols = symbols
            .Where(item => !KnownPlatformSymbols.Contains(item))
            .ToList();

        if (!string.IsNullOrEmpty(symbol))
        {
            newSymbols.Add(symbol);
        }

        editableCustomSymbols.Clear();
        editableCustomSymbols.AddRange(newSymbols.Distinct().OrderBy(item => item));
        customSymbolsDirty = true;
    }

    List<string> GetSymbols(BuildTargetGroup group)
    {
        string defineString = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        return defineString
            .Split(';')
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct()
            .ToList();
    }

    string GetCurrentPlatformSymbol(List<string> symbols)
    {
        foreach (PlatformPreset preset in presets)
        {
            if (IsCurrentPreset(symbols, preset.Symbol))
            {
                return string.IsNullOrEmpty(preset.Symbol) ? preset.Label : preset.Symbol;
            }
        }

        string customSymbol = symbols.FirstOrDefault(item => item.StartsWith("Ade_"));
        return string.IsNullOrEmpty(customSymbol) ? "未识别" : customSymbol;
    }

    bool IsCurrentPreset(List<string> symbols, string symbol)
    {
        string currentSymbol = symbols.FirstOrDefault(item => item.StartsWith("Ade_"));

        if (string.IsNullOrEmpty(symbol))
        {
            return string.IsNullOrEmpty(currentSymbol);
        }

        return currentSymbol == symbol;
    }

    void SyncCustomSymbols(BuildTargetGroup group, List<string> symbols)
    {
        string defineString = BuildDefineString(symbols);
        if (cachedCustomGroup != group)
        {
            LoadCustomSymbols(group, symbols);
            return;
        }

        if (!customSymbolsDirty && cachedDefineString != defineString)
        {
            LoadCustomSymbols(group, symbols);
        }
    }

    void LoadCustomSymbols(BuildTargetGroup group, List<string> symbols)
    {
        cachedCustomGroup = group;
        cachedDefineString = BuildDefineString(symbols);
        editableCustomSymbols.Clear();
        editableCustomSymbols.AddRange(symbols.OrderBy(item => item));
        customSymbolsDirty = false;
    }

    void SaveCustomSymbols(BuildTargetGroup group)
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !customSymbolsDirty)
        {
            return;
        }

        List<string> result = GetSanitizedCustomSymbols(true);

        string defineString = BuildDefineString(result);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defineString);
        LoadCustomSymbols(group, result);
        Debug.Log($"AdeConsole: 宏定义已保存，当前宏为 {defineString}");
        Repaint();
    }

    void LoadFeedLaunchMode()
    {
        cachedFeedLaunchMode = (FeedLaunchMode)PlayerPrefs.GetInt(
            FeedLaunchModePlayerPrefsKey,
            (int)FeedLaunchMode.None);
        selectedFeedLaunchMode = cachedFeedLaunchMode;
        launchModeDirty = false;
    }

    void SyncFeedLaunchMode()
    {
        FeedLaunchMode currentMode = (FeedLaunchMode)PlayerPrefs.GetInt(
            FeedLaunchModePlayerPrefsKey,
            (int)FeedLaunchMode.None);

        if (!launchModeDirty && currentMode != cachedFeedLaunchMode)
        {
            cachedFeedLaunchMode = currentMode;
            selectedFeedLaunchMode = currentMode;
        }
    }

    void SaveFeedLaunchMode()
    {
        if (!launchModeDirty)
        {
            return;
        }

        PlayerPrefs.SetInt(FeedLaunchModePlayerPrefsKey, (int)selectedFeedLaunchMode);
        PlayerPrefs.Save();
        cachedFeedLaunchMode = selectedFeedLaunchMode;
        launchModeDirty = false;
        Debug.Log($"AdeConsole: 推荐流启动模拟已切换为 {GetFeedLaunchModeLabel(selectedFeedLaunchMode)}");
    }

    int GetCurrentPresetIndex(List<string> symbols)
    {
        for (int i = 0; i < presets.Length; i++)
        {
            if (IsCurrentPreset(symbols, presets[i].Symbol))
            {
                return i;
            }
        }

        return presets.Length;
    }

    string[] GetPlatformPresetLabels()
    {
        List<string> labels = presets.Select(item => item.Label).ToList();
        labels.Add("自定义");
        return labels.ToArray();
    }

    int GetFeedLaunchModeIndex(FeedLaunchMode mode, FeedLaunchMode[] launchModeValues)
    {
        for (int i = 0; i < launchModeValues.Length; i++)
        {
            if (launchModeValues[i] == mode)
            {
                return i;
            }
        }

        return 0;
    }

    string GetFeedLaunchModeLabel(FeedLaunchMode mode)
    {
        switch (mode)
        {
            case FeedLaunchMode.FeedDirectPlay:
                return "复访启动";
            case FeedLaunchMode.FeedAcquisition:
                return "获客启动";
            default:
                return "正常启动";
        }
    }

    enum FeedLaunchMode
    {
        None = 0,
        FeedDirectPlay = 1,
        FeedAcquisition = 2,
    }

    List<string> GetSanitizedCustomSymbols(bool logWarnings)
    {
        List<string> sanitizedSymbols = editableCustomSymbols
            .Select(SanitizeSymbol)
            .Where(item => !string.IsNullOrEmpty(item))
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        if (logWarnings && sanitizedSymbols.Count != editableCustomSymbols.Count(item => !string.IsNullOrWhiteSpace(item)))
        {
            Debug.LogWarning("AdeConsole: 已自动忽略空白或重复条目。");
        }

        return sanitizedSymbols;
    }

    string SanitizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        string sanitized = symbol.Trim();
        if (sanitized.Contains(";"))
        {
            Debug.LogWarning($"AdeConsole: 宏 {sanitized} 包含非法字符 ';'，已忽略。");
            return null;
        }

        return sanitized;
    }

    string BuildDefineString(List<string> symbols)
    {
        return string.Join(";", symbols
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct()
            .OrderBy(item => item));
    }

    string FindOtherAssetPath(string typeName, string expectedPath)
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeName}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != expectedPath)
            {
                return path;
            }
        }

        return null;
    }

    void DeleteImportedPackageAssets(string packagePath, string[] importedAssets)
    {
        string[] existingAssets = importedAssets.Where(AssetExists).ToArray();
        if (existingAssets.Length == 0)
        {
            EditorUtility.DisplayDialog("未找到内容", $"当前工程中没有检测到来自\n{packagePath}\n的已导入内容。", "确定");
            return;
        }

        string assetList = string.Join("\n", existingAssets);
        bool confirmed = EditorUtility.DisplayDialog(
            "确认删除包内容",
            $"将删除以下内容：\n{assetList}\n\n来源包：\n{packagePath}",
            "删除",
            "取消");

        if (!confirmed)
        {
            return;
        }

        int deletedCount = 0;
        foreach (string assetPath in existingAssets)
        {
            if (AssetDatabase.DeleteAsset(assetPath))
            {
                deletedCount++;
            }
            else
            {
                Debug.LogWarning($"AdeConsole: 删除失败 {assetPath}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"AdeConsole: 已删除 {deletedCount} 项包内容");
        Repaint();
    }

    bool AnyAssetExists(IEnumerable<string> assetPaths)
    {
        return assetPaths.Any(AssetExists);
    }

    bool AssetExists(string assetPath)
    {
        return AssetDatabase.IsValidFolder(assetPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null;
    }

    void CreateAdeDataInfoAsset()
    {
        CreateScriptableAsset("AdeDataInfo", AdeDataInfoPath, asset =>
        {
            EnsureListField(asset, "SubscribeTmplIds");
            EnsureListField(asset, "FeedContentIDs");
        });
    }

    void CreateAdsDataAsset()
    {
        CreateScriptableAsset("AdsData", AdsDataPath, EnsureAdsDataStructure);
    }

    void EnsureResourceFolder()
    {
        if (AssetDatabase.IsValidFolder(ResourceFolderPath))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Ade_Framework/Resources"))
        {
            AssetDatabase.CreateFolder("Assets/Ade_Framework", "Resources");
        }

        AssetDatabase.CreateFolder("Assets/Ade_Framework/Resources", "ScriptableObject");
    }

    void TriggerShareFromConsole()
    {
        if (InvokeSingletonMethod("Ade_Framework.AdeSDK", "OnShare", (Action)(() => Debug.Log("AdeConsole: 分享成功"))))
        {
            Debug.Log("AdeConsole: 已调用分享接口");
        }
        else
        {
            Debug.LogWarning("AdeConsole: 当前环境不可用分享接口");
        }
    }

    void TriggerSubscribeFromConsole()
    {
        if (InvokeSingletonMethod("Ade_Framework.AdeSDK", "OnRequestSubscribeMessage", (Action)(() => Debug.Log("AdeConsole: 订阅成功"))))
        {
            Debug.Log("AdeConsole: 已调用订阅接口");
        }
        else
        {
            Debug.LogWarning("AdeConsole: 当前环境不可用订阅接口");
        }
    }

    void TriggerRankListFromConsole()
    {
        if (!InvokeSingletonMethod("Ade_Framework.AdeSDK", "RefreshLeaderboard"))
        {
            Debug.LogWarning("AdeConsole: 当前环境不可用排行榜接口");
        }
    }

    void TriggerFeedFromConsole()
    {
        if (!InvokeSingletonMethod("Ade_Framework.AdeSDK", "CheckAndRequestFeed"))
        {
            Debug.LogWarning("AdeConsole: 当前环境不可用推荐流接口");
        }
    }

    bool InvokeSingletonMethod(string typeName, string methodName, params object[] args)
    {
        try
        {
            Type targetType = FindType(typeName);
            if (targetType == null)
            {
                Debug.LogWarning($"AdeConsole: 未找到类型 {typeName}");
                return false;
            }

            object instance = targetType.GetProperty("Instance")?.GetValue(null);
            if (instance == null)
            {
                Debug.LogWarning($"AdeConsole: 未获取到 {typeName}.Instance");
                return false;
            }

            object[] actualArgs = args ?? Array.Empty<object>();
            Type[] argTypes = actualArgs.Select(item => item?.GetType() ?? typeof(object)).ToArray();
            var method = targetType.GetMethod(methodName, argTypes);
            if (method == null)
            {
                method = targetType
                    .GetMethods()
                    .FirstOrDefault(candidate =>
                        candidate.Name == methodName &&
                        candidate.GetParameters().Length == actualArgs.Length);
            }

            if (method == null)
            {
                Debug.LogWarning($"AdeConsole: 未找到方法 {typeName}.{methodName}");
                return false;
            }

            method.Invoke(instance, actualArgs);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"AdeConsole: 调用 {typeName}.{methodName} 失败\n{exception.Message}");
            return false;
        }
    }

    Type FindType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName, false);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    void CreateScriptableAsset(string typeName, string assetPath, Action<UnityEngine.Object> initializeAction = null)
    {
        EnsureResourceFolder();

        Type assetType = FindType(typeName);
        if (assetType == null || !typeof(ScriptableObject).IsAssignableFrom(assetType))
        {
            Debug.LogWarning($"AdeConsole: 无法创建资源，未找到类型 {typeName}");
            return;
        }

        var asset = ScriptableObject.CreateInstance(assetType);
        initializeAction?.Invoke(asset);
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    void EnsureListField(UnityEngine.Object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName);
        if (field == null || field.GetValue(target) != null)
        {
            return;
        }

        field.SetValue(target, new List<string>());
        EditorUtility.SetDirty(target);
    }

    void EnsureAdsDataStructure(UnityEngine.Object adsData)
    {
        Type adsDataType = adsData.GetType();
        var adDataField = adsDataType.GetField("AdData");
        if (adDataField == null)
        {
            return;
        }

        Type adsPlatformDataType = FindType("AdsPlatformData");
        Type adItemDataType = FindType("AdItemData");
        if (adsPlatformDataType == null || adItemDataType == null)
        {
            return;
        }

        object adDataValue = adDataField.GetValue(adsData);
        if (adDataValue == null)
        {
            adDataValue = Activator.CreateInstance(adsPlatformDataType);
            adDataField.SetValue(adsData, adDataValue);
            EditorUtility.SetDirty(adsData);
        }

        EnsureNestedObjectField(adDataValue, "InterstitialID", adItemDataType, adsData);
        EnsureNestedObjectField(adDataValue, "BannerID", adItemDataType, adsData);

        var rewardField = adsPlatformDataType.GetField("RewardID");
        if (rewardField != null && rewardField.GetValue(adDataValue) == null)
        {
            rewardField.SetValue(adDataValue, Array.CreateInstance(adItemDataType, 0));
            EditorUtility.SetDirty(adsData);
        }
    }

    void EnsureNestedObjectField(object target, string fieldName, Type fieldType, UnityEngine.Object dirtyAsset)
    {
        var field = target.GetType().GetField(fieldName);
        if (field == null || field.GetValue(target) != null)
        {
            return;
        }

        field.SetValue(target, Activator.CreateInstance(fieldType));
        EditorUtility.SetDirty(dirtyAsset);
    }

    struct PlatformPreset
    {
        public string Label;
        public string Symbol;
        public string Description;

        public PlatformPreset(string label, string symbol, string description)
        {
            Label = label;
            Symbol = symbol;
            Description = description;
        }
    }
}
