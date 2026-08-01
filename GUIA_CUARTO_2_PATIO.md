# Guía — Construir el cuarto 2: El patio de la procesión

> **Leyenda:** La Caja Ronca
> **Escena a crear:** `Patio.unity`
> **Estado:** los datos ya están configurados. Falta el trabajo de Editor, que es lo de abajo.

Esta guía es para hacerla de arriba a abajo, en orden. Cada paso dice **qué hacer** y **cómo verificar** que salió bien. Si algo no coincide con lo que dice acá, pará y avisá antes de seguir — es más fácil arreglarlo en el momento que tres pasos después.

---

## Antes de empezar

**Guardá siempre con `Ctrl+S` después de cada paso.** La mayoría de los "no veo el cambio" de las sesiones anteriores fueron por no guardar.

Los datos del cuarto ya están hechos, no hay que crearlos: `Assets/Datos/Leyenda_L4_PatioDeLaProcesion.asset` ya tiene el nombre, la leyenda (La Caja Ronca), la portada, y está encadenado como el cuarto **2** de la secuencia. Su campo `nombreEscena` dice `Patio`, así que la escena que vas a crear **tiene que llamarse exactamente `Patio`**.

---

## Paso 1 — Crear la escena duplicando la que ya funciona

No la armes de cero. `JUEGO.unity` ya tiene **todos los sistemas cableados** (fósforo, miedo, Presencia, pausa, victoria, derrota, audio, tutorial). Duplicarla y sacarle lo específico del cuarto 1 es mucho más rápido y seguro que rehacer ese cableado.

1. En el panel **Project**, andá a `Assets/Scenes/`.
2. Click derecho sobre **`JUEGO`** → **Duplicate** (o seleccionala y `Ctrl+D`).
3. Se crea `JUEGO 1` o similar. Renombrala a **`Patio`** (click derecho → Rename, o `F2`).

⚠️ Ya existe una escena llamada `JUEGO 1.unity` de antes (un backup viejo que no se usa). Si el duplicado toma otro nombre, no importa — lo importante es que termine llamándose `Patio`.

**Verificar:** en `Assets/Scenes/` tenés `Patio.unity`, y al abrirla se ve igual que `JUEGO`.

---

## Paso 2 — Agregarla a Build Settings

Si no está en la lista, `SceneManager.LoadScene("Patio")` va a fallar en tiempo de ejecución.

1. Menú **File → Build Profiles** (o Build Settings).
2. Con la escena `Patio` abierta, apretá **Add Open Scenes**. Si no aparece ese botón, arrastrá `Patio.unity` desde el Project a la lista de escenas.
3. Confirmá que quede **tildada** (habilitada).

**Verificar:** en la lista de escenas aparece `Assets/Scenes/Patio.unity` con su tilde.

---

## Paso 3 — Sacar lo que es del cuarto 1

Abrí `Patio.unity`. En la **Hierarchy** vas a ver estos objetos. Esta tabla dice qué se queda y qué se va:

### ✅ SE QUEDAN (son los sistemas compartidos, no los toques)

| Objeto | Qué tiene adentro |
|---|---|
| `FosforoManager` | FosforoManager + PresenciaManager |
| `FearManager` | La barra de miedo |
| `GameStateManager` | Estados del juego |
| `ManagerDeVictoria` | CondiciondeVictoria |
| `ManagerDerrota` | PantallaDerrota |
| `PauseManager` | Pausa |
| `GameManager` | Tutorial y textos flotantes |
| `Manager_Inventario` | Objetos de supervivencia |
| `GestorDeAudio` | Ambiente, voces, secuencia final |
| `Triangle` | El niño (jugador) |
| `Main Camera` | Cámara |
| `EventSystem` | Input de UI |
| Los `Canvas` | HUD (fósforos, barra de miedo, paneles de victoria/derrota) |

### ❌ SE VAN (son el escenario y los objetos del cuarto 1)

| Objeto | Por qué |
|---|---|
| `Square` | La geometría/paredes del cuarto de la abuela |
| `GameObject`, `GameObject (1)` | Piezas sueltas del escenario viejo |
| Los muebles y objetos interactuables | Buró, armario, cajones, canasta, la caja de galletas — todo eso es del cuarto 1 |

