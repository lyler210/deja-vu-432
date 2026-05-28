using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-shot editor utility — scans every .unity file in the project, finds every
/// <see cref="CameraFollow"/> component, sets its <c>smoothTime</c> to the value
/// in <see cref="TargetSmoothTime"/>, and saves the scene.
///
/// Use this when changing the camera-feel default in code (CameraFollow.cs) so
/// the change actually propagates to scenes that already have a Main Camera
/// configured. Scene-instance fields in Unity are serialized — changing the
/// C# default doesn't retroactively update existing instances.
///
/// Run it: Tools → Apply Camera Smoothing To All Scenes.
/// Idempotent — safe to re-run.
/// </summary>
public static class TuneCameras
{
    const float TargetSmoothTime = 0.08f;

    [MenuItem("Tools/Apply Camera Smoothing To All Scenes")]
    public static void Apply()
    {
        // Remember what scene was open so we can put the user back where they were.
        var originalScenePath = EditorSceneManager.GetActiveScene().path;

        // Prompt to save unsaved changes in the current scene before we start hopping around.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[TuneCameras] Cancelled — current scene had unsaved changes.");
            return;
        }

        var sceneGuids = AssetDatabase.FindAssets("t:Scene");
        int scenesUpdated = 0;
        int totalUpdated = 0;

        foreach (var guid in sceneGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            // Only touch project scenes, not anything from Packages/.
            if (!path.StartsWith("Assets/")) continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int inThisScene = 0;

            foreach (var cf in Object.FindObjectsByType<CameraFollow>(FindObjectsSortMode.None))
            {
                if (!Mathf.Approximately(cf.smoothTime, TargetSmoothTime))
                {
                    Undo.RecordObject(cf, "Apply Camera Smoothing");
                    cf.smoothTime = TargetSmoothTime;
                    EditorUtility.SetDirty(cf);
                    inThisScene++;
                }
            }

            if (inThisScene > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                scenesUpdated++;
                totalUpdated += inThisScene;
                Debug.Log($"[TuneCameras] {path}: updated {inThisScene} CameraFollow(s)");
            }
        }

        // Put the user back in the scene they started in.
        if (!string.IsNullOrEmpty(originalScenePath))
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

        Debug.Log($"[TuneCameras] Done — set smoothTime={TargetSmoothTime} on {totalUpdated} camera(s) across {scenesUpdated} scene(s).");
    }
}
