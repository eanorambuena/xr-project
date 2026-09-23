namespace VRCar.Obstacles
{
    /// <summary>
    /// Contrato mínimo que necesita el HUD para mostrar distancia a obstáculos.
    /// Permite que producción (ObstacleDetector) y tests (MockObstacleDetector)
    /// sean intercambiables sin acoplar código de producción a clases de test.
    /// </summary>
    public interface IObstacleDetector
    {
        float GetClosestDistance();
    }
}
