# Informe Hito 1 (XR) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Completar el informe IEEE del Hito 1 (`report/main.tex`) y el README, cumpliendo íntegramente la pauta de `specs.md`, dentro del límite de 5 páginas (sin referencias).

**Architecture:** Documento LaTeX único (`report/main.tex`, clase `IEEEtran`) con 4 secciones numeradas (I–IV) más bibliografía. Los diagramas se generan en TikZ inline (sin dependencias binarias); el storyboard queda como figura placeholder para arte del equipo. `references.bib` acumula las citas verificadas. `AGENTS.md` es la fuente de verdad de decisiones de alcance/hardware/software.

**Tech Stack:** LaTeX (`IEEEtran.cls`, vendorizado en `report/IEEEtran.cls`), TikZ/pgfplots para diagramas, BibTeX/`IEEEtran` bibliography style.

**Spec:** `specs.md` (pauta Hito 1) + decisiones registradas en `AGENTS.md`.

## Global Constraints

- Extensión máxima: **5 páginas** (sin contar referencias) — `specs.md:9`.
- Plantilla oficial IEEE conference (`IEEEtran`) — `specs.md:4`.
- Título/autores van en el bloque de título IEEE, **no** como sección numerada — `specs.md:17`.
- Presupuesto de páginas por sección: I Introducción ~0.5 pág, II Trabajos relacionados y marco teórico ~1 pág, III Propuesta de solución ~2.5 pág, IV Plan de trabajo ~0.5 pág — `specs.md:19-37`.
- III debe incluir: 3 casos de uso (usuario/acción/objetivo), descripción del sistema (interacciones, hardware/software, diagramas/wireframes, storyboard como figura), tecnologías y alcance (justificación, qué entra/sale del prototipo) — `specs.md:31-33`.
- El README debe documentar/actualizar la propuesta — `specs.md:12`.
- El informe se versiona en el repo con historial de cambios visible — `specs.md:42`.
- Entregable final: PDF compilado desde la fuente LaTeX, subido al buzón — `specs.md:13`.
- Hardware/software objetivo: **Meta Quest 3 + Unity (XR Interaction Toolkit)** — decidido en `AGENTS.md`.
- Alcance del prototipo: manejo básico (acelerar/frenar/girar en circuito simple) **+** percepción de escala/distancia — decidido en `AGENTS.md`.
- 3 casos de uso aprobados (CU1 juicio de esquinas, CU2 estacionamiento/obstáculo, CU3 manejo básico en circuito) — `AGENTS.md`.

---

### Task 1: Poblar `references.bib` con literatura verificada

**Files:**
- Modify: `report/references.bib`

**Interfaces:**
- Produces: claves BibTeX `slater2018`, `eljamiy2019`, `agrawal2018`, `pouke2022`, `chidambaram2025` — usadas por Task 2 vía `\cite{}`.

- [ ] **Step 1: Agregar las 5 entradas verificadas (autores/venue/DOI confirmados por búsqueda, no inventados)**

