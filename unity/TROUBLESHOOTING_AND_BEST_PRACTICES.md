# Troubleshooting y Mejores Prácticas: XR Interaction Toolkit

## Troubleshooting

### 1. XR Origin no aparece en el menú XR

**Síntomas**:
- No ves la opción "XR → XR Origin (VR)" en el menú contextual

**Causas**:
- XR Interaction Toolkit no está instalado
- Los ejemplos del paquete no están importados

**Soluciones**:
1. Verifica que `com.unity.xr.interaction.toolkit` está en Package Manager
2. En Package Manager, ve a "Samples" y haz clic en "Import"
3. Reinicia el editor
4. Si aún no funciona, reimporta el paquete:
   ```
   Haz clic derecho en el paquete → "Re-import"
   ```

---

### 2. Controllers no responden a input

**Síntomas**:
- Los controladores aparecen pero no reaccionan al input
- No puedes agarrar objetos

**Causas**:
- Input Actions no están configuradas
- El archivo XRInput.inputactions no está asignado
- XR Plugin Management no está configurado

**Soluciones**:
1. **Verificar Input Actions**:
   ```
   Window → XR → Setup Input Actions
   Selecciona "XRInput" y crea si no existe
   ```

2. **Asignar archivo de acciones**:
   - Selecciona el `XROrigin` en la jerarquía
   - En el inspector, busca "Input Action Manager"
   - Arrastra `XRInput.inputactions` al campo

3. **Verificar XR Plugin Management**:
   ```
   Edit → Project Settings → XR Plugin Management
   Selecciona tu plataforma (OpenXR recomendado)
   ```

---

### 3. Bajo rendimiento / FPS bajo

**Síntomas**:
- Juego lento, lag, fotogramas caídos
- Headset se siente poco fluido

**Causas**:
- Demasiados objetos dinámicos
- Render Pipeline no optimizado
- Luces dinámicas excesivas
- Resolución demasiado alta

**Soluciones**:

**A. Usar Universal Render Pipeline (URP)**:
1. `Window → TextureImporter → Import URP Assets`
2. `Edit → Project Settings → Graphics`
3. Selecciona "ForwardRenderer" en URP

**B. Optimizar objetos**:
```csharp
// Usar baking de luces
// Edit → Render Settings → Bake
// Bake shadows estáticas en lugar de dinámicas
```

**C. Usar LOD (Level of Detail)**:
```
1. Selecciona modelo 3D
2. En el Inspector → LOD Group
3. Configura niveles de detalle
```

**D. Limitar frame rate**:
```csharp
QualitySettings.targetFrameRate = 90; // Para VR
```

---

### 4. Objetos desaparecen o no se ven

**Síntomas**:
- Los objetos no aparecen en la escena
- XR Origin desaparece sin razón

**Causas**:
- Objetos fuera del volumen visible
- Capas incorrectas
- Culling incorrecta

**Soluciones**:
1. **Verificar posición**:
   ```
   Right-click en objeto → Frame Selected (o Shift+F)
   ```

2. **Verificar capas**:
   - Selecciona el objeto
   - En el inspector, verifica que la capa es visible
   - `Edit → Project Settings → Physics` - Verifica layer collisions

3. **Reset Transform**:
   - Clic derecho en componente Transform
   - "Copy Component"
   - Clic derecho → "Paste Component As New"

---

### 5. Error: "XR Plugin Management is not initialized"

**Síntomas**:
- Error en consola al Play
- Tracking no funciona

**Causas**:
- XR Plugin Management mal configurado
- Plataforma XR no seleccionada

**Soluciones**:
1. `Edit → Project Settings → XR Plugin Management`
2. Selecciona tu plataforma
3. Asegúrate de que hay un checkmark ✓
4. Reinicia el editor
5. Si persiste, elimina y reinstala el paquete

---

### 6. Input System errores de acción

**Síntomas**:
- "Error: Could not find an action"
- Controllers no responden

**Causas**:
- archivo .inputactions dañado o mal referenciado
- Paths de acciones incorrectos

**Soluciones**:
1. Elimina el archivo .inputactions
2. `Window → XR → Setup Input Actions`
3. Crea uno nuevo
4. Asigna en cada Controller

---

### 7. Build falla para hardware

**Síntomas**:
- Error al compilar APK o build
- "Target device API level too low"

**Causas**:
- Versión de API incorrecta
- Herramientas SDK no instaladas
- Dependencias faltantes

**Soluciones**:
1. **Para Android**:
   ```
   Edit → Project Settings → Player
   Mínimo API Level: 24 (recomendado 26+)
   Target API Level: 32+
   Scripting Backend: IL2CPP
   ```

2. **Instalar herramientas**:
   ```
   Android SDK Manager → API 32+
   NDK: ultima versión
   ```

---

### 8. Problemas con modelos de controladores

**Síntomas**:
- Controllers sin modelo visual
- Modelo invisible o mal posicionado

**Causas**:
- Modelo 3D no asignado
- Escala incorrecta
- Material faltante

**Soluciones**:
1. **Asignar modelo**:
   - Selecciona Controller
   - Inspector → "Model Prefab"
   - Arrastra un modelo compatible

