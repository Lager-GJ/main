# Plan — El shell de la antología (Semanas 2 y 7)

> **⚠️ Aviso para el equipo (Anavi):** este trabajo **mueve casi todos los scripts de sitio**
> y **renombra las 4 escenas**. Antes de que empiece:
> 1. **Pusheá todo lo que tengas** (aunque esté a medias, en una rama si hace falta).
> 2. **No trabajes** hasta que veas el commit `Semana 2: reorganiza Assets/Script -> Assets/_Project`.
> 3. Cuando lo veas, hacé **`git pull`** antes de seguir.
>
> Si tenías cambios sin pushear cuando esto entre, git no los va a poder emparejar con los
> archivos movidos y el merge va a doler. El movimiento se hace con `git mv`, así que el
> historial de cada archivo se conserva (`git log --follow` sigue funcionando).

Este documento describe qué se va a construir y por qué. El plan de producto completo
(las 8 semanas) vive fuera del repo, en `PLAN_MAESTRO.md`.

---

## 1. Qué problema resuelve

Hoy el juego **se juega pero no se puede salir de él**: arranca en una splash, pasa por
la historia y cae en la escena de juego, donde se puede ganar y perder — y ahí se acaba.
No hay menú, ni guardado, ni pausa, ni opciones, y ganar o perder deja al jugador atrapado
en la escena sin forma de reiniciar o volver.

El plan maestro parte esto en dos semanas que en realidad son una sola pieza — el
**envoltorio** alrededor del juego — así que se hacen juntas:

- **Semana 2:** escena de arranque, menú con las 5 leyendas, guardado, pausa, opciones, créditos.
- **Semana 7:** las 4 leyendas bloqueadas con su portada y teaser, y la narrativa de intro/final.

## 2. Qué va a existir al terminar

**Arranque y menú**
- El juego abre en `00_Boot`, carga el perfil guardado, aplica los volúmenes y salta solo al menú.
- Menú con **5 tarjetas**: "La Caja de Fósforos" jugable; Cantuña, La Dama Tapada,
  El Padre Almeida y La Caja Ronca con candado, teaser y sin responder al click.
- Paneles de **Opciones** (3 volúmenes), **Créditos** y **Cómo Jugar**.

**La partida**
- Entrar a la Leyenda 1 → páginas de historia → juego, igual que hoy.
- **Escape pausa de verdad**: el fósforo deja de consumirse, el miedo se congela y la
  tecla `E` no gasta un fósforo.
- Menú de pausa: Reanudar / Reiniciar / Salir al menú.
- **Ganar o perder muestra un panel** con botón para reintentar o volver al menú.

**Persistencia**
- Volúmenes y progreso sobreviven a cerrar y reabrir el juego, **también en WebGL**.

**Textos:** todo queda con `TODO:` — la estructura lista, la prosa se escribe después.

**Fuera de alcance:** las leyendas 2–5 jugables, tutorial interactivo, arte de portadas, audio real.

## 3. Los tres conflictos técnicos y cómo se resuelven

### 3.1 Dos clases `Terror.AudioManager`
El proyecto tiene `Assets/Script/Core/AudioManager.cs` (ganchos de SFX: fósforo, presencia,
miedo, victoria, derrota) y el shell trae otra clase con el mismo nombre y namespace
(volúmenes persistentes). Dos clases iguales no compilan (`CS0101`).

**Resolución:** el `AudioManager` existente pasa a llamarse **`AudioJuegoL1`** y se muda a la
carpeta de la Leyenda 1. El del shell conserva el nombre `AudioManager`.

Por qué en ese sentido y no al revés: sus clips (`sfxFosforoEncendido`, `sfxVictoria`…) son
todos de la Leyenda 1, tiene `[RequireComponent(typeof(AudioSource))]` y se suscribe a
`FearManager.Instance`/`GameStateManager.Instance`, que viven en la escena de juego — o sea,
**no puede ser el manager persistente del shell**. Son dos responsabilidades con dos ciclos
de vida distintos, no una duplicación.

Verificado antes de tocarlo: **su GUID no aparece en ninguna escena**, así que renombrarlo
no rompe ningún cableado. El rename de archivo y el de clase van en el mismo commit, porque
Unity exige que el nombre del archivo coincida con el de la clase.

### 3.2 `GameStateManager` con dos APIs
El borrador del shell fue escrito contra `EstadoJuego`/`EstadoActual`/`OnEstadoCambiado`;
este proyecto usa `GameState`/`CurrentState`/`OnStateChanged`.

**Resolución:** **gana la API de este proyecto** — es la que ya hablan `FearManager`,
`AudioManager` y `ObjetoInteractivo`. Al enum se le agrega `Pausa` **al final** (los enums
serializan por entero, insertar en el medio corrompería datos guardados), más `Pausar()`,
`Reanudar()` y un `OnDestroy` que limpia `Instance` (hoy falta, y es lo que causaría el bug
del "miedo al doble" al volver a entrar a la partida). Los dos archivos del shell que
dependían de la otra API se reescriben.

### 3.3 El guardado se pierde en WebGL
El `SaveSystem` del borrador usa `File.WriteAllText`. En WebGL eso escribe a un sistema de
archivos que se vuelca a IndexedDB **de forma asíncrona**: si el jugador cierra la pestaña
poco después de mover un slider, el guardado se pierde. Es el clásico "se me borraron los
datos" de itch.io.

