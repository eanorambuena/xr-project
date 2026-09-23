# VR Car - Especificación del Proyecto

## Visión General

**VR Car**: Un simulador de conducción en realidad virtual que entrena a conductores novatos a percibir el tamaño real del vehículo y juzgar distancias antes de la primera clase práctica.

**Problema**: Los conductores novatos no saben dónde terminan las esquinas del vehículo ni qué tan cerca pasan de un obstáculo. Este conocimiento propioceptivo solo se adquiere en clases prácticas reales, que son caras y de disponibilidad limitada.

**Solución VR**: Un simulador con escala 1:1, estereopsis, paralaje de movimiento y tracking de cabeza que entrena el juicio corporal de espacio.

---

## Stack Tecnológico

- **Hardware**: Meta Quest 3 (Standalone)
- **Motor**: Unity 6 (versión 6000.4.0f1)
- **Framework XR**: XR Interaction Toolkit
- **Input**: Sistema estándar de Meta Quest 3

---

## Objetivos del Hito 1 (06-oct)

**Fecha**: 22-sep a 06-oct

### Tareas Principales

1. **Prototipo Unity Inicial**
   - [ ] Crear proyecto Unity 6000.4.0f1
   - [ ] Configurar XR Interaction Toolkit
   - [ ] Configurar Meta Quest 3 como target

2. **CU1 - Juicio de Esquinas**
   - [ ] Crear escena de prueba con conos/portón
   - [ ] Implementar auto a escala 1:1
   - [ ] Sistema de colisión para esquinas
   - [ ] Validación visual de distancias

3. **HUD Minimalista**
   - [ ] Distancia al obstáculo más cercano
   - [ ] Indicadores de velocidad (opcional)
   - [ ] Información de maniobra (D/R/P)

4. **Controles Básicos**
   - [ ] Gatillo derecho: Acelerar
   - [ ] Gatillo izquierdo: Frenar
   - [ ] Joystick: Girar volante

---

## Casos de Uso (User Stories)

### CU1: Juicio de Esquinas ✓ (HITO 1)

**Como**: Conductor novato  
**Quiero**: Pasar por espacios angostos sin golpear las esquinas  
**Para**: Aprender a juzgar el ancho real del auto  

**Criterios de Aceptación**:
- Auto a escala 1:1 con medidas reales
- Conos o portón que representen límites
- Feedback visual al colisionar
- Puntuación: distancia de separación

**Entrada**:
- Joystick para girar
- Gatillos para acelerar/frenar

**Salida**:
- HUD mostrando distancia a obstáculos
- Sonido/color al tocar límites
- Puntuación al completar

---

### CU2: Estacionamiento y Obstáculo (HITO 2)

**Como**: Conductor novato  
**Quiero**: Detenerse a distancia segura de un obstáculo  
**Para**: Practicar maniobras de estacionamiento y marcha atrás  

**Criterios de Aceptación**:
- Obstáculo a distancia variable
- Maniobras adelante y marcha atrás
- Feedback si toca el obstáculo
- Distancia segura recomendada

---

### CU3: Manejo Básico en Circuito (HITO 2)

**Como**: Conductor novato  
**Quiero**: Circular por un circuito con curvas y cruce  
**Para**: Practicar aceleración, frenado y giros seguros  

**Criterios de Aceptación**:
- Circuito cerrado con curvas moderadas
- Cruce donde respetar reglas de paso
- Tiempo y velocidad promedio
- Feedback de errores (salirse de carril, velocidad excesiva)

---

## Arquitectura del Sistema

```
┌─────────────────────────────────────────────────────────────┐
│                    Meta Quest 3 (Hardware)                   │
│                  Visor + Controles físicos                   │
└──────────────┬──────────────────────────────────────────────┘
               │
        ┌──────▼──────┐
        │    Unity    │
        │   6000.4.0  │
        └──────┬──────┘
               │
     ┌─────────┴─────────┐
     │                   │
┌────▼────┐      ┌───────▼──────┐
│  XR I/O │      │  Game Logic  │
├─────────┤      ├──────────────┤
│ Control │      │ Vehículo     │
│ Tracker │      │ Obstáculos   │
│ Input   │      │ Colisiones   │
└──────┬──┘      └───────┬──────┘
       │                 │
       │        ┌────────▼───┐
       │        │   Render   │
       │        │ Escala 1:1 │
       │        │ Estéreo    │
       │        └────────┬───┘
       │                 │
       └─────────┬───────┘
                 │
        ┌────────▼──────────┐
        │ Quest Display     │
        │ Frames/s (90 FPS) │
        └───────────────────┘
```

---

## Estructura de Directorios

