# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository structure

This top-level directory (`game1-gm`) is a wrapper around the actual game project, which lives in `Main1Escenario/`. **`Main1Escenario/` is itself a separate git repository** (its own `.git`, remote `origin` at `https://github.com/Seanlim22004/Main1Escenario.git`), nested inside this one. When making commits, check which repo you're actually in — `git status`/`git log` run from the top level will not show `Main1Escenario`'s history, and vice versa.

All actual project files (Unity project, scripts, scenes) are under `Main1Escenario/`.

## Project overview

`Main1Escenario` is a Unity 2D project (Editor version `6000.3.14f1`, Unity 6). It uses:
- **Universal Render Pipeline (URP)** for 2D rendering — pipeline asset at `Assets/Settings/UniversalRP.asset`, renderer at `Assets/Settings/Renderer2D.asset`.
- **New Input System** (`com.unity.inputsystem`) — action map defined in `Assets/InputSystem_Actions.inputactions` (Player map with Move, Look, Attack, Interact, Crouch, etc.).
- **TextMesh Pro** for UI text.
- 2D packages: animation, aseprite, psdimporter, tilemap, spriteshape.

The project is early-stage: three scenes exist (`Intro.unity`, `Historia.unity`, `JUEGO.unity`) but only a small UI/story scripting layer has been built so far.

## Architecture

Scripts live under `Assets/Script/` (currently only `Assets/Script/UI/`):

- **`StoryManager.cs`** — Drives a simple paginated story sequence. Holds an array of `GameObject` story pages (`storyPages`), shows one at a time, and advances via `NextPage()` (wired to a UI Button). Ending the sequence currently just logs — the transition to loading the actual game scene (`JUEGO.unity`) is a TODO in the code (`SceneManager.LoadScene(...)` is commented out).
- **`IntroStoryController.cs`** — An alternative/complementary intro flow driven by a list of `StoryScene` structs (illustration sprite, text, optional audio clip), played automatically as a coroutine (`PlayStoryFlow`) with fade-in/hold/fade-out timing per scene. Also ends with a commented-out scene load into the game scene.
- **`FadeText.cs`** — Small reusable behavior: fades a `TextMeshProUGUI` component's alpha in from 0 whenever its GameObject is enabled (`OnEnable`). Attached to individual story page text elements so `StoryManager` activating a page automatically triggers a fade-in.

These three scripts are not currently wired together in code (no shared interface) — they represent two parallel approaches to presenting the intro/story (`StoryManager` for manual page-by-page, `IntroStoryController` for a timed auto-playing sequence), plus a shared visual helper (`FadeText`). When editing story/intro flow, check which of `StoryManager` or `IntroStoryController` the relevant scene (`Intro.unity` vs `Historia.unity`) actually uses before assuming both are active.

## Working with this codebase

This is a Unity Editor project — there is no CLI build/test/lint pipeline in the repo. Changes to `.cs` files are edited as text, but scene wiring (which GameObjects hold which components, Inspector-assigned references like `storyPages` or `introScenes`) lives in the `.unity` scene files and can only be fully verified by opening the project in the Unity Editor.

Do not hand-edit `.unity` or `.asset` files unless you understand Unity's YAML serialization format — small mistakes can corrupt scene/asset references silently.
