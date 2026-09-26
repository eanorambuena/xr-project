using NUnit.Framework;
using UnityEngine;
using VRCar.Core;

namespace VRCar.Tests.Core
{
    /// <summary>
    /// TDD Tests para la puntuación de CU1 (Juicio de Esquinas)
    /// "Puntuación: distancia de separación" + "Puntuación al completar"
    /// </summary>
    public class CornerJudgmentTests
    {
        // ------------------------------------------------------------
        // Holgura (clearance) = espacio libre entre el costado del auto y el cono más cercano
        // ------------------------------------------------------------
        [Test]
        public void Clearance_CenteredCar_IsSymmetric()
        {
            // Puerta de 3.0m entre centros, conos de radio 0.175, auto de 1.856 centrado en x=0
            float c = CornerJudgment.ComputeClearance(-0.928f, 0.928f, -1.5f, 1.5f, 0.175f);
            Assert.AreEqual(1.5f - 0.175f - 0.928f, c, 0.001f);
        }

        [Test]
        public void Clearance_OffCenterCar_UsesClosestSide()
        {
            // Auto desplazado 0.3m a la derecha
            float c = CornerJudgment.ComputeClearance(-0.628f, 1.228f, -1.5f, 1.5f, 0.175f);
            Assert.AreEqual(1.5f - 0.175f - 1.228f, c, 0.001f);
        }

        [Test]
        public void Clearance_Overlapping_IsNegative()
        {
            float c = CornerJudgment.ComputeClearance(-0.928f, 0.928f, -1.0f, 1.0f, 0.175f);
            Assert.Less(c, 0f);
        }

        // ------------------------------------------------------------
        // Registro de puertas y resumen final
        // ------------------------------------------------------------
        [Test]
        public void RegisterGate_Clean_CountsAsClean()
        {
            var score = new CornerJudgment.ScoreSheet(totalGates: 2);
            score.RegisterGate(0.30f, hit: false);
            Assert.AreEqual(1, score.GatesPassed);
            Assert.AreEqual(1, score.CleanGates);
            Assert.IsFalse(score.IsComplete);
        }

        [Test]
        public void RegisterGate_Hit_NotClean()
        {
            var score = new CornerJudgment.ScoreSheet(totalGates: 2);
            score.RegisterGate(0.05f, hit: true);
            Assert.AreEqual(1, score.GatesPassed);
            Assert.AreEqual(0, score.CleanGates);
        }

        [Test]
        public void Complete_AfterAllGates_ReportsMinClearance()
        {
            var score = new CornerJudgment.ScoreSheet(totalGates: 3);
            score.RegisterGate(0.40f, false);
            score.RegisterGate(0.12f, false);
            score.RegisterGate(0.25f, true);
            Assert.IsTrue(score.IsComplete);
            Assert.AreEqual(2, score.CleanGates);
            Assert.AreEqual(0.12f, score.MinCleanClearance, 0.0001f, "Holgura mínima solo de puertas limpias");
        }

        [Test]
        public void Summary_ContainsCleanGatesAndClearance()
        {
            var score = new CornerJudgment.ScoreSheet(totalGates: 2);
            score.RegisterGate(0.40f, false);
            score.RegisterGate(0.12f, false);
            string summary = score.Summary(elapsedSeconds: 42.4f);
            StringAssert.Contains("2/2", summary);
            StringAssert.Contains("0.12", summary);
            StringAssert.Contains("42", summary);
        }

        [Test]
        public void GateMessage_ShowsClearanceInCentimeters()
        {
            string msg = CornerJudgment.GateMessage(index: 0, total: 4, clearance: 0.237f, hit: false);
            StringAssert.Contains("1/4", msg);
            StringAssert.Contains("24 cm", msg);
        }

        [Test]
        public void GateMessage_Hit_SaysTouched()
        {
            string msg = CornerJudgment.GateMessage(index: 1, total: 4, clearance: -0.02f, hit: true);
            StringAssert.Contains("2/4", msg);
            StringAssert.Contains("tocaste", msg.ToLower());
        }
    }
}
