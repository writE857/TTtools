using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

public class AdeConsoleWindow : EditorWindow
{
    const string AdeDataInfoPath = "Assets/Ade_Framework/Resources/ScriptableObject/AdeDataInfo.asset";
    const string AdsDataPath = "Assets/Ade_Framework/Resources/ScriptableObject/AdsData.asset";
    const string EntryScriptPath = "Assets/Ade_Framework/Scriptes/1_Enter/Entry.cs";
    const string ADManagerScriptPath = "Assets/Ade_Framework/Scriptes/Ads/ADManager.cs";
    const string DebugAdScriptPath = "Assets/Ade_Framework/Scriptes/Debug/DebugAd.cs";
    const string ResourceFolderPath = "Assets/Ade_Framework/Resources/ScriptableObject";
    const string FeedLaunchModeEditorPrefsKey = "Ade.Editor.FeedLaunchMode";
    const string SidebarUIPrefabPath = "Assets/Ade_Framework/Resources/SidebarUI.prefab";
    const string NoAdsSymbol = "ADE_NO_ADS";
    const string DebugSymbol = "Ade_Debug";
    const string ProjectSettingsAssetPath = "ProjectSettings/ProjectSettings.asset";
    const float ListSelectHandleWidth = 16f;
    const float ListSelectContentOffset = 18f;

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
    bool rewardConfigDirty;
    int selectedPlatformPresetIndex;
    FeedLaunchMode selectedFeedLaunchMode;
    FeedLaunchMode cachedFeedLaunchMode;
    string rewardShareIdDraft = string.Empty;
    readonly AdItemDraft interstitialAdDraft = new();
    readonly AdItemDraft bannerAdDraft = new();
    readonly List<string> subscribeTemplateDrafts = new();
    readonly List<string> feedRepeatContentDrafts = new();
    readonly List<string> feedAcquisitionContentDrafts = new();
    readonly List<AdItemDraft> rewardAdDrafts = new();
    readonly List<GridAdDraft> gridAdDrafts = new();
    readonly MoreGamesDraft moreGamesDraft = new();
    readonly List<MoreGamesQueryDraft> moreGamesQueryDrafts = new();
    readonly List<string> editableCustomSymbols = new();
    readonly ListSelectionState customSymbolSelection = new();
    readonly ListSelectionState subscribeTemplateSelection = new();
    readonly ListSelectionState feedRepeatContentSelection = new();
    readonly ListSelectionState feedAcquisitionContentSelection = new();
    readonly ListSelectionState rewardAdSelection = new();
    readonly ListSelectionState gridAdSelection = new();
    readonly ListSelectionState moreGamesQuerySelection = new();
    ReorderableList customSymbolList;
    ReorderableList subscribeTemplateList;
    ReorderableList feedRepeatContentList;
    ReorderableList feedAcquisitionContentList;
    ReorderableList rewardAdList;
    ReorderableList gridAdList;
    ReorderableList moreGamesQueryList;
    ListSelectionState activeListSelection;
    ReorderableList activeReorderableList;
    GUIStyle sectionTitleStyle;
    GUIStyle sectionNoteStyle;
    GUIStyle summaryLabelStyle;
    GUIStyle pathLabelStyle;
    GUIStyle templateNameSelectedStyle;
    GUIStyle templateNameStyle;
    GameObject sidebarPrefabRoot;
    bool sidebarPrefabDirty;

    [MenuItem("Ade_Tools/Ade 控制台")]
    public static void OpenWindow()
    {
        GetWindow<AdeConsoleWindow>("Ade 控制台");
    }

    void OnEnable()
    {
        minSize = new Vector2(720f, 520f);
        LoadFeedLaunchMode();
        EnsureEditorStateInitialized();
    }

    void OnDisable()
    {
        if (sidebarPrefabDirty)
        {
            SaveSidebarPrefabContents();
        }

        UnloadSidebarPrefabContents();
    }

    void EnsureEditorStateInitialized()
    {
        if (customSymbolList != null && subscribeTemplateList != null && feedRepeatContentList != null && feedAcquisitionContentList != null && rewardAdList != null && gridAdList != null && moreGamesQueryList != null)
        {
            return;
        }

        customSymbolList = CreateCustomSymbolListEditor();
        subscribeTemplateList = CreateStringListEditor(subscribeTemplateDrafts, "订阅模板", subscribeTemplateSelection, () => rewardConfigDirty = true);
        feedRepeatContentList = CreateStringListEditor(feedRepeatContentDrafts, "复访流内容", feedRepeatContentSelection, () => rewardConfigDirty = true);
        feedAcquisitionContentList = CreateStringListEditor(feedAcquisitionContentDrafts, "获客流内容", feedAcquisitionContentSelection, () => rewardConfigDirty = true);
        rewardAdList = CreateAdItemListEditor(rewardAdDrafts, "激励参数", rewardAdSelection);
        gridAdList = CreateGridAdListEditor(gridAdDrafts, "格子广告参数", gridAdSelection);
        moreGamesQueryList = CreateMoreGamesQueryListEditor(moreGamesQueryDrafts, "更多游戏 Query", moreGamesQuerySelection);
        LoadRewardConfigDrafts();
    }

