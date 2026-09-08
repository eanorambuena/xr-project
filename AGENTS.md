# AGENTS.md — Decisiones del proyecto

Registro de decisiones y respuestas del equipo, recogidas durante el
"drill de dudas" para especificar el proyecto antes de escribir el
informe (Hito 1) y planificar el desarrollo. Este archivo es la fuente
de verdad para cualquier agente que trabaje en el repo — antes de asumir
un detalle del proyecto, revisar aquí primero.

## Proyecto

- **Título tentativo:** Percepción a Escala Real: un Simulador de
  Conducción en Realidad Virtual para el Aprendizaje del Tamaño y las
  Distancias del Vehículo (ver `report/main.tex`).
- **Dominio/usuarios:** alumnos de escuelas de manejo / conductores
  primerizos, aprendiendo a juzgar el tamaño del vehículo y distancias
  a obstáculos antes de sus clases prácticas reales.

## Hardware y software (stack objetivo)

- **Headset:** Meta Quest 3 (standalone). Se descartó Apple Vision Pro:
  sin controles físicos (difícil para input de manejo), SDK de Unity
  aún limitado (PolySpatial), y costo mucho mayor para un proyecto de
  curso.
- **Motor:** Unity, con XR Interaction Toolkit.
- **Alcance del prototipo:** simulación de manejo básica (acelerar,
  frenar, girar en un circuito simple) **+** percepción de escala/
  distancia (el usuario experimenta el tamaño 1:1 del vehículo y debe
  juzgar distancias a obstáculos). No es un simulador de manejo
  completo (sin tránsito, sin reglas de tránsito complejas, sin física
  vehicular avanzada) — eso queda fuera de alcance del prototipo de
  este curso.

## Trabajos relacionados / referencias

- El equipo no tenía referencias candidatas aún. Se pidió a Claude
  buscar literatura relevante (simuladores de conducción VR, percepción
  de profundidad/escala en VR, presencia, propiocepción) y dejarlas
  como candidatas en `report/references.bib` para que el equipo las
  revise antes de citarlas en el informe.

### Deep research de verificación (post-integración)

Tras integrar el texto de Matías, se pidió una verificación exhaustiva
de las 6 citas contra las fuentes primarias (no solo snippets de
búsqueda). Hallazgos:

- **LaValle~\cite{lavalle2016} — corregido.** Se descargó y leyó el
  Capítulo 1 completo del libro (gratis en msl.cs.uiuc.edu/vr/vrch1.pdf).
  La definición que el informe le atribuía ("sustituye la percepción
  sensorial... mediante tracking + renderizado sincronizado") **no es
  la definición real de LaValle**. Su definición real: *"Inducing
  targeted behavior in an organism by using artificial sensory
  stimulation, while the organism has little or no awareness of the
  interference."* El concepto de tracking+renderizado sí aparece en el
  libro, pero como parte de la discusión de *interactividad*
  (lazo abierto/cerrado), no como la definición de VR. Ya corregido en
  `main.tex` para atribuir correctamente cada idea. También se corrigió
  el año en `references.bib`: 2016 era el borrador, la edición impresa
  de Cambridge University Press es de **2017** (ISBN 978-1-107-19893-7).
- **Slater~\cite{slater2018} — confirmado.** Se descargó el PDF
  completo (publicationslist.org/data/melslater, copia del propio
  autor). La distinción inmersión/presencia y la cita de "Place
  Illusion" son fieles al texto. Detalle menor: el paper sí menciona
  resolución y estéreo como parte de inmersión, pero **no** menciona
  latencia — el informe ya lo cubre bien con la frase genérica
  "la literatura general del área", así que no fue necesario tocarlo.
- **Agrawal et al.~\cite{agrawal2018} — confirmado con datos duros
  no incluidos aún.** Se confirmó que el estudio comparó 3 grupos:
  V-RAPT (VR), RAPT (entrenamiento tradicional de escritorio/pantalla
  única) y placebo — validando la frase "métodos de entrenamiento en
  simuladores tradicionales" del informe. Encontrado (vía fuente
  secundaria, no verificado contra el PDF original por paywall):
  V-RAPT 86.25% vs. RAPT 62.36% vs. placebo 30.97% de peligros
  latentes anticipados. **No se agregó al informe** — el número viene
  de un resumen de terceros, no del paper mismo; si el equipo quiere
  usarlo, debería confirmarlo contra el PDF real antes de citarlo con
  cifras exactas.
- **El Jamiy \& Marsh~\cite{eljamiy2019}** — la subestimación de
  distancias en HMD está ampliamente corroborada en la literatura
  citante, aunque no se encontró el porcentaje exacto del paper
  original (bloqueado por Wiley). El informe ya lo redacta sin
  comprometerse a una cifra ("en cierto grado"), así que está a salvo.
- **Chidambaram et al.~\cite{chidambaram2025} — confirmado**, con un
  detalle curioso: el abstract dice que usaron **Meta Quest Pro**
  (no Quest 3) como hardware. El informe solo dice "visores Meta
  Quest" genérico, así que sigue siendo preciso — no hace falta tocar
  nada, pero el equipo no debería asumir que el paper usó Quest 3.
- **Pouke et al.~\cite{pouke2022} — confirmado.** El abstract completo
  confirma que el estudio es sobre plausibilidad física con avatares
  de escala corporal alterada (40 participantes, condición
  sincrónica/asincrónica de estimulación visuo-táctil) — no es
  específico de contextos automotrices, tal como ya se había señalado
  al integrar el texto (el informe ya lo aplica "a nivel teórico",
  correctamente acotado).

## Diagramas y assets visuales

- La mayoría de los diagramas (arquitectura del sistema, diagrama de
  bloques hardware/software, wireframes de interfaz) se harán en
  **TikZ** directamente en LaTeX — no requieren fotos.
