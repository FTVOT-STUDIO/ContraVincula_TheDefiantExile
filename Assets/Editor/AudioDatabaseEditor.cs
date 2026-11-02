using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AudioDatabase 인스펙터에 enum 생성 버튼을 추가합니다.
/// </summary>
[CustomEditor(typeof(AudioDatabase))]
public class AudioDatabaseEditor : Editor
{
    // 기존 enum 파일 경로로 수정하십시오 (세 enum이 한 파일에 있으면 같은 경로를 넣으면 됩니다)
    private const string BgmPath = "Assets/Scripts/Audio/AudioType.cs";
    private const string SfxPath = "Assets/Scripts/Audio/AudioType.cs";
    private const string EnvPath = "Assets/Scripts/Audio/AudioType.cs";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);

        // 플레이 중이거나 컴파일 중에는 비활성화
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || EditorApplication.isCompiling))
        {
            if (GUILayout.Button("Generate Data", GUILayout.Height(30)))
            {
                Generate((AudioDatabase)target);
            }
        }
    }

    private static void Generate(AudioDatabase db)
    {
        // 경로별로 enum 코드를 모음 (같은 경로면 한 파일에 함께 작성)
        var files = new Dictionary<string, StringBuilder>();
        if (!AppendEnum(files, BgmPath, "BgmType", db.BgmDatas)) return;
        if (!AppendEnum(files, SfxPath, "SfxType", db.SfxDatas)) return;
        if (!AppendEnum(files, EnvPath, "EnvType", db.EnvDatas)) return;

        // 새 필드(name, id) 형식으로 에셋 저장
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        foreach (var pair in files)
        {
            string text = "// 자동 생성 파일 - 직접 수정하지 마십시오. (AudioDatabase 인스펙터에서 생성)\n\n" + pair.Value;
            Directory.CreateDirectory(Path.GetDirectoryName(pair.Key));
            File.WriteAllText(pair.Key, text, new UTF8Encoding(false));
        }

        AssetDatabase.Refresh();    // 리컴파일 실행
        Debug.Log("[AudioDatabase] 오디오 Enum 생성 완료");
    }

    // 리스트 하나를 enum 코드로 변환
    private static bool AppendEnum<T>(Dictionary<string, StringBuilder> files, string path, string enumName, IReadOnlyList<T> list) where T : AudioEntry
    {
        var sb = new StringBuilder();
        var names = new HashSet<string>();
        var ids = new HashSet<int>();

        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");

        // 0번을 쓰는 항목이 없으면 기본값용 None 추가
        bool zeroUsed = false;
        foreach (T e in list)
        {
            if (e.id == 0) zeroUsed = true;
        }
        if (!zeroUsed)
        {
            sb.AppendLine("    None = 0,");
            names.Add("None");
        }

        foreach (T e in list)
        {
            string n = Sanitize(e.name);
            if (string.IsNullOrEmpty(n) || !names.Add(n) || !ids.Add(e.id))
            {
                Debug.LogError($"[AudioDatabase] {enumName}: 이름이 비었거나 중복입니다 → '{e.name}' (id {e.id})");
                return false;
            }
            sb.AppendLine($"    {n} = {e.id},");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        if (!files.TryGetValue(path, out StringBuilder fileSb))
        {
            fileSb = new StringBuilder();
            files[path] = fileSb;
        }
        fileSb.Append(sb);
        return true;
    }

    // enum 이름으로 쓸 수 없는 문자 정리 (공백, - 는 _ 로 변환)
    private static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var sb = new StringBuilder();
        foreach (char c in s.Trim())
        {
            if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
            else if (c == ' ' || c == '-') sb.Append('_');
        }
        if (sb.Length == 0) return null;
        if (char.IsDigit(sb[0])) sb.Insert(0, '_');
        return sb.ToString();
    }
}