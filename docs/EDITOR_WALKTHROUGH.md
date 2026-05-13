# Editor Walkthrough — Building Level 1

This is the click-by-click guide for assembling a playable Level 1 in the Unity Editor. We can't author Unity scenes from outside the Editor reliably, so you'll do this part by hand the first time. Subsequent levels are mostly copy-paste.

Estimated time: **45–60 minutes** the first time. You'll get faster.

---

## 0. Orient yourself

Open the project in Unity. You should see five main windows:
- **Hierarchy** (left) — list of every GameObject in the current scene.
- **Scene** (center, top) — the editable 2D world.
- **Game** (center, top, tab next to Scene) — what the player sees.
- **Inspector** (right) — properties of whatever is selected.
- **Project** (bottom) — files in `Assets/`.

If any are missing: **Window → Layouts → Default**.

---

## 1. Set up the project for 2D-platformer physics

> **Important first check — Input system:** the scripts I wrote use the legacy `Input.GetAxis`/`Input.GetKeyDown` API. In Unity 6's Universal 2D template, the Input System package is often installed and may disable the legacy input. Go to **Edit → Project Settings → Player → Other Settings → Configuration → Active Input Handling**. Set it to **Both** (or **Input Manager (Old)**). Unity will ask to restart — say yes. If you skip this, you'll see runtime errors like "You are trying to read Input using the UnityEngine.Input class".

1. **Edit → Project Settings → Tags and Layers**.
2. Under **Layers**, type a name into a blank slot:
   - User Layer 6: `Ground`
   - User Layer 7: `Player`
3. Under **Tags**, click the **+** and add a tag: `Player`.
4. Close Project Settings.

5. **Edit → Project Settings → Physics 2D**.
6. Scroll to **Layer Collision Matrix** at the bottom. Make sure `Player ↔ Ground` is checked (it is by default). Leave the rest as-is.
7. Set **Gravity Y** to `-30` for a snappier platformer feel (default is -9.81 which feels floaty).

---

## 2. Create the Level 1 scene

1. In the Project window, navigate to `Assets/Scenes`.
2. Right-click → **Create → Scene** → name it `Level1`.
3. Double-click `Level1.unity` to open it. You should see an empty scene with just a Main Camera (and a Global Light 2D if you used the Universal 2D template).
4. **File → Save** to commit the empty scene.

---

## 3. Build the Player

We'll build it as a GameObject first, then turn it into a prefab.

1. **Hierarchy → right-click → Create Empty** → rename to `Player`.
2. With `Player` selected, in the Inspector:
   - **Tag:** Player
   - **Layer:** Player
3. **Add Component → Sprite Renderer**.
4. In Project: `Assets/Pixel Adventure 1/Main Characters/Ninja Frog/Idle (32x32).png`. Click it. In Inspector:
   - **Sprite Mode:** Multiple
   - **Pixels Per Unit:** 32
   - **Filter Mode:** Point (no filter)
   - Click **Sprite Editor** → **Slice** → Type: Grid By Cell Size, Pixel Size 32×32 → **Slice** → **Apply**.
   - Drag the *first* sliced frame onto your Player's Sprite Renderer "Sprite" slot.
5. **Add Component → Rigidbody 2D**.
   - **Gravity Scale:** 3
   - **Collision Detection:** Continuous
   - **Constraints → Freeze Rotation Z:** ✅
6. **Add Component → Box Collider 2D** (or Capsule Collider 2D — capsule slides over edges better).
   - Click **Edit Collider** and shrink to fit the visible sprite (a bit narrower than the sprite is usually right).
7. **Add Component → Player Controller** (my script — type "Player Controller" in the search box).
8. **Add Component → Player Health** (my script).
9. Right-click `Player` in Hierarchy → **Create Empty** child → rename to `GroundCheck`. Position it just below the player's feet (e.g., local Y = -0.5).
10. Back on the Player, in the **PlayerController** component:
    - **Ground Check:** drag the GroundCheck child here.
    - **Ground Layer:** check `Ground` (uncheck Default).

**Save the scene** (Cmd+S / Ctrl+S).

---

## 4. Build the Ground

Quick-and-dirty version (no tilemap yet):

1. **Hierarchy → right-click → 2D Object → Sprites → Square**. Rename to `Floor`.
2. **Layer:** Ground.
3. Scale it: `(30, 1, 1)` for a long floor.
4. Move it below the player (e.g., Y = -3).
5. **Add Component → Box Collider 2D**.

