using NUnit.Framework;
using VRCar.UI;

namespace VRCar.Tests.UI
{
    public class ParkingSensorTests
    {
        [TestCase(5f, 0)]
        [TestCase(1.6f, 0)]
        [TestCase(1.2f, 1)]
        [TestCase(0.8f, 2)]
        [TestCase(0.45f, 3)]
        [TestCase(0.2f, 4)]
        public void Level_ByDistance(float d, int expected)
        {
            Assert.AreEqual(expected, ParkingSensor.Level(d));
        }

        [Test]
        public void BeepInterval_NoBeepFarAway()
        {
            Assert.IsTrue(float.IsPositiveInfinity(ParkingSensor.BeepInterval(2f)));
        }

        [Test]
        public void BeepInterval_ShorterWhenCloser_ContinuousAtEnd()
        {
            float far = ParkingSensor.BeepInterval(1.4f), mid = ParkingSensor.BeepInterval(0.8f), near = ParkingSensor.BeepInterval(0.4f);
            Assert.Greater(far, mid);
            Assert.Greater(mid, near);
            Assert.AreEqual(0f, ParkingSensor.BeepInterval(0.25f));
        }

        [Test]
        public void WarningDistance_IsEarlierTheFasterTheCarApproaches()
        {
            Assert.AreEqual(0.8f, ParkingSensor.WarningDistance(0.8f, 0f), 1e-4f, "Detenido: la distancia real");
            Assert.Less(ParkingSensor.WarningDistance(0.8f, 1f), ParkingSensor.WarningDistance(0.8f, 0.3f));
            Assert.AreEqual(0.8f, ParkingSensor.WarningDistance(0.8f, -1f), 1e-4f, "Alejándose no adelanta nada");
        }

        [Test]
        public void ContinuousBeep_StartsBeforeTheCarCanStop()
        {
            // A 1 m/s el pitido rápido (continuo) ya suena a ~90 cm, mucho antes de tocar el obstáculo
            float interval = ParkingSensor.BeepInterval(ParkingSensor.WarningDistance(0.85f, 1f));
            Assert.AreEqual(0f, interval);
            // Despacio y lejos sigue sonando lento
            Assert.Greater(ParkingSensor.BeepInterval(ParkingSensor.WarningDistance(1.2f, 0.2f)), 0.1f);
        }

        [Test]
        public void Label_ShowsCentimetersOrStop()
        {
            Assert.AreEqual("--", ParkingSensor.Label(3f));
            Assert.AreEqual("85 cm", ParkingSensor.Label(0.85f));
            Assert.AreEqual("STOP", ParkingSensor.Label(0.2f));
        }
    }
}
