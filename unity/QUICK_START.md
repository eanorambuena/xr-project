# Quick Start: XR Interaction Toolkit en 5 Minutos

## 1. Instalación Rápida (Unity ya debe estar abierto)

### Agregar paquetes al proyecto

Abre **Window → Package Manager** y agregar por nombre:

```
com.unity.xr.management
com.unity.xr.core-utils
com.unity.xr.interaction.toolkit
com.unity.inputsystem
```

**Tiempo**: 2-3 minutos de descarga/instalación

---

## 2. Configuración Rápida (1 minuto)

1. **Edit → Project Settings → XR Plugin Management**
   - Selecciona tu plataforma: `OpenXR`

2. **Window → XR → Setup Input Actions**
   - Crea las acciones de entrada automáticamente

---

## 3. Escena Básica (1 minuto)

### En la jerarquía (Hierarchy):

```
1. Right-click → XR → XR Origin (VR)

2. Selecciona XR Origin
   Right-click → XR → Controller (Left)
   Right-click → XR → Controller (Right)

3. Right-click → 3D Object → Cube
   - Add Component → "XR Grab Interactable"

4. Right-click → 3D Object → Plane
   - Scale: (10, 1, 10)
   - Position: (0, -0.5, 0)
```

---

## 4. Probar (1 minuto)

- Haz clic en **Play** (▶)
- En el simulador:
  - **Clic izquierdo**: Agarrar
  - **Mover mouse**: Mover controlador
  - **WASD**: Movimiento

---

## Comandos Rápidos

| Acción | Comando |
|--------|---------|
| Abrir Package Manager | Ctrl+Shift+P (o Window → Package Manager) |
| Guardar escena | Ctrl+S |
| Play/Stop | Ctrl+Shift+Space o Barra Espaciadora |
| Build settings | Ctrl+Shift+B |

---

## Estado de Verificación

```
✓ Paquetes instalados
✓ XR Plugin Management habilitado
✓ Input Actions configuradas
✓ XR Origin en escena
✓ Controller (Left) y (Right) agregados
✓ Objeto interactuable (Grab Interactable)
✓ Piso agregado
✓ Tested en Play Mode
```

---

## Pasos Siguientes

1. **Agregar más objetos interactuables**
2. **Crear scripts personalizados**
3. **Configurar modelos de controllers reales**
4. **Compilar para hardware XR**

---

**Tiempo total**: ~5 minutos ⏱️
