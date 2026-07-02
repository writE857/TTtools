using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Ade_Framework
{
    public class AdeDouyinCloudSaveWindow : EditorWindow
    {
        const string ConsoleUrl = "https://cloud.douyin.com/management/customdomain?app=ttb788c13529e909f607&env=env-VB5v2yy9z7&source=1&type=2";

        AdeDouyinCloudSaveSettings settings;
        SerializedObject serializedSettings;
        Vector2 scrollPosition;

        [MenuItem("Ade_Tools/抖音云存档工具")]
        public static void OpenWindow()
        {
            GetWindow<AdeDouyinCloudSaveWindow>("抖音云存档");
        }

        void OnEnable()
        {
            minSize = new Vector2(460f, 360f);
            LoadOrCreateSettings();
        }

        void OnGUI()
        {
            if (settings == null)
            {
                if (GUILayout.Button("创建配置"))
                {
                    LoadOrCreateSettings();
                }

                return;
            }

            serializedSettings.Update();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("抖音云 PlayerPrefs 存档", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("运行时只处理 AdeCloudPlayerPrefs 写入的 int / float / string。AdeSDK 初始化完成并登录后会自动读取这里的配置。", MessageType.Info);

            EditorGUILayout.Space(8f);
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableCloudSave"), new GUIContent("启用云存档"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("envId"), new GUIContent("环境 ID"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("collectionName"), new GUIContent("集合名"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("documentKey"), new GUIContent("文档 Key"));

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存配置", GUILayout.Height(26f)))
                {
                    SaveSettings();
                }

                if (GUILayout.Button("打开抖音云控制台", GUILayout.Height(26f)))
                {
                    Application.OpenURL(ConsoleUrl);
                }
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("项目接入", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("新代码建议直接用 AdeCloudPlayerPrefs。老项目可以用下面的按钮批量把运行时代码里的 PlayerPrefs. 替换成 global::Ade_Framework.AdeCloudPlayerPrefs.，Editor 目录和本工具目录会跳过。", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("扫描替换 PlayerPrefs 引用", GUILayout.Height(26f)))
                {
                    ReplaceRuntimePlayerPrefsReferences();
                }

                if (GUILayout.Button("定位配置资产", GUILayout.Height(26f)))
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.SelectableLabel(
                "初始化：AdeSDK.InitBack() -> AdeDouyinCloudSave.InitializeFromSettings(GameShow)\n" +
                "读写：AdeCloudPlayerPrefs.GetInt / SetInt / Save",
                EditorStyles.helpBox,
                GUILayout.Height(48f));

            EditorGUILayout.EndScrollView();
        }

        void LoadOrCreateSettings()
        {
            settings = AssetDatabase.LoadAssetAtPath<AdeDouyinCloudSaveSettings>(AdeDouyinCloudSaveSettings.AssetPath);
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AdeDouyinCloudSaveSettings.AssetPath));
                settings = CreateInstance<AdeDouyinCloudSaveSettings>();
                settings.name = AdeDouyinCloudSaveSettings.ResourceName;
                AssetDatabase.CreateAsset(settings, AdeDouyinCloudSaveSettings.AssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(AdeDouyinCloudSaveSettings.AssetPath);
            }

            serializedSettings = new SerializedObject(settings);
        }

        void SaveSettings()
        {
            serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AdeDouyinCloudSaveSettings.AssetPath);
        }

        void ReplaceRuntimePlayerPrefsReferences()
        {
            if (!EditorUtility.DisplayDialog("确认替换", "会修改 Assets 下运行时代码里的 PlayerPrefs. 引用，Editor 目录和抖音云工具目录会跳过。继续吗？", "替换", "取消"))
            {
                return;
            }

            int changedCount = 0;
            string assetsPath = Application.dataPath.Replace('\\', '/');
            string[] files = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);
            Regex playerPrefsRegex = new Regex(@"(?<![\w.])PlayerPrefs\.", RegexOptions.Compiled);

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace('\\', '/');
                if (path.IndexOf("/Editor/", StringComparison.Ordinal) >= 0 || path.IndexOf("/Ade_Framework/DouyinCloudSave/", StringComparison.Ordinal) >= 0)
                {
                    continue;
                }

                string text = File.ReadAllText(path);
                string replaced = playerPrefsRegex.Replace(text, "global::Ade_Framework.AdeCloudPlayerPrefs.");
                if (string.Equals(text, replaced, StringComparison.Ordinal))
                {
                    continue;
                }

                File.WriteAllText(path, replaced);
                changedCount++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("替换完成", $"已修改 {changedCount} 个脚本。", "确定");
        }
    }
}
