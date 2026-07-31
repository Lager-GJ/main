# Plan Maestro — Lumbre
## De dos prototipos separados a un juego terminado

**Producto:** juego de terror 2D en primera persona fija. Un niño en la casa de su abuela, de noche, con una caja de fósforos limitada. Cada fósforo da luz para explorar pero acerca a algo. Estudio: **LAGER**.

**Equipo:** David (sistemas, integración, balanceo) + Anavi (arte, audio, contenido, escenas). Los dos programan con asistencia de IA.

**Plazo:** entrega de clase/jam, ~1 mes o más desde 2026-07-31.

**Plataforma:** ⚠️ **sin definir** — hay builds de Windows (`.exe`) hechas, y el plan original apuntaba a WebGL/itch.io. Ver §3, decisión abierta #1.

---

# 1. Dónde estamos hoy (verificado en código y escenas, 2026-07-31)

El proyecto pasó por una etapa confusa: **dos personas construyeron el mismo juego en paralelo, sin saberlo**, en copias distintas del repo. Eso ya se resolvió: la versión de Anavi resultó sustancialmente más completa, y es la base actual.

**Rama de trabajo: `integracion-anavi`** en `final/Main1Escenario`. Parte del commit `a400b4b` de Anavi, más el shell de menú que aportamos encima.

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

- **El menú de cuartos ("Los secretos de la casa")**: el código está portado (`Assets/Script/Shell/`) pero **nada está colocado en ninguna escena**, ni existen los assets de datos. Ver §4, Fase B.
- **Tutorial duplicado**: `TutorialManager.cs` y `ManagerTutorial.cs` hacen casi lo mismo (los dos pausan con `Time.timeScale`, los dos muestran un panel tras una espera) y **los dos están colocados**. Uno sobra.
- **`JUEGO 1.unity`** existe pero no está en Build Settings — es un backup/experimento, no el flujo activo.
- **`PanelInspeccion.cs`** sigue en el proyecto sin que nadie lo use.

---

# 2. Las reglas del juego (mecánica confirmada)

Esto no cambió y sigue vigente:

1. **Encender un fósforo → alivio real.** La barra de miedo **baja** activamente mientras arde. Encender es un refugio momentáneo.
2. **El fósforo se apaga → el miedo empieza a subir**, cada vez más rápido según qué tan cerca esté la Presencia (multiplicador).
3. **Sin fósforo encendido, no se puede interactuar** con los objetos del escenario.
4. **Cada fósforo encendido acerca a la Presencia** — ese es el costo.
5. Los **ítems de supervivencia** encontrados reducen un 30% la velocidad de subida del miedo (acumulativo).

| Perilla | Valor hoy | Valor de diseño | Estado |
|---|---|---|---|
| Fósforos totales | **4** | 5 | ⚠️ desalineado |
| Duración de un fósforo | **5 s** | ~20 s | ⚠️ muy corto |
| Ítems para ganar | 3 | 3 | ✅ |
| Miedo: velocidad de subida | 5/s × multiplicadores | — | a balancear |
| Miedo: velocidad de bajada | 8/s | — | a balancear |

El desfase de fósforos viene de que el ajuste a 5/20s se hizo en la otra rama y no llegó a esta. **No es un simple "arreglarlo"** — con 5 segundos el juego es mucho más frenético, y puede que Anavi haya balanceado el resto del contenido alrededor de ese valor. Ver §3, decisión #3.

---

# 3. Decisiones abiertas (resolver antes de avanzar mucho)

Estas no son técnicas, son de producto. **Cada una bloquea o cambia parte del plan.**

### #1 — ¿WebGL, Windows, o los dos?
Hay builds de Windows commiteadas y el plan viejo decía WebGL/itch.io. Cambia bastante: WebGL obliga a cuidar el peso de audio/texturas (hoy hay ~18 `.wav` sin comprimir) y tiene sus propios dolores de carga. **Impacto: alto.** Decidir pronto.

### #2 — ¿Cuál es la condición de victoria real?
Hoy hay **dos caminos independientes**, cualquiera dispara la victoria:
- Juntar los 3 `ItemSupervivencia` → `InventarioSupervivencia.RecolectarItem()` llama a `Ganar()`.
- Agarrar la Llave (`esObjetivoDeVictoria: 1`) → `ObjetoInteractivo` llama a `Ganar()`.

Encima, `CondiciondeVictoria.cs` intercepta la victoria y **la convierte en derrota** si no quedan fósforos o el miedo llegó a 100. Probablemente la Llave sea residuo del sistema viejo y los ítems sean lo nuevo, pero hay que confirmarlo con Anavi. **Impacto: alto** — define de qué se trata el juego.

### #3 — ¿Fósforos 4/5s o 5/20s?
Ver la tabla de §2. Hay que jugarlo y decidir, no resolverlo por documento.

### #4 — ¿Qué hacemos con los 492 MB de builds en el repo?
`EXE/` y `Ejecutable/` son **la misma build de Windows, duplicada**, 246 MB cada una. El repo git pesa 135 MB y GitHub ya avisa por archivos de +50 MB. Cada clon y cada push se vuelven lentos. Recomendación: sacarlas del control de versiones (agregar al `.gitignore`) y distribuir las builds por otro lado (Drive, itch.io, releases de GitHub). **Impacto: medio** — no rompe nada, pero empeora con el tiempo.

### #5 — Acceso al repo compartido
David **no tiene permiso de escritura** en `Seanlim22004/Main1Escenario` (el repo real del equipo). Hoy se trabaja con un respaldo en `Lager-GJ/main`. Hay que resolverlo (pedir acceso, o acordar que Anavi integre) o el trabajo va a seguir viviendo en dos lugares.

---

# 4. El plan

