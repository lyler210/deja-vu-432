# Setup Guide

Total time: about 30 minutes the first time. You only do this once.

---

## 1. Install Unity Editor (Unity 6 LTS)

You already have Unity Hub. Open it.

1. In Unity Hub, click the **Installs** tab on the left → **Install Editor** button (top right).
2. In the "Official releases" list, pick the **latest Unity 6 LTS** (something like `6000.0.x LTS`). LTS = Long Term Support, the stable one.
3. On the "Add modules" page, check these boxes:
   - **WebGL Build Support** ← required to publish to Unity Play
   - **Documentation** (optional but handy)
   - You can skip iOS/Android/Linux build support unless you want them.
4. Click **Install**. This takes ~10 minutes.

> **Why Unity 6 LTS?** The scripts I wrote use the modern Rigidbody2D API (`linearVelocity` instead of the old `velocity`). They work on Unity 6 LTS and Unity 2023.x. They will NOT compile on Unity 2022.3 LTS without small tweaks. Stick with Unity 6.

---

## 2. Create the Unity project

1. In Unity Hub, click the **Projects** tab → **New project** button.
2. **Editor Version:** pick the Unity 6 LTS you just installed.
3. **Template:** choose **Universal 2D**. (Not "2D" — pick the URP one, "Universal 2D". It gives you better 2D lighting tools later.)
4. **Project name:** `DejaVu432`
5. **Location:** set this to `/Users/kieranmoynihan/Documents/Claude/Projects/Dejavu432` — the same folder where this guide lives. Unity will create its own `Assets/`, `Packages/`, `ProjectSettings/` folders inside it.

   *Heads up:* if you have a problem because the folder is "not empty," Unity Hub may refuse. In that case, point it one level deeper to `…/Dejavu432/UnityProject/`, and we'll move things in step 4.
6. Click **Create project**. First open takes 2–5 minutes while Unity builds its `Library/` cache.

---

## 3. Drop in the scripts

I've already written all your scripts to:

```
/Users/kieranmoynihan/Documents/Claude/Projects/Dejavu432/Assets/Scripts/
```

If Unity created `Assets/` in the same place: **you're already done with this step.** Open Unity and you should see a `Scripts` folder under `Assets` in the Project window at the bottom.

If Unity created `Assets/` somewhere else (e.g., inside a `UnityProject/` subfolder): drag the contents of my `Assets/Scripts/` folder into Unity's `Assets/` folder in the Editor (Project window). Unity will compile them in a few seconds — watch the bottom-right spinner.

When the compile finishes, check the **Console** window (Window → General → Console). You should see **zero red errors**. Yellow warnings are fine.

---

## 4. Import Pixel Adventure 1

1. Open this page in your browser: <https://assetstore.unity.com/packages/2d/characters/pixel-adventure-1-155360>
2. Click **Add to My Assets** (you'll need a free Unity account — same one as Unity Hub).
3. Back in the Unity Editor: **Window → Package Manager**.
4. Top-left dropdown: switch from **In Project** to **My Assets**.
5. Find "Pixel Adventure 1" → click **Download** → then **Import**.
6. The Import window shows a tree of files — leave everything checked, click **Import**.

The assets land under `Assets/Pixel Adventure 1/` in your Project window. Stuff you'll care about:
- `Main Characters/Ninja Frog/` — sprites + idle/run/jump anim strips.
- `Terrain/` — tileset for ground.
- `Traps/Spikes/` — the spike sprite.
- `Traps/Saw/` — sawblade.

---

## 5. Hook up GitHub

You said your repo is <https://github.com/lyler210/deja-vu-432>. Let's tie this local project to it.

Open a Terminal in your project folder:

```bash
cd "/Users/kieranmoynihan/Documents/Claude/Projects/Dejavu432"
git init
git remote add origin https://github.com/lyler210/deja-vu-432.git
git fetch origin
git checkout -b main
# pull the existing README/.gitignore from the remote without breaking our local stuff
git pull origin main --allow-unrelated-histories
git add .
git commit -m "Scaffold: scripts, docs, .gitignore"
git push -u origin main
```

> **If you hit a merge conflict on README.md / .gitignore:** keep MY versions (the new ones in this folder). The repo had a `:P` README and a basic gitignore; the new ones are more complete.

After this, day-to-day you just do `git add .`, `git commit -m "..."`, `git push`.

The included `.gitignore` already excludes `Library/`, `Temp/`, `Build/` — the heavy auto-generated Unity folders that should never be committed.

---

## 6. Ready

When all of the above is done:
- Unity Editor open on the DejaVu432 project ✅
- Console showing 0 errors ✅
- `Assets/Scripts/` populated with my .cs files ✅
- `Assets/Pixel Adventure 1/` populated with art ✅
- Git pushed to your repo ✅

**Now open `EDITOR_WALKTHROUGH.md` to build your first playable level.**
