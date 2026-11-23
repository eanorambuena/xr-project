# Pruebas del proyecto VRCar y qué puede hacer el conductor

Estado verificado: 23-11-2026, Unity 6000.4.0f1, **139 pruebas EditMode, 0 fallos**.
Cómo correrlas: `Window > General > Test Runner > EditMode > Run All`, o el menú `VRCar > Run EditMode Tests` (Ctrl+Shift+T).

Las pruebas automáticas validan la **lógica**. Lo que se *siente* (volante, espejos, VR) se valida con la sección "Pruebas manuales".

## 1. Qué puede hacer el conductor y qué prueba lo cubre

| Capacidad del conductor | Archivo de pruebas | Qué se comprueba |
|---|---|---|
| Acelerar (gatillo derecho) | InputMapperTests, VehiclePhysicsTests | El gatillo se mapea a aceleración, a fondo y sin presionar; la fuerza sube la velocidad; se respeta la velocidad máxima |
| Frenar (gatillo izquierdo) | InputMapperTests, VehicleSteeringTests | Frenado progresivo (sin parada instantánea), el auto queda quieto sin avanzar solo |
| Marcha atrás | VehicleSteeringTests | Se activa al mantener el freno con el auto detenido; se sale soltando el freno; no se activa en movimiento; es lenta; al detenerse con acelerador no da tirón hacia adelante |
| Girar el volante (joystick derecho) | InputMapperTests, VehicleSteeringTests | Izquierda, derecha y centro; el ángulo máximo sale del círculo de giro; el volante se mueve gradualmente; detenido con volante a tope el auto no gira |
| Giro realista de ruedas | VehicleVisualsTests, VehicleWheelPhysicsTests | Ackermann (la rueda interior gira más), mismo giro a izquierda y derecha, radio de giro igual al del Peugeot 308 |
| Estabilidad al maniobrar | VehicleWheelPhysicsTests | Sin derrape lateral en recta, giro sostenido ni marcha atrás con volante a tope; frenado en línea recta; suspensión estable |
| Pies y piernas (cuerpo del conductor) | DriverBodyTests | Torso, piernas y zapatos; sin brazos ni cabeza; sin colisionadores; no tapa la vista; el pie derecho va al acelerador y al freno (el freno gana), presión parcial, la pierna izquierda no se mueve, huesos mantienen su largo, pies en el piso |
| Mirar (cabeza) | DriverBodyTests, VROriginSetupTests | El cuerpo no se arrastra con la cabeza; altura de cámara y origen XR correctos y sin rotación |
| Espejos | PlanarMirrorTests | Reflejo plano simétrico, el frustum coincide con el vidrio, render cubre ambos ojos |
| Ver velocidad y marcha | ClusterDisplayTests, HUDManagerTests | Marcha D/R, velocidad en km/h, nunca negativa, muestra reversa |
| Sonido del motor | EngineSoundTests | Ralentí, tono sube con velocidad y acelerador, límites de tono y volumen |
| Sensor de estacionamiento | ParkingSensorTests, HUDManagerTests | Niveles por distancia, pitidos más rápidos al acercarse (continuo al final), etiqueta en cm o STOP, colores verde/amarillo/rojo |
| Detección de obstáculos | ObstacleDetectorTests, ObstacleDetectorLowTests | Distancia desde el parachoques delantero o trasero, el más cercano, desde varias esquinas, rango máximo, ignora lo que no es obstáculo, detecta un bordillo bajo atrás |
| Choques | CollisionFeedbackTests | Sonido y color rojo al chocar con obstáculo, se restaura al separarse, ignora objetos que no son obstáculos |
| Juicio del estacionamiento | ParkingJudgmentTests | Centrado y alineado, márgenes izquierdo y derecho, línea cruzada = fuera, ángulo, perspectiva del conductor, demasiado chueco = no aprobado, mensaje en cm |
| Reiniciar el ejercicio | ExerciseResetTests | Vuelve a la pose inicial y detiene el auto |
| Integración de sistemas | GameManagerTests | Encuentra física, entradas, detector y choques; estados correr/pausa |
| Compatibilidad VR | XRCompatibilityTests | Gatillos y joystick en XR, latencia, 72 Hz tipo Quest 3, sin fugas, inicialización sin errores |

## 2. Pruebas manuales y automáticas en Play (no cuentan en las 139)

- **Maniobra automática**: menú de Editor `AutoParkTest` (solo en Play). Estaciona en reversa con una maniobra de libro, valida que el giro calibrado permite estacionar y registra el recorrido en `Temp/autopark.log` (con cuadros de video en `Temp/video`).
- **Validación XR**: `XRValidation` y `validate-xr.sh` (ver `XR_VALIDATION_GUIDE.md`).

Lista de revisión manual en el visor o simulador (Meta XR Simulator):
1. Acelerar, frenar y retroceder se sienten graduales.
2. Volante a tope: el radio de giro es creíble.
3. Mirar a los lados mueve solo la cabeza; el cuerpo y el auto no giran.
4. Los pies se mueven con acelerador y freno.
5. Los espejos muestran bien lo de atrás y no se ven deformados.
6. El sensor pita y cambia de color al acercarse; el choque suena y se pone rojo.
7. Estacionar en perpendicular y ver el juicio con márgenes en cm.
8. Reiniciar el ejercicio.

## 3. Huecos conocidos (aún sin prueba)

- No hay pruebas PlayMode (comportamiento con tiempo real y escena completa).
- Espejos retrovisor y laterales: solo geometría, falta validar tamaño y ángulo correctos contra el auto real.
- Cabeza y mirada: solo se prueba que no arrastra el cuerpo; falta una prueba de rango de giro.
- El README dice "~50 pruebas": está desactualizado (son 139).
