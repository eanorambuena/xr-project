using NUnit.Framework;
using UnityEngine;
using VRCar.Core;

namespace VRCar.Tests.Core
{
    /// <summary>Estadísticas del intento, pantalla de resultado y fin de la experiencia</summary>
    public class ExperienceTests
    {
        private readonly System.Collections.Generic.List<GameObject> created = new System.Collections.Generic.List<GameObject>();
        private GameObject Make(string n) { var g = new GameObject(n); created.Add(g); return g; }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f; AudioListener.pause = false;
            foreach (var g in created) if (g != null) Object.DestroyImmediate(g);
            created.Clear();
        }

        // ---------------- ExperienceStats ----------------
        [Test]
        public void TheClock_StartsWhenTheCarFirstMoves_AndKeepsRunningWhileStopped()
        {
            var s = new ExperienceStats();
            s.Tick(1f, 0f);
            Assert.IsFalse(s.Started);
            Assert.AreEqual(0f, s.Elapsed);
            s.Tick(0.5f, 2f);
            Assert.IsTrue(s.Started);
            s.Tick(2f, 0f);   // se detiene a maniobrar: el tiempo sigue
            Assert.AreEqual(2.5f, s.Elapsed, 1e-4f);
        }

        [Test]
        public void Hits_CloseTogetherCountAsOneCrash()
        {
            var s = new ExperienceStats();
            Assert.IsTrue(s.RegisterHit(1.0f));
            Assert.IsFalse(s.RegisterHit(1.5f), "rebote seguido");
            Assert.IsFalse(s.RegisterHit(2.2f), "sigue rozando: el mismo choque");
            Assert.IsTrue(s.RegisterHit(3.2f), "otro choque, tras separarse");
            Assert.AreEqual(2, s.Collisions);
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var s = new ExperienceStats();
            s.Tick(1f, 3f); s.RegisterHit(1f);
            s.Reset();
            Assert.AreEqual(0, s.Collisions);
            Assert.AreEqual(0f, s.Elapsed);
            Assert.IsFalse(s.Started);
            Assert.IsTrue(s.RegisterHit(1.1f), "tras reiniciar, el primer golpe cuenta aunque sea reciente");
        }

        [Test]
        public void Passes_ComparesTheRoundedPercentageTheUserSees()
        {
            Assert.AreEqual(0.90f, ExperienceStats.PassAccuracy);
            Assert.IsTrue(ExperienceStats.Passes(1f, 0));
            Assert.IsTrue(ExperienceStats.Passes(0.90f, 0));
            Assert.IsTrue(ExperienceStats.Passes(0.896f, 0), "se ve como 90 %");
            Assert.IsFalse(ExperienceStats.Passes(0.894f, 0), "se ve como 89 %");
            Assert.IsFalse(ExperienceStats.Passes(0f, 0));
        }

        [Test]
        public void Passes_AllowsAtMostOneCrash()
        {
            Assert.AreEqual(1, ExperienceStats.MaxCollisions);
            Assert.IsTrue(ExperienceStats.Passes(0.99f, 0));
            Assert.IsTrue(ExperienceStats.Passes(0.99f, 1), "un golpe todavía aprueba");
            Assert.IsFalse(ExperienceStats.Passes(0.99f, 2), "dos golpes reprueban aunque haya quedado perfecto");
            Assert.IsFalse(ExperienceStats.Passes(0.5f, 0), "pocos choques no compensan una mala precisión");
        }

        [Test]
        public void GradeFor_HasAMiddleStateBetweenPassedAndFailed()
        {
            Assert.AreEqual(ExperienceGrade.Passed, ExperienceStats.GradeFor(0.90f, 1));
            // Cerca: precisión entre 75 y 90 %, o 2-3 choques con buena precisión
            Assert.AreEqual(ExperienceGrade.Regular, ExperienceStats.GradeFor(0.78f, 0));
            Assert.AreEqual(ExperienceGrade.Regular, ExperienceStats.GradeFor(0.95f, 2));
            Assert.AreEqual(ExperienceGrade.Regular, ExperienceStats.GradeFor(0.76f, 3));
            // Reprobado: muy poca precisión o demasiados choques
            Assert.AreEqual(ExperienceGrade.Failed, ExperienceStats.GradeFor(0.60f, 0));
            Assert.AreEqual(ExperienceGrade.Failed, ExperienceStats.GradeFor(0.99f, 4));
            Assert.AreEqual(ExperienceGrade.Failed, ExperienceStats.GradeFor(0f, 0));
        }

