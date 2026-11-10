# Menú principal Feral

Abre `Assets/Prototipo2/Scenes/MainMenu_Prototipo2.unity` y pulsa Play.

- Canvas uGUI con botones editables: Jugar, Partidas, Configuración, Cómo jugar y Salir.
- Fondo nocturno y logo independientes, basados en la referencia visual.
- El prefab `Assets/Prototipo2/UI/HamsterMenuVisual.prefab` contiene la malla y el material del jugador de Prototipo2, sin controladores ni física del juego.
- `Hamster portrait studio` contiene la cámara, luces y pivote. `MenuHamsterLook` gira el modelo hacia el mouse con límites de giro y suavizado. Se presenta en el Canvas mediante un RawImage y una RenderTexture transparente.
- Jugar abre `Assets/Scenes/Tutorial.unity`. El cierre existente del tutorial en `TutorialPipeLineManager` carga `Assets/Prototipo2/Prototipo2.unity`. Las tres escenas ya están habilitadas, en ese orden, en Build Settings.
- Se mantienen las pantallas secundarias existentes; esta entrega se centra en la presentación principal. Partidas no implementa un sistema nuevo de guardado.

## Edición

Modifica los RectTransform, textos y colores en `Feral UI/Home`. Los fondos de botón usan `MenuBrushGraphic`, un gráfico nativo de Canvas. La imagen del hámster está en `Feral UI/Hamster Portrait`.

`Feral > Menú principal > Reconstruir presentación` vuelve a preparar el menú desde los assets. Sobrescribe la presentación de esta escena: no lo uses para conservar ajustes manuales posteriores. Lee Prototipo2 sin guardarlo. `Capturar vista previa` guarda un render en `Temp/FeralMenu/MainMenu.png`.

## Verificación realizada

Unity 6000.5.9f1 compiló los scripts y ejecutó el constructor de escena. Se revisó el render a 1600 × 900: logo, cinco botones, texto y modelo frontal visibles, sin superposición entre botones y personaje. La captura está en `Docs/Previews/MainMenu.png`.

Pendiente de revisión en Play: seguimiento del mouse en extremos, navegación con teclado, entrada al tutorial y completar el tutorial hasta Prototipo2. El editor dejó de exponer su ventana durante esta revisión, por lo que no se declara validado ese recorrido en ejecución.
