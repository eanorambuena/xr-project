using NUnit.Framework;
using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    /// <summary>Tests del cuerpo virtual del conductor (torso y piernas sentados, sin brazos)</summary>
    public class DriverBodyTests
    {
        const float EyeHeight = 1.2f;
        GameObject origin;
        DriverBody body;

        [SetUp]
        public void Setup()
        {
            origin = new GameObject("XR Origin");
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(origin.transform, false);
            offset.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            body = origin.AddComponent<DriverBody>();
            body.Configure(offset.transform, EyeHeight);
            body.Build();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(origin);

        [Test]
        public void Build_CreatesTorsoLegsAndShoes()
        {
            foreach (var n in new[] { "Torso", "Pelvis", "Thigh_L", "Thigh_R", "Shin_L", "Shin_R", "Shoe_L", "Shoe_R" })
                Assert.IsNotNull(body.BodyRoot.Find(n), "Falta la pieza " + n);
        }

        [Test]
        public void Body_HasNoArmsNorHead()
        {
            foreach (Transform t in body.BodyRoot)
            {
                var n = t.name.ToLowerInvariant();
                Assert.IsFalse(n.Contains("arm") || n.Contains("hand") || n.Contains("head"),
                    "No debe haber brazos, manos ni cabeza: " + t.name);
            }
        }

        [Test]
        public void Body_HasNoColliders_SoItNeverAffectsThePhysics()
        {
            Assert.AreEqual(0, body.BodyRoot.GetComponentsInChildren<Collider>().Length);
        }

        [Test]
        public void Body_StaysWellBelowTheEyes_SoItNeverBlocksTheView()
        {
            // Nada del cuerpo puede acercarse a la cámara: el punto más alto queda >= 12 cm bajo los ojos
            float top = float.MinValue;
            foreach (var r in body.BodyRoot.GetComponentsInChildren<Renderer>())
                top = Mathf.Max(top, r.bounds.max.y - origin.transform.position.y);
            Assert.LessOrEqual(top, EyeHeight - 0.12f);
        }

        [Test]
        public void Pose_LegsPointForward_FeetOnTheCabinFloor()
        {
            var p = body.ComputePose(EyeHeight);
            Assert.Greater(p.kneeL.z, p.hip.z + 0.3f, "Las rodillas van delante de la cadera");
            Assert.Greater(p.ankleL.z, p.kneeL.z, "Los pies van delante de las rodillas");
            Assert.Less(p.ankleL.y, p.kneeL.y, "Los tobillos quedan más bajos que las rodillas");
            Assert.Less(p.shoulder.y, EyeHeight, "Los hombros quedan bajo los ojos");
            Assert.Less(p.shoulder.z, 0f, "Los hombros quedan detrás de los ojos");
        }

        [Test]
        public void Pose_ShinKeepsItsLength()
        {
            var p = body.ComputePose(EyeHeight);
            Assert.AreEqual(0.42f, Vector3.Distance(p.kneeL, p.ankleL), 0.01f);
        }

        [Test]
        public void Build_Twice_DoesNotDuplicateParts()
        {
            body.Build();
            Assert.AreEqual(2, origin.transform.childCount, "Camera Offset + una sola raíz del cuerpo");
        }

        [Test]
        public void Visible_TogglesTheWholeBody()
        {
            body.Visible = false;
            Assert.IsFalse(body.BodyRoot.gameObject.activeSelf);
            body.Visible = true;
            Assert.IsTrue(body.BodyRoot.gameObject.activeSelf);
        }

        [Test]
        public void Body_IsNotAChildOfTheCamera_SoHeadMovementDoesNotDragIt()
        {
            Assert.IsNull(body.BodyRoot.GetComponentInParent<Camera>());
            Assert.AreEqual(origin.transform, body.BodyRoot.parent);
        }

        // ------------------------------------------------------------------
        // Pierna derecha y pedales
        // ------------------------------------------------------------------
        [Test]
        public void Build_CreatesBothPedals()
        {
            Assert.IsNotNull(body.BodyRoot.Find("Pedal_Accel"));
            Assert.IsNotNull(body.BodyRoot.Find("Pedal_Brake"));
        }

        [Test]
        public void Throttle_MovesRightFootToTheAccelerator_AndItReturnsWhenReleased()
        {
            var shoe = body.BodyRoot.Find("Shoe_R");
            Vector3 rest = shoe.localPosition;

            body.ApplyPedals(1f, 0f, 10f);
            Assert.Greater(shoe.localPosition.z, rest.z + 0.02f, "El pie avanza hacia el acelerador");
            Assert.AreEqual(rest.x, shoe.localPosition.x, 0.01f, "El acelerador está en la línea del pie");

            body.ApplyPedals(0f, 0f, 10f);
            Assert.AreEqual(0f, Vector3.Distance(rest, shoe.localPosition), 0.002f, "Al soltar vuelve a su posición original");
        }

        [Test]
        public void Brake_MovesRightFootTowardsTheCentre_AndItReturnsWhenReleased()
        {
            var shoe = body.BodyRoot.Find("Shoe_R");
            Vector3 rest = shoe.localPosition;

            body.ApplyPedals(0f, 1f, 10f);
            Assert.Less(shoe.localPosition.x, rest.x - 0.1f, "El freno queda a la izquierda del acelerador");

            body.ApplyPedals(0f, 0f, 10f);
            Assert.AreEqual(0f, Vector3.Distance(rest, shoe.localPosition), 0.002f);
        }

        [Test]
        public void Brake_WinsOverThrottle()
        {
            var shoe = body.BodyRoot.Find("Shoe_R");
            float restX = shoe.localPosition.x;
            body.ApplyPedals(1f, 1f, 10f);
            Assert.Less(shoe.localPosition.x, restX - 0.1f);
            Assert.AreEqual(0f, body.AccelWeight, 0.01f);
        }

        [Test]
        public void PartialPress_MovesTheFootPartially()
        {
            var shoe = body.BodyRoot.Find("Shoe_R");
            float restZ = shoe.localPosition.z;
            body.ApplyPedals(1f, 0f, 10f);
            float fullZ = shoe.localPosition.z;
            body.ApplyPedals(0f, 0f, 10f);
            body.ApplyPedals(0.5f, 0f, 10f);
            Assert.Greater(shoe.localPosition.z, restZ);
            Assert.Less(shoe.localPosition.z, fullZ);
        }

        [Test]
        public void LeftLeg_NeverMoves()
        {
            var shoe = body.BodyRoot.Find("Shoe_L");
            Vector3 rest = shoe.localPosition;
            body.ApplyPedals(1f, 0f, 10f);
            body.ApplyPedals(0f, 1f, 10f);
            Assert.AreEqual(rest, shoe.localPosition);
        }

        [Test]
        public void RightLeg_KeepsItsBoneLengths_WhileMoving()
        {
            var thigh = body.BodyRoot.Find("Thigh_R");
            var shin = body.BodyRoot.Find("Shin_R");
            float thighLen = thigh.localScale.y;
            float shinLen = shin.localScale.y;
            foreach (var (t, b) in new[] { (1f, 0f), (0f, 1f), (0.4f, 0f), (0f, 0f) })
            {
                body.ApplyPedals(t, b, 10f);
                // El largo se codifica en la escala Y: no puede variar más que unos milímetros
                Assert.AreEqual(thighLen, thigh.localScale.y, 0.01f);
                Assert.AreEqual(shinLen, shin.localScale.y, 0.01f);
            }
        }

        [Test]
        public void Foot_StaysOnTheFloor_WhilePressing()
        {
            var shoe = body.BodyRoot.Find("Shoe_R");
            float restY = shoe.localPosition.y;
            body.ApplyPedals(1f, 0f, 10f);
            Assert.AreEqual(restY, shoe.localPosition.y, 0.06f);
        }

        [Test]
        public void LeftFoot_IsRetractedAndAwayFromTheBrake()
        {
            var p = body.ComputePose(EyeHeight);
            const float brakeSpacing = 0.13f; // pedalSpacing por defecto: el freno queda a esa distancia a la izquierda del pie derecho
            Assert.Less(p.ankleL.z, p.ankleR.z - 0.05f, "El pie izquierdo queda más atrás que el derecho");
            Assert.Less(p.ankleL.x, p.ankleR.x - brakeSpacing - 0.08f, "Y no sobre el pedal de freno, sino hacia la puerta");
        }

        [Test]
        public void LeftLeg_KeepsItsBoneLengths()
        {
            var p = body.ComputePose(EyeHeight);
            var hipL = new Vector3(-0.09f, p.hip.y, p.hip.z);
            Assert.AreEqual(0.40f, Vector3.Distance(hipL, p.kneeL), 0.01f);
            Assert.AreEqual(0.42f, Vector3.Distance(p.kneeL, p.ankleL), 0.01f);
        }

        [Test]
        public void LeftKnee_StaysBelowTheSteeringWheel()
        {
            // El aro del volante queda ~0.76 m sobre el piso: la rodilla (con su grosor) debe pasar por debajo
            var p = body.ComputePose(EyeHeight);
            Assert.Less(p.kneeL.y + 0.085f, 0.78f);
        }
    }
}
