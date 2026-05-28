using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor script — populates Level5.unity ("The Vertical Inferno") with geometry.
///
/// Level 5 is the hardest: a narrow vertical column with 8 zigzag platforms
/// going up. The side walls are deliberately tight to the platforms so the
/// player can use the "slam-and-slide" technique (jump with full force, hit
/// the far wall, slide down along it onto the next platform). Lyle's Level 2
/// uses the same trick — full-height jumps that get stopped by the wall.
///
/// Floor spikes punish missed jumps that drop you all the way to the bottom.
/// Save placement is critical: lose your save and you lose your progress.
///
/// Run from menu: Tools → Build Level 5.
/// </summary>
public static class BuildLevel5
{
    const string ScenePath              = "Assets/Scenes/Level5.unity";
    const string GeoParentName          = "LevelGeometry";
    const string DeathPrefabPath        = "Assets/Prefabs/DeathEffect.prefab";
    const string SaveMarkerPrefabPath   = "Assets/Prefabs/SaveMarker.prefab";
    const string NoFrictionMaterialPath = "Assets/Prefabs/NoFriction.physicsMaterial2D";
    const int    LevelNumber            = 5;
    const string NextSceneName          = "LevelSelect";

    // ---- Layout constants ----
    static readonly Vector2 SpawnPos      = new Vector2(0f, -1f);
    static readonly Vector2 LeftWallPos   = new Vector2(-5.5f, 12f);
    static readonly Vector2 RightWallPos  = new Vector2( 5.5f, 12f);
    static readonly Vector2 WallScale     = new Vector2(1f, 50f);
    static readonly Vector2 PitDepthPos   = new Vector2(0f, -15f);
    static readonly Vector2 PitDepthScale = new Vector2(40f, 1f);
    static readonly Color   DeathBarColor = new Color(0.72f, 0.10f, 0.10f);

    // Zigzag platform layout — alternating sides at X = ±3.5, 2.5-unit-wide
    // platforms, 2.5-unit vertical spacing. Total 8 platforms.
    const float PlatformAbsX   = 3.5f;
    const float PlatformWidth  = 2.5f;
    const float PlatformHeight = 0.5f;
    const float PlatformBaseY  = 1.5f;
    const float PlatformDeltaY = 2.5f;
    const int   PlatformCount  = 8;

    // Colors
    static readonly Color GroundColor    = new Color(0.32f, 0.68f, 0.32f);
    static readonly Color WallColor      = new Color(0.42f, 0.32f, 0.22f);
    static readonly Color PlatformColor  = new Color(0.55f, 0.42f, 0.28f);
    static readonly Color SpikeColor     = new Color(0.92f, 0.20f, 0.20f);
    static readonly Color GoalFloorColor = new Color(0.45f, 0.65f, 0.90f);
    static readonly Color GoalFlagColor  = new Color(1.00f, 0.85f, 0.20f);

    static Sprite cachedSquare;
    static PhysicsMaterial2D cachedNoFriction;

    [MenuItem("Tools/Build Level 5")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);

