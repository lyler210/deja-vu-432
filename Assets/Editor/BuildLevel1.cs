using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor script — populates Level1.unity with all geometry, spikes, tower,
/// channel, and goal. Run it from the Unity menu: Tools → Build Level 1.
///
/// What stays untouched in your scene:
///   - Main Camera (with CameraFollow)
///   - Player + GroundCheck
///   - Managers (GameManager / RewindManager / LevelManager / HUD)
///   - SpawnPoint  (we move it, but don't delete)
///   - DeathPit    (we move it, but don't delete)
///
/// What this script rebuilds every run (deleted-then-recreated under a
/// "LevelGeometry" parent):
///   - Walls, floor, spikes, tower steps, channel, goal trigger + floor.
///
/// Also: creates the DeathEffect prefab if it doesn't exist, wires it to
/// PlayerHealth, and bumps jumpForce on PlayerController.
/// </summary>
public static class BuildLevel1
{
    const string ScenePath              = "Assets/Scenes/Level1.unity";
    const string GeoParentName          = "LevelGeometry";
    const string DeathPrefabPath        = "Assets/Prefabs/DeathEffect.prefab";
    const string SaveMarkerPrefabPath   = "Assets/Prefabs/SaveMarker.prefab";
    const string NoFrictionMaterialPath = "Assets/Prefabs/NoFriction.physicsMaterial2D";

    // ---- Layout constants (tweak these to change the level) -------------
    static readonly Vector2 SpawnPos       = new Vector2(-13f, -1f);
    static readonly Vector2 FloorCenter    = new Vector2(-1.5f, -3f);
    static readonly Vector2 FloorScale     = new Vector2(27f, 1f);
    static readonly Vector2 LeftWallPos    = new Vector2(-15.5f, 4f);
    static readonly Vector2 RightWallPos   = new Vector2(20.5f, 4f);
    static readonly Vector2 WallScale      = new Vector2(1f, 24f);
    static readonly float   PlayerJump     = 25f;
    static readonly float   PlayerFallMult = 0.4f; // <1 = floatier fall (lower = slower)
    static readonly float   PlayerLowJump  = 1f;   // disable variable-jump-height extra gravity
    static readonly float   CamSmoothTime  = 0.05f; // SmoothDamp time-to-target; lower = tighter follow
    static readonly float   RespawnDelay   = 1.4f; // +1s longer so death lingers
    static readonly Vector2 PitDepthPos    = new Vector2(0f, -13f);
    static readonly Vector2 PitDepthScale  = new Vector2(40f, 1f);
    static readonly Color   DeathBarColor  = new Color(0.72f, 0.10f, 0.10f);

    // Colors (tweak to taste)
    static readonly Color GroundColor   = new Color(0.32f, 0.68f, 0.32f);
    static readonly Color WallColor     = new Color(0.42f, 0.32f, 0.22f);
    static readonly Color TowerColor    = new Color(0.55f, 0.42f, 0.28f);
    static readonly Color SpikeColor    = new Color(0.92f, 0.20f, 0.20f);
    static readonly Color GoalFloorColor = new Color(0.45f, 0.65f, 0.90f);
    static readonly Color GoalFlagColor  = new Color(1.00f, 0.85f, 0.20f);

    static Sprite cachedSquare;

    // Cached NoFriction material — assigned in Build(), used by MakeWall() so every
    // wall/floor/step collider is frictionless. Without this, pressing into a wall
    // creates enough friction to hold the player up against gravity (wall-stickiness).
    static PhysicsMaterial2D cachedNoFriction;

