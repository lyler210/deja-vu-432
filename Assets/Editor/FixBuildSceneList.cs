using UnityEngine;
using UnityEditor;

/// <summary>
/// One-shot editor utility — populates BOTH the global EditorBuildSettings
/// scene list and the Web Build Profile's scene list with all 8 scenes, so
/// SceneManager.LoadScene works for any of them in the editor and in builds.
///
/// Uses only stable Unity editor APIs (EditorBuildSettings.scenes and
/// SerializedObject) so it compiles across Unity 6 patch versions.
///
/// Idempotent — re-running just resets the lists to the canonical 8 scenes.
///
/// Run from menu: Tools → Fix Build Scene List.
/// </summary>
public static class FixBuildSceneList
{
    const string WebProfilePath = "Assets/Settings/Build Profiles/Web.asset";

    // Canonical scene order. MainMenu is first so builds start there.
    static readonly string[] ScenePaths =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/LevelSelect.unity",
        "Assets/Scenes/Settings.unity",
        "Assets/Scenes/Level1.unity",
        "Assets/Scenes/Level2.unity",
        "Assets/Scenes/Level3.unity",
        "Assets/Scenes/Level4.unity",
        "Assets/Scenes/Level5.unity",
    };

    [MenuItem("Tools/Fix Build Scene List")]
    public static void Apply()
    {
        // Verify every scene exists before changing anything.
        foreach (var p in ScenePaths)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(p)))
            {
                Debug.LogError($"[FixBuildSceneList] Scene not found in AssetDatabase: {p}");
                return;
            }
        }

        // Build the scene list.
        var scenes = new EditorBuildSettingsScene[ScenePaths.Length];
        for (int i = 0; i < ScenePaths.Length; i++)
            scenes[i] = new EditorBuildSettingsScene(ScenePaths[i], true);

        // 1) Global list — what builds use unless a profile overrides it.
        EditorBuildSettings.scenes = scenes;
        Debug.Log($"[FixBuildSceneList] Wrote {scenes.Length} scenes to global EditorBuildSettings.");

        // 2) Web build profile — populate its m_Scenes via SerializedObject.
        //    Even if override is off, Unity's Build Profiles UI displays this
        //    list, so populating it makes the editor UI match reality.
        var profileAsset = AssetDatabase.LoadAssetAtPath<Object>(WebProfilePath);
        if (profileAsset == null)
        {
            Debug.LogWarning($"[FixBuildSceneList] Web profile not found at {WebProfilePath} — skipping profile-specific list.");
        }
        else
        {
            var so = new SerializedObject(profileAsset);
            var scenesProp = so.FindProperty("m_Scenes");
            if (scenesProp == null)
            {
                Debug.LogWarning("[FixBuildSceneList] Web profile has no m_Scenes property — Unity may have renamed it.");
            }
            else
            {
                scenesProp.arraySize = scenes.Length;
                for (int i = 0; i < scenes.Length; i++)
                {
                    var element     = scenesProp.GetArrayElementAtIndex(i);
                    var enabledProp = element.FindPropertyRelative("enabled");
                    var pathProp    = element.FindPropertyRelative("path");
                    var guidProp    = element.FindPropertyRelative("guid");

                    if (enabledProp != null) enabledProp.boolValue = true;
                    if (pathProp != null)    pathProp.stringValue  = scenes[i].path;

                    // 'guid' on EditorBuildSettingsScene is the GUID struct,
                    // not a string. SerializedObject exposes its 4 uint fields.
                    if (guidProp != null)
                    {
                        var guidStr = AssetDatabase.AssetPathToGUID(scenes[i].path);
                        SetGuidIntegers(guidProp, guidStr);
                    }
                }

                // Some Unity versions also expose m_OverrideGlobalSceneList.
                var overrideProp = so.FindProperty("m_OverrideGlobalSceneList");
                if (overrideProp != null) overrideProp.boolValue = true;

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profileAsset);
                AssetDatabase.SaveAssetIfDirty(profileAsset);
                Debug.Log($"[FixBuildSceneList] Wrote {scenes.Length} scenes to Web build profile.");
            }
        }

        foreach (var s in scenes) Debug.Log($"  • {s.path}");
        AssetDatabase.SaveAssets();
    }

    /// <summary>Populate the 4 uint fields of a Unity GUID from a 32-char hex string.</summary>
    static void SetGuidIntegers(SerializedProperty guidProp, string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length != 32) return;
        var bytes = new byte[16];
        for (int i = 0; i < 16; i++)
            bytes[i] = byte.Parse(hex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);

        for (int wordIndex = 0; wordIndex < 4; wordIndex++)
        {
            var p = guidProp.FindPropertyRelative($"m_Value{wordIndex}");
            if (p == null) continue;
            uint v = (uint)bytes[wordIndex * 4]
                   | ((uint)bytes[wordIndex * 4 + 1] << 8)
                   | ((uint)bytes[wordIndex * 4 + 2] << 16)
                   | ((uint)bytes[wordIndex * 4 + 3] << 24);
            p.longValue = v;
        }
    }
}
