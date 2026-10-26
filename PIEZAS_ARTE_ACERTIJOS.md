# Piezas de arte: acertijos del cuarto 1

Desde 2026-10-22 el cuarto 1 se resuelve con una cadena de acertijos:

> foto del campo → marco vacío → cajón secreto del secretero → agenda (pista) → candado del joyero → llave pequeña → lata de galletas → llave del armario → cajón del armario → llave de la puerta → salir.

Todo lo que se ve de esto es **provisorio**: se hizo en código, con recortes del fondo. Está en `Assets/Recursos/Acertijos/`. Para cambiar una pieza, **reemplazar el PNG con el mismo nombre**. Así se mantiene su `.meta` y el juego la toma sola, sin tocar la escena.

**Estilo:** el del cuarto, lápiz en grises, igual que `FondoDefi.png`, `ArmAbr.png` y `BuroAB.png`. Las tarjetas grandes siguen el estilo de las `Ni-img` de Historia, con borde blanco de tiza.

## 1. En el cuarto

Son PNG transparentes, en el mismo ángulo y la misma luz que `FondoDefi`.

| Archivo | Tamaño | Qué es / dónde va |
|---|---|---|
| `Secretero_Cerrado.png` | 320 × 460 | **Mueble nuevo.** Escritorio de la abuela con tapa inclinada, casilleros arriba y 3 cajones. Va entre la silla y la máquina de coser. El fondo no lo tiene, así que la pieza puede incluir su sombra en el piso. |
| `Marco_Vacio.png` | 220 × 290 | Portarretratos colgado sobre el secretero, **sin foto**. Se ve la marca más clara de la foto que faltaba. |
| `Marco_ConFoto.png` | 220 × 290 | El mismo marco con la foto de la casa del campo puesta. |
| `Foto_Campo.png` | 340 × 460 | La foto suelta en el piso, junto a la tele. Es una foto vieja de la casa del campo. El provisorio usa el paisaje de la ventana del fondo. |

## 2. Primeros planos (pantalla completa)

| Archivo | Tamaño | Qué tiene que mostrar |
|---|---|---|
| `Secretero_PrimerPlano.png` | 900 × 900 | El secretero abierto: tapa bajada como escritorio, casilleros con papeles y frascos ("menjurjes"), 2 cajones abajo y uno ancho. En el centro de los casilleros va un **cajón chico escondido** (el secreto). |
| `Secretero_CajonSecretoAbierto.png` | 180 × 150 | Ese cajón chico, abierto y salido hacia adelante. Se pone encima cuando se abre. |
| `Joyero_Cerrado.png` | 700 × 560 | Joyero de la abuela, cerrado, con una **placa de candado de 4 números** en el frente. Los diales los dibuja el juego encima de la placa, así que la placa tiene que quedar lisa, en la zona central de abajo. |
| `Joyero_Abierto.png` | 700 × 560 | El mismo joyero abierto, con terciopelo adentro. Ahí aparece la llavecita. |
| Lata de galletas | — | Ya tiene arte (`galletas zoom.png`). Ahora se ve **sobre la cornisa del armario** dentro del primer plano y está cerrada con llave: haría falta una versión con **cerradura chica** en la tapa. |
| Cajón del armario | — | Ahora está cerrado con llave. Un cajón abierto con la **llave de la puerta** adentro ayudaría (hoy solo se ilumina). |

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