        [Test]
        public void AngleGrade_UpTo5Passes_UpTo10IsRegular_MoreFails()
        {
            Assert.AreEqual(ExperienceGrade.Passed, ExperienceStats.AngleGrade(0f));
            Assert.AreEqual(ExperienceGrade.Passed, ExperienceStats.AngleGrade(5f));
            Assert.AreEqual(ExperienceGrade.Passed, ExperienceStats.AngleGrade(5.04f), "se ve como 5.0°");
            Assert.AreEqual(ExperienceGrade.Regular, ExperienceStats.AngleGrade(5.06f), "se ve como 5.1°");
            Assert.AreEqual(ExperienceGrade.Regular, ExperienceStats.AngleGrade(10f));
            Assert.AreEqual(ExperienceGrade.Failed, ExperienceStats.AngleGrade(10.4f));
        }

        [Test]
        public void GradeFor_IsTheWorstOfPrecisionCrashesAndAlignment()
        {
            Assert.AreEqual(ExperienceGrade.Passed, ExperienceStats.GradeFor(0.95f, 0, 3f));
            Assert.AreEqual(ExperienceGrade.Regular, ExperienceStats.GradeFor(0.95f, 0, 7f), "perfecto pero algo torcido");
            Assert.AreEqual(ExperienceGrade.Failed, ExperienceStats.GradeFor(0.95f, 0, 12f), "muy torcido");
            Assert.AreEqual(ExperienceGrade.Regular, ExperienceStats.GradeFor(0.95f, 2, 2f));
            Assert.AreEqual(ExperienceGrade.Failed, ExperienceStats.GradeFor(0.60f, 0, 2f));
        }

        [Test]
        public void Passes_NeedsAllThree()
        {
            Assert.IsTrue(ExperienceStats.Passes(0.95f, 1, 5f));
            Assert.IsFalse(ExperienceStats.Passes(0.95f, 1, 6f));
            Assert.IsFalse(ExperienceStats.Passes(0.95f, 2, 0f));
            Assert.IsFalse(ExperienceStats.Passes(0.80f, 0, 0f));
        }

        [Test]
        public void Build_AlignmentCountsTowardTheGrade()
        {
            var s = new ExperienceStats();
            Assert.AreEqual(ExperienceGrade.Passed, s.Build(0.95f, "x", "", 4f, 0f).Grade);
            Assert.AreEqual(ExperienceGrade.Regular, s.Build(0.95f, "x", "", 8f, 0f).Grade);
            Assert.AreEqual(ExperienceGrade.Failed, s.Build(0.95f, "x", "", 15f, 0f).Grade);
        }

        [Test]
        public void GradeFor_Limits_UseTheRoundedPercentage()
        {
            Assert.AreEqual(ExperienceGrade.Regular, ExperienceStats.GradeFor(0.746f, 0), "se ve como 75 %");
            Assert.AreEqual(ExperienceGrade.Failed, ExperienceStats.GradeFor(0.744f, 0), "se ve como 74 %");
            Assert.AreEqual(ExperienceGrade.Regular, ExperienceStats.GradeFor(0.886f, 1));
        }

        [Test]
        public void Percent_IsClamped()
        {
            Assert.AreEqual(0, ExperienceStats.Percent(-0.3f));
            Assert.AreEqual(100, ExperienceStats.Percent(1.7f));
            Assert.AreEqual(91, ExperienceStats.Percent(0.906f));
        }

