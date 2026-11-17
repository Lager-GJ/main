# Plan: pulir el cuarto 1 de Lumbre

_Acordado el 2026-11-17 entre David y Claude, a partir de la partida grabada ese día por alguien que jugaba por primera vez. Se perdió sin encontrar la agenda ni el neceser._

## Decisiones

| Tema | Decisión |
|---|---|
| Nombre del juego | **Lumbre**. «El Cuco» queda como la marca de los fósforos. |
| Duración del cuarto 1 | Unos **5 minutos** para alguien que lo juega por primera vez |
| Fuente de la tensión | Miedo y exploración |
| Fósforos | Se mantienen **4**. Se puede alargar cada uno (hoy dura 20 s). |
| Al perder | Se mantiene «FREGASTE». Se agrega **«Intentar de nuevo»**, que reinicia el cuarto. |
| Qué se puede clicar | **Solo lo importante**: velador, foto del campo, agenda, neceser y armario (con su cajón y la lata) |
| Ayuda al jugador | **Solo un objetivo que cambia** con el progreso, sin pistas extra |
| El Cuco | Aparece durante la partida: sombras, la tele y otros momentos |
| Menú | 3 botones: **Jugar** (lleva a Niveles), **Opciones** y **Créditos** |
| Intro | Queda para después; la mejora David (textos, animaciones, audio) |
| Arte nuevo | Claude hace versiones provisionales (siluetas, llama por código) hasta que llegue el arte del equipo |
| Prioridad | Terminar el cuarto 1 antes de pasar al 2 |

## Cadena de pistas actual (no cambia)

1. **Pared 1**: en el velador está la caja de fósforos.
2. **Pared 3**: en el piso, junto a la tele, está la foto del campo. Atrás dice «Volvimos al campo, 1987».
3. **Pared 3**: junto a la máquina de coser está la agenda. Dice «Neceser: el mejor viaje de mi vida».
4. **Pared 2**: el neceser del tocador se abre con 1987, si ya tienes la foto y la agenda, y da la llave del armario.
5. **Pared 4**: la llave abre el cajón del armario; ahí está la lata, y al abrirla se termina el nivel.

---

## Fase 1: errores que confunden

> **Estado (2026-11-17):** hecho en la rama `feat/fase1-errores`, sin probar en Play.
> - Resueltos en código: 1.1, 1.2, 1.3, 1.5, 1.6, 1.7, 1.9 y 1.10.
> - 1.4 y 1.11: los subtítulos se achican si no entran. El video parece recortado en los bordes (falta confirmarlo); revisar en Play.
> - 1.8: revisar en Play.
> - El círculo que se ve sobre «Entendido» es una mancha de café dibujada en el papel (`Tutorial_Papel.png`), no un error.
> - **Diseño de interfaz del equipo** (hoja del 2026-11-17), con arte provisorio recortado de capturas (`Recursos/HUD/`, `Resources/UI/Papel_Tarjeta.png`) y la fuente Jaini (`Resources/Fuentes/`, OFL):
>   - mochila arriba a la izquierda; caja El Cuco con 5 estados arriba a la derecha; objetivo debajo;
>   - diálogo en banda degradada (967 px, a 829 px del borde de arriba); las descripciones de los primeros planos también salen ahí;
>   - mochila abierta y candado sobre la tarjeta de papel: casillas de 170×170 (punteada / 3 px / 7 px elegida) y 4 ruedas Jaini de 140×190 que abren solas con el código;
>   - etiqueta con verbo debajo del objeto resaltado (`EtiquetaObjeto`, en los 7 objetos clave).
> - Agregado después de la primera prueba: en todos los primeros planos (caja de fósforos, objetos del cuarto y de la bolsa) el **clic en el objeto interactúa** y el **botón «Voltear»** lo da vuelta (solo si tiene reverso). Sin línea de ayuda abajo, y el objeto queda centrado en la pantalla.

| # | Qué | En el video | Listo cuando |
|---|---|---|---|
| 1.1 | Con la caja en primer plano, la **E no prende** el fósforo | 0:46–1:06 | Con la caja en la mano, la E prende, o la caja se cierra sola al tocar «Entendido» |
| 1.2 | **Hoja de instrucciones** centrada y con animación al aparecer y desaparecer | 0:20 | La hoja aparece en el centro con una animación suave |
| 1.3 | **Textos que se chocan**: objetivo, subtítulos, sonidos («Algo camina…») y avisos de guardado | 0:06, 0:20, 1:10, 2:08 | Cada tipo de texto tiene su lugar y nunca hay dos en el mismo sitio. Los subtítulos salen de a uno, en fila. Al guardar algo sale un solo mensaje. |
| 1.4 | **Textos cortados** en el borde de abajo o de la izquierda | 1:48, 2:28 | Un subtítulo de 2 líneas se ve completo |
| 1.5 | El **acercamiento se sale del dibujo** y aparece una franja azul | 1:12, 1:24, 2:00, 2:26 | El zoom nunca muestra el fondo |
| 1.6 | **Luis se dibuja encima** de los primeros planos | 1:12, 1:18 | Luis queda detrás del primer plano |
| 1.7 | **Dos cajas a la vez**: la pintada en el cajón sigue visible detrás del primer plano | 0:06 | Solo se ve una caja |
| 1.8 | El **velador se abre solo** varias veces y su transición muestra dos dibujos encimados | 1:06, 1:18, 1:40 | Se abre solo cuando lo pide el jugador, con una transición limpia |
| 1.9 | **Reverso en blanco** de los objetos | 1:28, 3:00 | Ningún primer plano muestra un rectángulo blanco (en parte se resuelve con 2.2) |
| 1.10 | El **«4»** de la hoja de instrucciones parece una tecla | 0:20 | El 4 se muestra como número, no dentro de un recuadro |
| 1.11 | **HUD de la caja cortado** en el borde | 1:48 | Probado en 16:9 (1920×1080) y en la ventana de WebGL |

