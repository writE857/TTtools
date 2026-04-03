using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CodeGenerator : EditorWindow
{
    private static string folderPath = "Assets/Ade_Framework/Scriptes/DataGenerated/";

    public static void GenerateStaticClassFile(string className, List<string> propertyNames)
    {
        var indent = "    ";
        var code = "using System;\n\n";
        code += $"public static class {className}\n{{\n";

        // 生成静态字段
        foreach (var prop in propertyNames)
        {
            code += indent + $"public static string {prop} = \"{prop}\";\n";
        }

        code += "}\n";

        // 确保目录存在
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, className + ".cs");
        File.WriteAllText(filePath, code);
        Debug.Log("静态字段数据类已生成，路径：" + filePath);

        AssetDatabase.Refresh();
    }
}