        [Test]
        public void FormatTime_IsMinutesAndTwoDigitSeconds()
        {
            Assert.AreEqual("0:00", ExperienceStats.FormatTime(0f));
            Assert.AreEqual("1:23", ExperienceStats.FormatTime(83f));
            Assert.AreEqual("1:00", ExperienceStats.FormatTime(59.6f));
            Assert.AreEqual("10:05", ExperienceStats.FormatTime(605f));
            Assert.AreEqual("0:00", ExperienceStats.FormatTime(-4f));
        }

        [Test]
        public void Build_CollectsTheNumbers()
        {
            var s = new ExperienceStats();
            s.Tick(10f, 2f); s.RegisterHit(2f);
            var r = s.Build(0.91f, "Estacionamiento en batería", "Media");
            Assert.AreEqual(1, r.Collisions);
            Assert.AreEqual(10f, r.Seconds, 1e-4f);
            Assert.IsTrue(r.Passed);
            Assert.AreEqual("Media", r.DifficultyName);
            Assert.IsFalse(s.Build(0.5f, "x", null).Passed);
        }

        [Test]
        public void Build_CarriesTheAlignmentAndCentering_OnlyWhenTheCarEntered()
        {
            var s = new ExperienceStats();
            var entered = s.Build(0.9f, "x", "", 3.5f, 0.12f);
            Assert.IsTrue(entered.HasPose);
            Assert.AreEqual(3.5f, entered.AngleDeg, 1e-4f);
            Assert.AreEqual(0.12f, entered.CenterOffsetM, 1e-4f);
            Assert.IsFalse(s.Build(0f, "x", "", 30f, 1f).HasPose);
        }

        [Test]
        public void Build_TwoCrashesFailEvenWithAPerfectParking()
        {
            var s = new ExperienceStats();
            s.RegisterHit(1f); s.RegisterHit(5f);
            Assert.IsFalse(s.Build(1f, "x", "").Passed);
            var one = new ExperienceStats();
            one.RegisterHit(1f);
            Assert.IsTrue(one.Build(1f, "x", "").Passed);
        }

        // ---------------- ResultsScreen ----------------
        private ResultsScreen NewScreen() { var s = Make("Results").AddComponent<ResultsScreen>(); s.Build(); return s; }

        [Test]
        public void ResultsScreen_ShowsThePassedResult()
        {
            var screen = NewScreen();
            screen.Fill(new ExperienceResult { ScenarioTitle = "Estacionamiento en paralelo", DifficultyName = "Difícil", Collisions = 2, Seconds = 83f, Accuracy = 0.912f, Grade = ExperienceGrade.Passed, HasPose = true, AngleDeg = 2.14f, CenterOffsetM = 0.05f });
            Assert.AreEqual("APROBADO", screen.VerdictText);
            Assert.AreEqual("2", screen.CollisionsText);
            Assert.AreEqual("1:23", screen.TimeText);
            Assert.AreEqual("91 %", screen.AccuracyText);
            Assert.AreEqual("2.1°", screen.AngleText);
            Assert.AreEqual("5 cm", screen.CenteringText);
            StringAssert.Contains("Estacionamiento en paralelo", screen.SubtitleText);
            StringAssert.Contains("Difícil", screen.SubtitleText);
            StringAssert.Contains("90", screen.NoteText);
            StringAssert.Contains("1 choque", screen.NoteText);
        }

        [Test]
        public void ResultsScreen_ShowsTheFailedResult_AndNoDifficultyWhenThereIsNone()
        {
            var screen = NewScreen();
            screen.Fill(new ExperienceResult { ScenarioTitle = "X", DifficultyName = "", Collisions = 0, Seconds = 5f, Accuracy = 0.4f, Grade = ExperienceGrade.Failed });
            Assert.AreEqual("REPROBADO", screen.VerdictText);
            Assert.AreEqual("40 %", screen.AccuracyText);
            Assert.AreEqual("X", screen.SubtitleText);
        }

