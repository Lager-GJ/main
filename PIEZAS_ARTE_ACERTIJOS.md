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

## 2. Tarjeta del tutorial "Pulsa E" = hoja de instrucciones (2026-10-27)

Está en `Assets/Recursos/Tutorial/`. Es la hoja de instrucciones que sale de la caja de fósforos. Tiene papel viejo, el encabezado "INSTRUCCIONES", el texto, dos imágenes con una flecha entre ellas y el botón "Entendido". Los textos y los marcos los pone el juego en tinta oscura.

| Archivo | Tamaño | Qué es |
|---|---|---|
| `Tutorial_Papel.png` | 1140 × 720 | **Hecho (provisorio bueno).** Papel amarillento con borde roto, bordes tostados, una mancha de café, un pliegue en cruz y un marco doble de tinta con rombitos en las esquinas. Se estira a 1080 × 700 en pantalla. |
| `Tutorial_AOscuras.png` | 440 × 270 | El velador con el cajón abierto, **a oscuras** (casi negro). |
| `Tutorial_ConFosforo.png` | 440 × 270 | La misma imagen **con la luz cálida de un fósforo**. |

## 2b. Caja de fósforos «La Chispa» en primer plano (2026-10-27)

Está en `Assets/Recursos/Inicio/`. Sale del dibujo de Anavi (`Assets/Recursos/Matchbox .png`): su cara de adelante, enderezada y recortada con su contorno. El resto copia sus colores y su trazo.. Si el equipo la redibuja, que mantenga los tamaños.

| Archivo | Tamaño | Qué es |
|---|---|---|
| `Caja_Frente.png` | 607 × 870 | **Hecho, es arte del equipo.** Frente de la funda: «LA CHISPA», el demonio y la franja «Cada llama tiene un precio». El borde de arriba está roto, porque por ahí sale la bandeja. |
| `Caja_Reverso.png` | 607 × 870 | **Hecho (provisorio bueno).** La misma silueta espejada: «ADVERTENCIA», el demonio grande y oscuro con ojos amarillos detrás de 4 fósforos (las llamas crecen y la última es roja), «LA ÚLTIMA LLAMA LO LLAMA» y la franja roja «LEA LAS INSTRUCCIONES ANTES DE USAR». |
| `Caja_Bandeja.png` | 546 × 800 | **Provisorio.** La bandeja morada que sube al abrir: borde claro y fondo oscuro. |
| `Caja_Fosforo.png` | 74 × 430 | **Provisorio.** Un fósforo parado: palito crema y cabeza roja con brillo. El juego pone uno por cada fósforo que queda. |
| `Cajon_LaChispa.png` | 360 × 255 | **Hecho.** La caja acostada dentro del cajón, con sombra. Reemplaza a `CajaF` en el cajón del inicio. |

Mejoras posibles para el equipo:
- una bandeja dibujada a mano, con las paredes de adentro, como en `Matchbox .png`;
- la lija en el costado;
- un reverso dibujado a mano con el mismo texto.

## 2c. Cajones abiertos inclinados: ficha para el equipo (2026-10-27)

David lo pidió así: al abrir un cajón **no** cambia de ventana ni aparece una vista desde arriba flotando. El mueble se abre **en el cuarto**, a su escala y con Luis al lado. El cajón sale **inclinado, visto un poco desde arriba (~45°)**, para que se vea su **piso** y los objetos acostados encima.

Hoy hay un provisorio hecho deformando `Inicio/Cajon_Interior.png`. Para la versión final:
- respeten **el mismo tamaño de archivo y el mismo marco**: los muebles abiertos se superponen al cerrado píxel a píxel;
- **no dibujen los objetos** del cajón: el juego los pone y los acuesta con perspectiva;
- el **piso** del cajón tiene que ser **más claro** que las paredes, para que los objetos se distingan;
- usen trazo negro grueso y colores casi negros, como el resto del cuarto.

| Archivo | Tamaño | Dónde va | Esquinas del borde de arriba del cajón (px; fondo izq., fondo der., frente der., frente izq.) |
|---|---|---|---|
| `Inicio/Velador_CajonInclinado.png` | 626 × 626 | Reemplaza a `CajonAbr` (mismo marco que `Cajoncerr`). | (198,318) (402,318) (350,436) (132,436) |
| `Acertijos/Armario_CajonAbierto.png` | 300 × 130 | Encima de `ArmAbr`, con su esquina en (165,462) de `ArmAbr`. Tapa el cajón cerrado de abajo. | (38,15) (262,15) (282,78) (18,78). La llave de la puerta va acostada en (150,47). |
| `Acertijos/Secretero_CajonSecretoAbierto.png` | 140 × 116 | El cajón secreto, que sale de los casilleros. | (18,6) (122,6) (136,66) (4,66). La agenda va acostada en (70,35). |
| `Acertijos/Joyero_Abierto.png` | 220 × 170 | Joyero abierto: tapa levantada atrás y el terciopelo (piso) visto desde arriba, entre y 52 y 100. La placa del candado queda en el mismo lugar que en el cerrado (70..150, 104..140). | La llave pequeña va acostada en (110,78). |

