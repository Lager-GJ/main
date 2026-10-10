# Flujo de Git — LAGER / "Los secretos de la casa"

_Reorganización del 2026-08-29, al retomar el proyecto con el equipo completo (7 personas)._

## Repositorio

| | |
|---|---|
| **Remoto de trabajo** | `origin` → `git@github.com:Lager-GJ/main.git` |
| **Rama tronco** | `main` |
| **Carpeta del proyecto** | `final/Main1Escenario/` |
| **Unity** | `6000.3.14f1` |
| **Remoto histórico (solo lectura)** | `seanlim-viejo` → `https://github.com/Seanlim22004/Main1Escenario.git` — no pushear ahí |

## Equipo y ramas

`main` es tronco: **nadie commitea ni pushea directo**. Cada quien tiene una rama personal larga, sacada de `main`.

| Persona | Rol | Rama |
|---|---|---|
| David | dev | `dev/david` |
| Diego | dev | `dev/diego` |
| Vicky | dev | `dev/vicky` |
| Cris | dev | `dev/cris` |
| Emily | diseño | `arte/emily` |
| Gaby | diseño | `arte/gaby` |
| Sari | diseño | `arte/sari` |

## Limpieza de ramas hecha

Ramas remotas borradas (ya fusionadas o históricas):

| Rama borrada | Motivo | Respaldo |
|---|---|---|
| `integracion-anavi` | ya estaba fusionada en `main` (fue promovida a `main` el 2026-08-01) | está en `main` |
| `dev-b` | época del game jam | tag `backup-antes-de-david-2026-07-26` |
| `feature/panel-inspeccion-objetos` | 10 commits antiguos, todos ya contenidos en el tag | tag `backup-antes-de-david-2026-07-26` |
| `respaldo-main-vieja-2026-08-01` | respaldo del `main` paralelo previo | tag `backup-main-vieja-2026-08-01` (creado antes de borrar) |

No se perdió nada: todo lo recuperable quedó en tags.

**Tags de respaldo en el remoto:**
- `backup-antes-de-david-2026-07-26` — trabajo de compañeros (Seanlim22004 / Desu) previo al primer force-push.
- `backup-main-vieja-2026-08-01` — versión paralela que era `main` antes de promover `integracion-anavi`.

Respaldos locales solo en el clon de David (no están en el remoto): `backup-antes-de-reorganizar`, `respaldo-main-vieja-local`.

## Protección de `main` (GitHub)

Configurada el 2026-08-29 en Settings → Branches:

| Regla | Estado |
|---|---|
| PR obligatorio para mergear a `main` | ✅ |
| Aprobaciones requeridas | 1 |
| Resolución de conversaciones antes de mergear | ✅ |
| Force-push a `main` | 🚫 bloqueado |
| Borrar `main` | 🚫 bloqueado |
| La regla aplica a admins (David) | No — puede saltarse en emergencia |
| Checks de CI | ninguno (no hay pipeline) |

Quien tenga un clon viejo acostumbrado a `git push` directo a `main` va a empezar a recibir rechazo. Es esperado: hay que pasar por PR.

## Cómo trabajar (día a día)

### Primera vez — traer tu rama

```bash
cd final/Main1Escenario
git fetch origin
git checkout dev/tu-nombre      # o arte/tu-nombre
```

### Ciclo normal

```bash
# 1. Partir siempre de main actualizado
git checkout main
git pull origin main

# 2. Volver a tu rama y traer los últimos cambios de main
git checkout dev/tu-nombre
git merge origin/main           # (o: git rebase origin/main)

# 3. Trabajar, commitear
git add .
git commit -m "Descripción clara del cambio"

# 4. Subir tu rama
git push origin dev/tu-nombre
```

### Integrar a `main`

1. En GitHub, abrir un **Pull Request** de `dev/tu-nombre` → `main`.
2. Otra persona revisa, deja comentarios y **aprueba** (1 aprobación mínima).
3. Resolver todas las conversaciones del PR.
4. **Merge** desde la interfaz de GitHub.
5. Después del merge, actualizar tu rama local:
   ```bash
   git checkout main && git pull origin main
   git checkout dev/tu-nombre && git merge origin/main
   ```

## Reglas del equipo

- **Nunca** `push` directo a `main` — siempre PR.
- Mantener tu rama al día con `main` al menos una vez al día para evitar conflictos grandes.
- Antes de construir un sistema nuevo, revisar si alguien ya lo hizo en otra rama (el mayor riesgo del proyecto es duplicar trabajo — ya pasó antes y costó días).
- Los `.unity` / `.asset` son YAML diffable (Force Text ON). Aun así, coordinar quién toca qué escena para no chocar.
- Commits en español, mensaje claro de qué cambió.