        var deathPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>(DeathPrefabPath);
        var saveMarkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SaveMarkerPrefabPath);
        cachedNoFriction     = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionMaterialPath);

        WipeOldGeometry();
        var geo = new GameObject(GeoParentName);

        // Tall side walls — extend well above the top platform.
        MakeWall("LeftWall",  LeftWallPos,  WallScale, WallColor, geo);
        MakeWall("RightWall", RightWallPos, WallScale, WallColor, geo);

        // Bottom floor (full width between walls). Falling here means death.
        MakeWall("FloorBottom", new Vector2(0f, -3f), new Vector2(10f, 1f), GroundColor, geo);

        // Two spike pairs on the bottom floor — dense enough that fallers reliably die.
        foreach (var leftX in new[] { -3f, 1f })
        {
            MakeFloorSpike($"FloorSpike{leftX}a", new Vector2(leftX,      -2f), geo);
            MakeFloorSpike($"FloorSpike{leftX}b", new Vector2(leftX + 1f, -2f), geo);
        }

        // 8 zigzag platforms, alternating sides at X = ±3.5.
        for (int i = 0; i < PlatformCount; i++)
        {
            float x = (i % 2 == 0) ? -PlatformAbsX : PlatformAbsX;
            float y = PlatformBaseY + i * PlatformDeltaY;
            MakeWall($"Platform{i + 1}",
                     new Vector2(x, y),
                     new Vector2(PlatformWidth, PlatformHeight),
                     PlatformColor, geo);
        }

        // Top platform — wider, centered, gives the player a safe stage to land on
        // after the 8th zigzag and to reach the goal.
        float topPlatformY = PlatformBaseY + PlatformCount * PlatformDeltaY;  // 1.5 + 8*2.5 = 21.5
        MakeWall("TopPlatform", new Vector2(0f, topPlatformY), new Vector2(4f, 1f), GoalFloorColor, geo);

        // Goal sits on top of the top platform.
        MakeGoal("Goal", new Vector2(0f, topPlatformY + 1.5f), geo);

        // SpawnPoint + DeathPit
        var spawn = GameObject.Find("SpawnPoint");
        if (spawn != null) spawn.transform.position = SpawnPos;

        var pit = GameObject.Find("DeathPit");
        if (pit != null)
        {
            pit.transform.position = PitDepthPos;
            pit.transform.localScale = new Vector3(PitDepthScale.x, PitDepthScale.y, 1f);
            var col = pit.GetComponent<BoxCollider2D>();
            if (col != null) col.size = new Vector2(1f, 1f);
            var sr = pit.GetComponent<SpriteRenderer>();
            if (sr == null) sr = pit.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.color = DeathBarColor;
            sr.sortingOrder = -2;
        }

        // Defensive re-wiring of prefab refs (the scene was cloned from Level 1
        // so these should already point at the right assets, but resetting them
        // makes the script robust against future scene drift).
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health != null && deathPrefab != null) health.deathEffectPrefab = deathPrefab;

            var playerCol = player.GetComponent<BoxCollider2D>();
            if (playerCol != null && cachedNoFriction != null)
                playerCol.sharedMaterial = cachedNoFriction;
        }

        var rewind = Object.FindFirstObjectByType<RewindManager>();
        if (rewind != null && saveMarkerPrefab != null) rewind.saveMarkerPrefab = saveMarkerPrefab;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[BuildLevel5] Done. Geometry children: {geo.transform.childCount}");
    }

    // -------------------- helpers --------------------

    static void WipeOldGeometry()
    {
        var old = GameObject.Find(GeoParentName);
        if (old != null) Object.DestroyImmediate(old);
    }

    static Sprite GetSquareSprite()
    {
        if (cachedSquare != null) return cachedSquare;
        var tex = new Texture2D(2, 2);
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                tex.SetPixel(x, y, Color.white);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        cachedSquare = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2);
        return cachedSquare;
    }

    static GameObject MakeWall(string name, Vector2 pos, Vector2 scale, Color color, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        int ground = LayerMask.NameToLayer("Ground");
        if (ground >= 0) go.layer = ground;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = color;

        var box = go.AddComponent<BoxCollider2D>();
        if (cachedNoFriction != null) box.sharedMaterial = cachedNoFriction;
        return go;
    }

    static GameObject MakeFloorSpike(string name, Vector2 pos, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, 45f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = SpikeColor;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.65f, 0.65f);
        go.AddComponent<Trap>();
        return go;
    }

    static GameObject MakeGoal(string name, Vector2 pos, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.5f, 2f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = GoalFlagColor;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var goal = go.AddComponent<LevelGoal>();
        goal.nextSceneName    = NextSceneName;
        goal.thisLevelNumber  = LevelNumber;
        return go;
    }
}
