# Plan de desarrollo — Dev A (Unity)
## Sistema: Fósforo, Luz e Interacción

Eres el corazón del juego. Empiezas aquí desde el minuto uno — sin tu sistema no hay vertical slice.

**Proyecto base:** `Main1Escenario/` (Unity 6, URP, Input System nuevo). Ya existen las escenas `Intro.unity` y `Historia.unity` con scripts de UI (`StoryManager`, `IntroStoryController`, `FadeText`) para la intro narrativa — tu trabajo va en `JUEGO.unity`, la escena de gameplay real.

---

## Objetivo de tu sistema
El truco clave: **la luz ES el cursor**. "Objeto iluminado" = "objeto bajo el cursor mientras hay un fósforo encendido". El click solo necesita comprobar si hay un fósforo activo — **no construyas detección de iluminación por radio**, es sobre-ingeniería y el mayor riesgo de perder tiempo.

## Día 1 — Mañana (prioridad máxima)
- [ ] Validar la luz: `Light2D` (URP) o sprite radial aditivo + overlay negro que sigue el cursor. Decide en la primera hora.
  - Si a los 30-40 min URP 2D lighting no corre bien → pasa al plan B (overlay negro + sprite radial aditivo). No mires atrás.
- [ ] Sistema de fósforos: contador limitado (default 8-10, será perilla de dificultad), click enciende, temporizador de quemado, apagado automático.
- [ ] Regla de interacción: click sobre un objeto **solo válido** si hay un fósforo encendido.
- [ ] Objeto clickeado pasa a primer plano (escala/posición) + abre panel de inspección.

## Día 1 — Tarde/Noche
- [ ] Panel de inspección conectado a los datos del objeto (nombre + descripción) — estructura de datos la define Dev B (ScriptableObject de objeto).
- [ ] Pausar el consumo del fósforo mientras se inspecciona (regla de MVP).
- [ ] Feedback de encender/apagar fósforo (partícula o hook de sonido — el sonido real lo entrega Diseñador C).
- [ ] **Emitir el evento `FosforoEncendido`** (y `FosforoApagado`) para que Dev C conecte la mecánica de la Presencia. Este evento es la interfaz crítica entre tu sistema y el de Dev C — defínelo temprano y no lo cambies sin avisar.
  - Ya existe el contrato en `Assets/Script/Core/GameEvents.cs` (namespace `Terror`): llama a `GameEvents.RaiseFosforoEncendido()` cuando enciendas el fósforo y `GameEvents.RaiseFosforoApagado()` cuando se apague. Dev C ya está escuchando este evento (`Presencia/PresenciaController.cs`) — no dupliques el evento ni crees uno nuevo.

## Día 2 — Mañana
- [ ] Integrar sprites finales (fósforo/matchbox de Diseñador A) y la UI del panel de inspección (entregada por Diseñador B).
- [ ] Pulir sensación de la luz: radio, intensidad, flicker (coordinar con Diseñador C para el "efecto vela").

## Día 2 — Post Feature Freeze
- [ ] Solo bugfix del sistema de luz/interacción. Cero features nuevas.
- [ ] Juice: temblor de luz, transición al inspeccionar.

---

## Interfaces con el resto del equipo
- **→ Dev C (la Presencia):** emites el evento de "fósforo encendido" (contador de fósforos usados, o simplemente el evento en sí). Dev C escucha esto para subir su contador de cercanía.
- **← Dev B (datos y estado):** consumes el ScriptableObject de objeto (sprite, nombre, descripción, esObjetivo) para poblar el panel de inspección.
- **← Dev B (máquina de estados):** tu sistema de fósforos/interacción debe poder ser desactivado por Dev B cuando el juego entra en estado Victoria/Derrota.

## Riesgos que te tocan directamente
- Configurar URP 2D lighting se come tiempo → mitigación: overlay negro + sprite radial si no corre rápido.
- Sobre-ingeniería de "detección de iluminado" → la luz ES el cursor, no construyas detección por radio.
- Conflictos de merge: trabaja en tu **propia escena de prueba** (fósforo/luz) separada de la escena principal. Integra solo en los checkpoints (M1, M2, M3...), nunca de forma continua sobre `JUEGO.unity`.

## Checkpoints (recordatorio de hitos globales)
- **M0** (Día 1, fin hora 1): decisión de luz tomada, una luz sigue el cursor sobre un cuarto gris.
- **M1** (Día 1, mediodía): vertical slice jugable con cubos — fósforo limitado → luz temporal → click objeto iluminado → panel con texto.
- **M2** (Día 1, tarde): loop completo con 2 objetos objetivo + victoria.
- **M3** (Día 1, noche): checkpoint — el loop se siente bien.
