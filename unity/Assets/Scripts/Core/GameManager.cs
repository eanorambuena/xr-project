using UnityEngine;
using VRCar.Vehicle;
using VRCar.Obstacles;
using VRCar.UI;

namespace VRCar.Core
{
    /// <summary>
    /// Coordina la inicialización y control de todos los sistemas del juego
    /// Conecta VehiclePhysics, InputMapper, ObstacleDetector, HUDManager y CollisionFeedback
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // Componentes del vehículo
        private GameObject vehicleGameObject;
        private VehiclePhysics vehiclePhysics;
        private InputMapper inputMapper;
        private ObstacleDetector obstacleDetector;
        private CollisionFeedback collisionFeedback;

        // Sistema de UI
        private HUDManager hudManager;

        // Estado del juego
        private bool isGameRunning = false;

        private void Awake()
        {
            InitializeGameSystems();
        }

        private void Start()
        {
            ValidateAllSystems();
            StartGame();
        }

        /// <summary>
        /// Inicializar todos los sistemas del juego
        /// </summary>
        private void InitializeGameSystems()
        {
            // Encontrar vehículo
            vehicleGameObject = GameObject.FindGameObjectWithTag("Vehicle");
            if (vehicleGameObject == null)
            {
                Debug.LogError("No se encontró GameObject con tag 'Vehicle'");
                return;
            }

            // Obtener componentes del vehículo
            vehiclePhysics = vehicleGameObject.GetComponent<VehiclePhysics>();
            inputMapper = vehicleGameObject.GetComponent<InputMapper>();
            obstacleDetector = vehicleGameObject.GetComponent<ObstacleDetector>();
            collisionFeedback = vehicleGameObject.GetComponent<CollisionFeedback>();

            // Encontrar HUD
            hudManager = FindObjectOfType<HUDManager>();

            if (hudManager == null)
            {
                Debug.LogWarning("No se encontró HUDManager en la escena");
            }
        }

        /// <summary>
        /// Validar que todos los sistemas estén configurados correctamente
        /// </summary>
        private void ValidateAllSystems()
        {
            bool allSystemsValid = true;

            if (vehiclePhysics == null)
            {
                Debug.LogError("VehiclePhysics no encontrado");
                allSystemsValid = false;
            }

            if (inputMapper == null)
            {
                Debug.LogError("InputMapper no encontrado");
                allSystemsValid = false;
            }

            if (obstacleDetector == null)
            {
                Debug.LogError("ObstacleDetector no encontrado");
                allSystemsValid = false;
            }

            if (collisionFeedback == null)
            {
                Debug.LogWarning("CollisionFeedback no encontrado (opcional)");
            }

            if (!allSystemsValid)
            {
                Debug.LogError("Algunos sistemas no están configurados. El juego no puede iniciarse.");
                enabled = false;
            }
        }

        /// <summary>
        /// Iniciar el juego
        /// </summary>
        private void StartGame()
        {
            isGameRunning = true;
            Debug.Log("VR Car Hito 1: Juicio de Esquinas iniciado");
            Debug.Log("Configuración:");
            Debug.Log($"  - Velocidad máxima: {vehiclePhysics.maxSpeed} m/s ({vehiclePhysics.maxSpeed * 3.6f} km/h)");
            Debug.Log($"  - Rango de detección: {obstacleDetector.detectionRange}m");
        }

        /// <summary>
        /// Pausar el juego
        /// </summary>
        public void PauseGame()
        {
            isGameRunning = false;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Reanudar el juego
        /// </summary>
        public void ResumeGame()
        {
            isGameRunning = true;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Reiniciar el nivel
        /// </summary>
        public void RestartLevel()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Obtener estado actual del juego
        /// </summary>
        public bool IsGameRunning => isGameRunning;

        public VehiclePhysics GetVehiclePhysics() => vehiclePhysics;
        public InputMapper GetInputMapper() => inputMapper;
        public ObstacleDetector GetObstacleDetector() => obstacleDetector;
        public CollisionFeedback GetCollisionFeedback() => collisionFeedback;
        public HUDManager GetHUDManager() => hudManager;
    }
}
