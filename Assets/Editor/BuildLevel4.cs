using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor script — populates Level4.unity ("The Long Mile") with geometry.
///
/// Level 4 is a step harder than Level 1: more spike pairs on the floor, a
/// wider death gap, a small mid-platform with a single tricky spike, and a
/// taller 6-step climb to the goal.
///
/// Same contract as Tools → Build Level 1: keeps Main Camera, Player,
/// SpawnPoint, DeathPit, and Managers untouched, wipes and rebuilds the
/// "LevelGeometry" parent.
///
/// Run from menu: Tools → Build Level 4.
/// </summary>
public static class BuildLevel4
{
    const string ScenePath              = "Assets/Scenes/Level4.unity";
    const string GeoParentName          = "LevelGeometry";
    const string DeathPrefabPath        = "Assets/Prefabs/DeathEffect.prefab";
    const string SaveMarkerPrefabPath   = "Assets/Prefabs/SaveMarker.prefab";
    const string NoFrictionMaterialPath = "Assets/Prefabs/NoFriction.physicsMaterial2D";
    const int    LevelNumber            = 4;
    const string NextSceneName          = "LevelSelect";

    // ---- Layout constants -----------------------------------------------
    static readonly Vector2 SpawnPos      = new Vector2(-18f, -1f);
    static readonly Vector2 LeftWallPos   = new Vector2(-20.5f, 4f);
    static readonly Vector2 RightWallPos  = new Vector2( 22.5f, 4f);
    static readonly Vector2 WallScale     = new Vector2(1f, 24f);
    static readonly Vector2 PitDepthPos   = new Vector2(0f, -13f);
    static readonly Vector2 PitDepthScale = new Vector2(50f, 1f);
    static readonly Color   DeathBarColor = new Color(0.72f, 0.10f, 0.10f);

    // Colors
    static readonly Color GroundColor    = new Color(0.32f, 0.68f, 0.32f);
    static readonly Color WallColor      = new Color(0.42f, 0.32f, 0.22f);
    static readonly Color TowerColor     = new Color(0.55f, 0.42f, 0.28f);
    static readonly Color SpikeColor     = new Color(0.92f, 0.20f, 0.20f);
    static readonly Color GoalFloorColor = new Color(0.45f, 0.65f, 0.90f);
    static readonly Color GoalFlagColor  = new Color(1.00f, 0.85f, 0.20f);

    static Sprite cachedSquare;
    static PhysicsMaterial2D cachedNoFriction;

    [MenuItem("Tools/Build Level 4")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);

        var deathPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>(DeathPrefabPath);
        var saveMarkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SaveMarkerPrefabPath);
        cachedNoFriction     = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionMaterialPath);

        WipeOldGeometry();
        var geo = new GameObject(GeoParentName);

        // Walls
        MakeWall("LeftWall",  LeftWallPos,  WallScale, WallColor, geo);
        MakeWall("RightWall", RightWallPos, WallScale, WallColor, geo);

        // Floor 1: the long spike floor — X=-20 to X=2, top at Y=-2.5
        MakeWall("FloorLeft", new Vector2(-9f, -3f), new Vector2(22f, 1f), GroundColor, geo);

        // 4 spike pairs on Floor 1 (Level 1 has 2 — this is "a bit harder").
        int idx = 1;
        foreach (var leftX in new[] { -15f, -11f, -7f, -3f })
        {
            MakeFloorSpike($"Spike{idx}a", new Vector2(leftX,      -2f), geo);
            MakeFloorSpike($"Spike{idx}b", new Vector2(leftX + 1f, -2f), geo);
            idx++;
        }

        // 6-unit death gap from X=2 to X=8 (Level 1's is 5 wide).
        // Floor 2: small platform after the gap, with a single tricky spike.
        MakeWall("FloorRight", new Vector2(10f, -3f), new Vector2(4f, 1f), GroundColor, geo);
        MakeFloorSpike("Spike5", new Vector2(10f, -2f), geo);

        // 6-step staircase climbing up to the goal.
        // Each step: 1 unit wide, 2 tall, stepping right and up.
        for (int i = 0; i < 6; i++)
        {
            MakeWall($"TowerStep{i + 1}",
                     new Vector2(14f + i, -0.5f + 2f * i),  // centers go (14,-0.5) → (19,9.5)
                     new Vector2(1f, 2f),
                     TowerColor, geo);
        }

        // Top platform — wider so the player can land on it.
        MakeWall("TopPlatform", new Vector2(21f, 9.5f), new Vector2(3f, 1f), GoalFloorColor, geo);
        MakeGoal("Goal", new Vector2(21f, 11f), geo);

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

        // Wire DeathEffect + SaveMarker prefabs onto the existing Player/RewindManager
        // (carried over from the cloned Level 1 scene, but rewire defensively).
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
        Debug.Log($"[BuildLevel4] Done. Geometry children: {geo.transform.childCount}");
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