    [MenuItem("Tools/Build Level 1")]
    public static void Build()
    {
        // --- open the scene ---
        var scene = EditorSceneManager.OpenScene(ScenePath);

        // --- ensure the effect prefabs exist ---
        var deathPrefab      = EnsurePrefab<DeathEffect>(DeathPrefabPath, "DeathEffect");
        var saveMarkerPrefab = EnsurePrefab<SaveMarker>(SaveMarkerPrefabPath, "SaveMarker");
        var noFrictionMat    = EnsureNoFrictionMaterial();
        cachedNoFriction     = noFrictionMat; // so MakeWall can apply it to every wall collider

        // --- wipe old geometry & known orphans from the manual build ---
        WipeOldGeometry();

        var geo = new GameObject(GeoParentName);

        // --- walls ---
        MakeWall("LeftWall",  LeftWallPos, WallScale,  WallColor, geo);
        MakeWall("RightWall", RightWallPos, WallScale, WallColor, geo);

        // --- main floor: split into two segments with a 5-unit-wide death gap
        //     in the middle, where the 3rd spike pit used to be.
        // Left floor:  X = -15  .. 1.5   (16.5 wide, center X=-6.75)
        // Gap:         X =   1.5.. 6.5   (5 wide — falls into the void)
        // Right floor: X =   6.5.. 12    (5.5 wide, center X=9.25), tower sits on this.
        MakeWall("FloorLeft",  new Vector2(-6.75f, -3f), new Vector2(16.5f, 1f), GroundColor, geo);
        MakeWall("FloorRight", new Vector2( 9.25f, -3f), new Vector2( 5.5f, 1f), GroundColor, geo);

        // --- two pairs of floor spikes (the 3rd was replaced by the void gap).
        //     Spikes within each pair are 1 unit apart — the original spacing.
        int idx = 1;
        foreach (var leftX in new[] { -7f, -2f })
        {
            MakeFloorSpike($"Spike{idx}a", new Vector2(leftX,        -2f), geo);
            MakeFloorSpike($"Spike{idx}b", new Vector2(leftX + 1f,   -2f), geo);
            idx++;
        }

        // --- 6-step tower (each step 1 unit wider+right, 2 units taller, stacks straight up) ---
        // Step centers go (8,-1.5) → (9,0.5) → (10,2.5) → (11,4.5) → (12,6.5) → (13,8.5)
        // Top of step 6 at Y=9.5. Step 6 right edge (X=13.5) touches channel left wall edge (X=13.5).
        for (int i = 0; i < 6; i++)
        {
            MakeWall($"TowerStep{i + 1}",
                     new Vector2(8f + i, -1.5f + 2f * i),
                     new Vector2(1f, 2f),
                     TowerColor, geo);
        }

        // --- TAPERED spike channel ---
        // Each row is a 1-unit-tall wall segment that steps inward toward the goal.
        // Top of channel = 3 units wide (matches before), bottom = 1.5 units wide.
        // Spikes sit on the inner face of each wall segment.
        // Row 0 is the entry (no spike); the bottom row is the landing (no spike).
        const int   channelRows  = 11;
        const float channelTopY  = 7f;
        const float channelCtrX  = 16f;
        const float topGap       = 3.5f;
        const float bottomGap    = 2f;

        for (int i = 0; i < channelRows; i++)
        {
            float y    = channelTopY - i;
            float t    = i / (float)(channelRows - 1);   // 0 at top, 1 at bottom
            float gap  = Mathf.Lerp(topGap, bottomGap, t);
            float halfGap = gap * 0.5f;

            // Wall segment centers (each wall is 1 wide; the wall's inner edge is at center ± 0.5).
            float leftWallX  = channelCtrX - halfGap - 0.5f;
            float rightWallX = channelCtrX + halfGap + 0.5f;

            MakeWall($"ChannelL_{i:00}", new Vector2(leftWallX,  y), new Vector2(1f, 1f), WallColor, geo);
            MakeWall($"ChannelR_{i:00}", new Vector2(rightWallX, y), new Vector2(1f, 1f), WallColor, geo);

            // Spikes on inner face — skip first row (entry) and last row (landing clearance).
            if (i > 0 && i < channelRows - 1)
            {
                MakeChannelSpike($"LCS_{i:00}", new Vector2(leftWallX  + 0.5f, y), facingRight: true,  geo);
                MakeChannelSpike($"RCS_{i:00}", new Vector2(rightWallX - 0.5f, y), facingRight: false, geo);
            }
        }

        // --- goal area at bottom of channel ---
        // Goal floor matches the channel exit gap (2 units).
        MakeWall("GoalFloor", new Vector2(16f, -4.5f), new Vector2(2f, 1f), GoalFloorColor, geo);
        MakeGoal("Goal",      new Vector2(16f, -3.5f),                                     geo);

        // --- reposition SpawnPoint + DeathPit ---
        var spawn = GameObject.Find("SpawnPoint");
        if (spawn != null) spawn.transform.position = SpawnPos;

        var pit = GameObject.Find("DeathPit");
        if (pit != null)
        {
            // Position 4 units below the floor so the player can see the
            // barrier rising as they fall through the gap.
            pit.transform.position = PitDepthPos;
            pit.transform.localScale = new Vector3(PitDepthScale.x, PitDepthScale.y, 1f);

            // Drive the collider purely from transform.scale, so the visual
            // sprite (also scaled) lines up exactly with the killbox.
            var col = pit.GetComponent<BoxCollider2D>();
            if (col != null) col.size = new Vector2(1f, 1f);

            // Make the death barrier visible — a dark red strip across the bottom.
            var sr = pit.GetComponent<SpriteRenderer>();
            if (sr == null) sr = pit.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.color = DeathBarColor;
            sr.sortingOrder = -2; // render behind everything else
        }

        // --- wire DeathEffect prefab + tuning onto the Player ---
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health != null) health.deathEffectPrefab = deathPrefab;