Ordenado por prioridad, no por semanas fijas: primero lo que **desbloquea o arregla**, después lo que **agrega**.

## Fase A — Cerrar decisiones y arreglar lo torcido `[David + Anavi]`

*Objetivo: que no haya ambigüedad sobre qué es el juego, y que lo que hay funcione bien.*

- **[D]** Resolver las decisiones #1, #2 y #3 de §3 (conversación con Anavi + jugarlo).
- **[P]** Según #2: dejar **un solo** camino de victoria y borrar/desactivar el otro.
- **[P]** Según #3: alinear los números del fósforo, en código y en la escena.
- **[P]** Eliminar la duplicación de tutorial (`TutorialManager` vs `ManagerTutorial`): elegir uno, sacar el otro de la escena y del proyecto.
- **[P]** Sacar las builds del repo (#4) y agregar `EXE/`, `Ejecutable/` al `.gitignore`.
- **[P]** Limpiar lo muerto: `PanelInspeccion.cs` si sigue sin usarse, `JUEGO 1.unity` si es backup.

**✅ Listo cuando:** se puede jugar de principio a fin, hay una sola forma de ganar, y nadie duda de qué versión de qué sistema está activa.

## Fase B — El menú de cuartos `[David]`

*Objetivo: que el juego se sienta un producto con progresión, no una escena suelta.*

El código ya está (`Assets/Script/Shell/`). Falta el trabajo de Editor. **Va en `Nivel.unity`**, que ya es la pantalla de selección — no hay que crear escenas nuevas.

- **[P]** Crear los 4 assets `LeyendaDefinicion` + el `CatalogoLeyendas`, encadenados por `siguienteLeyenda`.
- **[P]/[U]** Armar las 4 tarjetas en `Nivel.unity` con `MenuTarjetaLeyenda` (estados: bloqueada con candado / **ENFRÉNTALO** / **ESCAPASTE**).
- **[P]** Colocar `MenuPrincipal` y el `AudioManager` del shell.
- **[P]** Colocar `L1Controller` en `JUEGO.unity` para que ganar marque el cuarto como completado y desbloquee el siguiente.
- **[A]** *(Anavi)* portadas de los cuartos 2-4 sin el candado dibujado encima (las actuales lo tienen incrustado en la ilustración).

**✅ Listo cuando:** entrás a Niveles, ves los 4 cuartos, solo el primero se puede jugar, y al ganarlo el segundo se desbloquea y queda así aunque cierres el juego.

## Fase C — Pulido del cuarto 1 `[David + Anavi]`

*Objetivo: que el primer cuarto esté realmente terminado, no solo funcional.*

- **[D]/[P]** **Balanceo con playtests reales**: 3-5 personas ajenas al proyecto. ¿Se gana muy fácil? ¿El miedo sube demasiado rápido? Solo tocar números, cero features nuevas.
- **[D]** Validar que se entienda el costo sin explicarlo: que digan solos *"encender fósforos es peligroso, pero quedarse a oscuras también"*.
- **[P]** Bug bash de casos borde: pausar durante una interacción, alt-tab, reiniciar tras derrota, salir al menú a media partida, spamear clicks, resolución distinta.
- **[U]** Legibilidad con brillo normal de monitor, no el tuyo calibrado.
- **[S]** *(Anavi)* revisar la mezcla de audio: que nada tape a nada, que el jumpscare no reviente los parlantes.

**✅ Listo cuando:** un desconocido lo juega completo sin ayuda y sin que truene.

## Fase D — Contenido: los cuartos 2 a 4 `[Anavi contenido + David sistemas]`

*Objetivo: que el juego tenga la duración que promete el menú.*

Acá es donde se ve si valió la pena la arquitectura: si el cuarto 1 está bien armado, cada cuarto nuevo debería ser sobre todo **contenido**, no sistemas nuevos.

- **[P]** Primero: hacer **un solo** cuarto nuevo (el 2, "El altillo olvidado") como prueba de que escala. Si cuesta mucho más de lo esperado, hay que arreglar la arquitectura antes de hacer los otros dos.
- **[A]/[D]** *(Anavi)* escenario, objetos e historia de cada cuarto.
- **[P]** Cablear cada cuarto al catálogo y al desbloqueo secuencial.

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
| **Las decisiones abiertas de §3 se arrastran** | Son lo primero de la Fase A. Un juego con dos condiciones de victoria no se puede balancear. |
| **El repo se vuelve inmanejable** por las builds | Sacarlas del control de versiones en la Fase A, no "después". |
| **David no puede pushear al repo real** | Resolver #5 pronto o el trabajo se sigue fragmentando en dos remotos. |
| **Se agregan cuartos antes de que el primero esté pulido** | La Fase C va antes que la D a propósito. Un cuarto bueno vale más que cuatro a medias. |
| **WebGL se descubre tarde** | Si se elige WebGL, hacer un build de prueba en la Fase A, no en la E. El audio sin comprimir es el riesgo obvio. |

---

# 7. Cómo trabajamos

**Reparto:** Anavi hace arte, audio, contenido y escenas. David hace sistemas, integración, balanceo y build.

**Coordinación:** antes de tocar un sistema que ya existe, avisar. La duplicación de esta semana (dos barras de miedo, dos pausas, dos tutoriales) salió de no hacerlo.

**Ramas:** el trabajo va en `integracion-anavi` hasta que se resuelva #5. Cuando se resuelva, acordar cómo se integra al repo real.

**Verificar antes de afirmar:** las escenas `.unity` referencian scripts por **GUID**, no por nombre de clase. Que un archivo exista no significa que esté puesto en la escena. Se chequea con el GUID del `.meta`, no con `grep` del nombre.

---

*Documento vivo. Al cerrar cada fase, marcar lo hecho y anotar las perillas que cambiaron.*
