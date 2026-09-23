# Guía de Configuración: Proyecto Unity con XR Interaction Toolkit

## Versión de Unity
- **Unity Editor**: 6000.4.0f1 (LTS - Long Term Support)
- **XR Interaction Toolkit**: Última versión compatible (v2.4.0+)

---

## 1. Requisitos Previos

### Hardware
- Computadora de desarrollo con procesador moderno
- Al menos 8GB de RAM (16GB recomendado)
- 15-20GB de espacio en disco disponible
- GPU compatible (NVIDIA/AMD/Intel)

### Software
- **Unity Hub** (última versión)
- **Unity Editor 6000.4.0f1**
- **Visual Studio** o **Visual Studio Code** (para edición de código)
- **Git** (para control de versiones)

---

## 2. Instalación de Unity Editor 6000.4.0f1

### Opción A: Usando Unity Hub (Recomendado)

1. **Descargar e instalar Unity Hub**
   ```bash
   # macOS
   brew install unity-hub
   
   # Windows: Descargar desde https://unity.com/download
   
   # Linux
   # Descargar desde https://unity.com/download
   ```

2. **Instalar Unity Editor 6000.4.0f1**
   ```bash
   # Abrir Unity Hub
   # Ir a "Installs" → "Install Editor"
   # Buscar y seleccionar "6000.4.0f1"
   # Hacer clic en "Install"
   ```

3. **Instalar módulos adicionales necesarios**
   - Al instalar, asegúrate de incluir:
     - **Windows Build Support** (si desarrollas para Windows)
     - **Linux Build Support** (si desarrollas para Linux)
     - **WebGL Build Support** (opcional)
     - **Android Build Support** (para XR móvil)
     - **iOS Build Support** (para XR móvil)

### Opción B: Instalación desde línea de comandos

```bash
# macOS/Linux
/path/to/Unity\ Hub.app/Contents/MacOS/Unity\ Hub -- --headless install --version 6000.4.0f1

# Windows
"C:\Program Files\Unity Hub\Unity Hub.exe" --headless install --version 6000.4.0f1
```

---

## 3. Crear un Nuevo Proyecto Unity

### Método A: Usando Unity Hub

1. Abre **Unity Hub**
2. Haz clic en **"New Project"**
3. Selecciona **Unity 6 (LTS)** como versión
4. Selecciona una plantilla:
   - **3D** (recomendado para XR)
   - **3D (URP)** (Universal Render Pipeline - mejor para rendimiento)
5. Configura el proyecto:
   - **Project Name**: `UnityXRProject` (o tu nombre preferido)
   - **Location**: Elige la ruta donde guardar el proyecto
   - **Editor Version**: 6000.4.0f1
6. Haz clic en **"Create Project"**

### Método B: Desde línea de comandos

```bash
# Crear proyecto en modo headless
/path/to/Unity -createProject -projectPath ./UnityXRProject \
  -buildBackend mono -force-module-tests -logFile -

# Esto creará un nuevo proyecto en la carpeta ./UnityXRProject
```

---

## 4. Estructura Básica del Proyecto

Después de crear el proyecto, la estructura será:

```
UnityXRProject/
├── Assets/              # Todos tus assets, scripts, escenas
├── Library/             # Caché de Unity (no versionar)
├── Logs/                # Archivos de log
├── Packages/            # Archivo manifest y dependencias
├── ProjectSettings/     # Configuración del proyecto
├── Temp/                # Archivos temporales (no versionar)
├── UserSettings/        # Preferencias del editor
└── .gitignore           # Excepciones de git
```

### Crear archivo .gitignore

```bash
cd UnityXRProject
cat > .gitignore << 'EOF'
# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/

# User settings
UserSettings/
EditorUserBuildSettings.asset

# IDE
.vscode/
.idea/
*.swp
*.swo
*~

# OS
.DS_Store
Thumbs.db

# Local settings
local_settings.json
EOF
```

---

## 5. Instalación de XR Interaction Toolkit

### Paso 1: Abrir Package Manager

1. En el editor Unity, ve a **Window** → **Package Manager**
2. En la esquina superior izquierda, haz clic en el ícono de **+** (más)
3. Selecciona **"Add package by name..."**

### Paso 2: Agregar paquetes necesarios

Instala estos paquetes en orden:

#### 1. XR Plugin Management
```
com.unity.xr.management
```

#### 2. XR Core Utilities (dependencia de XR Interaction Toolkit)
```
com.unity.xr.core-utils
```

#### 3. XR Interaction Toolkit
```
com.unity.xr.interaction.toolkit
```

#### 4. Input System (obligatorio)
```
com.unity.inputsystem
```

### Paso 3: Verificar instalación

En **Package Manager**, busca estos paquetes. Deberías ver:
- `XR Interaction Toolkit` (v2.4.0 o superior)
- `XR Plugin Management`
- `XR Core Utilities`
- `Input System`

---

## 6. Configuración Inicial del Proyecto

### Paso 1: Configurar Input System

1. Ve a **Edit** → **Project Settings**
2. Busca **"Input System"** (izquierda)
3. En **"Default Input Actions"**, si hay un warning, haz clic en **"Migrate"**

### Paso 2: Habilitar XR Plugin Management

1. Ve a **Edit** → **Project Settings** → **XR Plugin Management**
2. Deberías ver opciones para diferentes plataformas
3. Selecciona tu plataforma XR:
   - **Windows**: Selecciona "OpenXR"
   - **Android**: Selecciona "OpenXR" o "Oculus"
   - **iOS**: Selecciona "OpenXR"

