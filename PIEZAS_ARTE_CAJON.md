# Piezas de arte: el cajón del inicio (cuarto 1)

El juego arranca con Luis mirando el cajón de la mesita de la abuela. Hoy todo lo que se ve ahí es **provisorio**. Esta lista es lo que falta dibujar.

**Estilo:** el mismo de las tarjetas de Historia (`Assets/Recursos/Historia/Ni-img *.png`), es decir lápiz en grises con el borde blanco de tiza. La caja de fósforos ya tiene arte final (`CajaF.png`) y se usa tal cual.

## 1. El cajón visto desde arriba

| Pieza | Tamaño | Notas |
|---|---|---|
| Interior del cajón | 1400 × 800, PNG transparente | Vista cenital, fondo de madera vacío. Los objetos van aparte. |

## 2. Objetos sueltos dentro del cajón

Son PNG transparentes en vista cenital y en el mismo estilo. Al pasar el mouse se iluminan y al hacer clic abren su tarjeta.

| Objeto | Tamaño aprox. en pantalla | Qué tiene que contar |
|---|---|---|
| Rosario | 380 × 300 | El rosario de la abuela, que nunca deja |
| Hilo y aguja | 230 × 215 | Carrete de hilo negro con aguja |
| Cabo de vela | 250 × 150 | Vela consumida casi entera |
| Nota | 270 × 200 | Papel doblado con letra de la abuela ("Mijito: si estás leyendo esto…"). Queda debajo de la caja de fósforos. |
| **Foto vieja** (nuevo) | 170 × 125 | La abuela joven; a su lado alguien recortado con tijera |
| **Estampita** (nuevo) | 90 × 130 | Estampita de un santo, gastada |

## 3. Tarjetas de cada objeto

Cuando el jugador toca un objeto, se abre una tarjeta grande con el texto debajo y el botón `>`, igual que en Historia. Cada tarjeta es una ilustración de **1905 × 970** con su propio borde blanco, como las `Ni-img`.

- Rosario
- Hilo y aguja
- Cabo de vela
- Foto vieja (en primer plano, se nota el recorte)
- Estampita (de frente; si se puede, una segunda con el reverso escrito a lápiz)
- Nota (la letra de la abuela; el texto final está pendiente)

Mientras no haya tarjeta, el juego muestra el dibujo del objeto en grande sobre una tarjeta gris con borde de tiza generado en código.

## 4. Opcional

- Un borde de tarjeta dibujado a mano como sprite *9-slice*. Reemplaza al borde generado en código, tanto alrededor del cajón como de las tarjetas provisorias.

## Dónde se conecta cada pieza (para quien la importe)

En `JUEGO.unity`, GameObject raíz **`InicioCuarto`**, componente `InicioCuarto`:

| Pieza | Campo del Inspector |
|---|---|
| Interior del cajón | `Fondo Cajon` |
| Objeto suelto | `Elementos → [n] → Sprite` |
| Tarjeta de un objeto | `Elementos → [n] → Ilustracion` |
| Nota (objeto / tarjeta) | `Sprite Nota` / `Ilustracion Nota` |
| Borde 9-slice | `Marco Tarjeta` |

Los textos de cada tarjeta están en `Elementos → [n] → Textos`: una página por línea, y el botón `>` pasa a la siguiente.
