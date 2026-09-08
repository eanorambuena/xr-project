# xr-project

Proyecto de curso **IDI 3185 · Realidad Extendida** — DILAB.

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
  hito1-vrcar.pptx         # deck de la presentación oral
.devcontainer/             # entorno del Codespace (instala TeX Live solo)
.vscode/                   # extensiones y settings recomendados (LaTeX Workshop)
Makefile                   # make setup / pdf / watch / clean
specs.md                   # pauta de la entrega (Hito 1)
AGENTS.md                  # decisiones del proyecto (hardware, alcance, etc.)
```

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

## Estado

Compila limpio (sin warnings de referencias ni citas) en **4 páginas
incluyendo la bibliografía**, dentro del límite de 5 páginas de la pauta.
