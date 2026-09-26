using NUnit.Framework;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    /// <summary>Tests de la geometría Ackermann de las ruedas delanteras</summary>
    public class VehicleVisualsTests
    {
        const float L = 2.613f, T = 1.56f;

        [Test]
        public void Straight_BothWheelsStraight()
        {
            var (l, r) = VehicleVisuals.AckermannAngles(0f, L, T);
            Assert.AreEqual(0f, l); Assert.AreEqual(0f, r);
        }

        [Test]
        public void RightTurn_InnerRightWheelTurnsMore()
        {
            var (l, r) = VehicleVisuals.AckermannAngles(30f, L, T);
            Assert.Greater(r, l, "En un giro a la derecha la rueda derecha (interior) gira más");
            Assert.Greater(l, 0f);
            Assert.Less(l, 30f); Assert.Greater(r, 30f);
        }

        [Test]
        public void LeftTurn_IsMirrorOfRightTurn()
        {
            var (l1, r1) = VehicleVisuals.AckermannAngles(20f, L, T);
            var (l2, r2) = VehicleVisuals.AckermannAngles(-20f, L, T);
            Assert.AreEqual(-r1, l2, 1e-4f);
            Assert.AreEqual(-l1, r2, 1e-4f);
        }

        [Test]
        public void FullLock_OuterWheel_MatchesTurningCircle()
        {
            // A tope (35.1°) la rueda exterior describe el diámetro de giro de 10.4 m
            float delta = VehiclePhysics.ComputeMaxSteerAngle(L, T, 10.4f);
            var (outer, inner) = VehicleVisuals.AckermannAngles(delta, L, T);
            float outerRadius = L / UnityEngine.Mathf.Sin(outer * UnityEngine.Mathf.Deg2Rad);
            Assert.AreEqual(5.2f, outerRadius, 0.05f);
        }
    }
}
