using UnityEngine;

namespace VRCar.XR
{
    /// <summary>
    /// Configura la altura correcta del XROrigin y cámara para VR
    /// Implementa la especificación SDD:
    /// - SPEC 6.1: Altura de cámara y XROrigin
    ///
    /// Parámetros:
    /// - XROrigin height: 1.7m (altura promedio de conductor)
    /// - Camera offset: 0.17m (eye height en cockpit del auto)
    /// </summary>
    public class VROriginSetup : MonoBehaviour
    {
        // Alturas configurables
        [SerializeField]
        private float xrOriginHeight = 1.7f;     // Altura promedio conductor (m)

        [SerializeField]
        private float cameraEyeHeight = 0.17f;   // Altura de ojos sobre piso del auto (m)

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Configurar altura de XROrigin y cámara
        /// SPEC 6.1
        /// </summary>
        public void Initialize()
        {
            // Posicionar XROrigin a altura de conductor (1.7m)
            Vector3 xrOriginPos = transform.position;
            xrOriginPos.y = xrOriginHeight;
            transform.position = xrOriginPos;

            // Asegurarse de que no hay rotación
            transform.rotation = Quaternion.identity;

            // Configurar cámara principal (eye height sobre piso del auto)
            Camera mainCamera = GetComponentInChildren<Camera>();
            if (mainCamera != null)
            {
                Vector3 cameraLocalPos = mainCamera.transform.localPosition;
                cameraLocalPos.y = cameraEyeHeight;
                mainCamera.transform.localPosition = cameraLocalPos;
            }
        }

        /// <summary>
        /// Obtener altura total de los ojos del conductor en mundo
        /// = xrOriginHeight + cameraEyeHeight
        /// </summary>
        public float GetEyeHeightInWorld()
        {
            return xrOriginHeight + cameraEyeHeight;
        }
    }
}
