# Plan Maestro — Lumbre
## De dos prototipos separados a un juego terminado

**Producto:** juego de terror 2D. Un niño en la casa de su abuela, de noche, con una caja de fósforos limitada. Cada fósforo da luz para explorar pero acerca a algo. Estudio: **LAGER**.

**La idea que une todo:** cada cuarto tiene **su propia temática e historia**, ligadas al fuego y a la lumbre, y esas historias son **las leyendas**. El fósforo no es solo una mecánica: es el hilo narrativo entre los cuartos.

**Equipo:** David (sistemas, integración, balanceo) + Anavi (arte, audio, contenido, escenas). Los dos programan con asistencia de IA.

**Plazo:** entrega de clase/jam, ~1 mes o más desde 2026-07-31.

**Plataforma:** se decide al final del desarrollo. Hay builds de Windows hechas; el plan original apuntaba a WebGL/itch.io.

---

# 1. Dónde estamos hoy (verificado en código y escenas, 2026-07-31)

El proyecto pasó por una etapa confusa: **dos personas construyeron el mismo juego en paralelo, sin saberlo**, en copias distintas del repo. Eso ya se resolvió: la versión de Anavi resultó sustancialmente más completa, y es la base actual.

**Dónde se trabaja:** `final/Main1Escenario`, rama **`main`**, remoto **`Lager-GJ/main`**. Parte del commit `a400b4b` de Anavi, más el shell de menú que aportamos encima.

## Lo que YA funciona, verificado

**Flujo de escenas real** (según Build Settings):
```
Intro  →  Nivel  →  CreditosPan  →  Historia  →  JUEGO
```
- **`Intro.unity`** — menú principal, con botones Nuevo Juego / Niveles / Créditos / Salir (`Scriptcambio.cs`).
- **`Nivel.unity`** — **ya es una pantalla de selección de niveles paginada** (`PageManager` con páginas, `LevelLoader`, botones SIGUIENTE/Volver). Es acá donde tiene sentido que vivan las tarjetas de cuartos, no en `Intro`.
- **`CreditosPan.unity`** — créditos.
- **`Historia.unity`** — páginas de historia con `StoryManager` + fades.
- **`JUEGO.unity`** — el juego. 70 GameObjects, 106 componentes.

**Dentro de `JUEGO.unity`:**
- Fósforo (E enciende / Q apaga), luz que se atenúa, inventario visual de fósforos.
- **Barra de miedo funcional** con alivio real (baja con el fósforo encendido, sube en oscuridad × cercanía de la Presencia). Ese multiplicador de cercanía **sí está conectado** — `PresenciaManager` lo publica.
- **4 objetos interactuables colocados**: 3 `ItemSupervivencia` (coleccionables) + 1 `ObjetoInteractivo` (la Llave).
- Muebles interactuables con arte real: buró, armario, cajones, canasta, caja de fósforos.
- **Pausa jugable** (`PausaManager`, tecla P o Escape).
- **Pantallas de victoria y derrota** colocadas, la de derrota con jumpscare.
- **Audio real y cableado**: 18 `.wav` (ambiente de cuarto, grillos, latidos, voces, fósforo, cajones, puertas, pasos, jumpscare, final bueno, final malo, game over) + 8 scripts que los conectan a eventos reales. `AmbienteManager` reacciona al nivel de miedo.
- Tutorial y textos flotantes.

**Traducción honesta: el juego se juega de principio a fin.** Ya no es un prototipo de sistemas sueltos — es un juego con contenido, audio y pantallas de resultado. Lo que falta es pulido, decisiones pendientes y contenido adicional.

## Lo que está a medias

- **El menú de cuartos**: el código está portado (`Assets/Script/Shell/`) y los datos ya existen (`Assets/Datos/`, los 4 cuartos encadenados con sus portadas), pero **falta armar las tarjetas en `Nivel.unity`**. Ver §4, Fase B.
- **Todo el contenido vive en una sola escena** (`JUEGO.unity`). Para que cada cuarto tenga su propia historia y cargue solo lo suyo, hay que separarlo. Ver §4, Fase D.
- **Tutorial duplicado**: `TutorialManager.cs` y `ManagerTutorial.cs` hacen casi lo mismo y los dos están colocados. **Decisión: se quedan los dos por ahora**, no es prioridad.
- **`JUEGO 1.unity`** existe pero no está en Build Settings — backup/experimento.
- **`PanelInspeccion.cs`** sigue en el proyecto sin que nadie lo use.

---

# 2. Las reglas del juego (mecánica confirmada)

1. **Encender un fósforo → alivio real.** La barra de miedo **baja** activamente mientras arde. Encender es un refugio momentáneo.
2. **El fósforo se apaga → el miedo empieza a subir**, cada vez más rápido según qué tan cerca esté la Presencia (multiplicador).
3. **Sin fósforo encendido, no se puede interactuar** con los objetos del escenario.
4. **Cada fósforo encendido acerca a la Presencia** — ese es el costo.
5. **Se gana encontrando la caja de galletas** (el tesoro de la abuela). Es el único camino de victoria.
6. Los **objetos de supervivencia** ralentizan un 30% la subida del miedo (acumulativo). Son ayudas para aguantar más, **no** ganan la partida.
7. Encontrar la caja **sin fósforos o con el miedo al máximo** = derrota igual (`CondiciondeVictoria.cs`).

