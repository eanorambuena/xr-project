using UnityEngine;

namespace VRCar.Obstacles
{
    /// <summary>
    /// Marca una solera/cuneta (vereda): sigue siendo un Obstacle (el sensor la detecta y el auto no la sube),
    /// pero rozarla NO cuenta como choque en las estadísticas (estacionamiento en paralelo y pendiente).
    /// </summary>
    public class CurbSurface : MonoBehaviour { }
}