- El **storyboard** ya está resuelto: `report/figures/storyboard.jpg`
  (4 paneles — Calibración, Conducción CU1, Retroalimentación,
  Detención segura), agregado por el equipo y ya referenciado
  (activo, no comentado) en `main.tex` como `fig:storyboard`.
- La "Descripción del sistema" (III.B) fue reescrita por Matías con
  más detalle técnico (calibración de altura de ojos, control por
  gatillos/joystick, módulo de distancia por raycasting + HUD/audio,
  3 escenas independientes por caso de uso) — integrada en `main.tex`
  con 2 ajustes: el diagrama de arquitectura TikZ ahora incluye el
  cálculo de distancia y el HUD/audio (antes solo física+render), y
  la justificación de por qué no hay cybersickness se corrigió (el
  argumento original decía que evitar teletransporte reduce el
  mareo, lo cual no aplica bien a un simulador de manejo — se
  reemplazó por el argumento correcto: el habitáculo fijo en el
  campo visual periférico da un marco de referencia estático, mecanismo
  real de reducción de vección; se dejó como riesgo residual a
  evaluar en Entrega 2, no como problema resuelto).

## Casos de uso (aprobados)

1. **CU1 — Juicio de esquinas:** el usuario, sentado en el asiento del
   conductor, debe pasar el vehículo por un espacio angosto (ej. entre
   conos o un portón) sin golpear las esquinas, para practicar dónde
   termina el auto.
2. **CU2 — Estacionamiento/aproximación a obstáculo:** el usuario debe
   aparcar o detenerse a una distancia segura de un obstáculo (auto,
   muro, persona), para practicar el juicio de distancia frontal/
   trasera.
3. **CU3 — Manejo básico en circuito:** el usuario acelera, frena y
   gira en un circuito simple con curvas y un cruce, para practicar
   control básico del vehículo junto con la percepción de escala en
   movimiento.

## Assets vendorizados

- `report/IEEEtran.cls` — clase oficial IEEE, extraída de
  `IEEE_Conference_Template.zip` y vendorizada en el repo para poder
  compilar localmente sin depender de una instalación TeX Live
  completa.
- `IEEE_Conference_Template.zip` (raíz del repo) — archivo original
  completo (`IEEEtran.cls`, `fig1.png`, `conference_101719.pdf`,
  `IEEEtran_HOWTO.pdf`, `conference_101719.tex`), tal como lo provee
  IEEE. Se mantiene commiteado porque el sitio de IEEE bloquea
  descargas no-navegador (CloudFront WAF challenge) — no se puede
  re-descargar programáticamente si se vuelve a borrar.

## Plan de implementación (Hito 1)

- El plan spec-driven que completó el informe está en
  `docs/superpowers/plans/2026-09-07-hito1-informe-xr.md` (todas las
  tareas de contenido — referencias, casos de uso, diagramas,
  tecnologías/alcance, plan de trabajo, README — ya ejecutadas).
- Las 5 referencias en `report/references.bib` son reales,
  verificadas por búsqueda web (autor/venue/DOI confirmados), no
  inventadas.
- Verificación estructural hecha sin compilador LaTeX disponible en
  este entorno (no hay `pdflatex`/`tectonic`): claves `\cite{}` vs.
  `references.bib`, `\ref{}` vs. `\label{}`, y balance de entornos
  (`figure`/`table`/`tikzpicture`/`itemize`) — todo consistente. La
  compilación real y el conteo de páginas (límite 5, sin referencias)
  **deben verificarse en Overleaf** antes de subir el PDF al buzón.

## Pendiente (requiere al equipo, no a Claude)

- [x] Nombres de los integrantes: Estefanía Pakarati, Emmanuel
      Norambuena, Matías Harrison (ya en `main.tex`).
- [x] Correo de Emmanuel Norambuena: `eanorambuena@uc.cl` (ya en
      `main.tex`).
- [x] Correo de Matías Harrison: `matiasharrison@uc.cl` (ya en
      `main.tex`).
- [ ] Correo @uc.cl de Estefanía Pakarati (bloque de autores en
      `main.tex`, hoy con placeholder `correo1@uc.cl`).
- [x] Abstract redactado (`main.tex`): lidera con la mejora
      perceptual (juicio de tamaño/distancia) como aporte medible, y
      plantea el menor riesgo en clases prácticas como hipótesis a
      futuro, no como promesa — decisión explícita del usuario tras
      cuestionar que "menos sesiones" sea una consecuencia segura
      (practicar más en VR podría generar más interés en seguir
      practicando, no menos). Menciona Quest 3 + Unity explícitamente
      (a la espera de ejemplos del usuario sobre qué tan específico
      debería ser, por si se ajusta después).
- [x] Fechas oficiales de los 3 hitos del curso: Hito 1 (este informe)
      **8 de septiembre de 2026**, Hito 2 (prototipo lo-fi) **13 de
      octubre de 2026**, Hito 3 (prototipo final) **24 de noviembre de
      2026**. La tabla de cronograma en `main.tex` ya usa fechas
      intermedias reales, espaciadas en martes (día de clases) entre
      esos hitos — el equipo puede ajustar las fechas intermedias, las
      de Entrega 2/3 son fijas (dadas por el curso).
- [ ] Responsable(s) por actividad en el plan de trabajo (por ahora
      genérico "el equipo" — reemplazar por integrante si se reparten
      tareas).
- [ ] Arte final del storyboard: `report/figures/storyboard.png` (ver
      `report/figures/STORYBOARD.md`), y luego descomentar la figura
      en `report/main.tex`.
- [ ] Compilar en Overleaf, confirmar ≤5 páginas (sin referencias), y
      subir el PDF resultante al buzón.
