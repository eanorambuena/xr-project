using NUnit.Framework;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    public class EngineSoundTests
    {
        [Test] public void Idle_WhenStoppedNoThrottle() => Assert.AreEqual(0.8f, EngineSound.Pitch(0f, 0f), 1e-4f);
        [Test] public void PitchRises_WithSpeedAndThrottle()
        {
            Assert.Greater(EngineSound.Pitch(10f, 0f), EngineSound.Pitch(0f, 0f));
            Assert.Greater(EngineSound.Pitch(10f, 1f), EngineSound.Pitch(10f, 0f));
        }
        [Test] public void Reverse_SpeedCountsAsPositive() => Assert.AreEqual(EngineSound.Pitch(5f, 0f), EngineSound.Pitch(-5f, 0f), 1e-5f);
        [Test] public void Pitch_Clamped() => Assert.AreEqual(1.6f, EngineSound.Pitch(200f, 5f), 1e-4f);
        [Test] public void Volume_Range()
        {
            Assert.AreEqual(0.18f, EngineSound.Volume(0f), 1e-4f);
            Assert.AreEqual(0.35f, EngineSound.Volume(1f), 1e-4f);
        }
    }
}