**Si cambian las esquinas del velador**, hay que actualizarlas en `InicioCuarto` → "Borde Cajon En Dibujo". En los otros muebles, avísenle a David para mover la llave o la agenda.

## 2d. Objetos en primer plano: frente y reverso (2026-10-28)

Cada objeto que se toca pasa a primer plano, en la mano de Luis. Un clic lo da vuelta y el botón **Inspeccionar** lee su descripción o hace su acción: abrir la caja, probar la llave en la lata o abrir el candado del joyero. Por eso cada objeto necesita su **reverso**.

Todos los de esta tabla son **provisorios**. Varios reversos son el frente espejado y un poco más oscuro, y el equipo puede reemplazarlos por nombre. Conviene mantener el tamaño del frente; si cambia, que mantenga la proporción. En pantalla, el objeto entra en 640 × 500 y se agranda como mucho ×3.

| Archivo | Tamaño | Qué es |
|---|---|---|
| `Inicio/Cajon_FotoVieja.png` | 340 × 250 | Frente: la abuelita joven, en sepia. A la derecha se ve el hombro de alguien más; la foto está cortada con tijera, con el borde dentado. |
| `Inicio/Cajon_FotoVieja_Reverso.png` | 340 × 250 | Papel de atrás con el corte del otro lado, «Rosita y …» junto al corte, el sello «FOTO ESTUDIO» y manchas de pegamento del álbum. |
| `Inicio/Cajon_Estampita.png` | 240 × 350 | Estampita de un santo con aureola y borde dorado gastado: «Ruega por nosotros». |
| `Inicio/Cajon_Estampita_Reverso.png` | 240 × 350 | La oración impresa, borrosa, y con lápiz: «Que nunca se te apague la luz, mijito». |
| `Inicio/Cajon_Nota_Reverso.png` | 300 × 220 | La nota de atrás: el papel, con la letra que se transparenta al revés. |
| `Inicio/Cajon_Rosario_Reverso.png` | 420 × 340 | Espejado. |
| `Inicio/Cajon_HiloAguja_Reverso.png` | 260 × 240 | Espejado, con la etiqueta «N°40» del carrete. |
| `Inicio/Cajon_CaboVela_Reverso.png` | 280 × 170 | Espejado. |
| `Acertijos/Agenda_Reverso.png` | 260 × 320 | Contratapa: «Pedidos y medidas» y las iniciales «R. M.». **No tiene que mostrar el año**, porque la clave del joyero sale de la foto del campo. |
| `Acertijos/Llave_Pequena_Reverso.png`, `Llave_Armario_Reverso.png`, `Llave_Puerta_Reverso.png` | 256 × 256 | Espejadas. |
| `Acertijos/Joyero_Reverso.png` | 660 × 510 | Atrás del joyero: las bisagras y el grabado «Para mi Rosa». |
| `Acertijos/Lata_Frente.png` | 1000 × 603 | Recorte del dibujo de la lata del equipo (`borde textura-Galletas oscuridad.png`). |
| `Acertijos/Lata_Reverso.png` | 1000 × 603 | La otra cara, con una etiqueta de papel: «Hilos y botones (no tocar)». |

La foto del campo ya tenía su reverso, con el año (`Foto_Campo_Reverso.png`). La caja de fósforos tiene el suyo en §2b.

## 2e. Tele embrujada (2026-10-28)

La tele está dibujada en `FondoDefi`. Al tocarla por primera vez se prende sola con estática y, por un instante, en el reflejo de la pantalla se ve a Luis con la Presencia detrás. Todo lo de esta tabla está en `Assets/Recursos/Tele/` y es **provisorio**.

Los dibujos tienen la forma de la pantalla del fondo, que mide 238 × 186 px en `FondoDefi`, con las esquinas redondeadas. Están hechos al triple de tamaño.

| Archivo | Tamaño | Qué es |
|---|---|---|
| `Tele_Estatica_1..4.png` | 714 × 558 | Cuatro cuadros de estática que se alternan: ruido gris azulado, líneas de barrido y una franja clara que rueda. |
| `Tele_Reflejo.png` | 714 × 558 | El vidrio negro: Luis chico, con los ojos abiertos, y detrás, alta y borrosa, la Presencia con ojos amarillos (los mismos del reverso de la caja). **Es la pieza que más conviene que el equipo redibuje.** |
| `sfx_tele_estatica.wav` | 1.6 s | Ruido con zumbido, generado en código. |
| `sfx_tele_golpe.wav` | 1.1 s | Golpe grave para el reflejo, generado en código. |

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
