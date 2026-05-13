# Deja Vu 432

A challenging 2D platformer with a twist: dying sends you all the way back to the level start — but you can press **R** at any time to rewind to a checkpoint you saved earlier with **S**. The strategic question is always: *do I save here, or push further before I bank my safety net?*

**Team:** Kieran Moynihan, Lyle Racho
**Engine:** Unity 6 LTS (2D)
**Target:** WebGL build, published on [Unity Play](https://play.unity.com/)

## Project map

```
DejaVu432/
├── Assets/
│   ├── Scripts/
│   │   ├── Player/       # Movement + health
│   │   ├── Systems/      # Rewind, level, game manager
│   │   ├── World/        # Traps, kill zones, goal, checkpoint
│   │   ├── Camera/       # Camera follow
│   │   └── UI/           # Minimal HUD
│   ├── Scenes/           # (Created in Unity Editor)
│   ├── Sprites/          # (Pixel Adventure 1 imports here)
│   └── Prefabs/          # (Player, traps, etc. — built in Editor)
└── docs/
    ├── SETUP_GUIDE.md            # Install Unity, create project, hook up GitHub
    ├── EDITOR_WALKTHROUGH.md     # Click-by-click: build Level 1 in the Editor
    └── ROADMAP.md                # MVP scope, stretch features, week-by-week plan
```

## Where to start

1. Open `docs/SETUP_GUIDE.md` — do this first.
2. Then `docs/EDITOR_WALKTHROUGH.md` — builds your first playable scene.
3. Then `docs/ROADMAP.md` — what to do next.

## Controls (default)

| Action | Key |
| --- | --- |
| Move | A / D (or Left / Right) |
| Jump | Space |
| Save checkpoint | S |
| Rewind to checkpoint | R |

## Core mechanic, in one paragraph

The `RewindManager` continuously samples the player's position into a short history buffer. Pressing **S** stores the current position as a checkpoint. Pressing **R** plays the recent history back in reverse as a visual effect, then snaps the player to the saved point. **Dying clears the checkpoint** — you have to actually use it (press R) before dying, otherwise it's gone and you start the level over. That's the source of the game's tension.