```bibtex
@article{slater2018,
  author  = {Slater, Mel},
  title   = {Immersion and the illusion of presence in virtual reality},
  journal = {British Journal of Psychology},
  volume  = {109},
  number  = {3},
  pages   = {431--433},
  year    = {2018},
  doi     = {10.1111/bjop.12305}
}

@article{eljamiy2019,
  author  = {El Jamiy, Fatima and Marsh, Ronald},
  title   = {Survey on depth perception in head mounted displays: distance estimation in virtual reality, augmented reality, and mixed reality},
  journal = {IET Image Processing},
  volume  = {13},
  number  = {5},
  pages   = {707--712},
  year    = {2019},
  doi     = {10.1049/iet-ipr.2018.5920}
}

@article{agrawal2018,
  author  = {Agrawal, Ravi and Knodler, Michael and Fisher, Donald L. and Samuel, Siby},
  title   = {Virtual Reality Headset Training: Can It Be Used to Improve Young Drivers' Latent Hazard Anticipation and Mitigation Skills},
  journal = {Transportation Research Record},
  volume  = {2672},
  pages   = {20--30},
  year    = {2018},
  doi     = {10.1177/0361198118758311}
}

@article{pouke2022,
  author  = {Pouke, Matti and Center, Evan G. and Chambers, Alexis P. and Pouke, Sakaria and Ojala, Timo and LaValle, Steven M.},
  title   = {The Body Scaling Effect and Its Impact on Physics Plausibility},
  journal = {Frontiers in Virtual Reality},
  year    = {2022},
  eprint  = {2206.15218},
  archivePrefix = {arXiv}
}

@inproceedings{chidambaram2025,
  author    = {Chidambaram, Nishanth and Liu, Weichen and Bedmutha, Manas Satish and Weibel, Nadir and Chen, Chen},
  title     = {{DriveSimQuest}: A VR Driving Simulator and Research Platform on Meta Quest with Unity},
  booktitle = {Adjunct Proceedings of the 38th Annual ACM Symposium on User Interface Software and Technology (UIST Adjunct '25)},
  year      = {2025},
  eprint    = {2508.11072},
  archivePrefix = {arXiv}
}
```

- [ ] **Step 2: Verificar que no quedan claves duplicadas y que el archivo es BibTeX válido**

Run: `grep -c '^@' report/references.bib`
Expected: `5`

- [ ] **Step 3: Commit**

```bash
git add report/references.bib
git commit -m "docs: add verified references for related work section"
```

---

### Task 2: Redactar Sección II — Trabajos relacionados y marco teórico

**Files:**
- Modify: `report/main.tex` (sección `\label{sec:relacionados}`)

**Interfaces:**
- Consumes: claves de Task 1 (`slater2018`, `eljamiy2019`, `agrawal2018`, `pouke2022`, `chidambaram2025`).

- [ ] **Step 1: Reemplazar el bloque de comentarios TODO por el contenido real**

Reemplazar:
```latex
% TODO: citar con \cite{} y completar report/references.bib
% - Simuladores de conducción existentes (2D y VR) y sus limitaciones.
% - Literatura de percepción de profundidad y escala en VR (estereopsis,
%   presencia, propiocepción).
% - Brecha que aborda este proyecto: escala 1:1 + juicio corporal de
%   distancias, no solo reacción a reglas de tránsito.
```

por:
```latex
Los simuladores de conducción en VR existentes se han enfocado
mayoritariamente en el entrenamiento de reglas de tránsito y en la
anticipación de riesgos: \cite{agrawal2018} muestra que un
entrenamiento con visor VR mejora la capacidad de conductores
jóvenes de anticipar peligros latentes en comparación con
entrenamiento tradicional, y \cite{chidambaram2025} presenta
DriveSimQuest, una plataforma de simulación de conducción en Meta
Quest con Unity orientada a capturar señales conductuales (mirada,
gestos) del usuario. Ninguno de estos sistemas, sin embargo, evalúa
ni entrena explícitamente el juicio de tamaño del vehículo o de
distancias a escala 1:1, que es el problema que aborda este proyecto.

El sustento teórico de por qué la VR permite abordar esta brecha
mejor que un simulador 2D proviene de tres líneas de investigación.
Primero, la teoría de \textit{presencia} de Slater~\cite{slater2018}
distingue entre inmersión (una propiedad objetiva del sistema, p. ej.
campo de visión, tracking) y presencia (la sensación subjetiva de
"estar ahí"), y sostiene que mayor inmersión habilita mayor presencia
— un monitor 2D, con bajo campo de visión y sin tracking de cabeza,
limita estructuralmente cuánta presencia espacial puede generar.
Segundo, la literatura de percepción de profundidad en visores VR
\cite{eljamiy2019} documenta que, si bien los HMD subestiman
distancias respecto del mundo físico en cierto grado, proveen
estereopsis y paralaje de movimiento —señales de profundidad
ausentes en una pantalla plana— que son las que permiten a una
persona juzgar corporalmente qué tan cerca pasa de un obstáculo.
Tercero, el efecto de \textit{escalamiento corporal}
\cite{pouke2022} muestra que el propio cuerpo del usuario actúa como
referencia perceptual para estimar tamaños y distancias del entorno
virtual — lo que explica por qué experimentar el habitáculo del
vehículo a escala 1:1, con el propio cuerpo como referencia, es
necesario para que el usuario internalice el tamaño real del auto de
una forma que una vista en tercera persona en pantalla no logra.
```

