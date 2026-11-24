# AGENTS.md — Decisiones del proyecto

Registro de decisiones y respuestas del equipo, recogidas durante el
"drill de dudas" para especificar el proyecto antes de escribir el
informe (Hito 1) y planificar el desarrollo. Este archivo es la fuente
de verdad para cualquier agente que trabaje en el repo — antes de asumir
un detalle del proyecto, revisar aquí primero.

## Revisión de PRs con subagents

Cuando se pida "lanzar revieweres"/"reviewers" sobre un PR: usar el
prompt estándar de `docs/pr-review-prompt.md`, lanzando **dos subagents
independientes**, cada uno con nombre propio distinto (ej. Abel, Bert),
revisando el mismo PR desde cero (no confiar en resúmenes previos).

Un PR se considera listo solo cuando **ambos** reviewers devuelven
"Approval Recommended". Si alguno devuelve "Needs Further Review" o
"Changes Requested", evaluar qué es pertinente y corregir la PR antes
de cerrarla.

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

### Segunda ronda de deep research (verificación exhaustiva)

Segunda pasada, esta vez con acceso a **texto completo** de casi todas
las fuentes y con los metadatos contrastados contra **Crossref** (el
registro autoritativo de DOIs). Rutas que funcionaron cuando el editor
bloquea (403): PDFs de arXiv vía `curl`, la API de Crossref, la API de
Semantic Scholar (trae el abstract oficial), Unpaywall (encuentra
copias legales en repositorios; así se obtuvo Slater desde Zenodo).

**Errores encontrados y corregidos en esta ronda:**

- **Agrawal et al.~\cite{agrawal2018} — error factual, corregido.** El
  informe decía que la VR se comparó "con métodos de entrenamiento en
  simuladores tradicionales". Falso: el grupo de comparación fue
  **RAPT**, un programa de escritorio en pantalla 2D basado en vistas
  en planta de la vía. El **simulador de conducción fue el instrumento
  de evaluación** de los tres grupos, no de entrenamiento. La versión
  correcta favorece más nuestro argumento (VR le ganó a una pantalla
  plana con el mismo contenido). Se agregaron las cifras ya
  confirmadas contra el abstract del editor: n=36, V-RAPT 86,25\,\%
  vs. RAPT 62,36\,\% vs. placebo 30,97\,\%. Hardware del estudio:
  Oculus Rift.
- **Pouke et al.~\cite{pouke2022} — sobreatribución, corregida.** El
  informe decía que Pouke et al. "revelan" que tamaño y distancia se
  perciben de forma relativa al cuerpo. Eso es la *literatura previa
  que ellos revisan* (van der Hoort \& Ehrsson, Linkenauger, Ogawa),
  no su hallazgo. Su estudio **no logró replicar** el efecto sobre
  estimación de tamaños y encontró un efecto significativo en sentido
  contrario. Lo que sí confirmaron es el impacto sobre la
  plausibilidad física. Reescrito para atribuir cada cosa a quien
  corresponde e incluir la salvedad — es el mismo tipo de error que
  el de LaValle.
- **El Jamiy \& Marsh~\cite{eljamiy2019} — encuadre invertido,
  corregido.** El abstract oficial arranca diciendo que el visor
  "inevitablemente **disminuye** la percepción de profundidad". El
  informe lo usaba primero como evidencia de que la VR da buenas
  señales de profundidad y recién después concedía la subestimación.
  Eso lee la fuente a contrapelo. Reescrito para asumir la tensión de
  frente: **enseñamos juicio de distancias en un medio que se sabe que
  las distorsiona**, y el sesgo residual queda como variable a medir
  en las pruebas de usuario. Es la debilidad científica más obvia del
  proyecto y ahora está declarada, no escondida.
