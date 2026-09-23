using UnityEngine;

namespace VRCar.Obstacles
{
    /// <summary>
    /// Proporciona feedback visual y auditivo al colisionar con obstáculos
    /// Implementa la especificación SDD:
    /// - SPEC 5.1: Detección de colisión y sonido
    /// - SPEC 5.2: Cambio de color en colisión
    /// </summary>
    public class CollisionFeedback : MonoBehaviour
    {
        private Renderer vehicleRenderer;
        private AudioSource audioSource;
        private AudioClip collisionClip;
        private Color originalColor;
        private bool isColliding = false;

        private void Start()
        {
            // Guardar color original
            if (vehicleRenderer != null)
            {
                originalColor = vehicleRenderer.material.color;
            }

            // Intentar cargar sonido de colisión (placeholder)
            if (audioSource != null)
            {
                // En una aplicación real, esto sería asignado desde el inspector
                // o cargado desde un archivo de sonido
            }
        }

        /// <summary>
        /// Asignar referencias (usado por tests)
        /// </summary>
        public void SetReferences(Renderer renderer, AudioSource audio)
        {
            vehicleRenderer = renderer;
            audioSource = audio;
            originalColor = renderer.material.color;
        }

        /// <summary>
        /// Detección de colisión con obstáculos
        /// SPEC 5.1 y 5.2
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                ApplyCollisionFeedback();
                isColliding = true;
            }
        }

        /// <summary>
        /// Fin de colisión con obstáculos
        /// SPEC 5.2: Restaurar color original
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                RestoreCollisionFeedback();
                isColliding = false;
            }
        }

        /// <summary>
        /// Aplicar feedback de colisión (sonido + color rojo)
        /// </summary>
        private void ApplyCollisionFeedback()
        {
            // Cambiar color a rojo
            if (vehicleRenderer != null)
            {
                vehicleRenderer.material.color = Color.red;
            }

            // Reproducir sonido de colisión
            if (audioSource != null && collisionClip != null)
            {
                audioSource.PlayOneShot(collisionClip);
            }
        }

        /// <summary>
        /// Restaurar estado normal después de colisión
        /// </summary>
        private void RestoreCollisionFeedback()
        {
            // Restaurar color original
            if (vehicleRenderer != null)
            {
                vehicleRenderer.material.color = originalColor;
            }
        }

        /// <summary>
        /// Simular colisión (para testing)
        /// </summary>
        public void SimulateCollision(string tagName)
        {
            if (tagName == "Obstacle")
            {
                ApplyCollisionFeedback();
                isColliding = true;
            }
        }

        /// <summary>
        /// Simular fin de colisión (para testing)
        /// </summary>
        public void SimulateCollisionExit(string tagName)
        {
            if (tagName == "Obstacle" && isColliding)
            {
                RestoreCollisionFeedback();
                isColliding = false;
            }
        }

        /// <summary>
        /// Asignar clip de sonido para colisión
        /// </summary>
        public void SetCollisionClip(AudioClip clip)
        {
            collisionClip = clip;
        }
    }
}