- [ ] **Step 2: Verificar que no quedan comentarios TODO ni placeholders en la sección**

Run: `grep -n "TODO" report/main.tex`
Expected: sin resultados dentro del rango de `sec:relacionados` (puede haber otros TODO en secciones aún no tocadas por tareas posteriores).

- [ ] **Step 3: Commit**

```bash
git add report/main.tex
git commit -m "docs: write related work and theoretical framework section"
```

---

### Task 3: Completar los 3 casos de uso (Sección III)

**Files:**
- Modify: `report/main.tex` (subsección "Casos de uso")

**Interfaces:**
- Consumes: los 3 casos de uso aprobados en `AGENTS.md`.

- [ ] **Step 1: Reemplazar los 3 bloques placeholder (`Nombre.` / `\dots`) por el contenido real**

```latex
\textbf{Caso de uso 1: Juicio de esquinas.}
\begin{itemize}
    \item \textbf{Usuario:} alumno de escuela de manejo, sentado en el
    asiento del conductor dentro del simulador.
    \item \textbf{Acción:} conducir el vehículo a través de un espacio
    angosto (p. ej. entre conos o un portón) sin golpear las esquinas
    del auto.
    \item \textbf{Objetivo:} internalizar, con el propio cuerpo,
    dónde terminan físicamente las esquinas del vehículo.
\end{itemize}

\textbf{Caso de uso 2: Estacionamiento y aproximación a un obstáculo.}
\begin{itemize}
    \item \textbf{Usuario:} alumno de escuela de manejo.
    \item \textbf{Acción:} estacionar o detener el vehículo a una
    distancia segura de un obstáculo (otro auto, un muro, una
    persona), tanto en maniobras frontales como en reversa.
    \item \textbf{Objetivo:} practicar el juicio de distancia
    frontal/trasera antes de enfrentarlo en un vehículo real.
\end{itemize}

\textbf{Caso de uso 3: Manejo básico en circuito.}
\begin{itemize}
    \item \textbf{Usuario:} alumno de escuela de manejo.
    \item \textbf{Acción:} acelerar, frenar y girar en un circuito
    simple con curvas y un cruce.
    \item \textbf{Objetivo:} practicar el control básico del
    vehículo junto con la percepción de escala en movimiento, no solo
    en situaciones estáticas.
\end{itemize}
```

- [ ] **Step 2: Verificar que no quedan `\dots` ni "Nombre." placeholder en la subsección**

Run: `grep -n '\\dots\|Caso de uso.*Nombre' report/main.tex`
Expected: sin resultados.

- [ ] **Step 3: Commit**

```bash
git add report/main.tex
git commit -m "docs: write the three use cases"
```

---

### Task 4: Diagrama de arquitectura + wireframe HUD en TikZ (Descripción del sistema)

**Files:**
- Modify: `report/main.tex` (subsección "Descripción del sistema", añadir `\usepackage{tikz}` al preámbulo)

**Interfaces:**
- Produces: figuras `fig:arquitectura` y `fig:hud`, referenciadas por texto de la subsección.

- [ ] **Step 1: Agregar `tikz` al preámbulo**

```latex
\usepackage{tikz}
\usetikzlibrary{arrows.meta, positioning}
```