            var ctrl = player.GetComponent<PlayerController>();
            if (ctrl != null)
            {
                ctrl.jumpForce              = PlayerJump;
                ctrl.fallGravityMultiplier  = PlayerFallMult;
                ctrl.lowJumpGravityMultiplier = PlayerLowJump;
            }

            // Zero-friction material so the player doesn't stick to walls when
            // holding a direction against them.
            var playerCol = player.GetComponent<BoxCollider2D>();
            if (playerCol != null && noFrictionMat != null)
                playerCol.sharedMaterial = noFrictionMat;

            // --- walking animation: slice Idle + Run sprites and wire them up ---
            var idleFrames = EnsureSlicedSprites(
                "Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Idle (32x32).png");
            var runFrames  = EnsureSlicedSprites(
                "Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Run (32x32).png");

            if (idleFrames != null && idleFrames.Length > 0 &&
                runFrames  != null && runFrames.Length  > 0)
            {
                var anim = player.GetComponent<PlayerAnimator>();
                if (anim == null) anim = player.AddComponent<PlayerAnimator>();
                anim.idleFrames = idleFrames;
                anim.runFrames  = runFrames;

                // Make sure the SpriteRenderer is showing a valid frame.
                var sr = player.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sprite = idleFrames[0];

                Debug.Log($"[BuildLevel1] PlayerAnimator wired: idle={idleFrames.Length} run={runFrames.Length}");
            }
            else
            {
                Debug.LogWarning("[BuildLevel1] Could not load idle/run sprites — walking animation skipped.");
            }
        }

        // --- wire SaveMarker prefab onto RewindManager ---
        var rewind = Object.FindFirstObjectByType<RewindManager>();
        if (rewind != null) rewind.saveMarkerPrefab = saveMarkerPrefab;

        // --- bump respawn delay on LevelManager ---
        var levelMgr = Object.FindFirstObjectByType<LevelManager>();
        if (levelMgr != null) levelMgr.respawnDelay = RespawnDelay;

        // --- tighten the camera follow so it keeps up with fast vertical moves ---
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            var cf = mainCam.GetComponent<CameraFollow>();
            if (cf != null) cf.smoothTime = CamSmoothTime;
        }

        // --- save the scene ---
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[BuildLevel1] Done. Geometry children: {geo.transform.childCount}");
    }

    // ===================================================================
    //   helpers
    // ===================================================================

    static void WipeOldGeometry()
    {
        // Remove the LevelGeometry parent if it exists (children go with it).
        var old = GameObject.Find(GeoParentName);
        if (old != null) Object.DestroyImmediate(old);

        // Also remove any leftover top-level objects from the manual build.
        string[] strays =
        {
            "Floor", "Floor (1)", "Platform1", "Platform2", "Platform3",
            "Goal", "GoalFloor", "LeftWall", "RightWall",
        };
        foreach (var n in strays)
        {
            var go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    /// <summary>
    /// Ensure a sprite sheet PNG is set up as Multiple-mode + 32 PPU + Point filter,
    /// then grid-sliced into 32×32 cells. Returns the slices sorted in playback order.
    /// Safe to call repeatedly — only re-imports when settings need to change.
    /// </summary>
    static Sprite[] EnsureSlicedSprites(string assetPath, int cellSize = 32)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[BuildLevel1] No TextureImporter for {assetPath}");
            return null;
        }

        bool dirty = false;
        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            dirty = true;
        }
        if (Mathf.Abs(importer.spritePixelsPerUnit - 32f) > 0.5f)
        {
            importer.spritePixelsPerUnit = 32f;
            dirty = true;
        }
        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            dirty = true;
        }

        // If the spritesheet has 0 or 1 entries, grid-slice into cellSize×cellSize tiles.
        if (importer.spritesheet == null || importer.spritesheet.Length <= 1)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                Debug.LogWarning($"[BuildLevel1] Couldn't load texture {assetPath}");
                return null;
            }

            int cols = tex.width  / cellSize;
            int rows = tex.height / cellSize;
            var meta = new System.Collections.Generic.List<SpriteMetaData>();
            int idx = 0;
            // Top-down, left-right (so animation frame index 0 is the top-left cell).
            for (int row = rows - 1; row >= 0; row--)
            {
                for (int col = 0; col < cols; col++)
                {
                    meta.Add(new SpriteMetaData
                    {
                        name      = $"slice_{idx++:00}",
                        rect      = new Rect(col * cellSize, row * cellSize, cellSize, cellSize),
                        pivot     = new Vector2(0.5f, 0.5f),
                        alignment = (int)SpriteAlignment.Center,
                    });
                }
            }
            importer.spritesheet = meta.ToArray();
            dirty = true;
        }

        if (dirty)
        {
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        // Load every sub-asset that is a Sprite and sort by trailing number.
        var assets  = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var sprites = new System.Collections.Generic.List<Sprite>();
        foreach (var a in assets)
            if (a is Sprite sp) sprites.Add(sp);

        sprites.Sort((a, b) =>
            ExtractTrailingNumber(a.name).CompareTo(ExtractTrailingNumber(b.name)));
        return sprites.ToArray();
    }

    /// <summary>Pulls the trailing integer off a sprite slice name, e.g. "slice_07" → 7.</summary>
    static int ExtractTrailingNumber(string name)
    {
        int i = name.Length - 1;
        while (i >= 0 && char.IsDigit(name[i])) i--;
        if (i < name.Length - 1 && int.TryParse(name.Substring(i + 1), out var n))
            return n;
        return 0;
    }

    /// <summary>
    /// Load (or create) the zero-friction PhysicsMaterial2D that keeps the
    /// player from sticking to walls when pressing into them.
    /// </summary>
    static PhysicsMaterial2D EnsureNoFrictionMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionMaterialPath);
        if (mat != null) return mat;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        mat = new PhysicsMaterial2D("NoFriction")
        {
            friction   = 0f,
            bounciness = 0f,
        };
        AssetDatabase.CreateAsset(mat, NoFrictionMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    /// <summary>Load (or create) a prefab at the given path with component T attached.</summary>
    static GameObject EnsurePrefab<T>(string path, string objectName) where T : Component
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var tmp = new GameObject(objectName);
        tmp.AddComponent<T>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(tmp, path);
        Object.DestroyImmediate(tmp);
        return prefab;
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
        // Apply zero-friction material so the player can't stick to the side of
        // this wall by holding a direction against it.
        if (cachedNoFriction != null) box.sharedMaterial = cachedNoFriction;
        return go;
    }

    static GameObject MakeFloorSpike(string name, Vector2 pos, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = SpikeColor;
        // 45° rotation gives a diamond — reads as a spike from a distance.
        go.transform.rotation = Quaternion.Euler(0, 0, 45f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        // Shrink collider so hitbox sits INSIDE the visible diamond (the diamond's
        // tips extend further than a 1-unit AABB; a 0.65 box matches what you see).
        col.size = new Vector2(0.65f, 0.65f);
        go.AddComponent<Trap>();
        return go;
    }

    static GameObject MakeChannelSpike(string name, Vector2 pos, bool facingRight, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
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
        goal.nextSceneName = "LevelSelect"; // back to level select on win
        return go;
    }
}
