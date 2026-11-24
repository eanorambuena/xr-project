using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRCar.Core
{
    /// <summary>
    /// Vuelve al menú de escenarios. Ya no escucha botones: terminar la experiencia y volver al menú
    /// los maneja ExperienceController (B → resultado, B otra vez → menú). Se conserva el componente solo para
    /// que las escenas antiguas no pierdan el script; las escenas nuevas no lo agregan.
    /// </summary>
    public class ReturnToMenu : MonoBehaviour
    {
        /// <summary>Carga el menú (si está en el build)</summary>
        public static bool Go()
        {
            Time.timeScale = 1f;
            if (!Application.CanStreamedLevelBeLoaded(ScenarioCatalog.MenuScene))
            {
                Debug.LogWarning($"[ReturnToMenu] La escena '{ScenarioCatalog.MenuScene}' no está en el build (VRCar → 3. Crear menú de escenarios).");
                return false;
            }
            SceneManager.LoadScene(ScenarioCatalog.MenuScene);
            return true;
        }
    }
}