- [ ] **Step 2: Reemplazar el comentario placeholder de la subsección por contenido + 2 figuras TikZ**

```latex
El usuario interactúa con el sistema sentado en un asiento físico
(o silla) que hace de referencia postural, usando un Meta Quest 3
para la vista estereoscópica y el seguimiento de cabeza, y los
controles del Quest 3 para acelerar/frenar (gatillos) y girar
(joystick), simulando un volante simplificado. La Figura
\ref{fig:arquitectura} resume los componentes de hardware y software
y su flujo de datos: el Quest 3 reporta pose de cabeza y controles a
la aplicación Unity, que actualiza la física del vehículo (posición,
colisiones) y renderiza el entorno 1:1 de vuelta al visor.

\begin{figure}[htbp]
\centering
\begin{tikzpicture}[
  node distance=8mm and 10mm,
  block/.style={draw, rounded corners, align=center, minimum height=8mm, minimum width=24mm, font=\scriptsize},
  arr/.style={-Stealth, thick}
]
\node[block] (hmd) {Meta Quest 3\\(visor + controles)};
\node[block, right=of hmd] (unity) {Unity\\XR Interaction Toolkit};
\node[block, below=of unity] (fisica) {Física del vehículo\\y colisiones};
\node[block, left=of fisica] (render) {Render del entorno\\(escala 1:1)};

\draw[arr] (hmd.east) -- node[above, font=\tiny]{pose cabeza + input} (unity.west);
\draw[arr] (unity) -- (fisica);
\draw[arr] (fisica) -- (render);
\draw[arr] (render.west) -- (hmd.south west) node[midway, below, font=\tiny]{frames estéreo};
\end{tikzpicture}
\caption{Arquitectura de hardware y software del prototipo.}
\label{fig:arquitectura}
\end{figure}

Durante la conducción, un indicador HUD minimalista (Figura
\ref{fig:hud}), anclado al parabrisas virtual, muestra la distancia
estimada al obstáculo más cercano en los casos de uso de
estacionamiento, sin cubrir la vista directa del entorno —el juicio
de distancia principal lo hace el usuario con su propia percepción
espacial, no leyendo un número.

\begin{figure}[htbp]
\centering
\begin{tikzpicture}[font=\scriptsize]
\draw (0,0) rectangle (5,3);
\draw[dashed] (0.3,0.3) rectangle (4.7,2.4);
\node at (2.5,1.35) {vista del entorno (cámara VR)};
\node[draw, rounded corners, fill=white] at (2.5,2.7) {distancia: 0.8 m};
\end{tikzpicture}
\caption{Wireframe del HUD de distancia dentro del visor.}
\label{fig:hud}
\end{figure}
```

- [ ] **Step 3: Verificar sintaxis TikZ balanceada (llaves y `\begin`/`\end` parejos)**

Run: `grep -c '\\begin{tikzpicture}' report/main.tex; grep -c '\\end{tikzpicture}' report/main.tex`
Expected: ambos comandos devuelven `2`.

- [ ] **Step 4: Commit**

```bash
git add report/main.tex
git commit -m "docs: add system architecture and HUD wireframe diagrams"
```

---

### Task 5: Storyboard (figura placeholder) + descripción de paneles para el equipo

**Files:**
- Modify: `report/main.tex` (descomentar/ajustar bloque de figura storyboard)
- Create: `report/figures/STORYBOARD.md`

**Interfaces:**
- Produces: `report/figures/storyboard.png` como referencia esperada (no se crea en este task — la produce el equipo).

- [ ] **Step 1: Crear la descripción de paneles**