| Perilla | Valor | Estado |
|---|---|---|
| Fósforos totales | **4** | ✅ confirmado 31/07 |
| Duración de un fósforo | **5 s** | ✅ confirmado 31/07 |
| Objetos de supervivencia | 3 | ✅ |
| Reducción de miedo por objeto | 30% acumulativo | a balancear |
| Miedo: velocidad de subida | 5/s × multiplicadores | a balancear |
| Miedo: velocidad de bajada | 8/s | a balancear |

---

# 3. Decisiones ya tomadas (2026-07-31)

- **Victoria**: encontrar la caja de galletas, y solo eso. Los objetos de supervivencia ya no ganan la partida — se quitó ese segundo camino, que convivía con el primero.
- **Fósforos**: se quedan en 4 de 5 segundos. No se toca.
- **Plataforma**: se define al final del desarrollo.
- **Builds en el repo**: se quedan como están (492 MB entre `EXE/` y `Ejecutable/`, que son la misma build duplicada). Si el repo se vuelve incómodo de clonar, revisar.
- **Tutoriales duplicados**: se quedan los dos.

- **Repo:** de acá en adelante se trabaja **todo en `Lager-GJ/main`** (rama `main`). `Seanlim22004/Main1Escenario` queda como archivo histórico — solo sirvió para traer la versión de la que partimos, y David no tiene permiso de escritura ahí. En el proyecto local, `origin` apunta a Lager-GJ y el viejo quedó como `seanlim-viejo`.

## Lo único que sigue abierto

**Pedido de arte:** portadas de los cuartos 3 y 4 sin el candado dibujado dentro de la ilustración (las actuales lo tienen incrustado, así que se ven con candado doble).

---

# 4. El plan

Ordenado por prioridad, no por semanas fijas: primero lo que **desbloquea o arregla**, después lo que **agrega**.

## Fase A — Arreglar lo torcido ✅ (hecho 2026-07-31)

- [x] **[D]** Cerrar las decisiones de producto (§3).
- [x] **[P]** Dejar **un solo** camino de victoria: la caja de galletas. `InventarioSupervivencia` ya no llama a `Ganar()`.
- [x] **[P]** Corregir la dirección del alivio del miedo (subía con el fósforo encendido, ahora baja).
- [x] **[P]** Resolver la colisión de nombres de `AudioManager` (el de la leyenda pasa a `AudioJuegoL1`).
- [ ] **[P]** Limpiar lo muerto: `PanelInspeccion.cs` si sigue sin usarse, `JUEGO 1.unity` si es backup. *(no urgente)*

## Fase B — El menú de cuartos `[David, Editor]`

*Objetivo: que el juego se sienta un producto con progresión, no una escena suelta.*

Ya está hecho el código (`Assets/Script/Shell/`) **y los datos** (`Assets/Datos/`: los 4 cuartos encadenados, con portadas, solo el primero desbloqueado). **Va en `Nivel.unity`**, que ya es la pantalla de selección paginada de Anavi — no hay que crear escenas nuevas.

- [x] **[P]** Los 4 assets `LeyendaDefinicion` + el `CatalogoLeyendas`, encadenados por `siguienteLeyenda`.
- [ ] **[P]/[U]** Armar las 4 tarjetas en `Nivel.unity` con `MenuTarjetaLeyenda` (estados: bloqueada con candado / **ENFRÉNTALO** / **ESCAPASTE**).
- [ ] **[P]** Colocar `MenuPrincipal` y el `AudioManager` del shell.
- [ ] **[P]** Colocar `L1Controller` en `JUEGO.unity` para que ganar marque el cuarto como completado y desbloquee el siguiente.
- [ ] **[A]** *(Anavi)* portadas de los cuartos 3 y 4 sin el candado dibujado encima.

**✅ Listo cuando:** entrás a Niveles, ves los 4 cuartos, solo el primero se puede jugar, y al ganarlo el segundo se desbloquea y queda así aunque cierres el juego.

## Fase C — Pulido del cuarto 1 `[David + Anavi]`

*Objetivo: que el primer cuarto esté realmente terminado, no solo funcional.*

- **[D]/[P]** **Balanceo con playtests reales**: 3-5 personas ajenas al proyecto. ¿Se gana muy fácil? ¿El miedo sube demasiado rápido? Solo tocar números, cero features nuevas.
- **[D]** Validar que se entienda el costo sin explicarlo: que digan solos *"encender fósforos es peligroso, pero quedarse a oscuras también"*.
- **[P]** Bug bash de casos borde: pausar durante una interacción, alt-tab, reiniciar tras derrota, salir al menú a media partida, spamear clicks, resolución distinta.
- **[U]** Legibilidad con brillo normal de monitor, no el tuyo calibrado.
- **[S]** *(Anavi)* revisar la mezcla de audio: que nada tape a nada, que el jumpscare no reviente los parlantes.

