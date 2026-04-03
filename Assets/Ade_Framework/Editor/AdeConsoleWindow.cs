using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    const string NoAdsSymbol = "ADE_NO_ADS";
    const string ProjectSettingsAssetPath = "ProjectSettings/ProjectSettings.asset";
    const string BuiltinWebGLTemplateRoot = @"E:\UnityEditor\2021.3.21f1c1\Editor\Data\PlaybackEngines\WebGLSupport\BuildTools\WebGLTemplates";
    const string LivePathPrefabPath = "Assets/Ade_Framework/Resources/直播路径.prefab";

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
    string rewardFeedRepeatContentIdDraft = string.Empty;
    [SerializeField] GameObject livePathPrefabOverride;
    readonly AdItemDraft interstitialAdDraft = new();
    readonly AdItemDraft bannerAdDraft = new();
    readonly List<string> subscribeTemplateDrafts = new();
    readonly List<string> feedContentDrafts = new();
    readonly List<AdItemDraft> rewardAdDrafts = new();
    readonly List<string> editableCustomSymbols = new();
    ReorderableList customSymbolList;
    ReorderableList subscribeTemplateList;
    ReorderableList feedContentList;
    ReorderableList rewardAdList;
    GUIStyle sectionTitleStyle;
    GUIStyle sectionNoteStyle;
    GUIStyle summaryLabelStyle;
    GUIStyle summaryValueStyle;
    GUIStyle pathLabelStyle;
    GUIStyle templateCardStyle;
    GUIStyle templateCardSelectedStyle;
    GUIStyle templateButtonStyle;
    GUIStyle templateNameSelectedStyle;
    GUIStyle templateNameStyle;

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

    void EnsureEditorStateInitialized()
    {
        if (customSymbolList != null && subscribeTemplateList != null && feedContentList != null && rewardAdList != null)
        {
            return;
        }

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

        subscribeTemplateList = CreateStringListEditor(subscribeTemplateDrafts, "订阅模板");
        feedContentList = CreateStringListEditor(feedContentDrafts, "推荐流内容");
        rewardAdList = CreateAdItemListEditor(rewardAdDrafts, "激励参数");
        LoadRewardConfigDrafts();
    }

    void OnGUI()
    {
        EnsureEditorStateInitialized();
        BuildStyles();
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        List<string> symbols = GetSymbols(group);
        bool beganScrollView = false;

        try
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            beganScrollView = true;

            DrawHeader(group, symbols);
            EditorGUILayout.Space(12);

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

    void DrawHeader(BuildTargetGroup group, List<string> symbols)
    {
        BeginSectionCard("Ade 控制台", string.Empty);
        DrawSummaryRow("当前平台组", group.ToString());
        DrawSummaryRow("当前平台宏", GetCurrentPlatformSymbol(symbols));
        DrawSummaryRow("当前宏列表", symbols.Count > 0 ? string.Join("; ", symbols) : "无");
        EndSectionCard();
    }

    void DrawQuadrantLayout(BuildTargetGroup group, List<string> symbols)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawQuadrantColumn(() => DrawDefineSection(group, symbols));
            GUILayout.Space(10f);
            DrawQuadrantColumn(DrawPackageCleanupSection);
        }

        GUILayout.Space(10f);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawQuadrantColumn(() => DrawRewardConfigSection(group, symbols));
            GUILayout.Space(10f);
            DrawQuadrantColumn(DrawWebGLTemplateSection);
        }
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
        EditorGUILayout.LabelField("先选预设，再补充自定义宏。", sectionNoteStyle);

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

        EditorGUILayout.Space(4f);
        customSymbolList.DoLayoutList();

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

        DrawSummaryRow("预览结果", BuildDefineString(GetSanitizedCustomSymbols(false)));
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
        EditorGUILayout.LabelField("ByteGame 相关包会直接清理整个 Assets/Plugins/ByteGame。", sectionNoteStyle);
        EditorGUILayout.Space(4f);

        DrawNoAdsToolbar(EditorUserBuildSettings.selectedBuildTargetGroup, GetSymbols(EditorUserBuildSettings.selectedBuildTargetGroup));
        EditorGUILayout.Space(6f);

        livePathPrefabOverride = (GameObject)EditorGUILayout.ObjectField(
            "直播路径预制体",
            ResolveLivePathPrefab(),
            typeof(GameObject),
            false);

        EditorGUILayout.Space(4f);
        if (GUILayout.Button("给所有场景添加直播路径", GUILayout.Height(24)))
        {
            AddLivePathPrefabToAllScenes();
        }
        if (GUILayout.Button("删除所有场景里的该预制体", GUILayout.Height(24)))
        {
            RemoveLivePathPrefabFromAllScenes();
        }

        EditorGUILayout.Space(6f);
        DrawPackageCleanupButton(BgdtPackagePath);
        DrawPackageCleanupButton(MinigamePackagePath);
        EndSectionCard();
    }

    void DrawRewardConfigSection(BuildTargetGroup group, List<string> symbols)
    {
        BeginSectionCard("激励参数", "AdeDataInfo 与激励广告配置");

        UnityEngine.Object adeDataInfo = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdeDataInfoPath);
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

        EditorGUILayout.Space(4f);

        UnityEngine.Object adsData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdsDataPath);
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

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("还原参数", GUILayout.Height(24)))
            {
                LoadRewardConfigDrafts();
            }

            GUI.enabled = rewardConfigDirty && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
            if (GUILayout.Button("应用参数", GUILayout.Height(24)))
            {
                SaveRewardConfigDrafts();
            }
            GUI.enabled = true;
        }

        EndSectionCard();
    }

    void DrawNoAdsToolbar(BuildTargetGroup group, List<string> symbols)
    {
        bool noAdsEnabled = symbols.Contains(NoAdsSymbol);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawSummaryRow("当前模式", noAdsEnabled ? "无广模式" : "正常广告");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isUpdating && !noAdsEnabled;
                if (GUILayout.Button("启用无广模式", GUILayout.Height(24)))
                {
                    SetNoAdsMode(group, true);
                }

                GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isUpdating && noAdsEnabled;
                if (GUILayout.Button("恢复广告模式", GUILayout.Height(24)))
                {
                    SetNoAdsMode(group, false);
                }
                GUI.enabled = true;
            }

            EditorGUILayout.LabelField("通过 ADE_NO_ADS 宏统一控制。无广模式下激励广告会直接走成功回调，原有奖励照发。", sectionNoteStyle);
        }
    }

    void DrawRewardAdeDataInfoEditor()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("内容参数", EditorStyles.miniBoldLabel);
            string newShareId = EditorGUILayout.TextField("分享ID", rewardShareIdDraft);
            if (newShareId != rewardShareIdDraft)
            {
                rewardShareIdDraft = newShareId;
                rewardConfigDirty = true;
            }

            subscribeTemplateList.DoLayoutList();
            EditorGUILayout.Space(2f);
            feedContentList.DoLayoutList();

            string newFeedRepeatContentId = EditorGUILayout.TextField("复访流内容", rewardFeedRepeatContentIdDraft);
            if (newFeedRepeatContentId != rewardFeedRepeatContentIdDraft)
            {
                rewardFeedRepeatContentIdDraft = newFeedRepeatContentId;
                rewardConfigDirty = true;
            }
        }
    }

    void DrawWebGLTemplateSection()
    {
        BeginSectionCard("WebGL Template", "模板切换");

        string currentTemplate = GetCurrentWebGLTemplate();
        List<WebGLTemplateDraft> templates = GetAvailableWebGLTemplates(currentTemplate);
        DrawUnityStyleTemplateSelector(templates, currentTemplate);
        EndSectionCard();
    }

    void DrawUnityStyleTemplateSelector(List<WebGLTemplateDraft> templates, string currentTemplate)
    {
        EditorGUILayout.LabelField("WebGL Template", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width * 0.5f - 40f) / 110f));
        for (int i = 0; i < templates.Count; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int col = 0; col < columns && i + col < templates.Count; col++)
                {
                    DrawUnityTemplateCard(templates[i + col], currentTemplate);
                    if (col < columns - 1)
                    {
                        GUILayout.Space(10f);
                    }
                }
            }

            if (i + columns < templates.Count)
            {
                GUILayout.Space(4f);
            }
        }
    }

    void DrawUnityTemplateCard(WebGLTemplateDraft template, string currentTemplate)
    {
        bool isSelected = string.Equals(template.Value, currentTemplate, StringComparison.Ordinal);
        GUIStyle cardStyle = isSelected ? templateCardSelectedStyle : templateCardStyle;

        using (new EditorGUILayout.VerticalScope(GUILayout.Width(100f)))
        {
            Rect cardRect = GUILayoutUtility.GetRect(100f, 84f, GUILayout.Width(100f), GUILayout.Height(84f));
            GUI.Box(cardRect, GUIContent.none, cardStyle);

            Rect previewRect = new Rect(cardRect.x + 8f, cardRect.y + 8f, cardRect.width - 16f, 54f);
            Texture thumbnail = template.Thumbnail ?? EditorGUIUtility.IconContent("BuildSettings.Web.Small")?.image;
            if (thumbnail != null)
            {
                GUI.DrawTexture(previewRect, thumbnail, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.18f, 0.18f, 0.18f));
            }

            if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none) && !isSelected)
            {
                SetCurrentWebGLTemplate(template.Value);
            }

            Rect labelRect = new Rect(cardRect.x + 4f, cardRect.y + 64f, cardRect.width - 8f, 16f);
            GUI.Label(labelRect, template.DisplayName, isSelected ? templateNameSelectedStyle : templateNameStyle);
        }
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
            templates.Insert(0, new WebGLTemplateDraft(currentTemplate, GetTemplateDisplayName(currentTemplate), LoadTemplateThumbnail(currentTemplate)));
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

                templates.Add(new WebGLTemplateDraft(templateValue, folderName, LoadTemplateThumbnail(templateValue)));
            }
        }

        return templates;
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

    void DrawRewardOnlyEditor()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("广告参数", EditorStyles.miniBoldLabel);
            DrawAdItemDraftFields("插屏参数", interstitialAdDraft);
            DrawAdItemDraftFields("Banner 参数", bannerAdDraft);
            rewardAdList.DoLayoutList();
        }
    }

    void DrawAdItemDraftFields(string title, AdItemDraft draft)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            string newName = EditorGUILayout.TextField("Name", draft.Name);
            if (newName != draft.Name)
            {
                draft.Name = newName;
                rewardConfigDirty = true;
            }

            string newId = EditorGUILayout.TextField("ID", draft.Id);
            if (newId != draft.Id)
            {
                draft.Id = newId;
                rewardConfigDirty = true;
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

        templateCardStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(0, 0, 0, 0),
        };

        templateCardSelectedStyle = new GUIStyle(templateCardStyle);
        templateCardSelectedStyle.normal.background = MakeSolidTexture(new Color(0.16f, 0.39f, 0.87f, 0.85f));

        templateButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleCenter,
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

    void DrawSummaryRow(string label, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, summaryLabelStyle, GUILayout.Width(90f));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(value) ? "无" : value, summaryValueStyle);
        }
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

    Texture2D MakeSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    ReorderableList CreateStringListEditor(List<string> list, string header)
    {
        var reorderableList = new ReorderableList(list, typeof(string), true, true, true, true);
        reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            rect.y += 1f;
            rect.height = EditorGUIUtility.singleLineHeight;
            string updatedValue = EditorGUI.TextField(rect, list[index] ?? string.Empty);
            if (updatedValue != list[index])
            {
                list[index] = updatedValue;
                rewardConfigDirty = true;
            }
        };
        reorderableList.onAddCallback = _ =>
        {
            list.Add(string.Empty);
            rewardConfigDirty = true;
        };
        reorderableList.onRemoveCallback = reorderable =>
        {
            if (reorderable.index < 0 || reorderable.index >= list.Count)
            {
                return;
            }

            list.RemoveAt(reorderable.index);
            rewardConfigDirty = true;
        };
        reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
        reorderableList.footerHeight = 22f;
        return reorderableList;
    }

    ReorderableList CreateAdItemListEditor(List<AdItemDraft> list, string header)
    {
        var reorderableList = new ReorderableList(list, typeof(AdItemDraft), true, true, true, true);
        reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= list.Count)
            {
                return;
            }

            AdItemDraft item = list[index];
            Rect nameRect = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUIUtility.singleLineHeight);
            Rect idRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 6f, rect.width, EditorGUIUtility.singleLineHeight);

            string newName = EditorGUI.TextField(nameRect, "Name", item.Name);
            if (newName != item.Name)
            {
                item.Name = newName;
                rewardConfigDirty = true;
            }

            string newId = EditorGUI.TextField(idRect, "ID", item.Id);
            if (newId != item.Id)
            {
                item.Id = newId;
                rewardConfigDirty = true;
            }
        };
        reorderableList.onAddCallback = _ =>
        {
            list.Add(new AdItemDraft { Name = "激励", Id = string.Empty });
            rewardConfigDirty = true;
        };
        reorderableList.onRemoveCallback = reorderable =>
        {
            if (reorderable.index < 0 || reorderable.index >= list.Count)
            {
                return;
            }

            list.RemoveAt(reorderable.index);
            rewardConfigDirty = true;
        };
        reorderableList.elementHeight = EditorGUIUtility.singleLineHeight * 2f + 10f;
        reorderableList.footerHeight = 22f;
        return reorderableList;
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
            NormalizeStringListField(asset, "SubscribeTmplIds");
            NormalizeStringListField(asset, "FeedContentIDs");
            return;
        }

        if (assetType.Name == "AdsData")
        {
            NormalizeRewardArray(asset);
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

    void SetNoAdsMode(BuildTargetGroup group, bool enabled)
    {
        List<string> symbols = GetSymbols(group);
        symbols.RemoveAll(item => item == NoAdsSymbol);

        if (enabled)
        {
            symbols.Add(NoAdsSymbol);
        }

        string defineString = BuildDefineString(symbols);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defineString);
        LoadCustomSymbols(group, symbols);
        Debug.Log(enabled
            ? "AdeConsole: 已启用无广模式"
            : "AdeConsole: 已恢复广告模式");
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
        Debug.Log($"AdeConsole: WebGL Template 已切换为 {GetTemplateDisplayName(templateValue)}");
    }

    string GetTemplateDisplayName(string templateValue)
    {
        int separatorIndex = templateValue.IndexOf(':');
        return separatorIndex >= 0 && separatorIndex < templateValue.Length - 1
            ? templateValue.Substring(separatorIndex + 1)
            : templateValue;
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
            return System.IO.Path.Combine(BuiltinWebGLTemplateRoot, templateName, "thumbnail.png");
        }

        if (templateValue.StartsWith("PROJECT:", StringComparison.Ordinal))
        {
            string templateName = GetTemplateDisplayName(templateValue);
            return System.IO.Path.Combine(Application.dataPath, "WebGLTemplates", templateName, "thumbnail.png");
        }

        return null;
    }

    void AddLivePathPrefabToAllScenes()
    {
        GameObject prefab = ResolveLivePathPrefab();
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("未找到 Prefab", $"未找到可用预制体。\n默认路径：\n{LivePathPrefabPath}", "确定");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        string[] scenePaths = EditorBuildSettings.scenes
            .Select(item => item.path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct()
            .ToArray();

        if (scenePaths.Length == 0)
        {
            EditorUtility.DisplayDialog("未找到场景", "Build Settings 中没有可处理的场景。", "确定");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "批量添加直播路径",
            $"将检查并处理 Build Settings 中的 {scenePaths.Length} 个场景。\n已存在该 Prefab 的场景会自动跳过。",
            "开始",
            "取消");

        if (!confirmed)
        {
            return;
        }

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        int addedCount = 0;
        int skippedCount = 0;

        try
        {
            foreach (string scenePath in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (SceneContainsPrefab(scene, prefabPath))
                {
                    skippedCount++;
                    continue;
                }

                PrefabUtility.InstantiatePrefab(prefab, scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                addedCount++;
            }
        }
        finally
        {
            if (originalSetup != null && originalSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        EditorUtility.DisplayDialog(
            "处理完成",
            $"已新增 {addedCount} 个场景，跳过 {skippedCount} 个已存在场景。",
            "确定");
    }

    void RemoveLivePathPrefabFromAllScenes()
    {
        GameObject prefab = ResolveLivePathPrefab();
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("未找到 Prefab", $"未找到可用预制体。\n默认路径：\n{LivePathPrefabPath}", "确定");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        string[] scenePaths = EditorBuildSettings.scenes
            .Select(item => item.path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct()
            .ToArray();

        if (scenePaths.Length == 0)
        {
            EditorUtility.DisplayDialog("未找到场景", "Build Settings 中没有可处理的场景。", "确定");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "批量删除直播路径",
            $"将从 Build Settings 中的 {scenePaths.Length} 个场景里删除当前挂载预制体实例。",
            "删除",
            "取消");

        if (!confirmed)
        {
            return;
        }

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        int removedCount = 0;
        int untouchedCount = 0;

        try
        {
            foreach (string scenePath in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                List<GameObject> instances = FindPrefabInstancesInScene(scene, prefabPath);
                if (instances.Count == 0)
                {
                    untouchedCount++;
                    continue;
                }

                foreach (GameObject instance in instances)
                {
                    Undo.DestroyObjectImmediate(instance);
                    removedCount++;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            if (originalSetup != null && originalSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        EditorUtility.DisplayDialog(
            "处理完成",
            $"已删除 {removedCount} 个实例，{untouchedCount} 个场景未发现该预制体。",
            "确定");
    }

    bool SceneContainsPrefab(Scene scene, string prefabPath)
    {
        return FindPrefabInstancesInScene(scene, prefabPath).Count > 0;
    }

    List<GameObject> FindPrefabInstancesInScene(Scene scene, string prefabPath)
    {
        List<GameObject> matches = new List<GameObject>();
        HashSet<GameObject> uniqueRoots = new HashSet<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                if (source == null)
                {
                    continue;
                }

                string sourcePath = AssetDatabase.GetAssetPath(source);
                if (string.Equals(sourcePath, prefabPath, StringComparison.OrdinalIgnoreCase))
                {
                    GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(child.gameObject) ?? child.gameObject;
                    if (uniqueRoots.Add(instanceRoot))
                    {
                        matches.Add(instanceRoot);
                    }
                }
            }
        }

        return matches;
    }

    void LoadRewardConfigDrafts()
    {
        UnityEngine.Object adeDataInfo = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdeDataInfoPath);
        if (adeDataInfo != null)
        {
            EnsureListField(adeDataInfo, "SubscribeTmplIds");
            EnsureListField(adeDataInfo, "FeedContentIDs");
            rewardShareIdDraft = GetStringFieldValue(adeDataInfo, "ShareId");
            rewardFeedRepeatContentIdDraft = GetStringFieldValue(adeDataInfo, "FeedRepeatContentID");

            subscribeTemplateDrafts.Clear();
            subscribeTemplateDrafts.AddRange(GetOrCreateStringListField(adeDataInfo, "SubscribeTmplIds"));

            feedContentDrafts.Clear();
            feedContentDrafts.AddRange(GetOrCreateStringListField(adeDataInfo, "FeedContentIDs"));
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
        }

        rewardConfigDirty = false;
    }

    void SaveRewardConfigDrafts()
    {
        UnityEngine.Object adeDataInfo = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdeDataInfoPath);
        if (adeDataInfo != null)
        {
            SetFieldValue(adeDataInfo, "ShareId", rewardShareIdDraft ?? string.Empty);
            SetFieldValue(adeDataInfo, "FeedRepeatContentID", rewardFeedRepeatContentIdDraft ?? string.Empty);
            SetFieldValue(adeDataInfo, "SubscribeTmplIds", new List<string>(subscribeTemplateDrafts));
            SetFieldValue(adeDataInfo, "FeedContentIDs", new List<string>(feedContentDrafts));
            SaveReflectedAssetChanges(adeDataInfo);
        }

        UnityEngine.Object adsData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AdsDataPath);
        if (adsData != null)
        {
            EnsureAdsDataStructure(adsData);
            object adDataValue = GetFieldValue(adsData, "AdData");
            SaveAdItemDraft(interstitialAdDraft, GetFieldValue(adDataValue, "InterstitialID"));
            SaveAdItemDraft(bannerAdDraft, GetFieldValue(adDataValue, "BannerID"));
            SetRewardItemsFromDrafts(adDataValue, rewardAdDrafts);
            SaveReflectedAssetChanges(adsData);
        }

        rewardConfigDirty = false;
        Debug.Log("AdeConsole: 激励参数已保存");
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

    GameObject ResolveLivePathPrefab()
    {
        if (livePathPrefabOverride != null)
        {
            return livePathPrefabOverride;
        }

        livePathPrefabOverride = AssetDatabase.LoadAssetAtPath<GameObject>(LivePathPrefabPath);
        return livePathPrefabOverride;
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

    class AdItemDraft
    {
        public string Name = string.Empty;
        public string Id = string.Empty;
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
}