**Cómo sacarlos:** seleccionalos en la Hierarchy y apretá `Delete` (o `Backspace` en Mac).

Los objetos interactuables están **dentro de los Canvas**, así que desplegá cada Canvas con la flechita para encontrarlos. Dejá el Canvas en sí (es el HUD), borrá solo los objetos del escenario que estén colgando de él.

**Verificar:** le das Play y el juego arranca, se ve la interfaz (contador de fósforos, barra de miedo), podés encender un fósforo con **E** — pero el cuarto está vacío, sin muebles. Eso es exactamente lo que buscamos: el esqueleto funcionando, listo para recibir el contenido nuevo.

---

## Paso 4 — Poner el contenido del patio

Acá entra el arte y el diseño del cuarto nuevo — la parte creativa, y donde probablemente necesites a Anavi.

Lo mínimo para que el cuarto sea jugable:

1. **El escenario**: el fondo/las paredes del patio. Si Anavi todavía no lo tiene, poné cualquier imagen temporal para trabajar; se reemplaza después.
2. **Un objeto de victoria**: algo que represente el tesoro/objetivo del patio. Agregale el componente **`ObjetoInteractivo`** y tildá su casilla **`Es Objetivo De Victoria`**. Ese es el que termina el cuarto.
3. **(Opcional) Objetos de supervivencia**: agregales el componente **`ItemSupervivencia`**. Cada uno ralentiza el miedo un 30%. Si ponés alguno, ajustá `Items Totales` en el objeto `Manager_Inventario` para que coincida con cuántos hay.

**Verificar:** encendés un fósforo, ves los objetos, clickeás el objetivo, y aparece la pantalla de victoria.

---

## Paso 5 — Conectar la victoria al desbloqueo del siguiente cuarto

Esto hace que ganar el patio desbloquee el cuarto 3 y quede guardado.

1. En la Hierarchy, seleccioná el objeto **`GameStateManager`**.
2. **Add Component** → buscá **`L1Controller`** → agregalo.
3. En el Inspector, en el campo **`Definicion`**, arrastrá el asset `Assets/Datos/Leyenda_L4_PatioDeLaProcesion`.

*(El script se llama `L1Controller` por razones históricas — sirve para cualquier cuarto, solo hay que darle su definición. Si molesta el nombre, se puede renombrar después.)*

**Verificar:** ganás el cuarto, volvés al menú de niveles, y el cuarto 3 (El altillo olvidado) ya no tiene candado.

⚠️ Esto último solo se puede probar **después** de que existan las tarjetas del menú (paso 6).

---

## Paso 6 — Las tarjetas del menú (si todavía no están)

Esto es la Fase B del plan, y hace falta para ver la progresión entre cuartos. Va en **`Nivel.unity`**, que ya es la pantalla de selección paginada.

Si ya lo hiciste, saltealo. Si no, avisame y te guío paso a paso — es el mismo patrón de armar una tarjeta y duplicarla, con los datos que ya están listos en `Assets/Datos/`.

---

## Cuando termines

Avisame y reviso desde acá que todo haya quedado bien cableado (verifico por GUID, no por nombre, así no hay falsos positivos).

Después de eso, lo que sigue naturalmente es **la historia del patio**: la leyenda de La Caja Ronca, y cómo se cuenta. Puede ser una escena de historia previa (como `Historia` para el cuarto 1) o contarse dentro del propio cuarto con los textos flotantes que ya existen (`ControladorTextoFlotante`).

---

## Nota sobre lo que viene después

Esta guía duplica la escena para el cuarto 2. Eso funciona bien para **un** cuarto nuevo, pero si se hace lo mismo para el 3 y el 4, cualquier arreglo a un sistema compartido hay que repetirlo en 4 escenas.

Antes del cuarto 3 conviene **extraer los sistemas compartidos a un prefab** (un objeto reutilizable que cada escena instancia). Así se arregla en un lugar y se propaga solo. Está anotado en `PLAN_MAESTRO.md`, Fase D — hacer el cuarto 2 primero sirve justamente para medir cuánto duele antes de comprometerse con los otros dos.
