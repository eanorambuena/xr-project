using NUnit.Framework;
using UnityEngine;
using VRCar.XR;

namespace VRCar.Tests.XR
{
    /// <summary>
    /// TDD Tests para VROriginSetup
    /// Verifica la configuración correcta de altura de cámara y origen XR
    /// </summary>
    public class VROriginSetupTests
    {
        private GameObject xrOriginGameObject;
        private VROriginSetup vrOriginSetup;
        private GameObject mainCameraGameObject;
        private Camera mainCamera;

        [SetUp]
        public void Setup()
        {
            // Crear XROrigin
            xrOriginGameObject = new GameObject("XROrigin");
            vrOriginSetup = xrOriginGameObject.AddComponent<VROriginSetup>();

            // Crear cámara principal como hijo
            mainCameraGameObject = new GameObject("MainCamera");
            mainCameraGameObject.transform.SetParent(xrOriginGameObject.transform);
            mainCamera = mainCameraGameObject.AddComponent<Camera>();

            // Establecer como main camera
            mainCamera.tag = "MainCamera";
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(xrOriginGameObject);
        }

        // ============================================================
        // SPEC 6.1: Altura de Cámara
        // ============================================================
        /// <summary>
        /// TEST 6.1: VROriginSetup_Initialize_SetsCorrectHeight
        ///
        /// GIVEN: Iniciar escena con VROriginSetup
        /// WHEN: Start() se ejecuta
        /// THEN:
        ///   - XROrigin.position.y = 1.7m (altura promedio conductor)
        ///   - Camera.main.localPosition.y = 0.17m (sobre piso del auto)
        /// </summary>
        [Test]
        public void VROriginSetup_Initialize_SetsCorrectHeight()
        {
            // Execute: inicializar setup
            vrOriginSetup.Initialize();

            // Assert: XROrigin debe estar a 1.7m
            Assert.AreApproximatelyEqual(xrOriginGameObject.transform.position.y, 1.7f, 0.01f,
                "XROrigin debe estar a 1.7m (altura promedio conductor)");
        }

        /// <summary>
        /// TEST 6.1b: VROriginSetup_CameraHeightAboveFloor
        /// GIVEN: XROrigin configurado
        /// THEN: Camera está a 0.17m sobre piso del auto (eye height)
        /// </summary>
        [Test]
        public void VROriginSetup_CameraHeightAboveFloor()
        {
            vrOriginSetup.Initialize();

            // Camera local position debe ser 0.17m (eye height en auto)
            Assert.AreApproximatelyEqual(mainCamera.transform.localPosition.y, 0.17f, 0.01f,
                "Cámara debe estar 0.17m sobre piso del auto (eye height)");
        }

        /// <summary>
        /// TEST 6.1c: VROriginSetup_XROriginNotRotated
        /// GIVEN: VROriginSetup configurado
        /// THEN: XROrigin no debe estar rotado (identity rotation)
        /// </summary>
        [Test]
        public void VROriginSetup_XROriginNotRotated()
        {
            vrOriginSetup.Initialize();

            // XROrigin debe tener rotación identity
            Assert.AreEqual(xrOriginGameObject.transform.rotation, Quaternion.identity,
                "XROrigin debe tener rotación identity (sin rotación)");
        }

        /// <summary>
        /// TEST 6.1d: VROriginSetup_CameraOffsetCorrect
        /// GIVEN: Setup completado
        /// THEN: Position absoluta de cámara = XROrigin.y + Camera.localPosition.y
        ///       = 1.7 + 0.17 = 1.87m (altura de ojos del conductor)
        /// </summary>
        [Test]
        public void VROriginSetup_CameraOffsetCorrect()
        {
            vrOriginSetup.Initialize();

            float expectedWorldHeight = 1.7f + 0.17f; // 1.87m
            float actualWorldHeight = mainCamera.transform.position.y;

            Assert.AreApproximatelyEqual(actualWorldHeight, expectedWorldHeight, 0.01f,
                "Camera world height debe ser 1.87m (1.7m XROrigin + 0.17m offset)");
        }

        /// <summary>
        /// TEST 6.1e: VROriginSetup_MultipleInitializeCalls
        /// GIVEN: Initialize() llamado múltiples veces
        /// THEN: Valores no cambian (idempotent)
        /// </summary>
        [Test]
        public void VROriginSetup_MultipleInitializeCalls()
        {
            vrOriginSetup.Initialize();
            Vector3 positionAfterFirstInit = xrOriginGameObject.transform.position;

            vrOriginSetup.Initialize();
            Vector3 positionAfterSecondInit = xrOriginGameObject.transform.position;

            Assert.AreEqual(positionAfterFirstInit, positionAfterSecondInit,
                "Múltiples Initialize() calls deben ser idempotent");
        }
    }
}
