# Roadmap — Deja Vu 432

This is a suggested path from "I have scripts on disk" to "playable game on Unity Play." Adjust to your own pace.

## MVP definition (your Week-8 goal)

A single playable level where:
- ✅ Player moves & jumps
- ✅ Spikes/pits kill the player
- ✅ Death respawns at level start
- ✅ Pressing S saves a checkpoint
- ✅ Pressing R rewinds with a visual effect to the saved checkpoint
- ✅ Touching the goal flag ends the level (or loads Level 2)
- ✅ Built to WebGL and uploaded to Unity Play

Everything above is wired in the scripts I've delivered. The remaining work is **scene-building in the Unity Editor** + **WebGL build/publish**, both covered in `EDITOR_WALKTHROUGH.md`.

---

## Suggested weekly plan

### Week 1: setup + first jump
- Finish `SETUP_GUIDE.md` (Unity install, project create, scripts dropped in, GitHub hooked up).
- Get the player moving and jumping on a flat floor. No traps yet.
- Commit & push.

### Week 2: death loop
- Add a spike, a pit, and the kill zone at the bottom of the level.
- Verify dying respawns at the spawn point.
- HUD shows death count.

### Week 3: rewind mechanic
- Test save + rewind in an empty room. Tune `sampleInterval`, `playbackSpeed`, `maxHistorySeconds` until the animation feels good.
- Try the strategic loop: walk past a hazard, save, attempt the next hazard, rewind on near-miss.

### Week 4: build Level 1 properly
- Lay out an actual level with platforms, gaps, traps in interesting positions.
- Place the goal flag at the end.
- Iterate on difficulty (playtest with a friend who hasn't seen it).

### Week 5: Level 2 + scene transitions
- `File → Save As` Level1 → `Level2`. Modify the layout.
- On the `LevelGoal` in Level 1, set `Next Scene Name` to `Level2`.
- File → Build Settings → Add Open Scenes for both.

### Week 6: art pass
- Replace placeholder sprites with proper Pixel Adventure animations.
- Add a Tilemap for the ground (Window → 2D → Tile Palette).
- Add a parallax background (extra GameObjects at different camera depths).

### Week 7: polish
- Sound effects (jump, death, save, rewind whoosh).
- Music. Audio Mixer.
- Particle effect when checkpoint saves.
- Better death animation (sprite fade).

### Week 8: WebGL build + Unity Play upload
- Final WebGL build (see EDITOR_WALKTHROUGH §12).
- Test in browser.
- Upload to Unity Play. Set thumbnail, title, description.
- Ship it 🚀

---

## Stretch features (after MVP, in rough order of effort)

### Easy
- **Death count overlay** → already in HUD; just style it.
- **Multiple checkpoints**: change `RewindManager.savedCheckpoint` from `Vector3?` to a `List<Vector3>`, plus a key to cycle.
- **Limited rewinds per level**: counter in RewindManager that decrements on use.

### Medium
- **Visual rewind effect**: ghost trail (instantiate a faint sprite at each history sample during rewind).
- **Moving platforms**: simple Update() that lerps between two points.
- **Saw blade trap**: rotating child of an empty parent that animates along a path.
- **Choose-your-own-adventure branching**: two `LevelGoal` triggers in one room, each loading a different scene.

### Hard
- **Time-rewind for the whole world**: record positions of moving platforms and enemies too, not just the player.
- **Replay ghost**: show a translucent copy of your last attempt running alongside you.
- **Procedural levels**: generate platform layouts from a seed.

---

## Architecture notes (for when you want to extend)

- All inter-system communication goes through the **`PlayerHealth.OnPlayerDeath`** C# event. Add new listeners (e.g., a "death sound" component) by subscribing in their `OnEnable`.
- **Singletons** (`GameManager.Instance`, `RewindManager.Instance`) make global state easy to grab from anywhere but make sure only one exists per scene.
- **Manager GameObject** in each scene holds `RewindManager` + `LevelManager` + `HUD`. The first scene also holds `GameManager` (which then persists across scenes via `DontDestroyOnLoad`).
- **Prefabs**: turn your Player into a prefab (drag from Hierarchy to a `Prefabs/` folder in Project) so you can drop it into every new scene without redoing all the components.

---

## When things break

- **"NullReferenceException on X.Update"** → something you needed to drag into the Inspector is empty. Look at the Managers and Player components.
- **"Player falls through the floor"** → the floor isn't on the `Ground` layer, OR the player's collider isn't touching it (check Edit Collider).
- **"Rewind doesn't snap me back"** → check `RewindManager` is on a Managers GameObject and `LevelManager.player` is assigned. Check Console for `"Checkpoint saved at..."` log when you press S.
- **"Build fails"** → scroll up in the Console looking for the FIRST red error; later errors are usually downstream.