    void OnGUI()
    {
        EnsureEditorStateInitialized();
        BuildStyles();
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        List<string> symbols = GetSymbols(group);
        HandleKeyboardShortcuts(group, symbols);
        bool beganScrollView = false;

        try
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            beganScrollView = true;

            DrawQuadrantLayout(group, symbols);
        }
        catch (ExitGUIException)
        {
            throw;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (beganScrollView)
            {
                EditorGUILayout.EndScrollView();
            }
        }
    }

    void HandleKeyboardShortcuts(BuildTargetGroup group, List<string> symbols)
    {
        Event currentEvent = Event.current;
        if (currentEvent.type != EventType.KeyDown || !(currentEvent.control || currentEvent.command))
        {
            return;
        }

        if (currentEvent.keyCode == KeyCode.S)
        {
            SaveAllPendingEditorDrafts(group);
            currentEvent.Use();
            GUIUtility.ExitGUI();
        }

        if (currentEvent.keyCode == KeyCode.Z)
        {
            RevertPendingEditorDrafts(group, symbols);
            currentEvent.Use();
            GUIUtility.ExitGUI();
        }

        if (currentEvent.keyCode == KeyCode.A && SelectAllActiveList())
        {
            currentEvent.Use();
            GUIUtility.ExitGUI();
        }
    }

    bool SelectAllActiveList()
    {
        if (EditorGUIUtility.editingTextField || activeListSelection == null || activeReorderableList?.list == null)
        {
            return false;
        }

        int itemCount = activeReorderableList.list.Count;
        if (itemCount <= 0)
        {
            return false;
        }

        activeListSelection.SelectAll(itemCount);
        activeReorderableList.index = 0;
        Repaint();
        return true;
    }

    void SaveAllPendingEditorDrafts(BuildTargetGroup group)
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("AdeConsole: Unity 正在编译或刷新资源，暂不能保存。");
            return;
        }

        bool hasChanges = customSymbolsDirty || launchModeDirty || rewardConfigDirty || sidebarPrefabDirty;
        if (customSymbolsDirty)
        {
            SaveCustomSymbols(group);
        }

        if (launchModeDirty)
        {
            SaveFeedLaunchMode();
        }

        if (rewardConfigDirty)
        {
            SaveRewardConfigDrafts();
        }

        if (sidebarPrefabDirty)
        {
            SaveSidebarPrefabContents();
        }

        if (hasChanges)
        {
            AssetDatabase.SaveAssets();
        }
    }

    void RevertPendingEditorDrafts(BuildTargetGroup group, List<string> symbols)
    {
        bool reverted = false;
        if (customSymbolsDirty)
        {
            LoadCustomSymbols(group, symbols);
            reverted = true;
        }

        if (launchModeDirty)
        {
            LoadFeedLaunchMode();
            reverted = true;
        }

        if (rewardConfigDirty)
        {
            LoadRewardConfigDrafts();
            reverted = true;
        }

        if (sidebarPrefabDirty)
        {
            ReloadSidebarPrefabContents();
            reverted = true;
        }

        if (!reverted)
        {
            Undo.PerformUndo();
            return;
        }

        Repaint();
    }

    void DrawQuadrantLayout(BuildTargetGroup group, List<string> symbols)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawQuadrantColumn(() => DrawDefineSection(group, symbols));
            GUILayout.Space(10f);
            DrawQuadrantColumn(DrawWebGLTemplateSection);
        }

        GUILayout.Space(10f);

        DrawRewardConfigSection(group, symbols);
    }

    void DrawQuadrantColumn(Action drawContent)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.5f - 18f)))
        {
            drawContent?.Invoke();
        }
    }

    void DrawDefineSection(BuildTargetGroup group, List<string> symbols)
    {
        SyncCustomSymbols(group, symbols);
        SyncFeedLaunchMode();

        BeginSectionCard("宏定义", "平台预设、推荐流模拟、宏列表");
        DrawDefineStatusLine(group, symbols);
        EditorGUILayout.LabelField("先选预设，再补充自定义宏。", sectionNoteStyle);

        string[] presetLabels = GetPlatformPresetLabels();
        selectedPlatformPresetIndex = GetCurrentPresetIndex(editableCustomSymbols);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("平台预设", summaryLabelStyle, GUILayout.Width(90f));
            int nextPresetIndex = EditorGUILayout.Popup(selectedPlatformPresetIndex, presetLabels);
            if (nextPresetIndex != selectedPlatformPresetIndex)
            {
                selectedPlatformPresetIndex = nextPresetIndex;
                if (selectedPlatformPresetIndex < presets.Length)
                {
                    ApplyPlatformPresetToEditor(editableCustomSymbols, presets[selectedPlatformPresetIndex].Symbol);
                }
            }
        }
        string presetDescription = selectedPlatformPresetIndex < presets.Length
            ? presets[selectedPlatformPresetIndex].Description
            : "当前宏不是内置平台预设，可直接在下方列表继续编辑。";
        EditorGUILayout.LabelField(presetDescription, sectionNoteStyle);

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
        EditorGUILayout.LabelField($"当前模拟模式: {GetFeedLaunchModeLabel(selectedFeedLaunchMode)}。仅用于编辑器测试，构建包不读取。", sectionNoteStyle);

        DrawNoAdsDraftMode();
        EditorGUILayout.LabelField("广告模式通过 ADE_NO_ADS 宏控制，选择后需点击应用才写入项目。", sectionNoteStyle);
        DrawDebugDraftMode();

        EditorGUILayout.Space(4f);
        EnsureCustomSymbolListBinding();
        customSymbolList.DoLayoutList();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("复制宏", GUILayout.Height(24)))
            {
                EditorGUIUtility.systemCopyBuffer = BuildDefineString(GetSanitizedCustomSymbols(false));
            }

            GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isUpdating && (customSymbolsDirty || launchModeDirty);
            if (GUILayout.Button("应用", GUILayout.Height(24)))
            {
                SaveCustomSymbols(group);
                SaveFeedLaunchMode();
            }
            GUI.enabled = true;
        }

        EndSectionCard();
    }

    void DrawDefineStatusLine(BuildTargetGroup group, List<string> symbols)
    {
        string symbolList = symbols.Count > 0 ? string.Join("; ", symbols) : "无";
        EditorGUILayout.LabelField(
            $"当前: {group} / {GetCurrentPlatformSymbol(symbols)} / {symbolList}",
            sectionNoteStyle);
    }

    void EnsureCustomSymbolListBinding()
    {
        if (customSymbolList == null || customSymbolList.list != editableCustomSymbols)
        {
            customSymbolList = CreateCustomSymbolListEditor();
        }
    }

    void DrawNoAdsDraftMode()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("广告模式", summaryLabelStyle, GUILayout.Width(90f));
            bool noAdsEnabled = editableCustomSymbols.Contains(NoAdsSymbol);
            int currentModeIndex = noAdsEnabled ? 1 : 0;
            int nextModeIndex = GUILayout.Toolbar(currentModeIndex, new[] { "正常广告", "无广模式" }, GUILayout.Height(20f));
            if (nextModeIndex != currentModeIndex)
            {
                SetNoAdsModeInEditor(nextModeIndex == 1);
            }
        }
    }

    void DrawDebugDraftMode()
    {
        Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
        rowRect.y += 1f;
        rowRect.height = EditorGUIUtility.singleLineHeight;
        Rect toggleRect = new Rect(rowRect.x, rowRect.y, Mathf.Min(180f, rowRect.width), rowRect.height);

        bool hasDebugSymbol = editableCustomSymbols.Contains(DebugSymbol);
        bool nextHasDebugSymbol = EditorGUI.ToggleLeft(toggleRect, "启用 Ade_Debug", hasDebugSymbol);
        if (nextHasDebugSymbol != hasDebugSymbol)
        {
            SetManagedSymbol(DebugSymbol, nextHasDebugSymbol);
        }
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

    void DrawRewardConfigSection(BuildTargetGroup group, List<string> symbols)
    {
        BeginSectionCard("激励参数", "AdeDataInfo 与激励广告配置");

        UnityEngine.Object adeDataInfo = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdeDataInfoPath);
        UnityEngine.Object adsData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdsDataPath);

        float columnWidth = Mathf.Max(320f, (position.width - 54f) * 0.5f);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(columnWidth)))
            {
                DrawAssetBlock(
                    "AdsData",
                    AdsDataPath,
                    adsData,
                    FindOtherAssetPath("AdsData", AdsDataPath),
                    CreateAdsDataAsset);

                if (adsData != null)
                {
                    DrawRewardOnlyEditor();
                }
            }

            GUILayout.Space(8f);

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(columnWidth)))
            {
                DrawAssetBlock(
                    "AdeDataInfo",
                    AdeDataInfoPath,
                    adeDataInfo,
                    FindOtherAssetPath("AdeDataInfo", AdeDataInfoPath),
                    CreateAdeDataInfoAsset);

                if (adeDataInfo != null)
                {
                    DrawRewardAdeDataInfoEditor();
                }
            }
        }

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = rewardConfigDirty && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
            if (GUILayout.Button("应用参数", GUILayout.Height(24)))
            {
                SaveRewardConfigDrafts();
            }
            GUI.enabled = true;
        }

        EndSectionCard();
    }

    void DrawRewardAdeDataInfoEditor()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("内容参数", EditorStyles.miniBoldLabel);
            DrawCompactContentIdField("分享ID", rewardShareIdDraft, value => rewardShareIdDraft = value, 44f);
            subscribeTemplateList.DoLayoutList();

            EditorGUILayout.Space(2f);
            feedRepeatContentList.DoLayoutList();
            feedAcquisitionContentList.DoLayoutList();
        }
    }

    void DrawCompactContentIdField(string label, string value, Action<string> applyValue, float labelWidth)
    {
        Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
        rowRect.y += 1f;
        rowRect.height = EditorGUIUtility.singleLineHeight;

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        try
        {
            EditorGUIUtility.labelWidth = labelWidth;
            string newValue = EditorGUI.TextField(rowRect, label, value);
            if (newValue != value)
            {
                applyValue?.Invoke(newValue);
                rewardConfigDirty = true;
            }
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }
    }

    void DrawWebGLTemplateSection()
    {
        BeginSectionCard("WebGL Template", "模板切换");

        string currentTemplate = GetCurrentWebGLTemplate();
        List<WebGLTemplateDraft> templates = GetAvailableWebGLTemplates(currentTemplate);
        DrawUnityStyleTemplateSelector(templates, currentTemplate);
        DrawSidebarPrefabEditorSection();
        EndSectionCard();
    }

    void DrawUnityStyleTemplateSelector(List<WebGLTemplateDraft> templates, string currentTemplate)
    {
        EditorGUILayout.LabelField("WebGL Template", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width * 0.5f - 40f) / 84f));
        for (int i = 0; i < templates.Count; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int col = 0; col < columns && i + col < templates.Count; col++)
                {
                    DrawUnityTemplateCard(templates[i + col], currentTemplate);
                    GUILayout.Space(6f);
                }
            }

            if (i + columns < templates.Count)
            {
                GUILayout.Space(4f);
            }
        }
    }

    void DrawSidebarPrefabEditorSection()
    {
        EditorGUILayout.Space(4f);

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SidebarUIPrefabPath);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("SidebarUI.prefab", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();

                GUI.enabled = sidebarPrefabDirty;
                if (GUILayout.Button("保存", GUILayout.Width(56f)))
                {
                    SaveSidebarPrefabContents();
                }
                GUI.enabled = true;
            }

            if (prefabAsset == null)
            {
                EditorGUILayout.HelpBox("固定路径下未找到 SidebarUI.prefab。", MessageType.Warning);
                return;
            }

            if (!EnsureSidebarPrefabContentsLoaded())
            {
                EditorGUILayout.HelpBox("SidebarUI.prefab 加载失败。", MessageType.Warning);
                return;
            }

            DrawSidebarFixedImageSlot("mask/icon", "icon");
            DrawSidebarAppNameSlot("AppName1", "AppName2");
        }
    }

    bool EnsureSidebarPrefabContentsLoaded()
    {
        if (sidebarPrefabRoot != null)
        {
            return true;
        }

        try
        {
            sidebarPrefabRoot = PrefabUtility.LoadPrefabContents(SidebarUIPrefabPath);
            sidebarPrefabDirty = false;
            return sidebarPrefabRoot != null;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
    }

    void SaveSidebarPrefabContents()
    {
        if (sidebarPrefabRoot == null)
        {
            sidebarPrefabDirty = false;
            return;
        }

        PrefabUtility.SaveAsPrefabAsset(sidebarPrefabRoot, SidebarUIPrefabPath);
        sidebarPrefabDirty = false;
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(SidebarUIPrefabPath);
        Repaint();
    }

    void ReloadSidebarPrefabContents()
    {
        UnloadSidebarPrefabContents();
        EnsureSidebarPrefabContentsLoaded();
        Repaint();
    }

    void UnloadSidebarPrefabContents()
    {
        if (sidebarPrefabRoot != null)
        {
            PrefabUtility.UnloadPrefabContents(sidebarPrefabRoot);
        }

        sidebarPrefabRoot = null;
        sidebarPrefabDirty = false;
    }

    GameObject FindSidebarObject(string objectName)
    {
        Transform transform = FindSidebarTransform(sidebarPrefabRoot != null ? sidebarPrefabRoot.transform : null, objectName);
        return transform != null ? transform.gameObject : null;
    }

    Transform FindSidebarTransform(Transform current, string objectName)
    {
        if (current == null)
        {
            return null;
        }

        if (objectName.IndexOf('/') >= 0)
        {
            string[] segments = objectName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return FindSidebarTransformByPath(current, segments, 0);
        }

        if (string.Equals(current.name, objectName, StringComparison.Ordinal))
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform found = FindSidebarTransform(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    Transform FindSidebarTransformByPath(Transform current, string[] segments, int index)
    {
        if (current == null)
        {
            return null;
        }

        if (index >= segments.Length)
        {
            return current;
        }

        string segment = segments[index];
        foreach (Transform child in current)
        {
            if (!string.Equals(child.name, segment, StringComparison.Ordinal))
            {
                continue;
            }

            Transform matched = FindSidebarTransformByPath(child, segments, index + 1);
            if (matched != null)
            {
                return matched;
            }
        }

        foreach (Transform child in current)
        {
            Transform matched = FindSidebarTransformByPath(child, segments, index);
            if (matched != null)
            {
                return matched;
            }
        }

        return null;
    }

    void DrawSidebarFixedImageSlot(string objectName, string label)
    {
        GameObject gameObject = FindSidebarObject(objectName);
        if (gameObject == null)
        {
            EditorGUILayout.HelpBox($"{label} 节点未找到。", MessageType.Warning);
            return;
        }

        Image image = gameObject.GetComponent<Image>();
        if (image == null)
        {
            EditorGUILayout.HelpBox($"{label} 没有 Image 组件。", MessageType.Warning);
            return;
        }

        Rect rowRect = EditorGUILayout.GetControlRect(false, 64f);
        Rect labelRect = new Rect(rowRect.x, rowRect.y + 24f, 60f, EditorGUIUtility.singleLineHeight);
        Rect fieldRect = new Rect(rowRect.xMax - 64f, rowRect.y, 64f, 64f);

        GUI.Label(labelRect, label, EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        Sprite sprite = (Sprite)EditorGUI.ObjectField(fieldRect, image.sprite, typeof(Sprite), false);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(image, "编辑 SidebarUI 图片");
            image.sprite = sprite;
            MarkSidebarPrefabDirty(image);
        }
    }

    void DrawSidebarAppNameSlot(string clickObjectName, string appObjectName)
    {
        GameObject clickObject = FindSidebarObject(clickObjectName);
        GameObject appObject = FindSidebarObject(appObjectName);
        if (clickObject == null || appObject == null)
        {
            EditorGUILayout.HelpBox("AppName 节点未找到。", MessageType.Warning);
            return;
        }

        Text clickText = clickObject.GetComponent<Text>();
        Text appText = appObject.GetComponent<Text>();
        if (clickText == null || appText == null)
        {
            EditorGUILayout.HelpBox("AppName 节点缺少 Text 组件。", MessageType.Warning);
            return;
        }

        Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
        Rect labelRect = new Rect(rowRect.x, rowRect.y, 60f, rowRect.height);
        Rect fieldRect = new Rect(rowRect.x + 64f, rowRect.y, rowRect.width - 64f, rowRect.height);

        GUI.Label(labelRect, "appname", EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        string baseValue = string.IsNullOrEmpty(appText.text) ? StripClickPrefix(clickText.text) : appText.text;
        string value = EditorGUI.TextField(fieldRect, baseValue);
        if (EditorGUI.EndChangeCheck())
        {
            string clickValue = string.IsNullOrEmpty(value) ? value : $"点击{value}";
            Undo.RecordObject(clickText, "编辑 SidebarUI 文本");
            Undo.RecordObject(appText, "编辑 SidebarUI 文本");
            clickText.text = clickValue;
            appText.text = value;
            MarkSidebarPrefabDirty(appText);
        }
    }

    string StripClickPrefix(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.StartsWith("点击", StringComparison.Ordinal) ? value.Substring(2) : value;
    }

    void MarkSidebarPrefabDirty(UnityEngine.Object target)
    {
        if (target != null)
        {
            EditorUtility.SetDirty(target);
        }

        sidebarPrefabDirty = true;
    }

    void DrawUnityTemplateCard(WebGLTemplateDraft template, string currentTemplate)
    {
        bool isSelected = string.Equals(template.Value, currentTemplate, StringComparison.Ordinal);

        using (new EditorGUILayout.VerticalScope(GUILayout.Width(78f)))
        {
            Rect cardRect = GUILayoutUtility.GetRect(78f, 92f, GUILayout.Width(78f), GUILayout.Height(92f));
            Rect previewRect = new Rect(cardRect.x + 3f, cardRect.y, 72f, 72f);
            Rect labelRect = new Rect(cardRect.x, previewRect.yMax + 3f, cardRect.width, 16f);

            EditorGUI.DrawRect(previewRect, new Color(0.26f, 0.26f, 0.26f));
            Texture thumbnail = template.Thumbnail;
            if (thumbnail != null)
            {
                GUI.DrawTexture(previewRect, thumbnail, ScaleMode.ScaleToFit);
            }
            else
            {
                Texture fallbackIcon = EditorGUIUtility.IconContent("BuildSettings.WebGL.Small")?.image
                    ?? EditorGUIUtility.IconContent("BuildSettings.WebGL")?.image;
                if (fallbackIcon != null)
                {
                    GUI.DrawTexture(new Rect(previewRect.x + 4f, previewRect.y + 4f, 16f, 16f), fallbackIcon, ScaleMode.ScaleToFit);
                }
                else
                {
                    DrawFallbackTemplateHtml5Icon(new Rect(previewRect.x + 5f, previewRect.y + 4f, 13f, 15f));
                }
            }

            DrawTemplatePreviewBorder(previewRect, isSelected);
            if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none) && !isSelected)
            {
                SetCurrentWebGLTemplate(template.Value);
            }

            if (isSelected)
            {
                EditorGUI.DrawRect(labelRect, new Color(0.12f, 0.38f, 0.9f));
            }
            GUI.Label(labelRect, template.DisplayName, isSelected ? templateNameSelectedStyle : templateNameStyle);
        }
    }

    void DrawTemplatePreviewBorder(Rect rect, bool selected)
    {
        Color borderColor = selected ? new Color(0.12f, 0.38f, 0.9f) : new Color(0.12f, 0.12f, 0.12f);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), borderColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), borderColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), borderColor);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), borderColor);
    }

    void DrawFallbackTemplateHtml5Icon(Rect rect)
    {
        Color shieldColor = EditorGUIUtility.isProSkin
            ? new Color(0.78f, 0.78f, 0.78f)
            : new Color(0.42f, 0.42f, 0.42f);
        Color cutColor = new Color(0.26f, 0.26f, 0.26f);
        Color markColor = EditorGUIUtility.isProSkin
            ? new Color(0.36f, 0.36f, 0.36f)
            : new Color(0.9f, 0.9f, 0.9f);

        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height - 4f), shieldColor);
        EditorGUI.DrawRect(new Rect(rect.x + 2f, rect.yMax - 4f, rect.width - 4f, 2f), shieldColor);
        EditorGUI.DrawRect(new Rect(rect.x + 4f, rect.yMax - 2f, rect.width - 8f, 2f), shieldColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, 2f), cutColor);
        EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, 2f), cutColor);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 1f, rect.width - 4f, rect.height - 2f), "5", new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = markColor },
            fontSize = 9
        });
    }

    List<WebGLTemplateDraft> GetAvailableWebGLTemplates(string currentTemplate)
    {
        List<WebGLTemplateDraft> templates = new List<WebGLTemplateDraft>
        {
            new WebGLTemplateDraft("APPLICATION:Default", "Default", LoadTemplateThumbnail("APPLICATION:Default")),
            new WebGLTemplateDraft("APPLICATION:Minimal", "Minimal", LoadTemplateThumbnail("APPLICATION:Minimal")),
        };

        if (!string.IsNullOrWhiteSpace(currentTemplate) && templates.All(item => item.Value != currentTemplate))
        {
            templates.Insert(0, new WebGLTemplateDraft(currentTemplate, GetTemplateCardDisplayName(currentTemplate), LoadTemplateThumbnail(currentTemplate)));
        }

        string projectTemplateRoot = System.IO.Path.Combine(Application.dataPath, "WebGLTemplates");
        if (System.IO.Directory.Exists(projectTemplateRoot))
        {
            foreach (string directory in System.IO.Directory.GetDirectories(projectTemplateRoot))
            {
                string folderName = System.IO.Path.GetFileName(directory);
                string templateValue = $"PROJECT:{folderName}";
                if (templates.Any(item => item.Value == templateValue))
                {
                    continue;
                }

                templates.Add(new WebGLTemplateDraft(templateValue, GetTemplateCardDisplayName(templateValue), LoadTemplateThumbnail(templateValue)));
            }
        }

        return templates;
    }

    void DrawAdeDataInfoEditor(UnityEngine.Object adeDataInfo)
    {
        EnsureAdeDataInfoFeedFields(adeDataInfo);

        SerializedObject serializedObject = new SerializedObject(adeDataInfo);
        serializedObject.Update();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("内容配置", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShareId"), new GUIContent("ShareId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SubscribeTmplIds"), new GUIContent("订阅模板"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FeedRepeatContentIDs"), new GUIContent("复访流内容"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FeedAcquisitionContentIDs"), new GUIContent("获客流内容"), true);
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
            SerializedProperty gridAdProperty = adDataProperty.FindPropertyRelative("GridAdList");
            if (gridAdProperty != null)
            {
                EditorGUILayout.PropertyField(gridAdProperty, new GUIContent("格子广告"), true);
            }

            SerializedProperty moreGamesProperty = adDataProperty.FindPropertyRelative("MoreGames");
            if (moreGamesProperty != null)
            {
                EditorGUILayout.PropertyField(moreGamesProperty, new GUIContent("更多游戏"), true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveSerializedChanges(serializedObject, adsData);
            }
        }
    }

    void DrawRewardOnlyEditor()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("广告参数", EditorStyles.miniBoldLabel);
            DrawAdItemDraftFields("插屏参数", interstitialAdDraft);
            DrawAdItemDraftFields("Banner 参数", bannerAdDraft);
            EditorGUILayout.Space(2f);
            rewardAdList.DoLayoutList();
            EditorGUILayout.Space(2f);
            gridAdList.DoLayoutList();
            EditorGUILayout.Space(2f);
            DrawMoreGamesDraftFields();
        }
    }

    void DrawAdItemDraftFields(string title, AdItemDraft draft)
    {
        Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
        rowRect.y += 1f;
        rowRect.height = EditorGUIUtility.singleLineHeight;

        float titleWidth = Mathf.Min(78f, rowRect.width * 0.24f);
        Rect titleRect = new Rect(rowRect.x + 2f, rowRect.y, titleWidth, rowRect.height);
        Rect fieldRect = new Rect(titleRect.xMax + 6f, rowRect.y, rowRect.xMax - titleRect.xMax - 8f, rowRect.height);

        EditorGUI.LabelField(titleRect, title, EditorStyles.miniBoldLabel);
        if (DrawCompactAdItemFields(fieldRect, draft))
        {
            rewardConfigDirty = true;
        }
    }

    void DrawMoreGamesDraftFields()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("更多游戏参数", EditorStyles.miniBoldLabel);

            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
            rowRect.y += 1f;
            rowRect.height = EditorGUIUtility.singleLineHeight;
            if (DrawCompactMoreGamesFields(rowRect, moreGamesDraft))
            {
                rewardConfigDirty = true;
            }

            using (new EditorGUI.DisabledScope(moreGamesDraft.GridCount != MoreGamesPanelCount.One))
            {
                Rect positionRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
                positionRect.y += 1f;
                positionRect.height = EditorGUIUtility.singleLineHeight;
                if (DrawCompactMoreGamesPositionFields(positionRect, moreGamesDraft))
                {
                    rewardConfigDirty = true;
                }
            }

            moreGamesQueryList.DoLayoutList();
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

        pathLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
            richText = false,
        };

        templateNameStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
        };

        templateNameSelectedStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
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
        EditorGUILayout.Space(2f);
    }

    void EndSectionCard()
    {
        EditorGUILayout.EndVertical();
    }

    bool DrawInnerFoldout(bool expanded, string label)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
        expanded = EditorGUI.Foldout(rect, expanded, label, true, EditorStyles.foldoutHeader);
        return expanded;
    }

    void SaveSerializedChanges(SerializedObject serializedObject, UnityEngine.Object asset)
    {
        if (!serializedObject.ApplyModifiedProperties())
        {
            return;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
        serializedObject.UpdateIfRequiredOrScript();
    }

    ReorderableList CreateCustomSymbolListEditor()
    {
        var reorderableList = new ReorderableList(editableCustomSymbols, typeof(string), true, true, true, true);
        reorderableList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Scripting Define Symbols");
        };
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= editableCustomSymbols.Count)
            {
                return;
            }

            Rect selectRect = GetSelectionRect(rect);
            DrawSelectableRowBackground(rect, customSymbolSelection.IsSelected(index));
            DrawSelectionHandle(selectRect, customSymbolSelection.IsSelected(index));
            if (HandleSelectionClick(rect, selectRect, index, customSymbolSelection, reorderableList))
            {
                return;
            }

            Rect fieldRect = GetSingleLineFieldRect(rect);
            string updatedSymbol = EditorGUI.TextField(fieldRect, editableCustomSymbols[index]);
            if (updatedSymbol != editableCustomSymbols[index])
            {
                editableCustomSymbols[index] = updatedSymbol;
                UpdateCustomSymbolsDirty();
            }
        };
        reorderableList.onAddCallback = _ =>
        {
            editableCustomSymbols.Add(string.Empty);
            customSymbolSelection.SelectSingle(editableCustomSymbols.Count - 1);
            UpdateCustomSymbolsDirty();
        };
        reorderableList.onRemoveCallback = list =>
        {
            if (RemoveSelectedItems(editableCustomSymbols, list, customSymbolSelection))
            {
                UpdateCustomSymbolsDirty();
            }
        };
        reorderableList.onReorderCallback = list =>
        {
            customSymbolSelection.SelectSingle(list.index);
            UpdateCustomSymbolsDirty();
        };
        reorderableList.footerHeight = 22f;
        reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
        return reorderableList;
    }

    ReorderableList CreateStringListEditor(List<string> list, string header, ListSelectionState selection, Action onChanged)
    {
        var reorderableList = new ReorderableList(list, typeof(string), true, true, true, true);
        reorderableList.drawHeaderCallback = rect =>
        {
            rect.y += 1f;
            EditorGUI.LabelField(rect, header, EditorStyles.miniBoldLabel);
        };
        reorderableList.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
        {
            DrawSelectableRowBackground(rect, selection.IsSelected(index));
        };
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= list.Count)
            {
                return;
            }

            Rect selectionRect = GetSelectionRect(rect);
            DrawSelectionHandle(selectionRect, selection.IsSelected(index));
            if (HandleSelectionClick(rect, selectionRect, index, selection, reorderableList))
            {
                return;
            }

            Rect fieldRect = GetSingleLineFieldRect(rect);
            string updatedValue = EditorGUI.TextField(fieldRect, list[index] ?? string.Empty);
            if (updatedValue != list[index])
            {
                list[index] = updatedValue;
                onChanged?.Invoke();
            }
        };
        reorderableList.onAddCallback = _ =>
        {
            list.Add(string.Empty);
            selection.SelectSingle(list.Count - 1);
            onChanged?.Invoke();
        };
        reorderableList.onRemoveCallback = reorderable =>
        {
            if (RemoveSelectedItems(list, reorderable, selection))
            {
                onChanged?.Invoke();
            }
        };
        reorderableList.onReorderCallback = reorderable =>
        {
            selection.SelectSingle(reorderable.index);
            onChanged?.Invoke();
        };
        reorderableList.headerHeight = EditorGUIUtility.singleLineHeight + 2f;
        reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 2f;
        reorderableList.footerHeight = 18f;
        return reorderableList;
    }

    ReorderableList CreateAdItemListEditor(List<AdItemDraft> list, string header, ListSelectionState selection)
    {
        var reorderableList = new ReorderableList(list, typeof(AdItemDraft), true, true, true, true);
        reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
        reorderableList.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
        {
            DrawSelectableRowBackground(rect, selection.IsSelected(index));
        };
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= list.Count)
            {
                return;
            }

            AdItemDraft item = list[index];
            Rect selectionRect = GetSelectionRect(rect);
            DrawSelectionHandle(selectionRect, selection.IsSelected(index));
            if (HandleSelectionClick(rect, selectionRect, index, selection, reorderableList))
            {
                return;
            }

            Rect fieldRect = GetListContentRect(rect);
            fieldRect.y += 3f;
            fieldRect.height = EditorGUIUtility.singleLineHeight;
            if (DrawCompactAdItemFields(fieldRect, item))
            {
                rewardConfigDirty = true;
            }
        };
        reorderableList.onAddCallback = _ =>
        {
            list.Add(new AdItemDraft { Name = "激励", Id = string.Empty });
            selection.SelectSingle(list.Count - 1);
            rewardConfigDirty = true;
        };
        reorderableList.onRemoveCallback = reorderable =>
        {
            if (RemoveSelectedItems(list, reorderable, selection))
            {
                rewardConfigDirty = true;
            }
        };
        reorderableList.onReorderCallback = reorderable =>
        {
            selection.SelectSingle(reorderable.index);
            rewardConfigDirty = true;
        };
        reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 8f;
        reorderableList.footerHeight = 22f;
        return reorderableList;
    }

    ReorderableList CreateGridAdListEditor(List<GridAdDraft> list, string header, ListSelectionState selection)
    {
        var reorderableList = new ReorderableList(list, typeof(GridAdDraft), true, true, true, true);
        reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
        reorderableList.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
        {
            DrawSelectableRowBackground(rect, selection.IsSelected(index));
        };
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= list.Count)
            {
                return;
            }

            GridAdDraft item = list[index];
            Rect selectionRect = GetSelectionRect(rect);
            DrawSelectionHandle(selectionRect, selection.IsSelected(index));
            if (HandleSelectionClick(rect, selectionRect, index, selection, reorderableList))
            {
                return;
            }

            Rect fieldRect = GetListContentRect(rect);
            fieldRect.y += 3f;
            if (DrawCompactGridAdFields(fieldRect, item))
            {
                rewardConfigDirty = true;
            }
        };
        reorderableList.onAddCallback = _ =>
        {
            list.Add(new GridAdDraft
            {
                NameId = string.Empty,
                Type = GridAdType.Horizontal,
                Anchor = GridAnchorType.Bottom,
                Position = Vector2.zero,
                AdUnitId = string.Empty
            });
            selection.SelectSingle(list.Count - 1);
            rewardConfigDirty = true;
        };
        reorderableList.onRemoveCallback = reorderable =>
        {
            if (RemoveSelectedItems(list, reorderable, selection))
            {
                rewardConfigDirty = true;
            }
        };
        reorderableList.onReorderCallback = reorderable =>
        {
            selection.SelectSingle(reorderable.index);
            rewardConfigDirty = true;
        };
        reorderableList.elementHeight = EditorGUIUtility.singleLineHeight * 2f + 12f;
        reorderableList.footerHeight = 22f;
        return reorderableList;
    }

    ReorderableList CreateMoreGamesQueryListEditor(List<MoreGamesQueryDraft> list, string header, ListSelectionState selection)
    {
        var reorderableList = new ReorderableList(list, typeof(MoreGamesQueryDraft), true, true, true, true);
        reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
        reorderableList.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
        {
            DrawSelectableRowBackground(rect, selection.IsSelected(index));
        };
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= list.Count)
            {
                return;
            }

            MoreGamesQueryDraft item = list[index];
            Rect selectionRect = GetSelectionRect(rect);
            DrawSelectionHandle(selectionRect, selection.IsSelected(index));
            if (HandleSelectionClick(rect, selectionRect, index, selection, reorderableList))
            {
                return;
            }

            Rect fieldRect = GetListContentRect(rect);
            fieldRect.y += 3f;
            fieldRect.height = EditorGUIUtility.singleLineHeight;
            if (DrawCompactMoreGamesQueryFields(fieldRect, item))
            {
                rewardConfigDirty = true;
            }
        };
        reorderableList.onAddCallback = _ =>
        {
            list.Add(new MoreGamesQueryDraft());
            selection.SelectSingle(list.Count - 1);
            rewardConfigDirty = true;
        };
        reorderableList.onRemoveCallback = reorderable =>
        {
            if (RemoveSelectedItems(list, reorderable, selection))
            {
                rewardConfigDirty = true;
            }
        };
        reorderableList.onReorderCallback = reorderable =>
        {
            selection.SelectSingle(reorderable.index);
            rewardConfigDirty = true;
        };
        reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 8f;
        reorderableList.footerHeight = 22f;
        return reorderableList;
    }

    bool DrawCompactAdItemFields(Rect fieldRect, AdItemDraft item)
    {
        float gap = 6f;
        float nameWidth = Mathf.Floor((fieldRect.width - gap) * 0.38f);
        Rect nameRect = new Rect(fieldRect.x, fieldRect.y, nameWidth, fieldRect.height);
        Rect idRect = new Rect(nameRect.xMax + gap, fieldRect.y, fieldRect.xMax - nameRect.xMax - gap, fieldRect.height);

        bool changed = false;
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        try
        {
            EditorGUIUtility.labelWidth = 40f;
            string newName = EditorGUI.TextField(nameRect, "Name", item.Name);
            if (newName != item.Name)
            {
                item.Name = newName;
                changed = true;
            }

            EditorGUIUtility.labelWidth = 20f;
            string newId = EditorGUI.TextField(idRect, "ID", item.Id);
            if (newId != item.Id)
            {
                item.Id = newId;
                changed = true;
            }
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        return changed;
    }

    bool DrawCompactGridAdFields(Rect fieldRect, GridAdDraft item)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float gap = 6f;
        Rect firstRow = new Rect(fieldRect.x, fieldRect.y, fieldRect.width, lineHeight);
        Rect secondRow = new Rect(fieldRect.x, firstRow.yMax + 4f, fieldRect.width, lineHeight);

        float firstUsableWidth = firstRow.width - gap * 2f;
        float nameWidth = Mathf.Floor(firstUsableWidth * 0.38f);
        float typeWidth = Mathf.Floor(firstUsableWidth * 0.29f);
        Rect nameRect = new Rect(firstRow.x, firstRow.y, nameWidth, lineHeight);
        Rect typeRect = new Rect(nameRect.xMax + gap, firstRow.y, typeWidth, lineHeight);
        Rect anchorRect = new Rect(typeRect.xMax + gap, firstRow.y, firstRow.xMax - typeRect.xMax - gap, lineHeight);

        float secondUsableWidth = secondRow.width - gap;
        float idWidth = Mathf.Floor(secondUsableWidth * 0.48f);
        Rect idRect = new Rect(secondRow.x, secondRow.y, idWidth, lineHeight);
        Rect positionRect = new Rect(idRect.xMax + gap, secondRow.y, secondRow.xMax - idRect.xMax - gap, lineHeight);

        bool changed = false;
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        try
        {
            EditorGUIUtility.labelWidth = 50f;
            string newNameId = EditorGUI.TextField(nameRect, "名称ID", item.NameId);
            if (newNameId != item.NameId)
            {
                item.NameId = newNameId;
                changed = true;
            }

            EditorGUIUtility.labelWidth = 34f;
            GridAdType newType = (GridAdType)EditorGUI.EnumPopup(typeRect, "类型", item.Type);
            if (newType != item.Type)
            {
                item.Type = newType;
                changed = true;
            }

            GridAnchorType newAnchor = (GridAnchorType)EditorGUI.EnumPopup(anchorRect, "锚点", item.Anchor);
            if (newAnchor != item.Anchor)
            {
                item.Anchor = newAnchor;
                changed = true;
            }

            EditorGUIUtility.labelWidth = 44f;
            string newId = EditorGUI.TextField(idRect, "广告ID", item.AdUnitId);
            if (newId != item.AdUnitId)
            {
                item.AdUnitId = newId;
                changed = true;
            }

            Vector2 newPosition = DrawCompactVector2Field(positionRect, "位置", item.Position);
            if (newPosition != item.Position)
            {
                item.Position = newPosition;
                changed = true;
            }
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        return changed;
    }

    bool DrawCompactMoreGamesFields(Rect fieldRect, MoreGamesDraft item)
    {
        float gap = 6f;
        float usableWidth = fieldRect.width - gap * 2f;
        float countWidth = Mathf.Floor(usableWidth * 0.34f);
        float sizeWidth = Mathf.Floor(usableWidth * 0.31f);
        Rect countRect = new Rect(fieldRect.x, fieldRect.y, countWidth, fieldRect.height);
        Rect sizeRect = new Rect(countRect.xMax + gap, fieldRect.y, sizeWidth, fieldRect.height);
        Rect customRect = new Rect(sizeRect.xMax + gap, fieldRect.y, fieldRect.xMax - sizeRect.xMax - gap, fieldRect.height);

        bool changed = false;
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        try
        {
            EditorGUIUtility.labelWidth = 60f;
            MoreGamesPanelCount newCount = (MoreGamesPanelCount)EditorGUI.EnumPopup(countRect, "宫格数量", item.GridCount);
            if (newCount != item.GridCount)
            {
                item.GridCount = newCount;
                changed = true;
            }

            EditorGUIUtility.labelWidth = 34f;
            MoreGamesPanelSize newSize = (MoreGamesPanelSize)EditorGUI.EnumPopup(sizeRect, "尺寸", item.Size);
            if (newSize != item.Size)
            {
                item.Size = newSize;
                changed = true;
            }

            EditorGUIUtility.labelWidth = 70f;
            bool newCustomPosition = EditorGUI.Toggle(customRect, "自定义位置", item.CustomPosition);
            if (newCustomPosition != item.CustomPosition)
            {
                item.CustomPosition = newCustomPosition;
                changed = true;
            }
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        return changed;
    }

    bool DrawCompactMoreGamesPositionFields(Rect fieldRect, MoreGamesDraft item)
    {
        float gap = 6f;
        float usableWidth = fieldRect.width - gap;
        float topWidth = Mathf.Floor(usableWidth * 0.5f);
        Rect topRect = new Rect(fieldRect.x, fieldRect.y, topWidth, fieldRect.height);
        Rect leftRect = new Rect(topRect.xMax + gap, fieldRect.y, fieldRect.xMax - topRect.xMax - gap, fieldRect.height);

        bool changed = false;
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        try
        {
            EditorGUIUtility.labelWidth = 28f;
            int newTop = EditorGUI.IntField(topRect, "Top", item.Top);
            if (newTop != item.Top)
            {
                item.Top = newTop;
                changed = true;
            }

            int newLeft = EditorGUI.IntField(leftRect, "Left", item.Left);
            if (newLeft != item.Left)
            {
                item.Left = newLeft;
                changed = true;
            }
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        return changed;
    }

    bool DrawCompactMoreGamesQueryFields(Rect fieldRect, MoreGamesQueryDraft item)
    {
        float gap = 6f;
        float appIdWidth = Mathf.Floor((fieldRect.width - gap) * 0.42f);
        Rect appIdRect = new Rect(fieldRect.x, fieldRect.y, appIdWidth, fieldRect.height);
        Rect queryRect = new Rect(appIdRect.xMax + gap, fieldRect.y, fieldRect.xMax - appIdRect.xMax - gap, fieldRect.height);

        bool changed = false;
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        try
        {
            EditorGUIUtility.labelWidth = 48f;
            string newAppId = EditorGUI.TextField(appIdRect, "AppID", item.AppId);
            if (newAppId != item.AppId)
            {
                item.AppId = newAppId;
                changed = true;
            }

            EditorGUIUtility.labelWidth = 42f;
            string newQuery = EditorGUI.TextField(queryRect, "Query", item.Query);
            if (newQuery != item.Query)
            {
                item.Query = newQuery;
                changed = true;
            }
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        return changed;
    }

    Vector2 DrawCompactVector2Field(Rect rect, string label, Vector2 value)
    {
        float labelWidth = 28f;
        float axisWidth = 10f;
        float gap = 3f;
        Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);

        float inputStart = labelRect.xMax + gap;
        float inputWidth = Mathf.Max(28f, (rect.xMax - inputStart - axisWidth * 2f - gap * 3f) * 0.5f);
        Rect xLabelRect = new Rect(inputStart, rect.y, axisWidth, rect.height);
        Rect xRect = new Rect(xLabelRect.xMax + gap, rect.y, inputWidth, rect.height);
        Rect yLabelRect = new Rect(xRect.xMax + gap, rect.y, axisWidth, rect.height);
        Rect yRect = new Rect(yLabelRect.xMax + gap, rect.y, Mathf.Max(28f, rect.xMax - yLabelRect.xMax - gap), rect.height);

        EditorGUI.LabelField(labelRect, label, EditorStyles.miniLabel);
        EditorGUI.LabelField(xLabelRect, "X", EditorStyles.miniLabel);
        float newX = EditorGUI.FloatField(xRect, GUIContent.none, value.x);
        EditorGUI.LabelField(yLabelRect, "Y", EditorStyles.miniLabel);
        float newY = EditorGUI.FloatField(yRect, GUIContent.none, value.y);
        return new Vector2(newX, newY);
    }

    void SaveReflectedAssetChanges(UnityEngine.Object asset)
    {
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
    }

    void NormalizeAssetLists(UnityEngine.Object asset)
    {
        if (asset == null)
        {
            return;
        }

        Type assetType = asset.GetType();
        if (assetType.Name == "AdeDataInfo")
        {
            EnsureAdeDataInfoFeedFields(asset);
            NormalizeStringListField(asset, "SubscribeTmplIds");
            NormalizeStringListField(asset, "FeedContentIDs");
            NormalizeStringListField(asset, "FeedRepeatContentIDs");
            NormalizeStringListField(asset, "FeedAcquisitionContentIDs");
            return;
        }

        if (assetType.Name == "AdsData")
        {
            NormalizeRewardArray(asset);
            NormalizeGridAdList(asset);
            NormalizeMoreGamesQueries(asset);
        }
    }

    void NormalizeStringListField(UnityEngine.Object asset, string fieldName)
    {
        var field = asset.GetType().GetField(fieldName);
        if (field == null)
        {
            return;
        }

        if (!(field.GetValue(asset) is List<string> values))
        {
            return;
        }

        if (EditorGUIUtility.editingTextField)
        {
            return;
        }

        List<string> normalized = values
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct()
            .ToList();

        if (normalized.Count == values.Count && normalized.SequenceEqual(values))
        {
            return;
        }

        field.SetValue(asset, normalized);
        EditorUtility.SetDirty(asset);
    }

    void NormalizeRewardArray(UnityEngine.Object adsData)
    {
        var adDataField = adsData.GetType().GetField("AdData");
        if (adDataField == null)
        {
            return;
        }

        object adDataValue = adDataField.GetValue(adsData);
        if (adDataValue == null)
        {
            return;
        }

        var rewardField = adDataValue.GetType().GetField("RewardID");
        if (rewardField == null)
        {
            return;
        }

        if (!(rewardField.GetValue(adDataValue) is Array rewardArray))
        {
            return;
        }

        if (EditorGUIUtility.editingTextField)
        {
            return;
        }

        Type adItemType = FindType("AdItemData");
        if (adItemType == null)
        {
            return;
        }

        var nameField = adItemType.GetField("name");
        var idField = adItemType.GetField("ID");
        if (nameField == null || idField == null)
        {
            return;
        }

        List<object> validItems = new List<object>();
        foreach (object item in rewardArray)
        {
            if (item == null)
            {
                continue;
            }

            string nameValue = (nameField.GetValue(item) as string)?.Trim();
            string idValue = (idField.GetValue(item) as string)?.Trim();

            if (string.IsNullOrWhiteSpace(nameValue) && string.IsNullOrWhiteSpace(idValue))
            {
                continue;
            }

            nameField.SetValue(item, nameValue ?? string.Empty);
            idField.SetValue(item, idValue ?? string.Empty);
            validItems.Add(item);
        }

        if (validItems.Count == rewardArray.Length)
        {
            return;
        }

        Array normalizedArray = Array.CreateInstance(adItemType, validItems.Count);
        for (int i = 0; i < validItems.Count; i++)
        {
            normalizedArray.SetValue(validItems[i], i);
        }

        rewardField.SetValue(adDataValue, normalizedArray);
        EditorUtility.SetDirty(adsData);
    }

    void NormalizeGridAdList(UnityEngine.Object adsData)
    {
        var adDataField = adsData.GetType().GetField("AdData");
        if (adDataField == null)
        {
            return;
        }

        object adDataValue = adDataField.GetValue(adsData);
        if (adDataValue == null)
        {
            return;
        }

        var gridField = adDataValue.GetType().GetField("GridAdList");
        if (gridField == null)
        {
            return;
        }

        if (!(gridField.GetValue(adDataValue) is IEnumerable gridItems))
        {
            return;
        }

        if (EditorGUIUtility.editingTextField)
        {
            return;
        }

        Type gridAdType = FindType("GridAdData");
        if (gridAdType == null)
        {
            return;
        }

        var nameField = gridAdType.GetField("NameId");
        var idField = gridAdType.GetField("AdUnitId");
        if (nameField == null || idField == null)
        {
            return;
        }

        IList validItems = Activator.CreateInstance(typeof(List<>).MakeGenericType(gridAdType)) as IList;
        if (validItems == null)
        {
            return;
        }

        int sourceCount = 0;
        foreach (object item in gridItems)
        {
            sourceCount++;
            if (item == null)
            {
                continue;
            }

            string nameValue = (nameField.GetValue(item) as string)?.Trim();
            string idValue = (idField.GetValue(item) as string)?.Trim();
            if (string.IsNullOrWhiteSpace(nameValue) && string.IsNullOrWhiteSpace(idValue))
            {
                continue;
            }

            nameField.SetValue(item, nameValue ?? string.Empty);
            idField.SetValue(item, idValue ?? string.Empty);
            validItems.Add(item);
        }

        if (validItems.Count == sourceCount)
        {
            return;
        }

        gridField.SetValue(adDataValue, validItems);
        EditorUtility.SetDirty(adsData);
    }

    void NormalizeMoreGamesQueries(UnityEngine.Object adsData)
    {
        var adDataField = adsData.GetType().GetField("AdData");
        if (adDataField == null)
        {
            return;
        }

        object adDataValue = adDataField.GetValue(adsData);
        object moreGamesValue = GetFieldValue(adDataValue, "MoreGames");
        if (moreGamesValue == null)
        {
            return;
        }

        var queryField = moreGamesValue.GetType().GetField("Queries");
        if (queryField == null)
        {
            return;
        }

        if (!(queryField.GetValue(moreGamesValue) is IEnumerable queryItems))
        {
            return;
        }

        if (EditorGUIUtility.editingTextField)
        {
            return;
        }

        Type queryType = FindType("MoreGamesQueryData");
        if (queryType == null)
        {
            return;
        }

        var appIdField = queryType.GetField("AppId");
        if (appIdField == null)
        {
            return;
        }

        IList validItems = Activator.CreateInstance(typeof(List<>).MakeGenericType(queryType)) as IList;
        if (validItems == null)
        {
            return;
        }

        int sourceCount = 0;
        foreach (object item in queryItems)
        {
            sourceCount++;
            if (item == null)
            {
                continue;
            }

            string appIdValue = (appIdField.GetValue(item) as string)?.Trim();
            if (string.IsNullOrWhiteSpace(appIdValue))
            {
                continue;
            }

            appIdField.SetValue(item, appIdValue);
            validItems.Add(item);
        }

        if (validItems.Count == sourceCount)
        {
            return;
        }

        queryField.SetValue(moreGamesValue, validItems);
        EditorUtility.SetDirty(adsData);
    }

    string GetStringFieldValue(object target, string fieldName)
    {
        return GetFieldValue(target, fieldName) as string ?? string.Empty;
    }

    object GetFieldValue(object target, string fieldName)
    {
        if (target == null)
        {
            return null;
        }

        var field = target.GetType().GetField(fieldName);
        return field?.GetValue(target);
    }

    void SetFieldValue(object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        var field = target.GetType().GetField(fieldName);
        field?.SetValue(target, value);
    }

    List<string> GetOrCreateStringListField(UnityEngine.Object asset, string fieldName)
    {
        var field = asset.GetType().GetField(fieldName);
        if (field == null)
        {
            return new List<string>();
        }

        if (field.GetValue(asset) is List<string> values)
        {
            return values;
        }

        values = new List<string>();
        field.SetValue(asset, values);
        return values;
    }

    List<object> GetRewardItems(object adDataValue)
    {
        List<object> items = new List<object>();
        if (adDataValue == null)
        {
            return items;
        }

        var rewardField = adDataValue.GetType().GetField("RewardID");
        if (rewardField == null)
        {
            return items;
        }

        if (!(rewardField.GetValue(adDataValue) is Array rewardArray))
        {
            return items;
        }

        foreach (object item in rewardArray)
        {
            items.Add(item);
        }

        return items;
    }

    List<object> GetGridAdItems(object adDataValue)
    {
        List<object> items = new List<object>();
        if (adDataValue == null)
        {
            return items;
        }

        var gridField = adDataValue.GetType().GetField("GridAdList");
        if (gridField == null)
        {
            return items;
        }

        if (!(gridField.GetValue(adDataValue) is IEnumerable gridItems))
        {
            return items;
        }

        foreach (object item in gridItems)
        {
            items.Add(item);
        }

        return items;
    }

    List<object> GetMoreGamesQueryItems(object moreGamesValue)
    {
        List<object> items = new List<object>();
        if (moreGamesValue == null)
        {
            return items;
        }

        var queryField = moreGamesValue.GetType().GetField("Queries");
        if (queryField == null)
        {
            return items;
        }

        if (!(queryField.GetValue(moreGamesValue) is IEnumerable queryItems))
        {
            return items;
        }

        foreach (object item in queryItems)
        {
            items.Add(item);
        }

        return items;
    }

    void SetRewardItems(object adDataValue, List<object> rewardItems)
    {
        if (adDataValue == null)
        {
            return;
        }

        var rewardField = adDataValue.GetType().GetField("RewardID");
        if (rewardField == null)
        {
            return;
        }

        Type adItemType = FindType("AdItemData");
        if (adItemType == null)
        {
            return;
        }

        Array rewardArray = Array.CreateInstance(adItemType, rewardItems.Count);
        for (int i = 0; i < rewardItems.Count; i++)
        {
            rewardArray.SetValue(rewardItems[i], i);
        }

        rewardField.SetValue(adDataValue, rewardArray);
    }

    object CreateAdItemData(string nameValue, string idValue)
    {
        Type adItemType = FindType("AdItemData");
        if (adItemType == null)
        {
            return null;
        }

        object instance = Activator.CreateInstance(adItemType);
        SetFieldValue(instance, "name", string.IsNullOrWhiteSpace(nameValue) ? string.Empty : nameValue.Trim());
        SetFieldValue(instance, "ID", string.IsNullOrWhiteSpace(idValue) ? string.Empty : idValue.Trim());
        return instance;
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
        customSymbolSelection.Clear();
        UpdateCustomSymbolsDirty();
    }

    void SetNoAdsModeInEditor(bool enabled)
    {
        SetManagedSymbol(NoAdsSymbol, enabled);
    }

    void SetManagedSymbol(string symbol, bool enabled)
    {
        editableCustomSymbols.RemoveAll(item => item == symbol);
        if (enabled)
        {
            editableCustomSymbols.Add(symbol);
        }

        List<string> normalizedSymbols = editableCustomSymbols
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        editableCustomSymbols.Clear();
        editableCustomSymbols.AddRange(normalizedSymbols);
        customSymbolSelection.Clear();
        UpdateCustomSymbolsDirty();
    }

    void UpdateCustomSymbolsDirty()
    {
        customSymbolsDirty = BuildDefineString(editableCustomSymbols) != cachedDefineString;
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

        string customSymbol = symbols.FirstOrDefault(IsPlatformLikeSymbol);
        return string.IsNullOrEmpty(customSymbol) ? "未识别" : customSymbol;
    }

    bool IsCurrentPreset(List<string> symbols, string symbol)
    {
        string currentSymbol = symbols.FirstOrDefault(IsPlatformLikeSymbol);

        if (string.IsNullOrEmpty(symbol))
        {
            return string.IsNullOrEmpty(currentSymbol);
        }

        return currentSymbol == symbol;
    }

    bool IsPlatformLikeSymbol(string symbol)
    {
        return !string.IsNullOrEmpty(symbol)
            && symbol.StartsWith("Ade_", StringComparison.Ordinal)
            && symbol != DebugSymbol;
    }

    void SyncCustomSymbols(BuildTargetGroup group, List<string> symbols)
    {
        string defineString = BuildDefineString(symbols);
        if (cachedCustomGroup != group)
        {
            LoadCustomSymbols(group, symbols);
            return;
        }

        string editableDefineString = BuildDefineString(editableCustomSymbols);
        if (!customSymbolsDirty && (cachedDefineString != defineString || editableDefineString != defineString))
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
        customSymbolSelection.Clear();
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
        Repaint();
    }

    string GetCurrentWebGLTemplate()
    {
        if (!System.IO.File.Exists(ProjectSettingsAssetPath))
        {
            return "APPLICATION:Default";
        }

        string content = System.IO.File.ReadAllText(ProjectSettingsAssetPath);
        Match match = Regex.Match(content, @"(?m)^\s*webGLTemplate:\s*(.+)$");
        return match.Success ? match.Groups[1].Value.Trim() : "APPLICATION:Default";
    }

    void SetCurrentWebGLTemplate(string templateValue)
    {
        if (!System.IO.File.Exists(ProjectSettingsAssetPath))
        {
            Debug.LogWarning("AdeConsole: 未找到 ProjectSettings.asset");
            return;
        }

        string content = System.IO.File.ReadAllText(ProjectSettingsAssetPath);
        string updated = Regex.Replace(
            content,
            @"(?m)^(\s*webGLTemplate:\s*).+$",
            $"$1{templateValue}");

        if (updated == content)
        {
            Debug.LogWarning("AdeConsole: 未找到 webGLTemplate 配置项");
            return;
        }

        System.IO.File.WriteAllText(ProjectSettingsAssetPath, updated);
        AssetDatabase.Refresh();
    }

    string GetTemplateDisplayName(string templateValue)
    {
        int separatorIndex = templateValue.IndexOf(':');
        return separatorIndex >= 0 && separatorIndex < templateValue.Length - 1
            ? templateValue.Substring(separatorIndex + 1)
            : templateValue;
    }

    string GetTemplateCardDisplayName(string templateValue)
    {
        string templateName = GetTemplateDisplayName(templateValue);
        switch (templateName)
        {
            case "Ade_Debug":
                return "Debug横屏";
            case "Ade_Debug_Portrait":
                return "Debug竖屏";
            default:
                return templateName;
        }
    }

    Texture2D LoadTemplateThumbnail(string templateValue)
    {
        string thumbnailPath = GetTemplateThumbnailPath(templateValue);
        if (string.IsNullOrWhiteSpace(thumbnailPath) || !System.IO.File.Exists(thumbnailPath))
        {
            return null;
        }

        byte[] bytes = System.IO.File.ReadAllBytes(thumbnailPath);
        Texture2D texture = new Texture2D(2, 2);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.LoadImage(bytes);
        return texture;
    }

    string GetTemplateThumbnailPath(string templateValue)
    {
        if (templateValue.StartsWith("APPLICATION:", StringComparison.Ordinal))
        {
            string templateName = GetTemplateDisplayName(templateValue);
            return System.IO.Path.Combine(GetBuiltinWebGLTemplateRoot(), templateName, "thumbnail.png");
        }

        if (templateValue.StartsWith("PROJECT:", StringComparison.Ordinal))
        {
            string templateName = GetTemplateDisplayName(templateValue);
            return System.IO.Path.Combine(Application.dataPath, "WebGLTemplates", templateName, "thumbnail.png");
        }

        return null;
    }

    string GetBuiltinWebGLTemplateRoot()
    {
        string contentsPath = EditorApplication.applicationContentsPath;
        string applicationDirectory = System.IO.Path.GetDirectoryName(EditorApplication.applicationPath);
        string[] candidates =
        {
            System.IO.Path.Combine(contentsPath, "PlaybackEngines", "WebGLSupport", "BuildTools", "WebGLTemplates"),
            System.IO.Path.Combine(applicationDirectory ?? string.Empty, "Data", "PlaybackEngines", "WebGLSupport", "BuildTools", "WebGLTemplates"),
        };

        foreach (string candidate in candidates)
        {
            if (System.IO.Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    void LoadRewardConfigDrafts()
    {
        UnityEngine.Object adeDataInfo = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdeDataInfoPath);
        if (adeDataInfo != null)
        {
            EnsureAdeDataInfoFeedFields(adeDataInfo);
            rewardShareIdDraft = GetStringFieldValue(adeDataInfo, "ShareId");

            subscribeTemplateDrafts.Clear();
            subscribeTemplateDrafts.AddRange(GetOrCreateStringListField(adeDataInfo, "SubscribeTmplIds"));

            feedRepeatContentDrafts.Clear();
            feedRepeatContentDrafts.AddRange(GetOrCreateStringListField(adeDataInfo, "FeedRepeatContentIDs"));

            feedAcquisitionContentDrafts.Clear();
            feedAcquisitionContentDrafts.AddRange(GetOrCreateStringListField(adeDataInfo, "FeedAcquisitionContentIDs"));
        }

        UnityEngine.Object adsData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdsDataPath);
        if (adsData != null)
        {
            EnsureAdsDataStructure(adsData);
            object adDataValue = GetFieldValue(adsData, "AdData");
            LoadAdItemDraft(interstitialAdDraft, GetFieldValue(adDataValue, "InterstitialID"));
            LoadAdItemDraft(bannerAdDraft, GetFieldValue(adDataValue, "BannerID"));

            rewardAdDrafts.Clear();
            foreach (object rewardItem in GetRewardItems(adDataValue))
            {
                AdItemDraft draft = new AdItemDraft();
                LoadAdItemDraft(draft, rewardItem);
                rewardAdDrafts.Add(draft);
            }

            gridAdDrafts.Clear();
            foreach (object gridItem in GetGridAdItems(adDataValue))
            {
                GridAdDraft draft = new GridAdDraft();
                LoadGridAdDraft(draft, gridItem);
                gridAdDrafts.Add(draft);
            }

            LoadMoreGamesDraft(GetFieldValue(adDataValue, "MoreGames"));
        }

        rewardConfigDirty = false;
        subscribeTemplateSelection.Clear();
        feedRepeatContentSelection.Clear();
        feedAcquisitionContentSelection.Clear();
        rewardAdSelection.Clear();
        gridAdSelection.Clear();
        moreGamesQuerySelection.Clear();
    }

    void SaveRewardConfigDrafts()
    {
        UnityEngine.Object adeDataInfo = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdeDataInfoPath);
        if (adeDataInfo != null)
        {
            Undo.RecordObject(adeDataInfo, "应用 AdeDataInfo 参数");
            List<string> repeatContentIds = GetNormalizedStringDrafts(feedRepeatContentDrafts);
            List<string> acquisitionContentIds = GetNormalizedStringDrafts(feedAcquisitionContentDrafts);
            SetFieldValue(adeDataInfo, "ShareId", rewardShareIdDraft ?? string.Empty);
            SetFieldValue(adeDataInfo, "FeedRepeatContentIDs", repeatContentIds);
            SetFieldValue(adeDataInfo, "FeedAcquisitionContentIDs", acquisitionContentIds);
            SetFieldValue(adeDataInfo, "FeedRepeatContentID", repeatContentIds.Count > 0 ? repeatContentIds[0] : string.Empty);
            SetFieldValue(adeDataInfo, "SubscribeTmplIds", GetNormalizedStringDrafts(subscribeTemplateDrafts));
            SetFieldValue(adeDataInfo, "FeedContentIDs", new List<string>(acquisitionContentIds));
            SaveReflectedAssetChanges(adeDataInfo);
        }

        UnityEngine.Object adsData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdsDataPath);
        if (adsData != null)
        {
            Undo.RecordObject(adsData, "应用 AdsData 参数");
            EnsureAdsDataStructure(adsData);
            object adDataValue = GetFieldValue(adsData, "AdData");
            SaveAdItemDraft(interstitialAdDraft, GetFieldValue(adDataValue, "InterstitialID"));
            SaveAdItemDraft(bannerAdDraft, GetFieldValue(adDataValue, "BannerID"));
            SetRewardItemsFromDrafts(adDataValue, rewardAdDrafts);
            SetGridAdItemsFromDrafts(adDataValue, gridAdDrafts);
            SaveMoreGamesDraft(GetFieldValue(adDataValue, "MoreGames"));
            SaveReflectedAssetChanges(adsData);
        }

        rewardConfigDirty = false;
    }

    void LoadAdItemDraft(AdItemDraft draft, object item)
    {
        draft.Name = GetStringFieldValue(item, "name");
        draft.Id = GetStringFieldValue(item, "ID");
    }

    void SaveAdItemDraft(AdItemDraft draft, object item)
    {
        if (item == null)
        {
            return;
        }

        SetFieldValue(item, "name", draft.Name ?? string.Empty);
        SetFieldValue(item, "ID", draft.Id ?? string.Empty);
    }

    void LoadGridAdDraft(GridAdDraft draft, object item)
    {
        if (draft == null)
        {
            return;
        }

        if (item == null)
        {
            draft.NameId = string.Empty;
            draft.Type = GridAdType.Horizontal;
            draft.Anchor = GridAnchorType.Bottom;
            draft.Position = Vector2.zero;
            draft.AdUnitId = string.Empty;
            return;
        }

        draft.NameId = GetStringFieldValue(item, "NameId");

        object typeValue = GetFieldValue(item, "Type");
        draft.Type = typeValue is GridAdType type ? type : GridAdType.Horizontal;

        draft.AdUnitId = GetStringFieldValue(item, "AdUnitId");

        object anchorValue = GetFieldValue(item, "Anchor");
        draft.Anchor = anchorValue is GridAnchorType anchor ? anchor : GridAnchorType.Bottom;

        object positionValue = GetFieldValue(item, "Position");
        draft.Position = positionValue is Vector2 position ? position : Vector2.zero;
    }

    void SetRewardItemsFromDrafts(object adDataValue, List<AdItemDraft> drafts)
    {
        if (adDataValue == null)
        {
            return;
        }

        Type adItemType = FindType("AdItemData");
        if (adItemType == null)
        {
            return;
        }

        Array rewardArray = Array.CreateInstance(adItemType, drafts.Count);
        for (int i = 0; i < drafts.Count; i++)
        {
            object rewardItem = Activator.CreateInstance(adItemType);
            SetFieldValue(rewardItem, "name", drafts[i].Name ?? string.Empty);
            SetFieldValue(rewardItem, "ID", drafts[i].Id ?? string.Empty);
            rewardArray.SetValue(rewardItem, i);
        }

        var rewardField = adDataValue.GetType().GetField("RewardID");
        rewardField?.SetValue(adDataValue, rewardArray);
    }

    void SetGridAdItemsFromDrafts(object adDataValue, List<GridAdDraft> drafts)
    {
        if (adDataValue == null)
        {
            return;
        }

        var gridField = adDataValue.GetType().GetField("GridAdList");
        if (gridField == null)
        {
            return;
        }

        Type gridAdType = FindType("GridAdData");
        if (gridAdType == null)
        {
            return;
        }

        Type listType = typeof(List<>).MakeGenericType(gridAdType);
        IList gridList = Activator.CreateInstance(listType) as IList;
        if (gridList == null)
        {
            return;
        }

        foreach (GridAdDraft draft in drafts)
        {
            object gridItem = Activator.CreateInstance(gridAdType);
            SetFieldValue(gridItem, "NameId", draft.NameId ?? string.Empty);
            SetFieldValue(gridItem, "Type", draft.Type);
            SetFieldValue(gridItem, "AdUnitId", draft.AdUnitId ?? string.Empty);
            SetFieldValue(gridItem, "Anchor", draft.Anchor);
            SetFieldValue(gridItem, "Position", draft.Position);
            gridList.Add(gridItem);
        }

        gridField.SetValue(adDataValue, gridList);
    }

    void LoadMoreGamesDraft(object moreGamesValue)
    {
        moreGamesQueryDrafts.Clear();

        if (moreGamesValue == null)
        {
            moreGamesDraft.GridCount = MoreGamesPanelCount.Nine;
            moreGamesDraft.Size = MoreGamesPanelSize.Medium;
            moreGamesDraft.CustomPosition = false;
            moreGamesDraft.Top = 0;
            moreGamesDraft.Left = 0;
            return;
        }

        object gridCountValue = GetFieldValue(moreGamesValue, "GridCount");
        moreGamesDraft.GridCount = gridCountValue is MoreGamesPanelCount gridCount ? gridCount : MoreGamesPanelCount.Nine;

        object sizeValue = GetFieldValue(moreGamesValue, "Size");
        moreGamesDraft.Size = sizeValue is MoreGamesPanelSize size ? size : MoreGamesPanelSize.Medium;

        object customPositionValue = GetFieldValue(moreGamesValue, "CustomPosition");
        moreGamesDraft.CustomPosition = customPositionValue is bool customPosition && customPosition;

        object topValue = GetFieldValue(moreGamesValue, "Top");
        moreGamesDraft.Top = topValue is int top ? top : 0;

        object leftValue = GetFieldValue(moreGamesValue, "Left");
        moreGamesDraft.Left = leftValue is int left ? left : 0;

        foreach (object queryItem in GetMoreGamesQueryItems(moreGamesValue))
        {
            moreGamesQueryDrafts.Add(new MoreGamesQueryDraft
            {
                AppId = GetStringFieldValue(queryItem, "AppId"),
                Query = GetStringFieldValue(queryItem, "Query")
            });
        }
    }

    void SaveMoreGamesDraft(object moreGamesValue)
    {
        if (moreGamesValue == null)
        {
            return;
        }

        SetFieldValue(moreGamesValue, "GridCount", moreGamesDraft.GridCount);
        SetFieldValue(moreGamesValue, "Size", moreGamesDraft.Size);
        SetFieldValue(moreGamesValue, "CustomPosition", moreGamesDraft.CustomPosition);
        SetFieldValue(moreGamesValue, "Top", moreGamesDraft.Top);
        SetFieldValue(moreGamesValue, "Left", moreGamesDraft.Left);
        SetMoreGamesQueryItemsFromDrafts(moreGamesValue, moreGamesQueryDrafts);
    }

    void SetMoreGamesQueryItemsFromDrafts(object moreGamesValue, List<MoreGamesQueryDraft> drafts)
    {
        if (moreGamesValue == null)
        {
            return;
        }

        var queryField = moreGamesValue.GetType().GetField("Queries");
        if (queryField == null)
        {
            return;
        }

        Type queryType = FindType("MoreGamesQueryData");
        if (queryType == null)
        {
            return;
        }

        Type listType = typeof(List<>).MakeGenericType(queryType);
        IList queryList = Activator.CreateInstance(listType) as IList;
        if (queryList == null)
        {
            return;
        }

        foreach (MoreGamesQueryDraft draft in drafts)
        {
            object queryItem = Activator.CreateInstance(queryType);
            SetFieldValue(queryItem, "AppId", draft.AppId ?? string.Empty);
            SetFieldValue(queryItem, "Query", draft.Query ?? string.Empty);
            queryList.Add(queryItem);
        }

        queryField.SetValue(moreGamesValue, queryList);
    }

    void LoadFeedLaunchMode()
    {
        cachedFeedLaunchMode = (FeedLaunchMode)EditorPrefs.GetInt(
            FeedLaunchModeEditorPrefsKey,
            (int)FeedLaunchMode.None);
        selectedFeedLaunchMode = cachedFeedLaunchMode;
        launchModeDirty = false;
    }

    void SyncFeedLaunchMode()
    {
        FeedLaunchMode currentMode = (FeedLaunchMode)EditorPrefs.GetInt(
            FeedLaunchModeEditorPrefsKey,
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

        EditorPrefs.SetInt(FeedLaunchModeEditorPrefsKey, (int)selectedFeedLaunchMode);
        cachedFeedLaunchMode = selectedFeedLaunchMode;
        launchModeDirty = false;
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

    void CreateAdeDataInfoAsset()
    {
        CreateScriptableAsset("AdeDataInfo", AdeDataInfoPath, EnsureAdeDataInfoFeedFields);
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

    void EnsureAdeDataInfoFeedFields(UnityEngine.Object adeDataInfo)
    {
        if (adeDataInfo == null)
        {
            return;
        }

        EnsureListField(adeDataInfo, "SubscribeTmplIds");
        EnsureListField(adeDataInfo, "FeedContentIDs");
        EnsureListField(adeDataInfo, "FeedRepeatContentIDs");
        EnsureListField(adeDataInfo, "FeedAcquisitionContentIDs");

        List<string> repeatIds = GetOrCreateStringListField(adeDataInfo, "FeedRepeatContentIDs");
        string legacyRepeatId = GetStringFieldValue(adeDataInfo, "FeedRepeatContentID");
        bool changed = AddUniqueStringValue(repeatIds, legacyRepeatId);

        List<string> acquisitionIds = GetOrCreateStringListField(adeDataInfo, "FeedAcquisitionContentIDs");
        foreach (string legacyContentId in GetOrCreateStringListField(adeDataInfo, "FeedContentIDs"))
        {
            changed |= AddUniqueStringValue(acquisitionIds, legacyContentId);
        }

        if (changed)
        {
            EditorUtility.SetDirty(adeDataInfo);
        }
    }

    bool AddUniqueStringValue(List<string> target, string value)
    {
        if (target == null || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalizedValue = value.Trim();
        if (target.Any(item => string.Equals(item, normalizedValue, StringComparison.Ordinal)))
        {
            return false;
        }

        target.Add(normalizedValue);
        return true;
    }

    List<string> GetNormalizedStringDrafts(IEnumerable<string> source)
    {
        return (source ?? Enumerable.Empty<string>())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct()
            .ToList();
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
        Type gridAdDataType = FindType("GridAdData");
        Type moreGamesDataType = FindType("MoreGamesData");
        Type moreGamesQueryDataType = FindType("MoreGamesQueryData");
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

        var gridField = adsPlatformDataType.GetField("GridAdList");
        if (gridField != null && gridField.GetValue(adDataValue) == null && gridAdDataType != null)
        {
            Type listType = typeof(List<>).MakeGenericType(gridAdDataType);
            gridField.SetValue(adDataValue, Activator.CreateInstance(listType));
            EditorUtility.SetDirty(adsData);
        }

        if (moreGamesDataType != null)
        {
            EnsureNestedObjectField(adDataValue, "MoreGames", moreGamesDataType, adsData);
            object moreGamesValue = GetFieldValue(adDataValue, "MoreGames");
            var queryField = moreGamesDataType.GetField("Queries");
            if (moreGamesValue != null && queryField != null && queryField.GetValue(moreGamesValue) == null && moreGamesQueryDataType != null)
            {
                Type listType = typeof(List<>).MakeGenericType(moreGamesQueryDataType);
                queryField.SetValue(moreGamesValue, Activator.CreateInstance(listType));
                EditorUtility.SetDirty(adsData);
            }
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

    class AdItemDraft
    {
        public string Name = string.Empty;
        public string Id = string.Empty;
    }

    class GridAdDraft
    {
        public string NameId = string.Empty;
        public GridAdType Type = GridAdType.Horizontal;
        public string AdUnitId = string.Empty;
        public GridAnchorType Anchor = GridAnchorType.Bottom;
        public Vector2 Position = Vector2.zero;
    }

    class MoreGamesDraft
    {
        public MoreGamesPanelCount GridCount = MoreGamesPanelCount.Nine;
        public MoreGamesPanelSize Size = MoreGamesPanelSize.Medium;
        public bool CustomPosition;
        public int Top;
        public int Left;
    }

    class MoreGamesQueryDraft
    {
        public string AppId = string.Empty;
        public string Query = string.Empty;
    }

    class ListSelectionState
    {
        readonly HashSet<int> selectedIndices = new();
        int anchorIndex = -1;

        public bool IsSelected(int index)
        {
            return selectedIndices.Contains(index);
        }

        public void Clear()
        {
            selectedIndices.Clear();
            anchorIndex = -1;
        }

        public void SelectSingle(int index)
        {
            selectedIndices.Clear();
            if (index >= 0)
            {
                selectedIndices.Add(index);
                anchorIndex = index;
            }
            else
            {
                anchorIndex = -1;
            }
        }

        public void Toggle(int index)
        {
            if (index < 0)
            {
                return;
            }

            if (!selectedIndices.Add(index))
            {
                selectedIndices.Remove(index);
            }

            anchorIndex = index;
        }

        public void SelectRange(int index, int itemCount)
        {
            if (index < 0 || itemCount <= 0)
            {
                return;
            }

            if (anchorIndex < 0 || anchorIndex >= itemCount)
            {
                SelectSingle(index);
                return;
            }

            selectedIndices.Clear();
            int start = Mathf.Clamp(Mathf.Min(anchorIndex, index), 0, itemCount - 1);
            int end = Mathf.Clamp(Mathf.Max(anchorIndex, index), 0, itemCount - 1);
            for (int i = start; i <= end; i++)
            {
                selectedIndices.Add(i);
            }
        }

        public void SelectAll(int itemCount)
        {
            selectedIndices.Clear();
            if (itemCount <= 0)
            {
                anchorIndex = -1;
                return;
            }

            for (int i = 0; i < itemCount; i++)
            {
                selectedIndices.Add(i);
            }

            anchorIndex = 0;
        }

        public List<int> GetSelectedIndicesDescending()
        {
            return selectedIndices.OrderByDescending(i => i).ToList();
        }
    }

    class WebGLTemplateDraft
    {
        public string Value;
        public string DisplayName;
        public Texture2D Thumbnail;

        public WebGLTemplateDraft(string value, string displayName, Texture2D thumbnail)
        {
            Value = value;
            DisplayName = displayName;
            Thumbnail = thumbnail;
        }
    }

    Rect GetSelectionRect(Rect rect)
    {
        return new Rect(rect.x, rect.y, ListSelectHandleWidth, rect.height);
    }

    Rect GetSingleLineFieldRect(Rect rect)
    {
        return new Rect(rect.x + ListSelectContentOffset, rect.y + 1f, rect.width - ListSelectContentOffset - 2f, EditorGUIUtility.singleLineHeight);
    }

    Rect GetListContentRect(Rect rect)
    {
        return new Rect(rect.x + ListSelectContentOffset, rect.y, rect.width - ListSelectContentOffset - 2f, rect.height);
    }

    void DrawSelectableRowBackground(Rect rect, bool selected)
    {
        if (!selected)
        {
            return;
        }

        EditorGUI.DrawRect(rect, new Color(0.24f, 0.48f, 0.85f, 0.2f));
    }

    void DrawSelectionHandle(Rect selectionRect, bool selected)
    {
        Color handleColor = selected
            ? new Color(0.28f, 0.55f, 0.95f, 0.9f)
            : new Color(0.5f, 0.5f, 0.5f, 0.35f);
        Rect handleRect = new Rect(selectionRect.x + 4f, selectionRect.y + 3f, 3f, Mathf.Max(4f, selectionRect.height - 6f));
        EditorGUI.DrawRect(handleRect, handleColor);
    }

    bool HandleSelectionClick(Rect rowRect, Rect selectionRect, int index, ListSelectionState selection, ReorderableList reorderableList)
    {
        Event currentEvent = Event.current;
        if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
        {
            return false;
        }

        bool ctrl = currentEvent.control || currentEvent.command;
        bool shift = currentEvent.shift;
        bool clickedSelectionHandle = selectionRect.Contains(currentEvent.mousePosition);
        bool clickedRow = rowRect.Contains(currentEvent.mousePosition);
        if (clickedRow)
        {
            activeListSelection = selection;
            activeReorderableList = reorderableList;
        }

        bool clickedModifiedRow = (ctrl || shift) && clickedRow;
        if (!clickedSelectionHandle && !clickedModifiedRow)
        {
            return false;
        }

        if (shift)
        {
            selection.SelectRange(index, reorderableList.list != null ? reorderableList.list.Count : 0);
        }
        else if (ctrl)
        {
            selection.Toggle(index);
        }
        else
        {
            selection.SelectSingle(index);
        }

        reorderableList.index = index;
        currentEvent.Use();
        Repaint();
        return true;
    }

    bool RemoveSelectedItems<T>(List<T> list, ReorderableList reorderableList, ListSelectionState selection)
    {
        if (list == null || list.Count == 0)
        {
            return false;
        }

        List<int> selectedIndices = selection.GetSelectedIndicesDescending();
        if (selectedIndices.Count == 0)
        {
            if (reorderableList.index < 0 || reorderableList.index >= list.Count)
            {
                return false;
            }

            selectedIndices.Add(reorderableList.index);
        }

        int focusIndex = selectedIndices.Min();
        bool removedAny = false;
        foreach (int index in selectedIndices)
        {
            if (index < 0 || index >= list.Count)
            {
                continue;
            }

            list.RemoveAt(index);
            removedAny = true;
        }

        if (!removedAny)
        {
            return false;
        }

        selection.Clear();
        if (list.Count > 0)
        {
            int newIndex = Mathf.Clamp(focusIndex, 0, list.Count - 1);
            reorderableList.index = newIndex;
            selection.SelectSingle(newIndex);
        }
        else
        {
            reorderableList.index = -1;
        }

        return true;
    }
}
