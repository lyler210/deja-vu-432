using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The end-of-level flag. When the player enters the trigger, the next scene loads.
/// Leave nextSceneName blank to just log a "You win!" message (useful while building Level 1).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelGoal : MonoBehaviour
{
    [Tooltip("Scene name to load on touch. Must be added to File > Build Settings.")]
    public string nextSceneName = "";

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("[LevelGoal] Level complete! (No next scene set.)");
            return;
        }
        SceneManager.LoadScene(nextSceneName);
    }
}
