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

## Diagramas y assets visuales

- La mayoría de los diagramas (arquitectura del sistema, diagrama de
  bloques hardware/software, wireframes de interfaz) se harán en
  **TikZ** directamente en LaTeX — no requieren fotos.
- El **storyboard** (ilustración de un usuario interactuando con el
  sistema en una situación típica) sí requiere arte/foto real — se deja
  como placeholder (`\includegraphics` comentado, con caption listo) en
  `report/figures/` para que el equipo lo agregue.

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
- [ ] Correos @uc.cl de Estefanía Pakarati y Matías Harrison (bloque
      de autores en `main.tex`, hoy con placeholders
      `correo1@uc.cl` / `correo3@uc.cl`).
- [ ] Fechas de compromiso para Entrega 2 (prototipo lo-fi) y Entrega 3
      (prototipo final), para la sección "Plan de trabajo". Sin fecha
      real disponible aún — el informe usa placeholders `[FECHA]` a
      reemplazar por el equipo.
- [ ] Responsable(s) por actividad en el plan de trabajo (por ahora
      genérico "el equipo" — reemplazar por integrante si se reparten
      tareas).
- [ ] Arte final del storyboard: `report/figures/storyboard.png` (ver
      `report/figures/STORYBOARD.md`), y luego descomentar la figura
      en `report/main.tex`.
- [ ] Compilar en Overleaf, confirmar ≤5 páginas (sin referencias), y
      subir el PDF resultante al buzón.
