using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class ReplaceFontReference : EditorWindow
{
    private const string Title = "字体引用替换";
    private static readonly HashSet<string> SupportedFontTypeNames = new HashSet<string>
    {
        typeof(Font).FullName,
        "TMPro.TMP_FontAsset"
    };

    private static readonly HashSet<string> SearchExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".prefab",
        ".unity",
        ".asset",
        ".mat",
        ".controller",
        ".anim",
        ".playable"
    };

    private Object targetFont;
    private Object replaceFont;

    [MenuItem("Ade_Tools/字体引用替换")]
    private static void ShowWindow()
    {
        var window = GetWindow<ReplaceFontReference>(true, Title);
        window.minSize = new Vector2(520f, 220f);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("选择目标字体和替换字体，点击按钮后会批量替换项目中的字体引用。", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(8);

        targetFont = EditorGUILayout.ObjectField("目标字体", targetFont, typeof(Object), false);
        replaceFont = EditorGUILayout.ObjectField("替换字体", replaceFont, typeof(Object), false);

        var canReplace = ValidateSelection(out var errMsg);
        if (!string.IsNullOrEmpty(errMsg))
        {
            EditorGUILayout.HelpBox(errMsg, MessageType.Warning);
        }

        EditorGUILayout.Space(10);
        EditorGUI.BeginDisabledGroup(!canReplace);
        if (GUILayout.Button("替换引用", GUILayout.Height(36)))
        {
            DoReplace();
        }
        EditorGUI.EndDisabledGroup();
    }

    private bool ValidateSelection(out string errMsg)
    {
        errMsg = null;
        if (targetFont == null || replaceFont == null)
        {
            errMsg = "请先选择目标字体和替换字体。";
            return false;
        }

        if (ReferenceEquals(targetFont, replaceFont))
        {
            errMsg = "目标字体和替换字体不能是同一个资源。";
            return false;
        }

        if (!IsSupportedFont(targetFont) || !IsSupportedFont(replaceFont))
        {
            errMsg = "仅支持 UnityEngine.Font 或 TMP_FontAsset。";
            return false;
        }

        if (targetFont.GetType() != replaceFont.GetType())
        {
            errMsg = "目标字体与替换字体类型不一致，无法替换。";
            return false;
        }

        return true;
    }

    private void DoReplace()
    {
        if (!ValidateSelection(out var errMsg))
        {
            ShowNotification(new GUIContent(errMsg ?? "参数无效"));
            return;
        }

        var confirm = EditorUtility.DisplayDialog(
            Title,
            string.Format("确定将所有对字体\n{0}\n的引用替换为\n{1}\n吗？", targetFont.name, replaceFont.name),
            "确认",
            "取消");
        if (!confirm)
        {
            return;
        }

        try
        {
            if (!TryGetRefParts(targetFont, out var oldGuid, out var oldLocalId, out var parseErr))
            {
                throw new Exception(parseErr);
            }

            if (!TryGetRefParts(replaceFont, out var newGuid, out var newLocalId, out parseErr))
            {
                throw new Exception(parseErr);
            }

            var pattern = string.Format(
                @"fileID:\s*{0}\s*,\s*guid:\s*{1}\s*,\s*type:\s*(\d+)",
                Regex.Escape(oldLocalId),
                Regex.Escape(oldGuid));
            var regex = new Regex(pattern, RegexOptions.Compiled);

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new Exception("无法获取项目根目录。");
            }

            var assetRoot = Path.Combine(projectRoot, "Assets");
            var files = Directory.EnumerateFiles(assetRoot, "*.*", SearchOption.AllDirectories)
                .Where(path => SearchExtensions.Contains(Path.GetExtension(path)))
                .ToArray();

            var replacedFileCount = 0;
            var replacedRefCount = 0;

            for (var i = 0; i < files.Length; i++)
            {
                var filePath = files[i];
                var assetPath = "Assets" + filePath.Substring(assetRoot.Length).Replace('\\', '/');
                EditorUtility.DisplayProgressBar(Title, assetPath, (i + 1f) / files.Length);

                var content = File.ReadAllText(filePath);
                if (!content.Contains(oldGuid) || !content.Contains(oldLocalId))
                {
                    continue;
                }

                var hitCount = regex.Matches(content).Count;
                if (hitCount <= 0)
                {
                    continue;
                }

                var replacedContent = regex.Replace(content, match =>
                    string.Format("fileID: {0}, guid: {1}, type: {2}", newLocalId, newGuid, match.Groups[1].Value));

                File.WriteAllText(filePath, replacedContent);
                replacedFileCount++;
                replacedRefCount += hitCount;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var doneMsg = string.Format("替换完成，文件数：{0}，引用数：{1}", replacedFileCount, replacedRefCount);
            Debug.Log("[ReplaceFontReference] " + doneMsg);
            EditorUtility.DisplayDialog(Title, doneMsg, "确定");
            ShowNotification(new GUIContent("字体引用替换完成"));
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("[{0}] 替换失败: {1}", nameof(ReplaceFontReference), e));
            EditorUtility.DisplayDialog(Title, "替换失败:\n" + e.Message, "确定");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static bool IsSupportedFont(Object obj)
    {
        return obj != null && SupportedFontTypeNames.Contains(obj.GetType().FullName);
    }

    private static bool TryGetRefParts(Object obj, out string guid, out string localId, out string errMsg)
    {
        guid = null;
        localId = null;
        errMsg = null;

        if (obj == null)
        {
            errMsg = "目标对象为空。";
            return false;
        }

        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out long localFileId))
        {
            errMsg = "无法解析字体资源 GUID / LocalFileID。";
            return false;
        }

        localId = localFileId.ToString();
        return true;
    }
}