```markdown
# Storyboard — paneles a ilustrar/fotografiar

Guardar el resultado final como `report/figures/storyboard.png`
(usado por `report/main.tex`, figura `fig:storyboard`).

1. **Panel 1:** el usuario se coloca el Quest 3 y se sienta en la
   silla/asiento físico que simula el puesto del conductor.
2. **Panel 2:** vista en primera persona desde dentro del vehículo
   virtual — el usuario ve el habitáculo a escala 1:1 por primera vez.
3. **Panel 3:** el usuario conduce hacia un espacio angosto (CU1) y
   gira la cabeza para juzgar la distancia a los conos/portón.
4. **Panel 4:** el usuario se aproxima a un obstáculo y frena antes
   de la línea de distancia segura (CU2), con el HUD de distancia
   visible.
5. **Panel 5 (opcional):** vista externa (tercera persona, fuera del
   visor) mostrando a la persona físicamente en la silla, para dar
   contexto de la configuración real del setup.
```

- [ ] **Step 2: Ajustar el bloque de figura en `main.tex`**

```latex
\begin{figure}[htbp]
\centerline{\includegraphics[width=0.9\linewidth]{figures/storyboard.png}}
\caption{Storyboard de interacción típica del usuario con el sistema (ver \texttt{figures/STORYBOARD.md} para la descripción de cada panel).}
\label{fig:storyboard}
\end{figure}
```

- [ ] **Step 3: Commit**

```bash
git add report/main.tex report/figures/STORYBOARD.md
git commit -m "docs: add storyboard placeholder and panel description for the team"
```

---

### Task 6: Redactar "Tecnologías y alcance"

**Files:**
- Modify: `report/main.tex` (subsección "Tecnologías y alcance")

- [ ] **Step 1: Reemplazar el comentario placeholder por el contenido real**

```latex
Como hardware objetivo se eligió Meta Quest 3 por sobre alternativas
como Apple Vision Pro o un sistema PC VR con base stations (p. ej.
Valve Index): es standalone (no depende de un PC dedicado ni de
sensores externos, lo que facilita su uso en un contexto de escuela
de manejo), cuenta con controles físicos que permiten mapear
input de aceleración/freno/giro (a diferencia de Vision Pro, que solo
ofrece manos y ojos como input), y tiene un costo significativamente
menor que ambas alternativas — relevante para un proyecto de curso.
Como motor se eligió Unity con XR Interaction Toolkit, por su soporte
maduro y nativo para Quest y su documentación extensa para
interacción en VR.

Dentro del alcance del prototipo de este curso están: la simulación
de manejo básica (acelerar, frenar, girar en un circuito simple), la
percepción de escala 1:1 del vehículo, y los tres casos de uso
descritos (juicio de esquinas, estacionamiento/obstáculo, manejo en
circuito). Quedan fuera de alcance: la simulación de tránsito con
otros vehículos o peatones, la física vehicular avanzada (transmisión,
tracción, condiciones climáticas), y el cumplimiento de reglas de
tránsito complejas — funcionalidades propias de un simulador de
manejo completo, no del enfoque de este proyecto en percepción
espacial.
```

- [ ] **Step 2: Verificar que no queda el comentario placeholder original**

Run: `grep -n "Justificación técnica del hardware/software" report/main.tex`
Expected: sin resultados (el comentario fue reemplazado).

- [ ] **Step 3: Commit**

```bash
git add report/main.tex
git commit -m "docs: write technologies and scope section"
```

---

### Task 7: Redactar "Plan de trabajo" (Sección IV)

**Files:**
- Modify: `report/main.tex` (sección `\label{sec:plan}`)

- [ ] **Step 1: Reemplazar el comentario de ejemplo por una tabla real con placeholders de fecha**