**Resolución:** misma API pública y mismo JSON, pero el almacenamiento pasa a
**`PlayerPrefs` con `PlayerPrefs.Save()`**, que es el volcado síncrono explícito. Ningún
llamador cambia.

## 4. Cómo funciona la pausa

Hoy solo `FearManager` consulta el estado del juego; el fósforo, la presencia y la barra de
miedo corren sin freno. La pausa usa **dos mecanismos**, porque ninguno alcanza solo:

- **`Time.timeScale = 0`** congela todo lo que avanza por `Time.deltaTime`: el temporizador
  del fósforo, el miedo, la presencia y el movimiento. Esto cubre `BarraDeMiedo.cs` y
  `PresenciaManager.cs` **sin abrirlos** (son Latin-1 y editarlos es arriesgado).
- **Guardas de estado** en `FosforoManager` y `ObjetoInteractivo`, porque `timeScale = 0`
  **no detiene `Update()`**: sin la guarda, pulsar `E` durante la pausa igual gastaría un
  fósforo. De paso arreglan un bug actual — hoy se puede seguir jugando después de ganar o perder.

⚠️ `Time.timeScale` es global y **sobrevive a `LoadScene`**. Como hay caminos que cargan
escena sin pasar por el `SceneRouter`, este lleva un `Time.timeScale = 1f` al inicio de cada
carga **y** un hook a `SceneManager.sceneLoaded`. Sin eso, salir al menú estando en pausa
deja el menú congelado.

## 5. Estructura nueva de carpetas

```
Assets/_Project/
  Core/                    ← el shell: vive entre leyendas
    Boot/ Save/ Audio/ Routing/ Leyendas/ Estado/ UI/ Scenes/
  Data/                    ← catálogo + las 5 definiciones de leyenda
  Leyendas/
    L1_CajaFosforos/
      Scenes/   L1_Intro.unity · L1_Juego.unity
      Scripts/  Fosforo/ Miedo/ Demonio/ Jugador/ Historia/
      Data/     ← los 7 objetos ecuatorianos (Semana 5)
```

La idea: separar **el shell** (lo que envuelve y persiste) del **contenido** (cada leyenda),
para que la Leyenda 2 se construya enchufándose sin rehacer nada.

**No se borra ningún script.** Todo se mueve con `git mv`, conservando `.meta`, GUID e historial.

## 6. Renombrado de escenas

| Antes | Después |
|---|---|
| `Tutorial.unity` (vacía) | `00_Boot.unity` |
| `Intro.unity` | `01_Menu.unity` |
| `Historia.unity` | `L1_Intro.unity` |
| `JUEGO.unity` | `L1_Juego.unity` |

Ninguna escena se construye de cero: las cuatro ya existen con cámara, canvas y EventSystem.
`Tutorial.unity` estaba vacía y en Build Settings sin que nada la cargara, así que se
aprovecha como escena de arranque.

**Las escenas se renombran desde el Project window de Unity, nunca desde Finder ni con git** —
`EditorBuildSettings.asset` guarda la ruta además del GUID y solo se actualiza bien desde el Editor.

## 7. Orden de trabajo

| Fase | Quién | Qué |
|---|---|---|
| 0 | — | Este documento, commiteado y pusheado **antes** de mover nada |
| 1 | Claude Code | `git mv` de los scripts. **Unity cerrado.** Commit + push inmediato |
| — | David | **Compuerta:** abrir Unity, confirmar 0 errores y 0 "Missing script" |
| 2 | Claude Code | Portar los scripts del shell con los fixes de §3 |
| 3 | Claude Code | Pausa y guardas de estado |
| 4 | David | Editor: renombrar escenas, armar `00_Boot` y el menú, crear los assets |
| 5 | Ambos | Verificación end-to-end y commit final |

## 8. Cómo se verifica que quedó bien

- Arranca en `00_Boot` y salta solo al menú.
- Las 4 leyendas bloqueadas **no responden al click** y muestran candado.
- En pausa: el miedo no se mueve, la luz del fósforo no se atenúa y **`E` no gasta un fósforo**.
- Salir al menú y **que el menú responda** (prueba de que se restauró `timeScale`).
- Volver a entrar a la Leyenda 1 y que el miedo **tarde lo mismo en llenarse que la primera
  vez** — si tarda la mitad, hay dos `FearManager` vivos.
- La consola muestra **exactamente un** `[Fosforo] Encendido` por pulsación.
- Bajar el volumen, cerrar el juego, reabrirlo: el volumen se conservó.

## 9. Regla permanente que sale de esto

> **Nada relacionado con el gameplay puede ser `DontDestroyOnLoad`.**
> Solo el objeto `[Persistentes]` de `00_Boot` sobrevive entre escenas.

Si `FearManager` o `FosforoManager` se volvieran persistentes aparecería el bug del miedo
al doble. Además el GameObject `FosforoManager` carga **tres** componentes
(`FosforoManager`, `PresenciaManager`, `PresenciaFearBridge`) y los tres tienen guardas de
singleton que llaman `Destroy(gameObject)` — un duplicado los mataría a los tres de una.