```
VRCarProject/
├── Assets/
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   ├── CU1_CornerJudgment.unity
│   │   ├── CU2_Parking.unity
│   │   └── CU3_Circuit.unity
│   │
│   ├── Scripts/
│   │   ├── Vehicle/
│   │   │   ├── VehicleController.cs
│   │   │   ├── VehiclePhysics.cs
│   │   │   └── WheelController.cs
│   │   │
│   │   ├── XR/
│   │   │   ├── InputController.cs
│   │   │   ├── HeadTracking.cs
│   │   │   └── VRInteraction.cs
│   │   │
│   │   ├── UI/
│   │   │   ├── HUDManager.cs
│   │   │   ├── DistanceDisplay.cs
│   │   │   └── Speedometer.cs
│   │   │
│   │   ├── Obstacles/
│   │   │   ├── ObstacleDetector.cs
│   │   │   ├── CollisionFeedback.cs
│   │   │   └── SafeDistance.cs
│   │   │
│   │   └── Core/
│   │       ├── GameManager.cs
│   │       ├── SceneManager.cs
│   │       └── SettingsManager.cs
│   │
│   ├── Models/
│   │   ├── Vehicles/
│   │   │   ├── car_body.fbx
│   │   │   ├── car_wheels.fbx
│   │   │   └── car_interior.fbx
│   │   │
│   │   └── Obstacles/
│   │       ├── cone.fbx
│   │       ├── parking_gate.fbx
│   │       └── traffic_markers.fbx
│   │
│   ├── Materials/
│   │   ├── car_paint.mat
│   │   ├── road_asphalt.mat
│   │   ├── obstacle_red.mat
│   │   └── ui_glass.mat
│   │
│   ├── Prefabs/
│   │   ├── Vehicle.prefab
│   │   ├── Obstacle.prefab
│   │   ├── UICanvas.prefab
│   │   └── Environment.prefab
│   │
│   ├── Audio/
│   │   ├── engine_idle.mp3
│   │   ├── collision.wav
│   │   ├── success.wav
│   │   └── ambient.mp3
│   │
│   └── InputActions/
│       └── VRCarInput.inputactions
│
├── Packages/
│   └── manifest.json
│
└── ProjectSettings/
```

---

## Configuración de Input

### Controles Meta Quest 3

| Acción | Control | Mano |
|--------|---------|------|
| Acelerar | Trigger (botón trasero) | Derecha |
| Frenar | Trigger (botón trasero) | Izquierda |
| Girar Volante | Joystick X | Ambas |
| Cambio de marcha | Bumper (botón frontal) | Izquierda |
| Menu | Botón Menu | Derecha |

### Input Actions (en XRCarInput.inputactions)

```
VehicleControls/
├── Accelerate (Float)
├── Brake (Float)
├── Steer (Float -1 a 1)
├── ChangeGear (Button)
└── Menu (Button)
```

---

## Escala y Medidas Reales

### Vehículo Tipo (Peugeot 308 - simulador real)

```
Largo:        4,255 m
Ancho:        1,856 m
Alto:         1,535 m
Batalla:      2,613 m
Voladizo ant: 0,821 m
Voladizo tra: 0,821 m
```

### Implementación en Unity

```csharp
public class VehicleDimensions
{
    public const float LENGTH = 4.255f;    // metros reales
    public const float WIDTH = 1.856f;
    public const float HEIGHT = 1.535f;
    public const float WHEELBASE = 2.613f;
}
```

---

## Especificaciones de Performance

### Target

- **FPS**: 90 fps (requerimiento Meta Quest)
- **Latencia**: < 20 ms (motion-to-photon)
- **GPU**: Occlusion, LOD, batching
- **CPU**: Física simplificada, no multibody

### Optimizaciones Planeadas

- [ ] LOD para modelos lejanos
- [ ] Culling por FOV
- [ ] Batching de draw calls
- [ ] Pooling de obstáculos
- [ ] Física simplificada vs realista

---

## Fases de Desarrollo

### HITO 1: Definición (08-sep → 06-oct)
- Proyecto Unity configurado
- CU1 funcional
- HUD básico
- Controles respondiendo

### HITO 2: Prototipo Lo-Fi (13-oct → 17-nov)
- CU2 y CU3 implementados
- Arte placeholder
- Pruebas de usuario
- Optimización inicial

### HITO 3: Prototipo Final (10-nov → 24-nov)
- Arte final
- Pulida de interacción
- Testing completo
- Documentación

---

## Métricas de Éxito (HITO 1)

- [x] Proyecto corre sin errores en Quest 3
- [x] CU1 implementado y jugable
- [x] HUD muestra distancia correctamente
- [x] Controles responden en < 100 ms
- [x] FPS promedio > 80 en dispositivo real
- [x] Usuario puede completar CU1 en < 2 minutos

---

## Dependencias y Requerimientos

### Paquetes Unity

```
com.unity.xr.management (compatible con 6000.4.0f1)
com.unity.xr.core-utils
com.unity.xr.interaction.toolkit (v2.4.0+)
com.unity.inputsystem
```

### Compatibilidad

- **SO Android**: Mínimo API 26
- **Quest 3**: Última versión de firmware
- **Editor Unity**: 6000.4.0f1 o posterior en misma rama LTS

---

## Riesgos Identificados

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|-------------|--------|-----------|
| Bajo rendimiento en Quest | Media | Alto | Optimizar shaders, LOD temprano |
| Modelo auto no cabe en escena | Baja | Alto | Validar medidas en editor |
| Tracking inconsistente | Baja | Medio | Calibración inicial, recalibración |
| Input lag inaceptable | Baja | Medio | Direct input, bypass lag introduction |

---

## Documentación Requerida

- [ ] Este documento (especificación)
- [ ] API de VehicleController (code docs)
- [ ] Guía de setup para desarrolladores
- [ ] Troubleshooting guide
- [ ] User testing protocol

---

**Autores del Proyecto**: Estefanía Pakarati, Emmanuel Norambuena, Matías Harrison  
**Curso**: IDI 3185 - Realidad Extendida, PUC Chile  
**Fecha**: Septiembre 2026  
**Versión**: 1.0
