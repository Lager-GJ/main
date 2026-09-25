# Guía — El armario en primer plano

> **Escena:** `JUEGO.unity`
> **Implementado:** 2026-09-23
> **Estado:** funcionando con el arte actual (`ArmAbr.png`, una sola imagen). Para más dinamismo falta arte por capas (ver "Qué falta de arte").

Cuando el jugador abre el armario se pasa a una vista a pantalla completa con solo el armario. Adentro hay zonas clicables (ropa, puertas, cajón) y la lata de galletas, que es el objetivo de victoria del cuarto 1, escondida en el cajón.

---

## Cómo funciona

1. El botón `ArmarioB` (en el mundo) tiene `BotonLuzRequerida`: solo responde con el fósforo encendido. Su `OnClick` llama a `ControladorArmario.AlternarArmario()`.
2. `ControladorArmario` le pide a `PanelInspeccion` que muestre `PanelArmarioPrimerPlano`. Si la vista tiene `TransicionPanel`, entra con fade + un pequeño "pop" de escala (0.3 s).
3. Se cierra de tres maneras: click en el fondo oscuro, `Escape`, o **cuando se apaga el fósforo** (sin luz no se ve el interior). Sale con un fade corto (0.15 s).
4. **Mientras corre la secuencia de la lata no se puede cerrar** (`PanelInspeccion.CierreBloqueado`). Si se cerrara, la coroutine que dispara la victoria moriría con la vista.

El fósforo **no se pausa** dentro del armario: rebuscar cuesta luz, igual que el resto de la tensión.

## Jerarquía en la escena

```
Canvas (Screen Space Overlay, 1920×1080)
└─ PanelArmarioPrimerPlano     ← estirado a pantalla completa, fondo casi negro
   │                              Image + Button(→ PanelInspeccion.Cerrar) + CanvasGroup + TransicionPanel
   ├─ ArmarioInterior          ← 900×900, sprite ArmAbr. VistaArmario + AudioSource + Button "sumidero"
   │  ├─ Prenda_Vestido, _Camisita, _Abrigo, _Delantal, _Bufanda, _Traje
   │  ├─ Puerta_Izquierda, Puerta_Derecha
   │  ├─ Cajon
   │  └─ LataGalletas          ← empieza APAGADA; la revela el Cajon
   ├─ TextoChisteLata          ← abajo; lo usa SecuenciaAperturaLata
   └─ TextoPista               ← arriba; una línea por click en un hotspot
```

- El panel está justo **después de `OSCURO`** entre los hijos del Canvas: así la oscuridad por miedo no lo oscurece. Consecuencia: la barra de miedo y el emoji quedan tapados mientras el armario está abierto.
- El "sumidero" (un `Button` sin acciones en `ArmarioInterior`) existe para que un click sobre el dibujo **no** cierre el panel. Solo el fondo oscuro lo cierra.
- ⚠️ **No pongas esta vista en un Canvas de mundo.** La cámara se mueve (`CamaraController`) y una vista en mundo no la sigue; así estuvo al principio y se veía enorme y descentrada.

## Las zonas clicables

Cada zona es un `Image` casi transparente con `HotspotArmario`. Al pasar el mouse se ilumina; al click: flash, sonido, línea de texto (en orden, da la vuelta), sacudida del armario y, opcional, un cambio de miedo.

| Zona | Sonido | Sacudida (px) | Miedo |
|---|---|---|---|
| Vestido | explorar objeto | 3 | 0 |
| Camisita | explorar objeto | 3 | 0 |
| Abrigo | explorar objeto | 5 | +5 |
| Delantal | explorar objeto | 3 | 0 |
| Bufanda | explorar objeto | 3 | +3 |
| Traje | explorar objeto | 5 | +4 |
| Puertas (×2) | puerta abrir / cerrar | 6 | 0 |
| Cajón | cajón abrir / cerrar | 8 | 0 — **revela la lata** |

Recorrer toda la ropa suma +12 de miedo. Los valores son decisión de diseño provisional; se cambian en el campo `miedo` de cada zona (negativo = alivio).

Las zonas ignoran los clicks si el fósforo está apagado o si corre la secuencia de la lata.

## Cómo agregar o cambiar una zona

1. Duplicá uno de los hijos de `ArmarioInterior` (por ejemplo `Prenda_Vestido`).
2. Ubicalo con su `RectTransform`. Si medís sobre el sprite (626×626 px, origen arriba a la izquierda), la escala a pantalla es **900 / 626 ≈ 1.4377**. Centro: `x = (xc − 313) × 1.4377`, `y = (313 − yc) × 1.4377`.
3. Completá `HotspotArmario`: `textos`, `sonido` (y `sonidoSiguientes` si el 2.º click suena distinto), `sacudida`, `miedo`, y `revelar` si tiene que aparecer algo.
4. Que el `Image` quede con **Raycast Target** activo y alfa 0 (el script maneja el resalte).

Otras cosas que se ajustan en el Inspector: duración del fade y escala inicial en `TransicionPanel`; duración del texto y de la sacudida en `VistaArmario`; posición y tamaño de la lata en `LataGalletas`.

## La lata

`LataGalletas` es un `Image` + `Button` (→ `ObjetoInteractivo.InteractuarManual`) con `SecuenciaAperturaLata`. Al click: la tapa tiembla, se cambia al sprite abierto (hilos y agujas), se muestra el chiste y recién ahí se llama a `Ganar()`. Si el miedo llega a 100 durante la secuencia, se corta y se pierde.

Unity avisa `ObjetoInteractivo ... possibly missing Required Components` porque la lata ya no tiene `Collider2D` (usa el `Button`). **Es esperado y no afecta.**

## Qué falta de arte

Hoy el armario es una sola imagen, por eso todo el feedback es brillo + sacudida + texto. Para que se sienta vivo hace falta pedir a diseño (Emily, Gaby, Sari) el armario **por capas**:

- cada prenda por separado (para que se balanceen);
- el cajón abierto (sprite alternativo);
- puertas por separado.

`CajonAbr.png` / `Cajoncerr.png` son del **velador**, no del armario.

## Al editar esta escena desde afuera del Editor

Si Unity tiene `JUEGO.unity` abierto, su copia en memoria queda vieja. **No guardes**: recargá la escena (File → Open Scene → `JUEGO`) y elegí **Don't Save**. Si guardás, Unity pisa los cambios.
