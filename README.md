# xr-project

Proyecto de curso **IDI 3185 · Realidad Extendida** — DILAB.

**Equipo:** Estefanía Pakarati, Emmanuel Norambuena, Matías Harrison.

## Estado actual (Hito 2 · prototipo lo-fi, 13-oct-2026)

- **Prototipo jugable** en Unity: ejercicio de **estacionamiento perpendicular** (Caso de uso 2 del informe) con auto a escala 1:1, controles de Meta Quest 3 o teclado, HUD de distancia, sensor de retroceso, espejos y evaluación del estacionamiento. Ver [Interacción](#interacción).
- **Ejecutable de escritorio (Windows x64)** en [`Build/VRCar-Hito2-Windows.zip`](./Build/VRCar-Hito2-Windows.zip). Ver [Ejecutar el prototipo](#ejecutar-el-prototipo).
- **Tag de la entrega:** `hito-2`.
- **Pendiente / límites conocidos:** ver [Pendientes](#pendientes-y-límites-conocidos).

## Propuesta

Simulador de conducción en **realidad virtual** para el aprendizaje del
**tamaño real del vehículo y el juicio de distancias**. La idea central es
que un simulador 2D (juego o software de escritorio) no transmite la
escala 1:1 ni la profundidad estereoscópica necesarias para que un
conductor novato aprenda, con su propio cuerpo, dónde terminan las
esquinas del auto y qué tan cerca pasa de un obstáculo — algo que hoy solo
se adquiere dentro del vehículo real, con costo y riesgo asociados. La VR
permite replicar esa percepción espacial antes de la primera clase
práctica.

La propuesta técnica (hardware Meta Quest 3, motor Unity, alcance del
prototipo) está detallada en la Sección III del informe
(`report/main.tex`) y registrada en [`AGENTS.md`](./AGENTS.md).

Ver [`specs.md`](./specs.md) para la pauta del Hito 1,
[`AGENTS.md`](./AGENTS.md) para las decisiones de diseño/alcance del
proyecto, y [`report/`](./report) para el informe en LaTeX/IEEE.

## Estructura del repositorio

```
report/
  main.tex                 # informe (nuestro contenido, formato IEEE conference)
  references.bib           # bibliografía (6 referencias, verificadas contra fuente primaria)
  figures/
    fig1_arquitectura.png  # diagrama de arquitectura hardware/software
    fig3_storyboard.png    # storyboard del Caso de uso 1 (4 paneles)
  IEEEtran.cls             # clase oficial IEEE, vendorizada
  template/
    conference_101719.tex  # plantilla oficial IEEE sin modificar (referencia)
presentation/
  hito1-vrcar.pptx         # deck de la presentación oral (Hito 1)
unity/                     # proyecto Unity 6000.4.0f1 (prototipo VR)
  Assets/Scripts/          # lógica: Core, Vehicle, Obstacles, UI, XR
  Assets/Scenes/           # Parking_Perpendicular.unity (escena del prototipo)
  Assets/Tests/            # pruebas NUnit (Edit/Play Mode)
  Assets/Editor/           # generadores de escena y automatización de build
  ProjectSettings/, Packages/  # configuración y dependencias (versionadas)
Build/
  VRCar-Hito2-Windows.zip  # ejecutable de escritorio del Hito 2
docs/
  pr-media/                # capturas, GIF y video del prototipo
.devcontainer/             # entorno del Codespace (instala TeX Live solo)
.vscode/                   # extensiones y settings recomendados (LaTeX Workshop)
Makefile                   # make setup / pdf / watch / clean
specs.md                   # pauta de la entrega (Hito 1)
AGENTS.md                  # decisiones del proyecto (hardware, alcance, etc.)
```

## Stack tecnológico

| Capa | Elección | Por qué |
|---|---|---|
| Hardware objetivo | Meta Quest 3 (standalone) | Controles físicos para acelerar, frenar y girar; despliegue sin PC (ver informe, III.C) |
| Motor | **Unity 6000.4.0f1** | Versión fijada en `unity/ProjectSettings/ProjectVersion.txt` |
| Lenguaje | C# | Componentes pequeños: física, detección, HUD, feedback |
| XR | OpenXR (`com.unity.xr.openxr` 1.16.1) + XR Interaction Toolkit 3.4.0 | Estándar abierto, perfil de controles Meta Quest Touch habilitado |
| Entrada | Input System 1.19.0 + `UnityEngine.XR.InputDevices` | Gatillos y joystick del Quest, con teclado como alternativa sin visor |
| Física | `Rigidbody` + `AddForce` (auto de 1.200 kg) | Física arcade: simple y estable |
| Detección de obstáculos | Raycasts de 10 m, solo objetos con tag `Obstacle` | Distancia desde el parachoques al obstáculo |
| Pruebas | NUnit vía Unity Test Framework 1.6.0 | Lógica del vehículo, entrada, HUD y feedback |

## Instalación desde cero

1. Instalar **Unity Hub** y, desde él, **Unity 6000.4.0f1** con el módulo *Windows Build Support* (y *Android Build Support* si se quiere compilar el `.apk` para Quest 3).
2. Clonar el repositorio: `git clone git@github.com:eanorambuena/xr-project.git`.
3. En Unity Hub: **Add → Add project from disk** y elegir la carpeta **`unity/`** (no la raíz del repo). Unity descarga los paquetes de `Packages/manifest.json` solo.
4. Abrir `Assets/Scenes/Parking_Perpendicular.unity` y pulsar **Play** (sin visor se maneja con teclado).
5. Si la escena se pierde, se regenera desde el menú **VRCar → 1. Configurar XR** y **VRCar → 2. Crear escena de estacionamiento**.

## Ejecutar el prototipo

**Sin instalar Unity (ejecutable de Windows):**

1. Descargar [`Build/VRCar-Hito2-Windows.zip`](./Build/VRCar-Hito2-Windows.zip) y descomprimirlo.
2. Ejecutar `unity.exe`. No necesita intervención del desarrollador.
3. Con teclado se juega de inmediato. Con el visor, conectar el Quest 3 al PC con **Meta Quest Link** y dejarlo como runtime OpenXR activo antes de abrir el programa.

**Compilar de nuevo:** en Unity, **File → Build Profiles → Windows → Build**, con la escena `Parking_Perpendicular` en la lista. Para el Quest 3 (`.apk`) se necesita el módulo Android de Unity.

## Interacción

Objetivo del ejercicio: estacionar el auto en el espacio **A4** (líneas amarillas) sin tocar los autos vecinos.

| Acción | Quest 3 (controles) | Teclado (sin visor) |
|---|---|---|
| Acelerar | Gatillo derecho | W / ↑ |
| Frenar | Gatillo izquierdo | S / ↓ / Espacio |
| Reversa | Detenido, mantener el freno ~0,6 s (en reversa el gatillo izquierdo retrocede y el derecho frena) | Igual con S |
| Girar el volante | Joystick derecho | A / D / ← → |
| Reiniciar ejercicio | Mantener A o X 1 s | R |
| Mirar | Mover la cabeza (el conductor no se desplaza) | — |

Respuestas del sistema:

- **HUD de distancia** con semáforo: verde a más de 1,5 m, amarillo entre 1,0 y 1,5 m, rojo a 1,0 m o menos.
- **Sensor de retroceso**: barras y pitidos según la distancia al obstáculo.
- **Choque**: el auto se pinta de rojo, suena un beep y vibran los controles.
- **Espejos laterales e interior**, velocímetro con marcha D/R y evaluación del estacionamiento (márgenes izquierdo y derecho, tope y ángulo).

Capturas, GIF y video de la escena están en [`docs/pr-media/`](./docs/pr-media).

## Pruebas

El proyecto tiene **104 métodos de prueba NUnit** escritos en `unity/Assets/Tests/` (Edit Mode y Play Mode). Se ejecutan desde **Window → General → Test Runner**. Hasta esta entrega **no hay un CI que las corra ni resultados guardados en el repositorio**: el estado de las pruebas se verifica abriendo el Test Runner en Unity.

## Pendientes y límites conocidos

- **`.apk` para Meta Quest 3**: no está en esta entrega; el equipo de build no tiene instalado el módulo Android de Unity.
- **Calibración de altura de ojos**: el informe la propone, pero hoy la altura es fija.
- **Medir la holgura lateral**: los rayos van rectos hacia adelante y hacia atrás, no hay detección lateral.
- **Sonido de choque y de proximidad**: son sonidos generados por código; el audio definitivo queda para el arte final.
- **CI real**: el archivo `unity/.github/workflows/ci.yml` solo cuenta pruebas con `grep`, y GitHub no lo ejecuta porque no está en `.github/` de la raíz.
- **Casos de uso 1 y 3** del informe (juicio de esquinas y circuito) siguen planificados para los hitos siguientes.
- Los documentos dentro de `unity/*.md` usan una numeración de hitos antigua ("Hito 1" para el prototipo). Aquí manda el calendario del curso: Hito 1 = informe (8-sep), Hito 2 = prototipo lo-fi (13-oct), Hito 3 = prototipo final (24-nov).

## Compilar el informe

El informe usa la clase `IEEEtran` (plantilla oficial IEEE conference:
https://www.ieee.org/content/dam/ieee-org/ieee/web/org/pubs/conference-latex-template_10-17-19.zip,
copia sin modificar en `report/template/conference_101719.tex`, y
`report/IEEEtran.cls` vendorizada en el repo).

### Setup

En un **Codespace nuevo no hay que hacer nada**: `.devcontainer/` instala
todo al crearlo. En cualquier otro entorno Debian/Ubuntu, un comando:

```bash
make setup
```

Instala TeX Live y `latexmk`. (Detalle no obvio: `IEEEtran.bst` viene en
el paquete `texlive-publishers`; sin él bibtex falla y *todas* las citas
salen sin resolver. La `IEEEtran.cls` vendorizada no cubre el `.bst`.)

### Compilar

```bash
make pdf      # compila -> report/.build/main.pdf
make watch    # recompila solo cada vez que guardas
make clean    # borra los auxiliares
```

`make` a secas lista los comandos disponibles.

### En VS Code

Al abrir el repo, VS Code ofrece instalar las extensiones de
`.vscode/extensions.json`. Con **LaTeX Workshop** queda una experiencia
equivalente a Overleaf: el PDF se abre en una pestaña al lado del `.tex`,
se recompila al guardar, y con ctrl+click se salta entre el código y el
PDF (SyncTeX). Los auxiliares van a `report/.build/` (ignorado por git).

### En Overleaf

El repo está sincronizado con Overleaf, que ya incluye la clase y el
estilo bibliográfico. Ojo: Overleaf publica sus cambios como ramas
`overleaf-YYYY-MM-DD-HHMM`, así que un `git pull` de `main` no las
muestra — hay que hacer `git fetch --all` y revisarlas.

## Estado del informe (Hito 1)

Compila limpio (sin warnings de referencias ni citas) en **4 páginas
incluyendo la bibliografía**, dentro del límite de 5 páginas de la pauta.
