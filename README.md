# Lumbre — Los secretos de la casa

Juego de terror 2D hecho en Unity 6 (`6000.3.14f1`). Un niño explora la casa de su abuela de noche con una caja de fósforos limitada: cada fósforo da luz para explorar pero acerca a algo. Estudio: **LAGER**.

## Estado del proyecto

**"Los secretos de la casa"**: un menú de una sola casa con **4 cuartos** que se desbloquean en secuencia (terminar uno desbloquea el siguiente):

1. **La habitación prohibida** (`L1_CajaFosforos`) — desbloqueado por defecto. La mecánica original de fósforos/miedo.
2. **El altillo olvidado** (`L2_Cantuna`)
3. **El recibidor del velo** (`L3_DamaTapada`)
4. **El patio de la procesión** (`L4_PadreAlmeida`) — último, no desbloquea nada más.

Cada cuarto muestra uno de 3 estados en su tarjeta de menú: **bloqueada** (candado), **activa** ("ENFRÉNTALO"), o **completada** ("ESCAPASTE", rejugable).

## Repositorio

Este directorio (`final/Main1Escenario/`) es el repo real y activo. Todo el trabajo va a `Lager-GJ/main` (`origin`, remoto de trabajo). `seanlim-viejo` es un archivo histórico de solo lectura, no usar.

## Documentación

- **`PLAN_MAESTRO.md`** — fuente de verdad para alcance y prioridades del proyecto.
- **`../CLAUDE.md`** y **`../final/CLAUDE.md`** — guía técnica detallada para trabajar en el código: arquitectura, convenciones de escenas, gotchas conocidos.
- **`GUIA_CUARTO_2_PATIO.md`** — notas específicas del cuarto 2.

## Arquitectura (resumen)

```
Assets/_Project/
  Core/          — shell persistente: boot, guardado, audio, ruteo entre escenas, estado de juego, UI de menú
  Data/          — catálogo de leyendas/cuartos (ScriptableObjects)
  Leyendas/L1_CajaFosforos/  — contenido del cuarto 1 (fósforos, miedo, presencia, historia)
```

Ver `final/CLAUDE.md` para el detalle completo (namespaces, gotchas de edición de escenas YAML, archivos Latin-1, etc.).

## Plataforma

WebGL (itch.io). El guardado usa `PlayerPrefs` (no I/O de archivos) por el flush asíncrono de IndexedDB en WebGL.

## Comandos

No hay pipeline de build/test/lint por CLI. El proyecto se abre y prueba desde el Editor de Unity 6. La serialización de texto forzado está activada, así que los `.unity`/`.asset` son YAML diffable.
