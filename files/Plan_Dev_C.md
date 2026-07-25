# Plan de desarrollo — Dev C (Unity)
## Sistema: La Presencia (mecánica de costo del reto)

Rol nuevo y crítico: implementas el sistema que hace que **"todo tenga un costo"** — este es el sistema que el jurado va a evaluar más de cerca, porque es la respuesta directa al reto oficial de la game jam. Trabajas en escena/script separado, y te conectas a Dev A (evento de fósforo) y Dev B (barra de miedo).

**Proyecto base:** `Main1Escenario/` (Unity 6, URP, Input System nuevo). Ya existen las escenas `Intro.unity` y `Historia.unity` con scripts de UI para la intro narrativa — tu trabajo va en `JUEGO.unity`, la escena de gameplay real.

## Código ya implementado (punto de partida)
Ya existe una primera versión funcional en `Assets/Script/`:
- **`Core/GameEvents.cs`** — el contrato de eventos estático compartido entre Dev A, B y C (namespace `Terror`). Define `OnFosforoEncendido`, `OnFosforoApagado` (los dispara Dev A) y `OnCercaniaPresenciaCambiada(int nivel, float multiplicador)` (lo disparas tú). Nadie tiene referencia directa a la escena de otro dev — solo se conectan a través de estos eventos.
- **`Presencia/PresenciaController.cs`** — tu sistema: escucha `OnFosforoEncendido`, sube `NivelActual` (fijo o con `probabilidadDeAvance`), y dispara `OnCercaniaPresenciaCambiada` con el multiplicador de miedo correspondiente (array `multiplicadorMiedoPorNivel`, configurable en el Inspector). Tiene `ReiniciarPresencia()` para cuando Dev B reinicie el juego.
- **`Presencia/PresenciaTestTrigger.cs`** — simulador temporal del fósforo de Dev A (tecla `Espacio` dispara `GameEvents.RaiseFosforoEncendido()`), para que puedas probar tu sistema en tu propia escena de prueba **antes** de que el sistema real de Dev A exista. Bórralo de la escena principal una vez integrado el sistema real (déjalo solo en tu escena de prueba si quieres seguir usándolo).

Pendiente para ti: el feedback perceptible (sonido/silueta/HUD) y el pulido de timing siguen siendo tuyos — el script actual solo loguea el nivel por consola (`Debug.Log`).

---

## El reto oficial que debes cumplir
> Cuando el jugador enciende un fósforo (beneficio: luz para buscar), inmediatamente aumenta el riesgo de que la Presencia se acerque (costo: mayor riesgo de derrota).

Frase de validación a repetir a los mentores: *"Cuando el jugador enciende un fósforo, inmediatamente aumenta el riesgo de que la Presencia se acerque y la barra de miedo suba más rápido."*

Debe ser **mecánico, no solo narrativo**: la Presencia necesita un contador/estado real que el jugador pueda rastrear (sonido, silueta, contador de acercamientos), y que afecte directamente la barra de miedo / condición de derrota (de Dev B).

## Día 1 — Mañana (prioridad alta, junto con Dev A)
- [ ] Diseñar el contador/estado de "cercanía de la Presencia" (ej. 0 a 5 niveles de acercamiento).
- [ ] Escuchar el evento `FosforoEncendido` de Dev A: cada fósforo sube el contador (fijo o probabilístico — decide y documenta cuál).
- [ ] Definir qué pasa en cada nivel: nivel bajo = ambiente normal; nivel alto = riesgo de derrota inminente.

## Día 1 — Tarde/Noche
- [ ] Conectar el contador de la Presencia a la barra de miedo de Dev B: cercanía alta = miedo sube más rápido. **No implementes esto solo como un efecto visual/sonoro aislado — debe llamar/afectar la lógica de Dev B directamente.**
- [ ] Feedback perceptible del avance: sonido que se acerca, silueta breve en la periferia, o contador visible en HUD (aunque sea texto de debug al inicio).
- [ ] Probar que el jugador SIENTA el trade-off: más fósforos = más seguro para buscar pero más riesgo de la Presencia.

## Día 2 — Mañana
- [ ] (Opcional / diversificador) Dejar una marca visual persistente donde "estuvo" la Presencia (silla movida, puerta entreabierta, objeto caído) — coordina con Diseñador A/C, solo si sobra tiempo.
- [ ] Pulir el timing: que la amenaza se sienta justa, no aleatoria o injusta.
- [ ] Integrar sonido/arte final de la Presencia con Diseñador C (silueta/sombra sutil, no un personaje completo).

## Día 2 — Post Feature Freeze
- [ ] Balanceo fino junto con Dev B y el equipo de diseño: cuántos fósforos hasta que la Presencia sea crítica.
- [ ] Solo bugfix de esta mecánica. Cero features nuevas — es la más visible para el jurado, no la conviertas en el lugar donde experimentas de último minuto.

---

## Interfaces con el resto del equipo
- **← Dev A:** escuchas su evento `FosforoEncendido` (y posiblemente `FosforoApagado`). No implementes tu propia detección de si hay luz encendida — usa el evento que Dev A expone, para no duplicar lógica ni desincronizarte.
- **→ Dev B:** expones tu contador/nivel de cercanía de la Presencia para que Dev B lo use al calcular la velocidad de subida de la barra de miedo. Verifica en cada checkpoint que esta conexión realmente mueva la barra de miedo en el juego, no solo en teoría.
- **→ Diseñador C:** tu sistema necesita hooks de audio (sonido de acercamiento en distintos niveles de intensidad) y coordinación de silueta/arte con Diseñador A.

## Riesgos que te tocan directamente
- "La mecánica de costo queda solo como un susto sin afectar reglas reales" → es el riesgo #1 de tu sistema. Debe modificar directamente la barra de miedo o la condición de derrota de Dev B — valida siempre con la frase del reto.
- Dificultad frustrante o Presencia injusta/aleatoria → fósforos, velocidad de miedo y ritmo de la Presencia son perillas de balanceo; ajústalas con playtest (M3 en adelante), no antes ni por intuición.
- Conflictos de merge: trabaja en tu propia escena de prueba (Presencia), separada de la principal. Integra solo en los checkpoints.

## Checkpoints (recordatorio de hitos globales)
- **M1** (Día 1, mediodía): la Presencia debe estar conectada desde este hito — aunque sea solo un contador visible en pantalla que sube con cada fósforo. No la dejes para el final.
- **M2** (Día 1, tarde): barra de miedo (Dev B) conectada a la Presencia + derrota.
- **M3** (Día 1, noche): checkpoint clave para ti — el costo debe **sentirse jugando**, no solo describirse. Si no se siente, este es el momento de ajustar, no el Día 2.
