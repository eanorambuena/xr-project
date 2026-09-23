# Plantillas de Scripts para XR Interaction Toolkit

## 1. Objeto Interactuable Básico

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SimpleGrabbable : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Color originalColor;
    
    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        originalColor = GetComponent<Renderer>().material.color;
        
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }
    
    private void OnGrab(SelectEnterEventArgs args)
    {
        // Cambiar color al agarrar
        GetComponent<Renderer>().material.color = Color.green;
        Debug.Log("Objeto agarrado: " + gameObject.name);
    }
    
    private void OnRelease(SelectExitEventArgs args)
    {
        // Restaurar color
        GetComponent<Renderer>().material.color = originalColor;
        Debug.Log("Objeto liberado: " + gameObject.name);
    }
}
```

---

## 2. Teleportador VR

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRTeleporter : MonoBehaviour
{
    [SerializeField] private XRRayInteractor rayInteractor;
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private float teleportRayLength = 30f;
    [SerializeField] private LayerMask teleportLayer;
    
    private LineRenderer lineRenderer;
    
    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
    }
    
    private void Update()
    {
        // Dibujar línea de raycast
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            lineRenderer.SetPosition(0, rayInteractor.transform.position);
            lineRenderer.SetPosition(1, hit.point);
            lineRenderer.enabled = true;
            
            // Teleportar al hacer clic (input detection needed)
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }
}
```

---

## 3. Sistema de Interacción Personalizado

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CustomInteractionManager : MonoBehaviour
{
    [SerializeField] private XRInteractionManager interactionManager;
    
    private void Start()
    {
        // Escuchar eventos globales de interacción
        interactionManager.interactableRegistered += OnInteractableRegistered;
        interactionManager.interactableUnregistered += OnInteractableUnregistered;
    }
    
    private void OnInteractableRegistered(InteractableRegisteredEventArgs args)
    {
        Debug.Log($"Interactuable registrado: {args.interactableObject.transform.name}");
    }
    
    private void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
    {
        Debug.Log($"Interactuable no registrado: {args.interactableObject.transform.name}");
    }
    
    private void OnDestroy()
    {
        interactionManager.interactableRegistered -= OnInteractableRegistered;
        interactionManager.interactableUnregistered -= OnInteractableUnregistered;
    }
}
```

---

## 4. Controlador de UI en VR

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class VRUIController : MonoBehaviour
{
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private XRRayInteractor rayInteractor;
    
    private void Start()
    {
        // Configurar UI para VR
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Escala apropiada para VR
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1920, 1080);
        rectTransform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
    }
    
    public void OnButtonClicked()
    {
        Debug.Log("Botón presionado en VR");
    }
}
```

---

## 5. Detector de Proximidad

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ProximityDetector : MonoBehaviour
{
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private float detectionRadius = 2f;
    
    private void Update()
    {
        // Detectar objetos cercanos a la cabeza del usuario
        Collider[] collidersNearby = Physics.OverlapSphere(
            xrOrigin.transform.position, 
            detectionRadius
        );
        
        foreach (Collider collider in collidersNearby)
        {
            // Realizar lógica con objetos cercanos
            Debug.Log($"Objeto cercano: {collider.name}");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (xrOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(xrOrigin.transform.position, detectionRadius);
        }
    }
}
```

---

## 6. Sistema de Puntuación/Estadísticas

```csharp
using UnityEngine;
using TMPro;

public class XRGameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    
    private int score = 0;
    private int objectsInteracted = 0;
    
    public static XRGameManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void AddScore(int points)
    {
        score += points;
        objectsInteracted++;
        UpdateUI();
        Debug.Log($"Puntuación: {score}");
    }
    
    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Puntuación: {score}\nObjetos: {objectsInteracted}";
        }
    }
    
    public void ResetScore()
    {
        score = 0;
        objectsInteracted = 0;
        UpdateUI();
    }
}
```

---

## 7. Controlador de Animaciones VR

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRHandAnimator : MonoBehaviour
{
    [SerializeField] private Animator handAnimator;
    [SerializeField] private XRController xrController;
    
    private void Update()
    {
        // Animar basado en grip
        if (xrController.TryGetFeatureValue(
            UnityEngine.XR.CommonUsages.grip, 
            out float gripValue))
        {
            handAnimator.SetFloat("Grip", gripValue);
        }
        
        // Animar basado en trigger
        if (xrController.TryGetFeatureValue(
            UnityEngine.XR.CommonUsages.trigger, 
            out float triggerValue))
        {
            handAnimator.SetFloat("Trigger", triggerValue);
        }
    }
}
```

---

## 8. Sonidos y Efectos en VR

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRInteractionFeedback : MonoBehaviour
{
    [SerializeField] private AudioClip grabSound;
    [SerializeField] private AudioClip releaseSound;
    [SerializeField] private ParticleSystem grabParticles;
    
    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    
    private void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        audioSource = GetComponent<AudioSource>();
        
        grabInteractable.selectEntered.AddListener(PlayGrabFeedback);
        grabInteractable.selectExited.AddListener(PlayReleaseFeedback);
    }
    
    private void PlayGrabFeedback(SelectEnterEventArgs args)
    {
        audioSource.PlayOneShot(grabSound);
        if (grabParticles != null)
            grabParticles.Play();
    }
    
    private void PlayReleaseFeedback(SelectExitEventArgs args)
    {
        audioSource.PlayOneShot(releaseSound);
    }
}
```

---

## 9. Configuración de Project Settings (Programática)

```csharp
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.Management;

public class XRProjectSetup
{
    [MenuItem("XR Setup/Configure Project")]
    public static void ConfigureProject()
    {
        // Configurar Input System
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        
        // Configurar calidad
        QualitySettings.vSyncCount = 1;
        QualitySettings.targetFrameRate = 90; // Ideal para VR
        
        Debug.Log("Proyecto configurado para VR");
    }
}
#endif
```

---

## 10. Prefab Manager para Objetos VR

```csharp
using UnityEngine;
using System.Collections.Generic;

public class XRPrefabManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> interactablePrefabs = new List<GameObject>();
    
    private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
    
    public GameObject InstantiateInteractable(string prefabName, Vector3 position)
    {
        GameObject prefab = GetPrefab(prefabName);
        if (prefab == null)
        {
            Debug.LogError($"Prefab no encontrado: {prefabName}");
            return null;
        }
        
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        return instance;
    }
    
    private GameObject GetPrefab(string name)
    {
        if (prefabCache.ContainsKey(name))
            return prefabCache[name];
        
        GameObject prefab = interactablePrefabs.Find(p => p.name == name);
        if (prefab != null)
            prefabCache[name] = prefab;
        
        return prefab;
    }
}
```

---

## Cómo Usar Estos Scripts

1. **Crear carpeta**: `Assets/Scripts/XR/`
2. **Crear archivo**: `Assets/Scripts/XR/[NombreDelScript].cs`
3. **Copiar código**: Pega el código de la plantilla
4. **Ajustar referencias**: En el inspector, arrastra los GameObjects necesarios
5. **Probar**: Entra en Play Mode

---

## Scripts Recomendados por Caso de Uso

| Caso de Uso | Script |
|---|---|
| Agarrar objetos | SimpleGrabbable |
| Teletransporte | VRTeleporter |
| Interfaz de usuario | VRUIController |
| Efectos visuales/sonido | VRInteractionFeedback |
| Animación de manos | VRHandAnimator |
| Sistema de puntuación | XRGameManager |

---

**Nota**: Estos scripts son plantillas. Personalízalos según tus necesidades específicas.