**✅ Listo cuando:** un desconocido lo juega completo sin ayuda y sin que truene.

## Fase D — Cada cuarto con su propio contenido `[Anavi contenido + David sistemas]`

*Objetivo: que cada cuarto tenga su leyenda, su temática de fuego, y cargue solo lo suyo.*

Esta es la fase donde el juego se vuelve lo que dice ser. Hoy **todo el contenido vive en `JUEGO.unity`** — una sola escena con todo adentro. Para que cada cuarto tenga su historia propia y una carga liviana, hay que separarlos.

**Primero, la arquitectura de contenido `[David]`:**
- **[P]** Que cada cuarto sea **su propia escena**, con sus propios objetos, arte y audio — no una variante de la misma. Así cargar un cuarto no arrastra el contenido de los otros.
- **[P]** Que lo compartido (fósforo, miedo, Presencia, pausa, HUD) viva en un solo lugar reutilizable, en vez de copiarse por escena. Si cada cuarto duplica esos sistemas, mantenerlos se vuelve inviable.
- **[P]** Que la historia de cada cuarto sea **dato, no código**: la leyenda, sus textos y sus objetos deberían poder escribirse sin tocar scripts.

**Después, el contenido `[Anavi]`:**
- **[A]/[D]** Escenario, objetos, leyenda y textos de cada cuarto.
- **[S]** Ambiente sonoro propio de cada uno.

**Estrategia: hacer primero un solo cuarto nuevo** (el 2, "El altillo olvidado") de punta a punta. Es la única forma real de saber si la arquitectura escala. Si ese cuarto cuesta mucho más de lo esperado, hay que arreglar la arquitectura **antes** de hacer los otros dos, no después.

**✅ Listo cuando:** se juegan los 4 cuartos en secuencia, cada uno con su leyenda y su ambiente, y agregar uno nuevo es sobre todo trabajo de contenido, no de sistemas.

**✅ Listo cuando:** se juegan los 4 cuartos en secuencia, cada uno desbloqueando el siguiente.

## Fase E — Entrega

- **[P]** Build de la plataforma que se haya decidido en #1, **probada en una máquina que no sea la de desarrollo**.
- **[D]** Página de itch.io (si aplica): capturas, gif, descripción, créditos.
- **[D]** Textos finales: revisar que no queden `TODO:` ni "New Text" en ninguna pantalla.

---

# 5. Lo que NO vamos a hacer

Sin esta lista, un mes se convierte en tres:

Múltiples escenarios por cuarto · combate · diálogos ramificados · múltiples finales por cuarto · movimiento en profundidad · multijugador · logros · voces grabadas · móvil · rehacer sistemas que ya funcionan solo porque los escribió otra persona.

---

# 6. Riesgos

| Riesgo | Cómo lo evitamos |
|---|---|
| **Volver a duplicar trabajo** (ya pasó una vez, costó días) | Reparto claro: Anavi arte/audio/contenido, David sistemas/integración. Antes de construir algo, chequear si ya existe del otro lado. |
| **Todo el contenido en una sola escena** no escala a 4 cuartos con historias propias | La Fase D empieza por la arquitectura, no por el contenido. Y se prueba con **un** cuarto antes de comprometerse con tres. |
| **El repo pesa 135 MB** por las builds duplicadas | Decisión tomada: se quedan. Si clonar o pushear se vuelve molesto, revisar. |
| ~~David no puede pushear al repo real~~ | ✅ resuelto: se trabaja todo en `Lager-GJ/main`, donde sí tiene permiso. |
| **Se agregan cuartos antes de que el primero esté pulido** | La Fase C va antes que la D a propósito. Un cuarto bueno vale más que cuatro a medias. |
| **WebGL se descubre tarde** | Si se elige WebGL, hacer un build de prueba en la Fase A, no en la E. El audio sin comprimir es el riesgo obvio. |

---

# 7. Cómo trabajamos

**Reparto:** Anavi hace arte, audio, contenido y escenas. David hace sistemas, integración, balanceo y build.

**Coordinación:** antes de tocar un sistema que ya existe, avisar. La duplicación de esta semana (dos barras de miedo, dos pausas, dos tutoriales) salió de no hacerlo.

**Repo:** todo va a **`Lager-GJ/main`**, rama `main`. Ahí David tiene permiso de escritura, así que no hay motivo para trabajar en ramas paralelas salvo que se quiera aislar algo grande. `Seanlim22004/Main1Escenario` es archivo histórico: sirvió para traer la versión de la que partimos y no se toca más.

**Verificar antes de afirmar:** las escenas `.unity` referencian scripts por **GUID**, no por nombre de clase. Que un archivo exista no significa que esté puesto en la escena. Se chequea con el GUID del `.meta`, no con `grep` del nombre.

---

*Documento vivo. Al cerrar cada fase, marcar lo hecho y anotar las perillas que cambiaron.*