## Fase 2: un cuarto que se entienda

| # | Qué | Listo cuando |
|---|---|---|
| 2.1 | **Modo oscuro en las 4 paredes.** Hoy solo la pared 1 se ve en la oscuridad (por la luz de la puerta) y las otras quedan negras. | Sin fósforo, las 4 paredes se ven igual de oscuras pero se distinguen |
| 2.2 | **Solo lo importante se puede clicar.** Dejan de ser clicables la tele, la foto del estante, el retrato, la foto de la familia, la Virgen, la cruz, la máquina de coser, el buró y la canasta; siguen en el dibujo. | Solo 5 cosas (más el cajón y la lata) brillan y responden al clic |
| 2.3 | **El resaltado calza con el objeto.** Hoy hay siluetas puestas encima del dibujo y se ven espacios al pasar el mouse. | El resaltado sigue la forma del objeto y no queda una silueta doble |
| 2.4 | **Revisar el arte viejo mezclado con el nuevo.** Claude arma la lista de cada imagen que usa la escena (nueva o vieja) y David decide. | Hay una lista y cada pieza vieja está cambiada o pedida al equipo |
| 2.5 | **Objetivo que cambia**, fijo y legible en una esquina: «Encuentra la caja de galletas» → «El cajón del armario tiene llave» → «El neceser pide una clave» → «Abre el cajón del armario» | Siempre se ve qué hacer ahora, sin taparse con otros textos |
| 2.6 | **La foto del piso se tiene que ver.** Hoy mide 75×31 y pasa desapercibida. | Alguien nuevo la encuentra sin ayuda |
| 2.7 | **Ajustar el tiempo de cada fósforo.** Primero probar 30 s (2 min de luz en total). | Una primera partida dura cerca de 5 min y se puede ganar |

## Fase 3: perder, ganar y guardar

| # | Qué | Listo cuando |
|---|---|---|
| 3.1 | Después de «FREGASTE», un botón **«Intentar de nuevo»** reinicia el cuarto sin repetir la intro | Al perder se vuelve a jugar en un clic |
| 3.2 | **Guardar el progreso al ganar**: ya existe (`L1Controller` → `SaveSystem`), falta comprobar que funciona con la pantalla final nueva | Al ganar el cuarto 1, al volver al menú el cuarto 2 sigue desbloqueado en Niveles, incluso después de cerrar el juego |

## Fase 4: el miedo (el Cuco)

Cada fósforo que se apaga acerca un poco más al Cuco, como promete el juego: «Cada fósforo que se apaga, algo se acerca».

| Momento | Qué pasa |
|---|---|
| Se apaga el fósforo 1 | Se oyen pasos y una sombra cruza detrás de la cortina (pared 2) |
| Se apaga el fósforo 2 | La tele se prende sola, con estática y la silueta del Cuco (pared 3) |
| Se apaga el fósforo 3 | En la oscuridad aparecen dos ojos en el borde de la pantalla, y el espejo de la pared 4 muestra una sombra |
| Último fósforo | La sombra queda junto a Luis; si se apaga, viene «FREGASTE» |

Las siluetas y los ojos serán provisionales (negros, hechos por Claude) hasta que llegue el arte del equipo.

## Fase 5: menú

| # | Qué | Listo cuando |
|---|---|---|
| 5.1 | **3 botones**: Jugar (lleva a Niveles), Opciones y Créditos. Se quitan «Niveles» y «EXIT». | El menú tiene 3 botones, alineados |
| 5.2 | Título **LUMBRE** donde la **L es un fósforo encendido** con la llama en movimiento (provisional, por código) | La llama se mueve en el título |
| 5.3 | **Animaciones de botones** (al pasar el mouse y al hacer clic) y **transiciones** con fundido entre escenas | Ningún cambio de pantalla es un corte seco |
| 5.4 | El botón «Omitir intro y tutorial »» dice solo **«Omitir»** (está en `Historia.unity`) | — |

## Para después

- La intro (David): textos, animaciones y audio.
- El arte final del equipo para las sombras del Cuco, la L de LUMBRE y las piezas viejas de 2.4.
- El cuarto 2.

## Cómo trabajamos

- Una rama y un PR a `main` por fase, en orden de 1 a 5. Las fases 1 y 2 son las que más cambian la partida.
- Después de las fases 2 y 4: otra partida grabada con alguien que no conozca el juego, para comprobar que dura unos 5 minutos y que se entiende qué hacer.
