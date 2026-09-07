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
  main.tex               # informe (nuestro contenido, formato IEEE conference)
  references.bib         # bibliografía
  figures/                # diagramas, wireframes, storyboard
  IEEEtran.cls            # clase oficial IEEE, vendorizada para compilar sin TeX Live completo
  template/
    conference_101719.tex  # plantilla oficial IEEE sin modificar (referencia)
specs.md                  # pauta de la entrega (Hito 1)
AGENTS.md                  # decisiones del proyecto (hardware, alcance, etc.)
```

## Compilar el informe

El informe usa la clase `IEEEtran` (plantilla oficial IEEE conference:
https://www.ieee.org/content/dam/ieee-org/ieee/web/org/pubs/conference-latex-template_10-17-19.zip,
copia de la plantilla sin modificar en `report/template/conference_101719.tex`,
y `report/IEEEtran.cls` vendorizada en el repo para no depender de una
instalación TeX Live completa). La forma más simple es subir `report/` a
[Overleaf](https://www.overleaf.com/), que ya incluye la clase.
Localmente, con `pdflatex`/`bibtex` instalados:

```bash
cd report
pdflatex main.tex
bibtex main
pdflatex main.tex
pdflatex main.tex
```
