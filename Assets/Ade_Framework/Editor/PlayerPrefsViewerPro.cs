using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class PlayerPrefsViewerPro : EditorWindow
{
    Vector2 scroll;
    string search = "";
    Dictionary<string, string> prefs = new Dictionary<string, string>();
    HashSet<string> recentlyModified = new HashSet<string>();
    DateTime lastModifiedTime;

    string newKey = "";
    string newValue = "";
    string newType = "string";

    readonly string[] typeOptions = new[] { "int", "float", "string" };

    [MenuItem("Ade_Tools/PlayerPrefs 查看器")]
    static void Init()
    {
        var window = GetWindow<PlayerPrefsViewerPro>("PlayerPrefs Viewer Pro");
        window.Refresh();
    }

    void OnGUI()
    {
        DrawToolbar();
        DrawBulkAdd();
        DrawList();
    }

    void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        search = GUILayout.TextField(search, GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.textField, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            Refresh();
        if (GUILayout.Button("全部清空", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("确认清除", "是否清除所有 PlayerPrefs 数据？", "确定", "取消"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Refresh();
            }
        }
        GUILayout.EndHorizontal();
    }

    void DrawBulkAdd()
    {
        //GUILayout.Space(5);
        //EditorGUILayout.LabelField("🔁 批量添加键值对（格式：key:type:value 每行一组）", EditorStyles.boldLabel);
        //bulkInput = EditorGUILayout.TextArea(bulkInput, GUILayout.MinHeight(60));

        //if (GUILayout.Button("批量添加"))
        //{
        //    var lines = bulkInput.Split('\n');
        //    foreach (var line in lines)
        //    {
        //        var parts = line.Trim().Split(':');
        //        if (parts.Length != 3) continue;

        //        var key = parts[0];
        //        var type = parts[1].ToLower();
        //        var val = parts[2];

        //        switch (type)
        //        {
        //            case "int":
        //                if (int.TryParse(val, out int i)) PlayerPrefs.SetInt(key, i);
        //                break;
        //            case "float":
        //                if (float.TryParse(val, out float f)) PlayerPrefs.SetFloat(key, f);
        //                break;
        //            case "string":
        //                PlayerPrefs.SetString(key, val);
        //                break;
        //        }
        //        recentlyModified.Add(key);
        //        lastModifiedTime = DateTime.Now;
        //    }

        //    PlayerPrefs.Save();
        //    Refresh();
        //}

        GUILayout.Space(10);
        EditorGUILayout.LabelField("➕ 手动添加单个键值", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        newKey = GUILayout.TextField(newKey, GUILayout.Width(150));
        newType = typeOptions[EditorGUILayout.Popup(Array.IndexOf(typeOptions, newType), typeOptions, GUILayout.Width(80))];
        newValue = GUILayout.TextField(newValue, GUILayout.Width(150));
        if (GUILayout.Button("添加", GUILayout.Width(60)))
        {
            AddNewEntry(newKey, newType, newValue);
        }
        GUILayout.EndHorizontal();
    }

    void AddNewEntry(string key, string type, string value)
    {
        if (string.IsNullOrEmpty(key)) return;
        try
        {
            switch (type)
            {
                case "int":
                    PlayerPrefs.SetInt(key, int.Parse(value));
                    break;
                case "float":
                    PlayerPrefs.SetFloat(key, float.Parse(value));
                    break;
                case "string":
                    PlayerPrefs.SetString(key, value);
                    break;
            }

            PlayerPrefs.Save();
            recentlyModified.Add(key);
            lastModifiedTime = DateTime.Now;
            Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError("添加失败: " + e.Message);
        }
    }

    void DrawList()
    {
        GUILayout.Space(10);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        // 先将字典的键复制到一个临时列表中，避免在遍历时修改字典
        var filtered = prefs.Where(kvp => string.IsNullOrEmpty(search) || kvp.Key.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        foreach (var kvp in filtered)
        {
            bool isModified = recentlyModified.Contains(kvp.Key) && (DateTime.Now - lastModifiedTime).TotalSeconds < 10;
            GUIStyle style = new GUIStyle(EditorStyles.label);
            if (isModified) style.normal.textColor = Color.green;

            GUILayout.BeginHorizontal();
            GUILayout.Label(kvp.Key, style, GUILayout.Width(240));
            string newVal = GUILayout.TextField(kvp.Value, GUILayout.Width(300));

            // 只有在值发生变化时才更新
            if (newVal != kvp.Value)
            {
                DetectTypeAndSave(kvp.Key, newVal);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                PlayerPrefs.DeleteKey(kvp.Key);
                PlayerPrefs.Save();
                Refresh();
                break;
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    void DetectTypeAndSave(string key, string newVal)
    {
        string oldVal = prefs[key];
        prefs[key] = newVal;

        if (int.TryParse(newVal, out int i))
            PlayerPrefs.SetInt(key, i);
        else if (float.TryParse(newVal, out float f))
            PlayerPrefs.SetFloat(key, f);
        else
            PlayerPrefs.SetString(key, newVal);

        PlayerPrefs.Save();
        recentlyModified.Add(key);
        lastModifiedTime = DateTime.Now;
    }

    void Refresh()
    {
        prefs.Clear();
        recentlyModified.Clear();

#if UNITY_EDITOR_WIN
        string regPath = @"Software\Unity\UnityEditor\" + Application.companyName + "\\" + Application.productName;
        var keys = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regPath);
        if (keys != null)
        {
            foreach (var name in keys.GetValueNames())
            {
                string cleanKey = CleanKeyName(name);
                object val = keys.GetValue(name);
                string strVal = FormatValueFromRegistry(cleanKey, val);
                prefs[cleanKey] = strVal;
            }
        }
#elif UNITY_EDITOR_OSX
        prefs["提示"] = "macOS 不支持直接读取 PlayerPrefs 值，请使用其他存储方式。";
#endif
    }

    string CleanKeyName(string rawKey)
    {
        int idx = rawKey.LastIndexOf("_h");
        if (idx > 0)
        {
            return rawKey.Substring(0, idx);
        }
        return rawKey;
    }

    string FormatValueFromRegistry(string key, object val)
    {
        if (val is byte[] bytes)
        {
            // Float 值：Unity 通常是 4 字节 float（如 float x = 3.14f）
            if (bytes.Length == 4)
            {
                float f = BitConverter.ToSingle(bytes, 0);
                if (!float.IsNaN(f) && !float.IsInfinity(f))
                    return f.ToString("F3");
            }

            // UTF-8 字符串值
            try
            {
                string str = System.Text.Encoding.UTF8.GetString(bytes);
                if (!string.IsNullOrWhiteSpace(str)) return str;
            }
            catch { }

            // fallback 显示 HEX
            return BitConverter.ToString(bytes);
        }

        // int、string 等原始类型
        return val?.ToString() ?? "";
    }
}
