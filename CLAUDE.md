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

The project is early-stage: three scenes exist (`Intro.unity`, `Historia.unity`, `JUEGO.unity`). It's a horror game jam project built around three parallel systems, one per dev (`files/Plan_Dev_A.md` / `_B.md` / `_C.md`), integrating in `JUEGO.unity`: fósforo/luz/interacción (Dev A), estado de juego + barra de miedo (Dev B), "la Presencia" cost mechanic (Dev C).

All cross-system communication goes through a single shared static contract, **`Core/GameEvents.cs`** (namespace `Terror`): `OnFosforoEncendido`/`OnFosforoApagado` (Dev A raises, others listen), `OnCercaniaPresenciaCambiada(int nivel, float multiplicador)` (Dev C raises, Dev B listens). Each dev works in their own scripts/scene and only touches other systems through this contract or through another system's public `Instance` singleton (e.g. `FosforoController.Instance.EstaEncendido`) — don't add direct cross-system calls that bypass `GameEvents`.

## Architecture

Scripts live under `Assets/Script/`, one folder per dev system plus shared/UI folders. **All production scripts use `namespace Terror`.**

### `Assets/Script/UI/` — intro/story presentation (pre-dates the per-dev split)

- **`StoryManager.cs`** — Drives a simple paginated story sequence. Holds an array of `GameObject` story pages (`storyPages`), shows one at a time, and advances via `NextPage()` (wired to a UI Button). Ending the sequence currently just logs — the transition to loading the actual game scene (`JUEGO.unity`) is a TODO in the code (`SceneManager.LoadScene(...)` is commented out).
- **`IntroStoryController.cs`** — An alternative/complementary intro flow driven by a list of `StoryScene` structs (illustration sprite, text, optional audio clip), played automatically as a coroutine (`PlayStoryFlow`) with fade-in/hold/fade-out timing per scene. Also ends with a commented-out scene load into the game scene.
- **`FadeText.cs`** — Small reusable behavior: fades a `TextMeshProUGUI` component's alpha in from 0 whenever its GameObject is enabled (`OnEnable`). Attached to individual story page text elements so `StoryManager` activating a page automatically triggers a fade-in.

These three scripts are not currently wired together in code (no shared interface) — they represent two parallel approaches to presenting the intro/story (`StoryManager` for manual page-by-page, `IntroStoryController` for a timed auto-playing sequence), plus a shared visual helper (`FadeText`). When editing story/intro flow, check which of `StoryManager` or `IntroStoryController` the relevant scene (`Intro.unity` vs `Historia.unity`) actually uses before assuming both are active.

### `Assets/Script/Fosforo/` — match/light system (Dev A)

- **`FosforoController.cs`** — Singleton (`Instance`). `F` key lights a match (`Encender()`) if not already lit and `fosforosRestantes > 0`; burns down over `duracionQuemado` seconds then auto-extinguishes (`Apagar()`). Optionally drives a `Light2D` that follows the cursor. Raises `GameEvents.OnFosforoEncendido`/`OnFosforoApagado`.
- **`ObjetoInteractable.cs`** / **`InteraccionClick.cs`** / **`PanelInspeccion.cs`** — click-to-inspect: a click is only valid while `FosforoController.Instance.EstaEncendido`; raycasts for an `ObjetoInteractable`, which shows its `vistaPrimerPlano` GameObject via the `PanelInspeccion` singleton (only one inspection view open at a time; Escape or an outside click closes it). No ScriptableObject data layer exists here yet — objects are plain scene GameObjects with a name and a foreground view, not data assets with sprite/description/objective flags.

### `Assets/Script/Presencia/` — cost/risk mechanic (Dev C)

- **`PresenciaController.cs`** — Listens to `GameEvents.OnFosforoEncendido`; each light-up has a chance (`probabilidadDeAvance`) to advance `NivelActual` (0..`nivelMaximo`). Raises `GameEvents.OnCercaniaPresenciaCambiada(nivel, multiplicador)` using `multiplicadorMiedoPorNivel[nivel]`.
- **`PresenciaHUD.cs`** — Debug-only `OnGUI` readout of the current nivel/multiplicador.
- **`PresenciaTestTrigger.cs`** — test helper for the Presencia system.

### `Assets/Script/Core/` — shared contract + game state/fear (Dev B)

- **`GameEvents.cs`** — the shared static event contract described above.
- **`GameStateManager.cs`** — Singleton (`Instance`) driving `GameState`: `Inicio -> Juego -> Derrota/Victoria -> Reinicio`. Subscribe to `OnStateChanged` rather than polling `CurrentState`. `Start()` calls `IniciarJuego()` immediately (no start-screen exists yet, so entering `JUEGO.unity` begins play directly — swap this for a real "start" trigger once Día 2's start screen exists). `Reiniciar()` reloads the active scene. Nothing currently calls `Ganar()` — no win condition/objective system exists yet (the ScriptableObject data contract described in `Plan_Dev_B.md` hasn't been reconciled with Dev A's plain-GameObject `ObjetoInteractable`).
- **`FearManager.cs`** — Singleton (`Instance`) fear bar (`miedoActual`, 0–100), one-way (never decreases). Rises at `velocidadSubidaOscuridad`/sec, multiplied by the Presencia multiplier from `GameEvents.OnCercaniaPresenciaCambiada`, while no match is lit; holds steady while lit (tracked via `GameEvents.OnFosforoEncendido`/`OnFosforoApagado`, not a direct reference to `FosforoController`). Only ticks while `GameStateManager.CurrentState == GameState.Juego`. Triggers `Perder()` at 100.
- **`AudioManager.cs`** — Singleton (`Instance`) audio hooks for fósforo-encendido, Presencia-cerca (nivel ≥ `umbralNivelPresenciaCerca`), miedo-alto, victoria, derrota — all wired to the real events above. Clips are unassigned placeholders pending final assets from Diseñador C. Subscribes in `Start()` (not `OnEnable()`) so other singletons' `Awake()` has already run.
- **`FearBarUI.cs`** — Bridges `FearManager.OnMiedoCambiado` to a Minecraft-style segmented bar: assign an array of `Image` (`segmentos`, e.g. 5) and it fills them left-to-right by enabling/disabling each as `miedoActual` crosses each segment's threshold. Optionally assign `spriteLleno`/`spriteVacio` to swap sprites per segment instead of just toggling visibility.

### `Assets/Script/Debug/` — temporary test-only script

- **`DebugHUD.cs`** — Not part of production code; nothing depends on it. `OnGUI` overlay showing state/miedo/fósforo, plus `P`/`G`/`R` keys to force derrota/victoria/reinicio for testing (fósforo itself is lit with `F`, handled by `FosforoController`). Safe to delete the whole folder once manual testing is done.

## Working with this codebase

This is a Unity Editor project — there is no CLI build/test/lint pipeline in the repo. Changes to `.cs` files are edited as text, but scene wiring (which GameObjects hold which components, Inspector-assigned references like `storyPages` or `introScenes`) lives in the `.unity` scene files and can only be fully verified by opening the project in the Unity Editor.

Do not hand-edit `.unity` or `.asset` files unless you understand Unity's YAML serialization format — small mistakes can corrupt scene/asset references silently.