2. **Ajustar escala**:
   ```
   En Model Prefab:
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale: (1, 1, 1)
   ```

---

## Mejores Prácticas

### 1. Estructura de Proyecto

```
Assets/
├── Scripts/
│   ├── Interaction/
│   ├── UI/
│   ├── Managers/
│   └── Utilities/
├── Scenes/
│   ├── MainScene.unity
│   ├── MenuScene.unity
│   └── LevelScene.unity
├── Prefabs/
│   ├── Interactables/
│   ├── Controllers/
│   └── UI/
├── Models/
├── Audio/
├── Materials/
└── InputActions/
    └── XRInput.inputactions
```

---

### 2. Convenciones de Nomenclatura

```csharp
// Clases
public class VRManager { }
public class InteractableObject { }
public class XRHandAnimator { }

// Variables privadas
private Rigidbody rb;
private Transform cachedTransform;
private XRGrabInteractable grabInteractable;

// Variables públicas (usar [SerializeField])
[SerializeField] private float interactionDistance = 2f;
[SerializeField] private LayerMask interactableLayer;

// Constantes
private const float GRAB_DISTANCE = 1f;
private const int MAX_GRAB_DISTANCE = 5;

// Enums
public enum InteractionType { Grab, Point, Use }
```

---

### 3. Performance Tips

**A. Caché referencias**:
```csharp
private void Start()
{
    // ✓ Bien: Caché una vez
    rb = GetComponent<Rigidbody>();
    grabInteractable = GetComponent<XRGrabInteractable>();
}

private void Update()
{
    // ✓ Bien: Usar la referencia en caché
    rb.velocity = Vector3.zero;
    
    // ✗ Malo: GetComponent cada frame
    // GetComponent<Rigidbody>().velocity = Vector3.zero;
}
```

**B. Usar object pooling**:
```csharp
public class BulletPool : MonoBehaviour
{
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    
    public GameObject GetBullet()
    {
        if (bulletPool.Count > 0)
            return bulletPool.Dequeue();
        else
            return Instantiate(bulletPrefab);
    }
    
    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
    }
}
```

**C. Optimizar Update()**:
```csharp
private float updateTimer = 0f;
private float updateInterval = 0.1f; // Actualizar cada 0.1 segundos

private void Update()
{
    updateTimer += Time.deltaTime;
    if (updateTimer >= updateInterval)
    {
        ExpensiveCalculation();
        updateTimer = 0f;
    }
}
```

---

### 4. Seguridad y Validación

```csharp
public class SafeInteractable : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;
    
    private void Start()
    {
        // Validar referencias
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }
        
        if (grabInteractable == null)
        {
            Debug.LogError("XRGrabInteractable no encontrado", gameObject);
            enabled = false;
            return;
        }
        
        // Suscribirse de forma segura
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }
    
    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }
}
```

---

### 5. Pruebas en Editor vs Hardware

**Testing en Editor**:
```
1. Usa Play Mode para pruebas rápidas
2. Simula input con mouse y teclado
3. Verifica lógica antes de compilar
```

**Testing en Hardware**:
```
1. Compila APK/build final
2. Prueba en dispositivo real
3. Verifica tracking y performance
4. Test con usuarios reales
```

---

### 6. Debugging en VR

```csharp
public class VRDebugger : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;
    
    private void Update()
    {
        // Mostrar info de debug en pantalla VR
        string debugInfo = $@"
FPS: {1f / Time.deltaTime:F0}
Position: {XROrigin.position}
Controllers Active: {leftController.isActiveAndEnabled}, {rightController.isActiveAndEnabled}
Grabbed Objects: {grabbedObjects.Count}
";
        
        if (debugText != null)
            debugText.text = debugInfo;
    }
}
```

---

### 7. Versionado y Git

```bash
# .gitignore para Unity XR
Library/
Temp/
Logs/
UserSettings/
Builds/
obj/

# Archivos del editor
*.csproj
*.sln
.vs/

# Archivos temporales
*.tmp
*.swp
```

---

### 8. Compilación y Distribución

**Checklist antes de compilar**:
- [ ] Escenas configuradas en Build Settings
- [ ] Input Actions asignadas
- [ ] XR Plugin Management habilitado
- [ ] Icono de aplicación configurado
- [ ] Versión correcta en PlayerSettings
- [ ] Pruebas en Play Mode pasadas
- [ ] Performance verificado
- [ ] Permisos de usuario solicitados

---

## Recursos de Depuración

### Consola de Unity
```
Window → General → Console
Ver errores y warnings en tiempo real
```

### Profiler
```
Window → Analysis → Profiler
Monitorear: CPU, GPU, Memory, Physics
```

### Frame Debugger
```
Window → Analysis → Frame Debugger
Inspeccionar cada frame renderizado
```

---

## Checklist de Producción

- [ ] Código comentado y documentado
- [ ] Sin warning en consola
- [ ] Performance optimizado (90+ FPS)
- [ ] Pruebas en múltiples dispositivos
- [ ] Gestos intuitivos implementados
- [ ] Audio y feedback visual añadido
- [ ] Seguridad de datos verificada
- [ ] Documentación actualizada

---

**Última actualización**: Septiembre 2026
**Versión**: 1.0