Make a few platforms by duplicating Floor (Cmd+D / Ctrl+D) and moving them. Stagger heights with gaps between them — small jumps for now.

> **Stretch:** when you're ready for tilemaps, **Window → 2D → Tile Palette** and use the Pixel Adventure terrain. For MVP, plain squares are fine.

---

## 5. Add a death-floor (so falling kills you)

1. **Hierarchy → Create Empty** → rename `DeathPit`.
2. **Add Component → Box Collider 2D** → **Is Trigger:** ✅
3. Scale Box Collider 2D Size to `(200, 1)`. Position it way below the level (Y = -15).
4. **Add Component → Kill Zone** (my script).

Now falling = death = respawn at level start.

---

## 6. Add a spike trap

1. In Project: `Assets/Pixel Adventure 1/Traps/Spikes/Idle.png`. Slice if needed (cell size 16×16), Pixels Per Unit 16, Filter Mode Point.
2. **Hierarchy → Create Empty** → rename `Spike`.
3. **Add Component → Sprite Renderer** → drag in the spike sprite.
4. **Add Component → Box Collider 2D** (auto-sized works).
5. **Add Component → Trap** (my script). Tick "Is Trigger" on the collider (the Trap script's `Reset()` auto-does this when added).
6. Place it on top of a platform near a jump.

Duplicate it for more.

---

## 7. Add the goal flag

1. **Hierarchy → Create Empty** → rename `Goal`.
2. **Add Component → Sprite Renderer** → pick any easy-to-see sprite (e.g., one of the items in Pixel Adventure 1 → Items → Checkpoints, or just leave default).
3. **Add Component → Box Collider 2D** → **Is Trigger** ✅
4. **Add Component → Level Goal** (my script).
5. Leave `nextSceneName` blank for now (we only have one level).

Place it at the far right of the level.

---

## 8. Set up the player spawn point

1. **Hierarchy → Create Empty** → rename `SpawnPoint`.
2. Move it to where you want the player to appear (start of level).
3. (Cosmetic — you can give it a gizmo icon in the Inspector by clicking the colored cube next to the GameObject name.)

---

## 9. The Managers GameObject

1. **Hierarchy → Create Empty** → rename `Managers`.
2. **Add Component → Game Manager** (only in this scene — it survives scene changes).
3. **Add Component → Rewind Manager**.
4. **Add Component → Level Manager**:
   - **Player:** drag the `Player` here.
   - **Spawn Point:** drag the `SpawnPoint` here.
5. **Add Component → HUD**.

---

## 10. Set up the camera

1. Select `Main Camera` in Hierarchy.
2. **Add Component → Camera Follow** (my script).
3. **Target:** drag the `Player`.
4. **Projection:** Orthographic (it should already be).
5. **Size:** 6 is a good default for 32-px-per-unit pixel art.

---

## 11. Hit play

1. **File → Save** the scene.
2. Press the **Play** button at the top of the Editor.
3. Click in the Game window so it receives input.

You should be able to:
- Move with **A/D**.
- Jump with **Space**.
- Press **S** to save a checkpoint (watch the HUD).
- Walk into a spike → respawn at start (HUD death count increments, checkpoint cleared).
- Save mid-level → walk a bit → press **R** → see a rewind animation snap you back.

If something doesn't work, check the **Console** for red errors and grep your way through. The scripts are heavily commented; read `RewindManager.cs` to understand the core loop.

---

## 12. WebGL build (publishing to Unity Play)

When Level 1 plays end-to-end:

1. **File → Build Settings**.
2. Click **Add Open Scenes** → adds Level1 to the list.
3. Select **WebGL** → click **Switch Platform** (one-time, ~5 minutes).
4. Click **Build** → choose an empty folder somewhere (NOT inside your project — outputs go in `/Users/kieranmoynihan/Documents/Claude/Projects/Dejavu432/Builds/` and that folder is git-ignored).
5. The build takes ~10–20 minutes the first time. Have a coffee.

6. When done: head to <https://play.unity.com/> → log in with the same Unity account → **Upload your project** → **Browse** to the build folder → upload as a ZIP (you'll need to zip the build folder first).
7. Set title, description, screenshot. Publish.

> **Common gotcha:** WebGL builds fail if you have `unsafe` C# code, missing references, or compile errors. If a WebGL build fails, run **Edit → Player → Resolution and Presentation** and confirm settings, and check **Console** for errors first.

---

## What's next?

Once Level 1 is playable and built to WebGL, see `ROADMAP.md` for what to tackle next (Level 2, polish, choose-your-own-adventure branching, etc.).
