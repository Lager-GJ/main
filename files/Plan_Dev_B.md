# Plan de desarrollo — Dev B (Unity)
## Sistema: Estado de juego, miedo y datos

Trabajas en escena/scripts separados de Dev A y Dev C; integran en los checkpoints. Tu sistema es el "pegamento" que convierte los eventos de los otros dos en victoria, derrota y progreso real.

**Proyecto base:** `Main1Escenario/` (Unity 6, URP, Input System nuevo). Ya existen las escenas `Intro.unity` y `Historia.unity` con scripts de UI (`StoryManager`, `IntroStoryController`, `FadeText`) para la intro narrativa — tu trabajo va en `JUEGO.unity`, la escena de gameplay real. Nota: en ambos scripts de intro existentes, `SceneManager.LoadScene(...)` está comentado — probablemente serás tú quien conecte finalmente esa transición hacia `JUEGO.unity` y las pantallas de victoria/derrota.

---

## Día 1 — Mañana
- [ ] Barra de miedo: sube en oscuridad, baja levemente con luz encendida (versión base, todavía sin la Presencia).
- [ ] ScriptableObject de objeto: `sprite`, `nombre`, `descripcion`, `esObjetivo` — para que artistas y lead llenen datos sin tocar código. Esta es la estructura que Dev A consume para el panel de inspección.
- [ ] Máquina de estados básica: `Inicio -> Juego -> Derrota/Victoria -> Reinicio`.

## Día 1 — Tarde/Noche
- [ ] Condición de victoria: llave + caja de dulces encontradas (ambos `esObjetivo == true` recolectados/inspeccionados).
- [ ] Condición de derrota: barra de miedo al 100% (susto + reinicio).
- [ ] **Conectar la barra de miedo a la mecánica de la Presencia (Dev C):** la cercanía de la Presencia debe subir la barra más rápido. Esta conexión es la que hace que el reto oficial ("todo tiene un costo") sea mecánico y no solo narrativo — no te saltes este paso ni lo dejes para el final.
- [ ] Audio Manager con hooks a eventos: encender fósforo, acercamiento de la Presencia, miedo alto, victoria, derrota.

## Día 2 — Mañana
- [ ] Efectos de miedo en pantalla (viñeta/distorsión que empeora con el miedo).
- [ ] Integrar música y SFX finales en los hooks del Audio Manager (llegan de Diseñador C).
- [ ] Pantallas de inicio / victoria / derrota conectadas.

## Día 2 — Post Feature Freeze
- [ ] Balanceo con el lead/diseño: nº de fósforos, duración, velocidad de subida del miedo.
- [ ] Solo bugfix de estados y condiciones. Cero features nuevas.

---

## Interfaces con el resto del equipo
- **← Dev A:** no dependes directamente de su evento de fósforo (eso lo escucha Dev C), pero sí necesitas que tu barra de miedo reaccione a "hay luz encendida o no" — probablemente el mismo evento o estado que expone Dev A.
- **← Dev C (la Presencia):** consumes su contador/nivel de cercanía para acelerar la subida de la barra de miedo. Esta es tu conexión más importante — verifica en cada checkpoint que de verdad esté cableada, no solo que ambos sistemas "funcionen por separado".
- **→ Dev A:** tu máquina de estados puede necesitar desactivar la interacción de fósforos de Dev A cuando el juego pasa a Victoria/Derrota.
- **→ Todos:** el ScriptableObject de datos de objeto es el contrato que usan Diseñador A (sprites) y el lead (textos) para poblar contenido sin tocar código — mantenlo estable una vez definido.

## Riesgos que te tocan directamente
- "La mecánica de costo queda solo como susto sin afectar reglas reales" → tu barra de miedo/condición de derrota DEBE modificarse directamente por la cercanía de la Presencia, no solo un sonido o imagen. Valida con la frase: "Cuando el jugador enciende un fósforo, inmediatamente ______".
- Conflictos de merge: trabaja en tu propia escena de prueba (estado/miedo), separada de la principal. Integra solo en checkpoints.
- Dificultad frustrante: fósforos, velocidad de miedo y ritmo de la Presencia son perillas — ajústalas con playtest al final (M3 en adelante), no antes.

## Checkpoints (recordatorio de hitos globales)
- **M1** (Día 1, mediodía): vertical slice — tu barra de miedo base debe existir aunque sea con cubos.
- **M2** (Día 1, tarde): 2 objetos objetivo + victoria; barra de miedo conectada a la Presencia + derrota.
- **M3** (Día 1, noche): checkpoint — el costo (Presencia) se percibe jugando, no solo leyendo. Ajustar números.