### Paso 3: Crear entrada predeterminada para XR Interaction Toolkit

1. Ve a **Window** → **XR** → **Setup Input Actions**
2. Selecciona un nombre para el archivo de acciones (ej: `XRInput`)
3. Haz clic en **"Create Input Actions"**

Esto creará un archivo `XRInput.inputactions` con las acciones de XR preconfiguradas.

---

## 7. Crear una Escena Básica con XR

### Paso 1: Crear nueva escena

1. **File** → **New Scene**
2. Selecciona **"3D"** (si no estaba seleccionado)
3. Guarda la escena: **Ctrl+S** (Cmd+S en Mac)
   - Nombre: `MainScene` o similar

### Paso 2: Agregar XR Origin

1. **Right-click** en la jerarquía → **XR** → **XR Origin (VR)**
2. Se creará automáticamente con:
   - `XR Origin` (objeto padre)
   - `Camera Offset` (para manejar el offset de la cámara)
   - `Main Camera` (cámara VR)

### Paso 3: Agregar manillares (controllers)

1. En **XR Origin**:
   - **Right-click** → **XR** → **Controller (Left)**
   - **Right-click** → **XR** → **Controller (Right)**

2. Configura los modelos:
   - Selecciona cada controlador
   - En el inspector, busca "Model Prefab"
   - Asigna un modelo 3D apropiado

### Paso 4: Agregar piso

```csharp
// Agregar un plano como piso:
// 1. Right-click → 3D Object → Plane
// 2. Scale: (10, 1, 10)
// 3. Position: (0, -0.5, 0)
// 4. Material: Crea un material gris
```

### Paso 5: Agregar objetos interactuables

Crea un cubo interactuable:

1. **Right-click** → **3D Object** → **Cube**
2. Añade el componente **"XR Grab Interactable"**:
   - En el inspector → **Add Component** → Busca "XR Grab Interactable"
3. Configura:
   - **Grab Interaction Mode**: "Select" o "Move"
   - **Throw on Detach**: true (para permitir lanzar)

---

## 8. Script Básico de Interacción

Crea un script `InteractableObject.cs`:

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class InteractableObject : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    
    private void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        
        // Suscribirse a eventos de interacción
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }
    
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("¡Objeto agarrado!");
        // Cambiar color, reproducir sonido, etc.
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        Debug.Log("¡Objeto liberado!");
    }
}
```

---

## 9. Compilar y Probar

### En Editor (Play Mode)

1. Asegúrate de tener la escena abierta
2. Haz clic en el botón **"Play"** (▶)
3. En el simulador de XR:
   - Usa el mouse para simular los controladores
   - Usa las teclas WASD para mover

### Compilar para hardware XR

1. **File** → **Build Settings**
2. Selecciona tu plataforma:
   - **Windows** (para Meta Quest link via PC)
   - **Android** (para Meta Quest)
   - **iOS** (para Apple Vision Pro)
3. Configura:
   - **Scene in Build**: Arrastra `MainScene` al área
   - **Build Type**: Selecciona según la plataforma
4. Haz clic en **"Build"** o **"Build and Run"**

---

## 10. Checklist de Configuración

- [ ] Unity 6000.4.0f1 instalado
- [ ] Proyecto creado
- [ ] XR Interaction Toolkit instalado
- [ ] Input System configurado
- [ ] XR Plugin Management habilitado
- [ ] XR Origin creado
- [ ] Controllers agregados
- [ ] Objetos interactuables creados
- [ ] Tested en Play Mode
- [ ] Compilado para hardware XR

---

## 11. Recursos Útiles

### Documentación Oficial
- [XR Interaction Toolkit - Manual](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest/)
- [Unity XR Developer Portal](https://developers.xr.unity.com/)
- [OpenXR Documentation](https://khronos.org/openxr/)

### Tutoriales Recomendados
- Unity Learn: XR Development Pathway
- Meta Developer Documentation
- Apple Vision Pro Developer Documentation

### GitHub
- [XR Interaction Toolkit Samples](https://github.com/Unity-Technologies/XR-Interaction-Toolkit)

---

## 12. Solución de Problemas Comunes

### Problema: XR Origin no aparece
**Solución**: Asegúrate de que:
- XR Interaction Toolkit está instalado
- Has importado los ejemplos del paquete (opcional pero recomendado)
- Reinicia el editor

### Problema: Los controllers no funcionan
**Solución**:
- Verifica que el archivo `XRInput.inputactions` está asignado
- Comprueba que XR Plugin Management está configurado
- Revisa la consola para errores

### Problema: Bajo rendimiento
**Solución**:
- Usa **Universal Render Pipeline (URP)** en lugar de Built-in
- Reduce la cantidad de luces dinámicas
- Optimiza los modelos 3D
- Usa LOD (Level of Detail) para objetos lejanos

---

## 13. Próximos Pasos

1. **Agregar más interacciones**: ray cast, teleport, agarre mejorado
2. **Mejorar la interfaz de usuario**: menús, panel de información
3. **Optimización**: bake lighting, caché de shaders
4. **Multiplayer**: sincronización de poses y objetos
5. **Deployment**: testing en hardware real

---

**Última actualización**: Septiembre 2026
**Versión del documento**: 1.0
