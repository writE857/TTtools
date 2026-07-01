using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdeSceneAndPackageToolWindow : EditorWindow
{
    const string LivePathSceneTextEditorPrefsKey = "Ade.Editor.LivePathSceneTexts";
    const string BgdtPackagePath = @"E:\UnityTools\Editor\TTtool\com.bytedance.bgdt-cp-3.0.271.unitypackage";
    const string MinigamePackagePath = @"E:\UnityTools\Editor\TTtool\minigame.202601131148.unitypackage";
    const string LivePathPrefabPath = "Assets/Ade_Framework/Resources/直播路径.prefab";

    [SerializeField] GameObject livePathPrefabOverride;
    [SerializeField] List<LivePathSceneTextDraft> livePathSceneTextDrafts = new();

    Vector2 scrollPosition;
    bool livePathSceneTextDirty;
    GUIStyle sectionTitleStyle;
    GUIStyle sectionNoteStyle;
    GUIStyle pathLabelStyle;

    [MenuItem("Ade_Tools/场景与包工具")]
    public static void OpenWindow()
    {
        GetWindow<AdeSceneAndPackageToolWindow>("场景与包工具");
    }

    void OnEnable()
    {
        minSize = new Vector2(520f, 420f);
        LoadLivePathSceneTextDrafts();
    }

    void OnGUI()
    {
        BuildStyles();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        try
        {
            DrawLivePathSection();
            EditorGUILayout.Space(10f);
            DrawPackageCleanupSection();
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    void DrawLivePathSection()
    {
        BeginSectionCard("直播路径", "Build Settings 场景批量工具");

        livePathPrefabOverride = (GameObject)EditorGUILayout.ObjectField(
            "直播路径预制体",
            ResolveLivePathPrefab(),
            typeof(GameObject),
            false);

        EditorGUILayout.Space(4f);
        SyncLivePathSceneTextDraftsWithBuildSettings();
        DrawLivePathSceneTextList();

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = livePathSceneTextDirty && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
            if (GUILayout.Button("应用文字", GUILayout.Height(24f)))
            {
                SaveLivePathSceneTextDrafts();
            }

            GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isUpdating;
            if (GUILayout.Button("添加/更新 Build Settings 场景", GUILayout.Height(24f)))
            {
                AddLivePathPrefabToBuildSettingsScenes();
            }
            GUI.enabled = true;
        }

        if (GUILayout.Button("删除 Build Settings 场景里的该预制体", GUILayout.Height(24f)))
        {
            RemoveLivePathPrefabFromBuildSettingsScenes();
        }

        EndSectionCard();
    }

    void DrawPackageCleanupSection()
    {
        BeginSectionCard("包清理", "按包文件名识别清理目标");
        EditorGUILayout.LabelField("ByteGame 相关包会直接清理整个 Assets/Plugins/ByteGame。", sectionNoteStyle);
        EditorGUILayout.Space(6f);

        DrawPackageCleanupButton(BgdtPackagePath);
        DrawPackageCleanupButton(MinigamePackagePath);

        EndSectionCard();
    }

    void DrawLivePathSceneTextList()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("场景文字", EditorStyles.boldLabel);

            for (int i = 0; i < livePathSceneTextDrafts.Count; i++)
            {
                LivePathSceneTextDraft item = livePathSceneTextDrafts[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField(item.SceneName, GUILayout.Width(120f));
                    }

                    string updatedText = EditorGUILayout.TextField(item.Text ?? string.Empty);
                    if (updatedText != item.Text)
                    {
                        item.Text = updatedText;
                        livePathSceneTextDirty = true;
                    }
                }
            }
        }
    }

    void DrawPackageCleanupButton(string packagePath)
    {
        string[] importedAssets = GetImportedAssetsByPackageFileName(packagePath);
        string buttonLabel = GetCleanupButtonLabel(packagePath);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isUpdating;
            if (GUILayout.Button(buttonLabel, GUILayout.Height(24f)))
            {
                DeleteImportedPackageAssets(packagePath, importedAssets);
            }
            GUI.enabled = true;

            EditorGUILayout.LabelField(
                AnyAssetExists(importedAssets) ? "当前工程里检测到这些导入内容" : "当前工程里未检测到这些导入内容",
                EditorStyles.miniLabel);
        }
    }

    string[] GetImportedAssetsByPackageFileName(string packagePath)
    {
        string fileName = System.IO.Path.GetFileName(packagePath).ToLowerInvariant();
        if (fileName.Contains("bgdt") || fileName.Contains("bytedance"))
        {
            return new[] { "Assets/Plugins/ByteGame" };
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
                Debug.LogWarning($"AdeSceneAndPackageTool: 删除失败 {assetPath}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"AdeSceneAndPackageTool: 已删除 {deletedCount} 项包内容");
    }

    bool AnyAssetExists(IEnumerable<string> assetPaths)
    {
        return assetPaths.Any(AssetExists);
    }

    bool AssetExists(string assetPath)
    {
        return AssetDatabase.IsValidFolder(assetPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null;
    }

    void LoadLivePathSceneTextDrafts()
    {
        livePathSceneTextDrafts ??= new List<LivePathSceneTextDraft>();
        livePathSceneTextDrafts.Clear();

        string json = EditorPrefs.GetString(LivePathSceneTextEditorPrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                LivePathSceneTextStore store = JsonUtility.FromJson<LivePathSceneTextStore>(json);
                if (store?.Items != null)
                {
                    livePathSceneTextDrafts.AddRange(store.Items.Where(item => item != null));
                }
            }
            catch
            {
                EditorPrefs.DeleteKey(LivePathSceneTextEditorPrefsKey);
            }
        }

        SyncLivePathSceneTextDraftsWithBuildSettings();
        livePathSceneTextDirty = false;
    }

    void SaveLivePathSceneTextDrafts()
    {
        LivePathSceneTextStore store = new LivePathSceneTextStore
        {
            Items = livePathSceneTextDrafts
                .Select(item => new LivePathSceneTextDraft
                {
                    ScenePath = item.ScenePath,
                    SceneName = item.SceneName,
                    Text = item.Text ?? string.Empty,
                })
                .ToList()
        };

        EditorPrefs.SetString(LivePathSceneTextEditorPrefsKey, JsonUtility.ToJson(store));
        livePathSceneTextDirty = false;
    }

    void SyncLivePathSceneTextDraftsWithBuildSettings()
    {
        livePathSceneTextDrafts ??= new List<LivePathSceneTextDraft>();

        string[] scenePaths = EditorBuildSettings.scenes
            .Select(item => item.path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct()
            .ToArray();

        Dictionary<string, LivePathSceneTextDraft> existing = livePathSceneTextDrafts
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.ScenePath))
            .ToDictionary(item => item.ScenePath, item => item, StringComparer.OrdinalIgnoreCase);

        string defaultText = GetLivePathDefaultText();
        List<LivePathSceneTextDraft> synced = new List<LivePathSceneTextDraft>(scenePaths.Length);

        foreach (string scenePath in scenePaths)
        {
            if (existing.TryGetValue(scenePath, out LivePathSceneTextDraft draft))
            {
                draft.SceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                synced.Add(draft);
            }
            else
            {
                synced.Add(new LivePathSceneTextDraft
                {
                    ScenePath = scenePath,
                    SceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath),
                    Text = defaultText,
                });
            }
        }

        livePathSceneTextDrafts.Clear();
        livePathSceneTextDrafts.AddRange(synced);
        SaveLivePathSceneTextDrafts();
    }

    string GetLivePathSceneText(string scenePath)
    {
        LivePathSceneTextDraft draft = livePathSceneTextDrafts.FirstOrDefault(item =>
            string.Equals(item.ScenePath, scenePath, StringComparison.OrdinalIgnoreCase));
        return draft != null ? draft.Text ?? string.Empty : GetLivePathDefaultText();
    }

    string GetLivePathDefaultText()
    {
        GameObject prefab = ResolveLivePathPrefab();
        if (prefab == null)
        {
            return string.Empty;
        }

        Text text = prefab.GetComponentInChildren<Text>(true);
        return text != null ? text.text ?? string.Empty : string.Empty;
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

    void AddLivePathPrefabToBuildSettingsScenes()
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

        SyncLivePathSceneTextDraftsWithBuildSettings();

        bool confirmed = EditorUtility.DisplayDialog(
            "批量添加直播路径",
            $"将检查并处理 Build Settings 中的 {scenePaths.Length} 个场景。\n已存在该 Prefab 的场景会自动更新文字。",
            "开始",
            "取消");

        if (!confirmed)
        {
            return;
        }

        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        int addedCount = 0;
        int updatedCount = 0;

        try
        {
            foreach (string scenePath in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                string sceneText = GetLivePathSceneText(scenePath);
                List<GameObject> existingInstances = FindPrefabInstancesInScene(scene, prefabPath);

                if (existingInstances.Count > 0)
                {
                    if (ApplyLivePathTextToInstances(existingInstances, sceneText))
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                    updatedCount++;
                    continue;
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                ApplyLivePathText(instance, sceneText);
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

        EditorUtility.DisplayDialog("处理完成", $"已新增 {addedCount} 个场景，更新 {updatedCount} 个场景。", "确定");
    }

    void RemoveLivePathPrefabFromBuildSettingsScenes()
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

        EditorUtility.DisplayDialog("处理完成", $"已删除 {removedCount} 个实例，{untouchedCount} 个场景未发现该预制体。", "确定");
    }

    bool ApplyLivePathTextToInstances(List<GameObject> instances, string sceneText)
    {
        bool changed = false;
        foreach (GameObject instance in instances)
        {
            changed |= ApplyLivePathText(instance, sceneText);
        }
        return changed;
    }

    bool ApplyLivePathText(GameObject instance, string sceneText)
    {
        if (instance == null)
        {
            return false;
        }

        Text text = instance.GetComponentInChildren<Text>(true);
        if (text == null)
        {
            return false;
        }

        if (string.Equals(text.text, sceneText, StringComparison.Ordinal))
        {
            return false;
        }

        Undo.RecordObject(text, "Update Live Path Text");
        text.text = sceneText;
        EditorUtility.SetDirty(text);
        return true;
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
                if (!string.Equals(sourcePath, prefabPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(child.gameObject) ?? child.gameObject;
                if (uniqueRoots.Add(instanceRoot))
                {
                    matches.Add(instanceRoot);
                }
            }
        }

        return matches;
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

        pathLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
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

    [Serializable]
    class LivePathSceneTextDraft
    {
        public string ScenePath = string.Empty;
        public string SceneName = string.Empty;
        public string Text = string.Empty;
    }

    [Serializable]
    class LivePathSceneTextStore
    {
        public List<LivePathSceneTextDraft> Items = new();
    }
}