- **Slater~\cite{slater2018} — matizado.** Dos ajustes: (a) el informe
  atribuía a "la literatura general" una definición de inmersión que
  Slater da él mismo, y mejor — inmersión es el grado en que el
  sistema sostiene las *contingencias sensomotoras naturales* de la
  percepción; (b) "requisito estructural para habilitar presencia" era
  más fuerte que lo que Slater afirma (él habla de correspondencia
  entre niveles). Además se incorporó su propio contraejemplo de la
  pantalla ("apenas giras la cabeza dejas de percibir el mundo
  virtual"), que es exactamente nuestro argumento. Confirmado de nuevo:
  Slater **no** menciona latencia; sí resolución y estéreo.

**Confirmados sin cambios necesarios:**

- **LaValle~\cite{lavalle2016}** — leído el Cap. 1 completo otra vez.
  Definición verbatim: *"Inducing targeted behavior in an organism by
  using artificial sensory stimulation, while the organism has little
  or no awareness of the interference."* Y el pasaje de interactividad
  verbatim: *"Does the sensory stimulation depend on actions taken by
  the organism? If the answer is 'no', then the VR system is called
  open-loop; otherwise, it is closed-loop."* Ambas cosas están hoy
  correctamente atribuidas en el informe.
- **Chidambaram et al.~\cite{chidambaram2025}** — leído el PDF completo
  (7 pág.). Usa **Meta Quest Pro** (no Quest 3) más un volante y
  pedales **Logitech G923**. Dato aprovechado: eligieron Unity antes
  que Unreal y una arquitectura **standalone en vez de PC VR**,
  priorizando despliegue por sobre calidad gráfica — exactamente
  nuestras dos decisiones, así que ahora se cita en III.C para
  respaldarlas.

**Metadatos completados** (todos verificados contra Crossref):
`agrawal2018` le faltaba `number = {33}`; `pouke2022` le faltaba
volumen 3, artículo 869603 y su DOI (solo tenía el arXiv);
`chidambaram2025` le faltaba el DOI `10.1145/3746058.3758372`, páginas
y editorial. `slater2018` y `eljamiy2019` ya estaban completos.

### Deep research de verificación (primera ronda, post-integración)

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

## Escenarios del simulador (menú de selección)

La app arranca en un **menú de escenarios** (`ScenarioMenu`, escena `ScenarioMenu`, primera del
build) y desde ahí se entra a cada escenario. Lista y orden en `ScenarioCatalog.cs`; una escena
está "disponible" si existe en el build (si no, el menú la muestra como "próximamente").

| # | Escenario | Escena | Caso de uso | Estado |
|---|---|---|---|---|
| 1 | Estacionamiento en batería | `Parking_Perpendicular` | CU2 | Hecho (dificultad: ancho del espacio 3.0 / 2.5 / 2.2 m) |
| 2 | Estacionamiento en paralelo | `Parking_Parallel` | CU2 | Hecho (dificultad: 3 niveles) |
| 3 | Estacionar en pendiente | `Parking_Slope` | CU2 | Hecho (dificultad: inclinación 7 / 12 / 18 % y largo del hueco) |

Ideas para más adelante (fuera del menú por ahora): calle angosta con autos a ambos lados (`Street_Narrow`, CU1 / CU3).
Se probó y descartó un "reversa al portón" (garaje con portón, `Parking_Gate`): quedaba muy parecido al estacionamiento en batería.

- **Dirección con los controles como volante:** `WheelSteering` (en el auto; lo agrega `CreateVehicle`). El giro del volante es la
  inclinación de la línea mano izquierda → mano derecha en el espacio del auto (derecha abajo = a la derecha); no depende de
  hacia dónde mire el usuario ni de la distancia entre las manos. `lockAngle` = 70° de giro para el tope de las ruedas,
  zona muerta 2°, suavizado 0.06 s. Con los dos controles rastreados (`isTracked`) reemplaza al joystick en `InputMapper`;
  sin visor o sin un control, siguen el teclado y el joystick. Recentrar: tecla C o mantener 1 s el botón de menú
  izquierdo (el cero queda guardado al cambiar de escena). `VehicleVisuals` gira el volante del auto 1:1 con las manos.
  Para probar sin visor: en Play, en el componente `WheelSteering` del auto, marcar `Simulate` y mover `Simulated Angle`.
  Aceleración (gatillo derecho) y freno (gatillo izquierdo) siguen en gatillos.
- **Marchas R/N/D y freno de mano:** el freno es solo freno. La caja tiene 3 marchas (`GearMode`: Reversa, Neutro, Directa) y el auto
  **parte en Neutro**: hay que subir a D (o bajar a R) para moverse. **Agarre derecho = subir** (R→N→D, `ShiftGearUp`), **agarre
  izquierdo = bajar** (D→N→R, `ShiftGearDown`); solo se cambia con el auto casi detenido (`maxGearShiftSpeed` 0,6 m/s). El cluster
  muestra R/N/D. **Y (control izquierdo) = freno de mano** (también Espacio/H en PC): detiene el auto y, si ya se movió ≥
  `minTravelBeforeFinish` (0,25 m), dispara `VehiclePhysics.OnHandbrakeFinished` → `ExperienceController` termina el intento
  (abre `ResultsScreen`). X queda libre (mantener A/X reinicia). Ya no existe el botón de reversa Y ni `reverseRequested`/`ToggleReverse`.
  Un futuro selector USB solo tiene que llamar `SetGear`. NO implementar pedales/cambio USB hasta que se pida.
- **Radio (`CarRadio`):** 3 pistas `.mp3` en `Assets/Resources/Radio/` (hay que ponerlas a mano). Joystick derecho: click = on/off,
  izq/der = pista, arriba/abajo = volumen 0–20 (teclado: T on/off, `,` `.` pista, `+` `-` volumen). `OnRadioFeedback` se muestra
  2,5 s en el mensaje del HUD (`HUDManager`).
- **HUD de distancia:** muestra "Frente: x m · Atrás: y m" (`ObstacleDetector.GetFrontDistance/GetRearDistance`; `GetClosestDistance`
  sigue la marcha). **Espejos laterales:** `PlanarMirror.sideMagnification` = 1,12 (ajustable 1,08–1,18) en `Mirror_Left/Right`; interior 1,0.
- **Volante / joystick:** si el volante de 2 controles está quieto, el joystick izquierdo (`AlternativeSteerInput`) puede girar.
  `InputMapper` expone `OnSelectPressed` (A), `OnBackPressed` (B), `OnReturnToMenuHeld` (B sostenido; en el auto B ya termina, así que casi no se usa).
- **Cuneta no cuenta como choque (paralelo y pendiente):** las veredas de esas escenas llevan `CurbSurface` (además del tag
  `Obstacle`: el sensor las detecta y el auto no las sube). `CollisionFeedback` da el feedback (sonido, vibración) pero no dispara
  `ObstacleHit`, así que no suma choques. Hay que regenerar las escenas. En batería el tope sí sigue contando.
- **Sensor de estacionamiento:** los pitidos se adelantan según la velocidad (`ParkingSensor.WarningDistance` = distancia −
  velocidad de acercamiento × `ReactionSeconds` 0,6 s): el pitido rápido/continuo suena antes de chocar, no al tocar. Barras y "STOP"
  siguen usando la distancia real.
- **Guía de manejo:** `InstructionsScreen` (la crea `ExperienceController` en cada escenario) muestra "CÓMO SE MANEJA" frente al
  conductor (volante, acelerar, frenar, marchas con los agarres, terminar B / freno de mano Y, mantener A/X reinicia, mantener menú izq. centra el volante) con el
  diseño del menú y sobre todo lo demás (mismo método que `ResultsScreen`: `ScreenKit.AlwaysOnTop`). Se ve mientras `ExperienceStats.Started` sea falso y
  desaparece cuando el auto empieza a moverse (velocidad ≥ `StartSpeed`); al reiniciar el ejercicio vuelve a aparecer, y se oculta al
  terminar. No congela el juego. Se coloca hacia donde mira el auto y se re-coloca durante el primer segundo (el visor tarda en fijar la cabeza).
- **Controles del menú:** joystick arriba/abajo elige escenario, izquierda/derecha la dificultad,
  gatillo o A/X inicia. Con teclado: flechas, números, Enter. La dificultad elegida queda en
  `ScenarioSession` y la leen las escenas al iniciar.
- **Fin de la experiencia:** `ExperienceController` (en el GameManager de cada escenario). **B del control derecho**
  (o Esc / M) termina: congela el juego (`timeScale = 0`) y muestra `ResultsScreen` frente al conductor, con el diseño del
  menú (`ScreenKit` tiene las piezas comunes) y dibujada por encima de todo con la cámara principal (`ScreenKit.AlwaysOnTop`: materiales sin test de profundidad y cola 4000; NO usar una segunda cámara, en el Quest dejaba el efecto "hall de espejos"). Ahí **A**
  (o Enter / Espacio) reintenta (recarga la escena, misma dificultad) y **B** otra vez vuelve al menú. Se ignoran los
  botones los primeros 0.6 s. Datos (`ExperienceStats`): choques/topes = golpes contra objetos con tag `Obstacle`
  (`CollisionFeedback.ObstacleHit`; golpes a menos de 0.75 s cuentan como uno); tiempo = desde que el auto se mueve por
  primera vez hasta pulsar B; **precisión** = % de la superficie del auto que queda dentro del rectángulo pintado
  (`ParkingAccuracy`, vía `IParkingScore` que implementan `ParkingJudgment` y `ParallelParkingJudgment`); aprueba con
  `ExperienceStats.PassAccuracy` = 90 % (se compara el porcentaje redondeado que ve el usuario) **y** con un máximo de
  `ExperienceStats.MaxCollisions` = 1 choque. El estado final es el **peor de tres criterios** (`ExperienceStats.GradeFor`):
  precisión (APROBADO ≥ 90 %, REGULAR ≥ 75 %), choques (≤ 1 / ≤ 3) y **alineación** (≤ 5° / ≤ 10°, `PassAngle` y
  `RegularAngle`); aprueba solo si los tres aprueban y reprueba si cualquiera reprueba. REGULAR es el estado
  intermedio (ámbar). Cada dato de la pantalla se pinta con el color de su propio estado. La pantalla también muestra **alineación** (°
  respecto a las líneas) y **centrado** (cm; batería: de lado a lado, paralelo: entre el auto de adelante y el de atrás),
  el centrado es solo informativo y no afecta el aprobado (`IParkingScore.AngleDegrees` / `CenterOffset`). Mantener A/X sigue
  reiniciando el ejercicio sin salir (`ExerciseReset`, que además pone en cero choques y tiempo). `ReturnToMenu` ya
  no escucha botones: solo queda `ReturnToMenu.Go()`.
  Reiniciar el ejercicio sigue siendo R o mantener A/X.
- **Flecha del objetivo:** en los dos escenarios de estacionamiento hay una flecha amarilla flotante
  (`TargetMarker`, objeto `TargetMarker`) que apunta hacia abajo al espacio objetivo, por encima de los
  autos vecinos; sube y baja y siempre mira al conductor, y sigue al espacio cuando cambia la dificultad
  (también en la pendiente). Desaparece apenas el auto cruza la línea del espacio.
  Se agrega en el generador con `AddTargetMarker(spot, offsetLocal)`.
- **Batería:** la dificultad cambia el ancho del espacio A4 (`PerpendicularParkingCourse`, en el objeto
  `ParkingLot`): mueve las líneas amarillas, la línea de fondo y los dos autos vecinos, y el ancho que usa
  `ParkingJudgment`. Media (2.5 m) es el espacio original; el auto mide 1.86 m.
- **Paralelo:** calle de 10 m, hueco junto a la solera derecha de 7.5 / 6.3 / 5.6 m (fácil / media /
  difícil; el auto mide 4.26 m). El auto parte en el carril derecho antes de la fila: hay que pasar
  de largo y entrar en reversa. Evaluación (`ParallelParkingJudgment`): "bien estacionado" si queda
  dentro del hueco, a ≤ 30 cm de la solera y a ≤ 5° de paralelo; informa distancia a la solera,
  margen delante y detrás, y ángulo.
- **Pendiente:** la misma calle del paralelo, en subida hacia +X. `SlopeCourse` (objeto `SlopeCourse`)
  inclina en juego todo lo que cuelga de `SlopeRoot` (4° / 7° / 10° según la dificultad). El auto parte en un
  **tramo plano** (`FlatStart`, x = -52) antes de la "bisagra" (x = -40, origen de `SlopeRoot`), así no arranca
  rodando cuesta abajo; sube hasta pasar el hueco y entra en reversa, cuesta abajo. Para subir la rampa,
  `VehiclePhysics.alignPitchToGround` (activado por `SlopeCourse`) hace que el auto cabecee según el suelo.
  Edificios, postes y árboles cuelgan de `Upright` y se mantienen verticales. Como se mueven al iniciar,
  **nada bajo `SlopeRoot` puede ser estático**. Se evalúa con `ParallelParkingJudgment`. La física del
  auto rueda cuesta abajo si se suelta el freno estando en Neutro o sin acelerar (a propósito); el freno de mano (Y) lo detiene y termina el intento.
- **Generar las escenas** (las escenas se crean desde el Editor; los `.unity` no se escriben a mano):
  menú `VRCar → 3. Crear menú de escenarios`, `4. Crear escena: estacionamiento en paralelo`,
  `5. Crear escena: estacionar en pendiente`, `6. Crear TODAS las escenas`; o por el puente
  (`Temp/vrcar_cmd.txt`): `scene-menu`, `scene-parallel`, `scene-slope`, `scenes`.
  Cada generador deja las escenas existentes en el build (menú primero).
- **Para agregar un escenario:** una entrada en `ScenarioCatalog`, un generador `SceneSetup.<Nombre>.cs`
  (clase parcial; reutiliza `CreateVehicle`, `CreateXROrigin`, `CreateMirrors`, `CreateHUD`), el nombre
  de su escena en `SceneSetup.ScenePathsInOrder()` y el componente `ExperienceController` en su GameManager (y un `IParkingScore` si el escenario tiene precisión).

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
- **Compilación verificada localmente** (8-sep-2026). Se instaló TeX
  Live en el entorno, así que ya no hace falta Overleaf para comprobar:

  ```bash
  cd report && latexmk -pdf -outdir=.build main.tex
  ```

  Resultado: `exit=0`, **sin ningún warning** en la pasada final (ni
  referencias ni citas sin resolver), sin cajas desbordadas, y
  **4 páginas incluyendo la bibliografía** — bajo el límite de 5.
- Detalle a recordar: el paquete Debian que trae `IEEEtran.bst` es
  **`texlive-publishers`**. Sin él, bibtex falla con "I couldn't open
  style file IEEEtran.bst" y las referencias quedan sin resolver. El
  `.cls` vendorizado en el repo no cubre el `.bst`.

## Entorno de desarrollo

- **Setup en un comando:** `make setup` instala TeX Live y `latexmk`.
  En un Codespace nuevo ni eso hace falta: `.devcontainer/devcontainer.json`
  lo corre solo como `postCreateCommand` e instala las 4 extensiones.
- **Compilar:** `make pdf` (o `make watch` para recompilar al guardar,
  `make clean` para borrar auxiliares). `make` a secas lista los comandos.
- `.vscode/extensions.json` recomienda **Claude Code**, **LaTeX
  Workshop** y el corrector ortográfico con diccionario español.
- `.vscode/settings.json` deja LaTeX Workshop configurado como un
  Overleaf local: recompila al guardar, PDF en pestaña al lado del
  `.tex`, SyncTeX para saltar entre código y PDF, y auxiliares en
  `report/.build/` (ignorado por git).
- **Ojo con Overleaf y git:** Overleaf publica sus cambios como ramas
  `overleaf-YYYY-MM-DD-HHMM`, no directamente en `main`. Un `git pull`
  de `main` **no** las revela: hay que hacer `git fetch --all --prune` y
  revisar cuáles faltan con
  `git merge-base --is-ancestor <rama> main`. Tampoco hay que asumir que
  un archivo está al día porque conserve el nombre — Overleaf reexporta
  imágenes con el mismo nombre y distinto contenido; comparar con
  `git hash-object`.

## Pendiente (requiere al equipo, no a Claude)

- [x] Nombres de los integrantes: Estefanía Pakarati, Emmanuel
      Norambuena, Matías Harrison (ya en `main.tex`).
- [x] Correo de Emmanuel Norambuena: `eanorambuena@uc.cl` (ya en
      `main.tex`).
- [x] Correo de Matías Harrison: `matiasharrison@uc.cl` (ya en
      `main.tex`).
- [x] Correo de Estefanía Pakarati: `estefania.pakarati@uc.cl` (ya en
      `main.tex`, agregado por ella vía Overleaf).
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
- [x] Arte final del storyboard: entregado por el equipo como
      `report/figures/storyboard.jpg` (4 paneles) y ya activo en
      `report/main.tex`.
- [x] **Compilar y confirmar ≤5 páginas** — resuelto: compila limpio en
      4 páginas incluyendo bibliografía (ver "Plan de implementación"
      más arriba). Ya no es un riesgo abierto. Queda margen de ~1
      página si alguien quiere ampliar algo.
- [ ] Subir el PDF compilado al buzón del curso.
- [ ] Decisión del equipo: el abstract y las keywords están en inglés
      y el resto del informe en español (traducción hecha por Matías).
      Es una convención aceptada en papers en español, así que se dejó
      como está — pero conviene que el equipo lo confirme
      deliberadamente y no por inercia.
