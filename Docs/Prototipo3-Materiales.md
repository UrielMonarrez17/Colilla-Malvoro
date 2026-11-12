# Prototipo3: assets rosas

## Causa reproducida

El 11 de noviembre de 2026 se reprodujo el error en una compilación Windows de `Prototipo3.unity` con Unity 6000.5.9f1 y Direct3D 11. La escena del editor tenía materiales válidos, pero seis mecanismos generados durante la partida recibían `Hidden/InternalErrorShader` en el ejecutable:

- Laboratorio de química.
- Oficina de Morales.
- Postes reflectores 1, 2 y 3.
- Interruptor de la subestación.

`StoryWorldBinder.EnsureMechanismExists` creaba sus primitivas sin asignarles material. La captura de un material afectado produjo 6,400 píxeles magenta en una imagen de 96 × 96. El fallo se mantuvo de día, de noche, al utilizar trampas y después de regenerar la ciudad.

Evidencia anterior a la corrección: `Logs/material-player-baseline.txt` y `Logs/material-player-baseline-sample-8.png`. El reporte también contiene la muestra de diagnóstico; los seis mecanismos son los objetos afectados del juego.

## Corrección

`RuntimeMaterials.CreateOpaque` crea una instancia del material URP Lit `Assets/Prototipo3/Resources/Prototipo3Materials/GeneratedOpaque.mat`. La referencia en Resources incluye el material y su shader en la compilación, sin depender del material predeterminado de `CreatePrimitive`. Cada mecanismo conserva su propia instancia y sus cambios de color.

Se asigna el mismo material explícito a los modelos de respaldo de Lulú y los contenedores de botín, que también se construían sin material cuando faltaba su prefab.

Los materiales importados con texturas y los efectos transparentes existentes conservaron sus referencias y parámetros. Las pruebas iniciales mostraron que los efectos con `Sprites/Default` se renderizaban correctamente; no se sustituyeron sus shaders. Tampoco se modificó la lógica de trampas, la niebla ni las misiones.

Referencia técnica: Unity documenta la inclusión de shaders mediante [materiales referenciados desde Resources](https://docs.unity.cn/6000.7/Documentation/ScriptReference/Shader.Find.html).

## Repetir la validación

Con el proyecto cerrado en otras instancias de Unity, ejecutar desde PowerShell en la raíz:

```powershell
& .\Tools\RunMaterialValidation.ps1 -Build
```

Sin `-Build`, ejecuta sólo las comprobaciones del editor. La prueba abre la escena real en Play Mode sin guardarla. La compilación de diagnóstico usa `FERAL_MATERIAL_VALIDATION`; el verificador no se incluye en compilaciones normales.

Se revisan materiales, shaders y muestras renderizadas durante el día, la noche, el uso de trampas y la regeneración. Se comprueba que completar un mecanismo conserve su color verde sin alterar otro mecanismo ni el material original. También se prueban botín sin prefab y, en el ejecutable, Lulú sin prefab. Las capturas del editor esperan la compilación de sus variantes antes de leer los píxeles.

Reportes: `Logs/material-editor.txt`, `Logs/material-player.txt` y `Logs/material-build-result.txt`. Las imágenes `material-editor-sample-*.png` y `material-player-sample-*.png` se identifican en cada reporte.

Resultado final del 11 de noviembre de 2026: **PASS, 0 incidencias**, tanto en el editor como en el ejecutable. La compilación terminó con **0 errores**. Las muestras de los seis mecanismos corregidos registraron **0 píxeles magenta**; también pasaron la independencia de colores y los modelos de respaldo. La escena guardada `Prototipo3.unity` no fue modificada.

El ejecutable de diagnóstico se genera en `Logs/MaterialValidationBuild/FeralMaterials.exe`; se cierra automáticamente al terminar la prueba. No es una compilación para distribuir a jugadores.