        [Test]
        public void ResultsScreen_ShowsTheMiddleState()
        {
            var screen = NewScreen();
            screen.Fill(new ExperienceResult { ScenarioTitle = "X", Collisions = 2, Seconds = 30f, Accuracy = 0.95f, Grade = ExperienceGrade.Regular });
            Assert.AreEqual("REGULAR", screen.VerdictText);
            StringAssert.Contains("Regular", screen.NoteText);
            StringAssert.Contains("75", screen.NoteText);
        }

        [Test]
        public void ResultsScreen_WithoutAnEnteredCar_ShowsDashesForAlignmentAndCentering()
        {
            var screen = NewScreen();
            screen.Fill(new ExperienceResult { ScenarioTitle = "X", Accuracy = 0f, HasPose = false, AngleDeg = 40f, CenterOffsetM = 2f });
            Assert.AreEqual("—", screen.AngleText);
            Assert.AreEqual("—", screen.CenteringText);
        }

        [Test]
        public void ResultsScreen_BuildsOnce_AndStartsHidden()
        {
            var screen = NewScreen();
            int children = screen.transform.childCount;
            screen.Build();
            Assert.AreEqual(children, screen.transform.childCount);
            Assert.IsFalse(screen.IsShown);
        }

        // ---------------- InstructionsScreen ----------------
        [Test]
        public void InstructionsScreen_BuildsOnce_AndStartsHidden()
        {
            var screen = Make("Guide").AddComponent<InstructionsScreen>();
            screen.Build();
            int children = screen.transform.childCount;
            screen.Build();
            Assert.AreEqual(children, screen.transform.childCount);
            Assert.IsFalse(screen.IsShown);
        }

        [Test]
        public void InstructionsScreen_IsShownUntilTheCarMoves()
        {
            var screen = Make("Guide").AddComponent<InstructionsScreen>();
            var stats = new ExperienceStats();
            screen.Refresh(!stats.Started);
            Assert.IsTrue(screen.IsShown, "Antes de arrancar se ve");
            stats.Tick(0.1f, ExperienceStats.StartSpeed + 0.1f);
            screen.Refresh(!stats.Started);
            Assert.IsFalse(screen.IsShown, "Al moverse el auto desaparece");
            screen.Refresh(!stats.Started);
            Assert.IsFalse(screen.IsShown, "Y no vuelve a aparecer");
            stats.Reset();
            screen.Refresh(!stats.Started);
            Assert.IsTrue(screen.IsShown, "Al reiniciar el ejercicio vuelve a mostrarse");
        }

        // ---------------- ExperienceController ----------------
        [Test]
        public void Finish_FreezesTheGame_AndShowsTheResult()
        {
            var c = Make("GM").AddComponent<ExperienceController>();
            c.Stats.Tick(5f, 2f); c.Stats.RegisterHit(1f);
            c.Finish();
            Assert.AreEqual(ExperienceController.Phase.Finished, c.CurrentPhase);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(c.ResultsView.IsShown);
            Assert.AreEqual(1, c.LastResult.Collisions);
            Assert.AreEqual(5f, c.LastResult.Seconds, 1e-4f);
            Assert.IsFalse(c.LastResult.Passed, "sin juicio de estacionamiento, la precisión es 0");
            Assert.AreEqual("REPROBADO", c.ResultsView.VerdictText);
        }

        [Test]
        public void Finish_Twice_DoesNothingTheSecondTime()
        {
            var c = Make("GM").AddComponent<ExperienceController>();
            c.Finish();
            var first = c.ResultsView;
            c.Stats.RegisterHit(100f);
            c.Finish();
            Assert.AreSame(first, c.ResultsView);
            Assert.AreEqual(0, c.LastResult.Collisions);
        }

        [Test]
        public void ResetStats_StartsTheCountOver()
        {
            var c = Make("GM").AddComponent<ExperienceController>();
            c.Stats.Tick(5f, 2f); c.Stats.RegisterHit(1f);
            c.ResetStats();
            Assert.AreEqual(0, c.Stats.Collisions);
            Assert.AreEqual(0f, c.Stats.Elapsed);
        }
    }
}
