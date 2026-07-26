# Plan — El shell de la antología (Semanas 2 y 7)

> ## 🟡 Estado al cerrar la sesión del 2026-07-26 — LEER PRIMERO
>
> **El código está terminado y commiteado localmente (5 commits). El trabajo de Editor
> (Fase 4) está hecho pero SIN COMMITEAR** — falta que David confirme visualmente que el
> menú se ve bien (en curso, últimu paso: verificar tamaño/orden de las tarjetas) y que yo
> haga el commit final de escenas + assets (último paso pendiente).
>
> **Cambio de alcance a mitad de camino, no reflejado en el resto de este documento tal
> como se escribió originalmente:** el menú dejó de ser "5 leyendas ecuatorianas" y pasó a
> ser **"Los secretos de la casa"** — 4 cuartos que se desbloquean en secuencia (no 5
> leyendas independientes). Ver `CLAUDE.md` → "Product direction" para el detalle completo
> y actualizado. Las secciones 2, 5 y 6 de abajo describen el plan **original**, previo a
> ese cambio — quedan como referencia histórica de qué se pensaba construir, no como el
> estado final.
>
> **⚠️ No se pudo pushear nada — David no tiene permiso de escritura en el repo** (`git
> push` → 403). Los 5 commits de esta sesión existen solo en esta máquina. Pendiente:
> pedir acceso o hacer fork+PR.
>
> Ver la sección **10. Cierre de sesión (2026-07-26)** al final de este documento para el
> detalle completo de qué se hizo, qué falta, y los próximos pasos concretos.

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

---

## 10. Cierre de sesión (2026-07-26)

### Qué se hizo — código (commiteado localmente, 5 commits)

```
99748d3  Fix alivio real del miedo, puente Presencia->FearManager, y numeros de fosforo
c94c72c  Coloca PresenciaFearBridge en la escena (JUEGO.unity)
c4c0a67  Documenta el plan del shell de la antologia (Semanas 2 y 7)
dcfbb7f  Semana 2: reorganiza Assets/Script -> Assets/_Project
e202ac2  Semana 2: shell de la antologia (guardado, menu, router, pausa)
```

Rama de respaldo `backup-antes-de-reorganizar` apunta al commit justo antes del `git mv`
masivo, por si algo necesita revertirse.

### Qué se hizo — Editor (hecho, pero SIN commitear todavía)

- Las 4 escenas renombradas y movidas a `Assets/_Project/...` (ver §6).
- `00_Boot`: objeto `[Persistentes]` con `SceneRouter` + `AudioManager` + `BootLoader`.
- `01_Menu`: `MenuPrincipal` en el `Canvas`, 4 tarjetas (`Tarjeta_L1`..`L4`, no 5 — ver el
  aviso de arriba sobre el cambio de alcance), cada una con `MenuTarjetaLeyenda` cableado
  (Menu/Leyenda/TextoNombre/ImagenPortada/Candado/TextoEstado) y su `Button.OnClick →
  OnClickTarjeta()`. El botón "Como Jugar" (antes muerto) sigue sin recablear a ningún panel
  — pendiente.
- `L1_Intro`: `StoryManager.nombreDeLaEscenaDelJuego` corregido de `JUEGO` a `L1_Juego`.
- 4 assets `LeyendaDefinicion` en `_Project/Data/` (`L1_CajaFosforos` → `L2_Cantuna` →
  `L3_DamaTapada` → `L4_PadreAlmeida`, encadenados por `siguienteLeyenda`) + 1
  `CatalogoLeyendas.asset` con las 4 en orden. **No hay una 5ta** (se creó `L5_CajaRonca` y
  se borró al confirmar que son 4 cuartos, no 5).
- 4 sprites recortados de los mockups de Anavi en `Assets/Recursos/Niveles/` (uno por
  cuarto), asignados al campo `Portada` de cada `LeyendaDefinicion`.
- Arreglado a mano (edición directa del `.unity`, ver `CLAUDE.md` → "Editing scenes outside
  the Editor"): orden de hijos de cada tarjeta (Portada detrás del texto, no encima),
  anchors de `Portada` a stretch-fill, y tamaño/posición uniforme de las 4 tarjetas
  (320×400, separadas en X). **Esto último — que se vea bien — todavía no lo confirmó
  David visualmente**, quedó como el último paso pendiente antes de cerrar la Fase 4.

### Lo que falta, en orden

1. **David**: confirmar en Play (desde `00_Boot`, pestaña `Game`, no `Scene`) que las 4
   tarjetas se ven del mismo tamaño, separadas, con la imagen de fondo y el texto legible
   encima.
2. Si algo más se ve mal, seguir ajustando (probablemente vía edición directa del `.unity`,
   es lo que mejor funcionó esta sesión frente a la navegación manual del Inspector).
3. **Claude**: commitear el trabajo de Editor (escenas + assets + sprites) — Fase 5 del
   plan original.
4. Correr el checklist completo de la sección 8 de este documento (pausa, salir al menú,
   miedo al doble, persistencia).
5. Cablear el botón "Como Jugar" a un panel (hoy no hace nada).
6. Decidir qué hacer con el acceso de push (pedirle permiso a Anavi, o fork+PR) para que
   este trabajo deje de existir solo en esta máquina.
7. Cuando haya arte limpio (sin candado dibujado) de "El recibidor del velo" y "El patio de
   la procesión", reemplazar esos 2 sprites.

### Aprendizajes de esta sesión, para la próxima

- **La navegación manual de Unity por texto es lenta y propensa a error** (crear objetos en
  el menú equivocado, no encontrar el Inspector, olvidar guardar). Para tareas de layout
  UI (tamaños, anchors, orden de hijos, posiciones), es más confiable que Claude edite el
  `.unity` directamente con un script Python que parsee los bloques YAML y verifique
  integridad padre/hijo después — así se hizo para arreglar las tarjetas del menú.
- **El olvido de `Ctrl+S` fue la causa de la mayoría de los "no veo el cambio"** en esta
  sesión. Recordar pedirlo explícitamente después de cada paso de Editor.
- **Reabrir una escena sin guardar ("Don't Save") es la forma segura de sincronizar** el
  estado en disco (editado por Claude) con lo que Unity tiene en memoria, sin arriesgarse a
  que Unity pise el archivo bueno con datos viejos.
