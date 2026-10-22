# Tutorial: recorrido guiado

La escena `Assets/Scenes/Tutorial.unity` conserva la ciudad y sus edificios. El grupo
`Tutorial_EntornoGuiado` contiene el suelo continuo, límites, coberturas y puntos de
referencia guardados en el editor. El gestor reutiliza este grupo en Play Mode.

## Recorrido

1. Madriguera (5,146): WASD; mesa de trabajo al oeste.
2. Salida por el pasillo sur y giro al este (22.5,125): Shift y estamina.
3. Control (28.8,125): contenedor dorado al norte; F entrega madera y metal y abre la barrera.
4. Ciudad: Lulú (45,148), tres contenedores con desperdicios y retorno a Lulú.
5. Anochecer gradual y video `NightfallFeral.mp4`; el tigre inicia la persecución.
6. Dash hacia (55,126), giro al sur y avance agachado al oeste por Z < 125.
   Las coberturas en Z=124.3 separan al jugador de la patrulla. El sigilo requiere
   movimiento y llegar al acceso oeste; permanecer quieto no completa la fase.
7. Entrada al refugio: amanecer, recuperación y mesa de fabricación.
8. Trampa de púas: 1 madera + 1 metal; tecla 1 y F en la entrada del refugio.
   Solo esta puerta y esta receta completan el tutorial.

## Edición y navegación

`Feral > Tutorial > Construir recorrido guiado y hornear navegacion` actualiza el
recorrido explícitamente. Abrir el editor no mueve ni guarda objetos automáticamente.
La navegación del tutorial se guarda en `Assets/Scenes/Tutorial/GuidedLayout/`;
los assets de navegación anteriores de la ciudad se conservan.
El horneado deja la puerta de control abierta; un NavMeshObstacle la bloquea durante
el juego hasta saquear el contenedor. Los diez segmentos se comprueban con rutas
completas y se registran en `Logs/tutorial-navigation.txt`.

`TutorialPlayValidation.Run` es una prueba batch de integración: comprueba el
arranque, referencias, loot, transición nocturna, adrenalina, receta y colocación.
Invoca algunas transiciones directamente; no sustituye una partida manual con
teclado ni verifica la dificultad de la persecución. Resultados en
`Logs/tutorial-play-validation.txt`; vista aérea en `Logs/tutorial-overview.png`.

## Cinemáticas

La introducción conserva `TestInitialVideo.mp4`. Espera a que VideoPlayer prepare
el clip; Espacio, Escape o Enter permiten omitirlo. El video nocturno se reproduce
al terminar el atardecer, con el jugador y enemigo pausados; Escape/Enter lo omiten.

`NightfallFeral.mp4`: 6 segundos, 1920x1080, H.264 Constrained Baseline y AAC. Es un plano ilustrado
con acercamiento suave, fundidos y audio grave sintetizado, no animación esquelética.
Imagen fuente: `Assets/UI/Videos/NightfallFeral.png`, generada con la herramienta
integrada imagegen. Se usó este prompt:

> Create a cinematic 16:9 frame for a low-poly stylized survival game. A feral tiger stares straight into the camera, intense amber eyes, centered symmetrical close-up, visible head and shoulders, deserted city alley background fading into dark navy fog. Angular low-poly 3D aesthetic, dramatic cold moonlight and faint orange rim light, ominous dusk, no gore. Lower third has large perfectly legible Spanish text: CORRE. No other text, no watermark.

El MP4 se compuso con FFmpeg: zoom progresivo de 1 a 1.072, fundido inicial de
0.8 s y final de 0.6 s; audio a 55 y 82.4 Hz con modulación lenta.

## Validación realizada

- Unity 6000.5.7f1: compilación satisfactoria.
- Diez segmentos de navegación: PASS.
- Prueba de integración en Play Mode: 17 comprobaciones PASS.
- Vista aérea renderizada y revisada; MP4 comprobado con ffprobe.
- La escena todavía registra una advertencia de script faltante preexistente y el
  video inicial registra timestamps H.264 corregidos por Unity. Ninguno impidió
  completar la prueba. El MP4 nuevo se convirtió posteriormente a Baseline para
  evitar esa advertencia de codificación.
- Pendiente una partida manual completa para valorar dificultad, ritmo y encuadre
  del HUD con distintas resoluciones. La prueba automatizada usa transiciones
  directas y no certifica la experiencia completa de control con teclado.