```latex
Hacia el prototipo lo-fi (Entrega 2) el foco es validar la
interacción básica (movimiento, HUD, un caso de uso) sin arte final;
hacia el prototipo final (Entrega 3) se integran los 3 casos de uso,
el entorno completo y las pruebas con usuarios. La Tabla
\ref{tab:cronograma} detalla las actividades comprometidas; las
fechas y responsables exactos quedan sujetos a lo que defina el
equipo (marcados como \texttt{[FECHA]} mientras no estén
confirmados).

\begin{table}[htbp]
\caption{Cronograma hacia las próximas entregas}
\begin{center}
\begin{tabular}{|p{3.4cm}|p{1.6cm}|p{1.6cm}|}
\hline
\textbf{Actividad} & \textbf{Resp.} & \textbf{Fecha} \\
\hline
Prototipo Unity: escena base + movimiento del vehículo & Equipo & [FECHA] \\
\hline
Implementar CU1 (juicio de esquinas) + HUD de distancia & Equipo & [FECHA] \\
\hline
Entrega 2 — prototipo lo-fi & Equipo & [FECHA] \\
\hline
Implementar CU2 y CU3 & Equipo & [FECHA] \\
\hline
Arte final (storyboard, entorno) e integración & Equipo & [FECHA] \\
\hline
Pruebas con usuarios y ajustes & Equipo & [FECHA] \\
\hline
Entrega 3 — prototipo final & Equipo & [FECHA] \\
\hline
\end{tabular}
\label{tab:cronograma}
\end{center}
\end{table}
```

- [ ] **Step 2: Verificar que la tabla compila sintácticamente (columnas balanceadas: 3 `&` por fila de datos, `\\` al final de cada fila)**

Run: `grep -n '&.*&' report/main.tex | grep -c '\\\\'`
Expected: `7` (las 7 filas de actividades).

- [ ] **Step 3: Commit**

```bash
git add report/main.tex
git commit -m "docs: write work plan section with schedule table"
```

---

### Task 8: Sincronizar README con el contenido final del informe

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Revisar que la sección "Propuesta" siga siendo consistente con la introducción y el alcance final del informe (Quest 3 + Unity, CU1-CU3, alcance in/out) — agregar una frase que mencione el hardware/motor objetivo**

```markdown
La propuesta técnica (hardware Meta Quest 3, motor Unity, alcance del
prototipo) está detallada en la Sección III del informe
(`report/main.tex`) y registrada en [`AGENTS.md`](./AGENTS.md).
```

Agregar esta línea al final de la sección "Propuesta" en `README.md`.

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: sync README proposal with final report scope"
```

---

### Task 9: Checklist de rúbrica + intento de compilación

**Files:** ninguno nuevo — verificación.

- [ ] **Step 1: Intentar compilar localmente**

Run: `cd report && pdflatex -interaction=nonstopmode main.tex`
Expected: si `pdflatex` no está instalado (confirmado que no lo está en este entorno), documentar en el commit que la compilación debe verificarse en Overleaf antes de subir el PDF al buzón.

- [ ] **Step 2: Checklist manual contra `specs.md`**

Verificar, marcando cada uno, que:
- [ ] Título/autores en bloque IEEE (no sección numerada) — `main.tex` ya lo tiene; autores siguen como placeholder "Integrante 1/2/3" pendiente de `AGENTS.md`.
- [ ] Sección I completa (ya estaba redactada antes de este plan).
- [ ] Sección II con citas `\cite{}` y sin TODO (Task 2).
- [ ] Sección III con 3 casos de uso, descripción del sistema + 2 figuras + storyboard placeholder, tecnologías y alcance (Tasks 3-6).
- [ ] Sección IV con tabla de cronograma (Task 7).
- [ ] README actualizado (Task 8).
- [ ] `references.bib` con 5 entradas verificadas (Task 1).

- [ ] **Step 3: Commit final si hubo ajustes del checklist**

```bash
git add -A
git commit -m "docs: address rubric checklist follow-ups"
```

---

## Pendiente fuera de este plan (requiere al equipo)

- Nombres/correos reales de los integrantes (bloque de autores).
- Fechas reales de Entrega 2 / Entrega 3 (reemplazar `[FECHA]`).
- Arte final del storyboard (`report/figures/storyboard.png`).
- Compilar en Overleaf y subir el PDF resultante al buzón.
