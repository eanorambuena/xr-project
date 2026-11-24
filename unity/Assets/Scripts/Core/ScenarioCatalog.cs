using UnityEngine;

namespace VRCar.Core
{
    /// <summary>Un escenario de práctica del simulador (una escena de Unity)</summary>
    public class ScenarioInfo
    {
        public readonly string Id;
        public readonly string Title;
        /// <summary>Caso de uso del informe: CU1 juicio de esquinas, CU2 estacionamiento, CU3 manejo básico</summary>
        public readonly string UseCase;
        public readonly string SceneName;
        /// <summary>true si el escenario cambia con el nivel de dificultad elegido en el menú</summary>
        public readonly bool HasDifficulty;
        public readonly string Description;

        public ScenarioInfo(string id, string title, string useCase, string sceneName, bool hasDifficulty, string description)
        {
            Id = id; Title = title; UseCase = useCase; SceneName = sceneName;
            HasDifficulty = hasDifficulty; Description = description;
        }
    }

    /// <summary>
    /// Lista de escenarios del simulador. El menú la muestra en este orden; un escenario está
    /// "disponible" si su escena existe en el build (si no, aparece como "próximamente").
    /// </summary>
    public static class ScenarioCatalog
    {
        /// <summary>Nombre de la escena del menú (índice 0 del build)</summary>
        public const string MenuScene = "ScenarioMenu";

        public static readonly string[] DifficultyNames = { "Fácil", "Media", "Difícil" };

        public static readonly ScenarioInfo[] All =
        {
            new ScenarioInfo("parking-perpendicular", "Estacionamiento en batería", "CU2", "Parking_Perpendicular", true,
                "Estaciona en el espacio A4 entre dos autos. A mayor dificultad, más angosto el espacio (3.0 / 2.5 / 2.2 m). Practica el juicio de distancia frontal y trasera."),
            new ScenarioInfo("parking-parallel", "Estacionamiento en paralelo", "CU2", "Parking_Parallel", true,
                "Entra en reversa al hueco junto a la solera. A mayor dificultad, más corto el espacio. Practica el largo del auto, la distancia a la solera y los espejos."),
            new ScenarioInfo("parking-slope", "Estacionar en pendiente", "CU2", "Parking_Slope", true,
                "Estaciona en paralelo en una calle en subida. A mayor dificultad, más empinada y hueco más corto. Si sueltas el freno, el auto rueda."),
        };

        /// <summary>true si la escena del escenario está en el build</summary>
        public static bool IsAvailable(ScenarioInfo scenario)
            => scenario != null && Application.CanStreamedLevelBeLoaded(scenario.SceneName);

        public static ScenarioInfo FindBySceneName(string sceneName)
        {
            foreach (var s in All) if (s.SceneName == sceneName) return s;
            return null;
        }
    }

    /// <summary>Elección hecha en el menú, que leen las escenas al iniciar (se conserva al cambiar de escena)</summary>
    public static class ScenarioSession
    {
        public const int DifficultyCount = 3;

        /// <summary>0 = fácil, 1 = media, 2 = difícil. Si se abre una escena directo en el Editor, queda en media.</summary>
        public static int Difficulty { get; private set; } = 1;

        public static void SetDifficulty(int value) => Difficulty = Mathf.Clamp(value, 0, DifficultyCount - 1);
    }
}
