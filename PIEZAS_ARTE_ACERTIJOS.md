# Piezas de arte: acertijos del cuarto 1

Desde 2026-10-22 el cuarto 1 se resuelve con una cadena de acertijos:

> foto del campo → marco vacío → cajón secreto del secretero → agenda (pista) → candado del joyero → llave pequeña → lata de galletas → llave del armario → cajón del armario → llave de la puerta → salir.

Todo lo que se ve de esto es **provisorio**: se hizo en código, con recortes del fondo. Está en `Assets/Recursos/Acertijos/`. Para cambiar una pieza, **reemplazar el PNG con el mismo nombre**. Así se mantiene su `.meta` y el juego la toma sola, sin tocar la escena.

**Estilo:** el del cuarto, lápiz en grises, igual que `FondoDefi.png`, `ArmAbr.png` y `BuroAB.png`. Las tarjetas grandes siguen el estilo de las `Ni-img` de Historia, con borde blanco de tiza.

## 0. Cambio de perspectiva (2026-10-26)

Ya **no hay ventanas de primer plano**. Al tocar un mueble pasa esto:
1. La cámara se acerca al mueble del cuarto.
2. El mueble se abre **en su lugar**: se cambia el dibujo cerrado por el abierto, del mismo tamaño y en la misma posición.
3. Lo que tiene adentro se toca ahí mismo.

Por eso todas las piezas de muebles van **en la perspectiva y la escala del cuarto**, como `ArmCerr`/`ArmAbr`, `Cajoncerr`/`CajonAbr` y `BuroCerr`/`BuroAB`. Usan el mismo estilo: relleno casi negro, contorno negro grueso y vetas.

Hay dos cosas más:
- El juego oscurece todo con la luz real: el fósforo, la puerta y una luz suave sobre el mueble que se mira. Se dibuja sin sombras de luz propias.
- Al acercarse, la cámara amplía 4 veces en el velador y el joyero, y ~2 veces en el secretero. Cuanto más resolución tengan esas piezas, mejor se van a ver de cerca.

## 1. En el cuarto

| Archivo | Tamaño | Qué es / dónde va |
|---|---|---|
| `Secretero_Cerrado.png` | 380 × 700 | **Mueble nuevo**, entre la silla y la máquina de coser. Es el escritorio de la abuela, con casilleros arriba (2 filas × 3), una tapa inclinada con cerradura y abajo 2 cajones chicos y uno ancho. El casillero **del medio de abajo** es un panel liso: ahí está el cajón secreto. |
| `Secretero_Abierto.png` | 380 × 700 | El mismo secretero, **mismo tamaño y posición**, con la tapa bajada como escritorio (papeles, tintero, casilleritos). Las zonas para tocar están puestas sobre este dibujo: si se cambia la distribución, hay que mover las zonas en la escena. |
| `Secretero_CajonSecretoAbierto.png` | 120 × 110 | El cajón secreto salido hacia adelante desde el casillero del medio, visto un poco desde arriba. Adentro va la agenda. |
| `Marco_Vacio.png` | 160 × 210 | Portarretratos colgado sobre el secretero, **sin foto**. Se ve la marca de la foto que faltaba. |
| `Marco_ConFoto.png` | 160 × 210 | El mismo marco con la foto de la casa del campo. |
| `Foto_Campo_Piso.png` | 170 × 70 | La foto **acostada en el piso**, delante de la tele, en la perspectiva del piso. El ícono del inventario sigue siendo `Foto_Campo.png`, la foto de frente. |
| `Joyero_Cerrado.png` | 220 × 170 | Joyero sobre la cómoda, cerrado, con una **placa de candado de 4 números** en el frente. **Tapa la cajita que ya está dibujada abierta en `FondoDefi`**: tiene que cubrirla entera, incluida la tapa levantada. Los números los pone el juego encima. |
| `Joyero_Abierto.png` | 220 × 170 | El mismo joyero abierto, con terciopelo adentro. Ahí aparece la llavecita. |
| Velador | — | Ya está: `Cajoncerr.png` → `CajonAbr.png`. Los objetos del cajón (rosario, estampas, caja de fósforos, nota, carrete, venda) hoy son los dibujos vistos desde arriba, achatados para que parezcan acostados. **Faltan versiones en la perspectiva del cajón abierto**, y más chicas. |
| Armario | — | Ya está: `ArmCerr.png` → `ArmAbr.png`. La **lata** ahora va en el piso del armario, debajo de la ropa. Haría falta una lata cerrada con cerradura en esa perspectiva. |

## 2. Tarjeta del tutorial "Pulsa E"

Está en `Assets/Recursos/Tutorial/`. Es una tarjeta con dos imágenes y una flecha entre ellas.

| Archivo | Tamaño | Qué es |
|---|---|---|
| `Tutorial_AOscuras.png` | 440 × 270 | El velador con el cajón abierto, **a oscuras** (casi negro). |
| `Tutorial_ConFosforo.png` | 440 × 270 | La misma imagen **con la luz cálida de un fósforo**. |

## 3. Íconos del inventario

PNG transparentes de 256 × 256 que se leen bien en chico (≈ 110 px).

| Archivo | Objeto |
|---|---|
| `Icono_Inventario.png` | Botón del inventario: una bolsa o morral de Luis |
| `Ranura.png` | Casillero vacío del inventario (160 × 160) |
| `Foto_Campo.png` | (se reusa la foto) |
| `Agenda.png` | Agenda de costura de la abuela, tapa roja (260 × 320) |
| `Llave_Pequena.png` | Llavecita del joyero |
| `Llave_Armario.png` | Llave del cajón del armario |
| `Llave_Puerta.png` | Llave grande y vieja de la puerta del cuarto |

## 4. Tarjetas (opcional, mejora mucho)

Al hacer clic en un objeto del inventario se abre una tarjeta como las de Historia: **1905 × 970**, con su propio borde. Sin tarjeta, el juego muestra el ícono en grande sobre gris.

- **Foto del campo:** la foto en primer plano.
- `Foto_Campo_Reverso.png` (provisorio, 340 × 460): el reverso de la foto con la letra de la abuela, "Volvimos al campo, **1987**". **El año tiene que ser el mismo que el código del joyero** (campo `codigo` de `CandadoNumerico` en `JUEGO`).
- **Agenda:** 2 o 3 páginas de pedidos de costura y una que dice "Clave del joyero: ¿cuándo nos mudamos de vuelta al campo?".
- **Cada llave:** opcional.

## 5. Pantalla de victoria

`PanelVictoria` todavía muestra la lata de galletas. Ahora **se gana saliendo por la puerta**, así que hace falta otra imagen: Luis saliendo al pasillo, o la puerta abierta con luz.

## 6. Audio

| Sonido | Para qué |
|---|---|
| **Respiración** (loop) | La Presencia, desde el primer fósforo consumido. Hoy se usa `ambloop_cuarto_terror` como reemplazo provisorio. |
| Clic de candado / diales | El candado numérico del joyero. Hoy usa `ui_click`. |
| Llave en cerradura | Al usar una llave. Hoy usa `sfx_cajon_abrir`. |
