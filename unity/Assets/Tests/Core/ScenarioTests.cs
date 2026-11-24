using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRCar.Core;

namespace VRCar.Tests.Core
{
    /// <summary>Catálogo de escenarios, sesión de dificultad, lógica del menú y ajuste del hueco por dificultad</summary>
    public class ScenarioTests
    {
        private GameObject go;
        [TearDown] public void TearDown() { if (go != null) Object.DestroyImmediate(go); ScenarioSession.SetDifficulty(1); }

        // ---------------- Catálogo ----------------
        [Test]
        public void Catalog_HasTheThreeScenarios_WithUniqueIdsAndScenes()
        {
            var all = ScenarioCatalog.All;
            Assert.AreEqual(3, all.Length);
            CollectionAssert.AreEqual(new[] { "Parking_Perpendicular", "Parking_Parallel", "Parking_Slope" }, all.Select(s => s.SceneName).ToArray());
            Assert.AreEqual(all.Length, all.Select(s => s.Id).Distinct().Count());
            Assert.AreEqual(all.Length, all.Select(s => s.SceneName).Distinct().Count());
            foreach (var s in all)
            {
                Assert.IsNotEmpty(s.Title); Assert.IsNotEmpty(s.Description); Assert.IsNotEmpty(s.UseCase);
            }
        }

        [Test]
        public void Catalog_BothParkingScenariosHaveDifficulty()
        {
            Assert.IsTrue(ScenarioCatalog.FindBySceneName("Parking_Parallel").HasDifficulty);
            Assert.IsTrue(ScenarioCatalog.FindBySceneName("Parking_Perpendicular").HasDifficulty);
            Assert.IsTrue(ScenarioCatalog.FindBySceneName("Parking_Slope").HasDifficulty);
        }

        [Test]
        public void Catalog_MenuSceneIsNotAScenario()
        {
            Assert.IsNull(ScenarioCatalog.FindBySceneName(ScenarioCatalog.MenuScene));
            Assert.AreEqual(ScenarioSession.DifficultyCount, ScenarioCatalog.DifficultyNames.Length);
        }

        [Test]
        public void Session_ClampsTheDifficulty()
        {
            ScenarioSession.SetDifficulty(9);
            Assert.AreEqual(ScenarioSession.DifficultyCount - 1, ScenarioSession.Difficulty);
            ScenarioSession.SetDifficulty(-4);
            Assert.AreEqual(0, ScenarioSession.Difficulty);
        }

        // ---------------- Menú ----------------
        private ScenarioMenu NewMenu()
        {
            go = new GameObject("Menu");
            return go.AddComponent<ScenarioMenu>();
        }

        [Test]
        public void Menu_StartsOnTheFirstScenario_AndMovesWithoutWrapping()
        {
            var menu = NewMenu();
            Assert.AreEqual(0, menu.SelectedIndex);
            menu.Move(-1);
            Assert.AreEqual(0, menu.SelectedIndex, "No da la vuelta hacia arriba");
            menu.Move(1); menu.Move(1);
            Assert.AreEqual(2, menu.SelectedIndex);
            for (int i = 0; i < 10; i++) menu.Move(1);
            Assert.AreEqual(ScenarioCatalog.All.Length - 1, menu.SelectedIndex, "No da la vuelta hacia abajo");
        }

        [Test]
        public void Menu_SelectNumber_ChoosesDirectly_AndIgnoresInvalidNumbers()
        {
            var menu = NewMenu();
            menu.SelectNumber(2);
            Assert.AreEqual(1, menu.SelectedIndex);
            menu.SelectNumber(0); menu.SelectNumber(99);
            Assert.AreEqual(1, menu.SelectedIndex);
        }

        [Test]
        public void Menu_Difficulty_MovesOneStepAndStopsAtTheLimits_InEveryScenario()
        {
            var menu = NewMenu();
            for (int n = 1; n <= ScenarioCatalog.All.Length; n++)
            {
                menu.SelectNumber(n);
                Assert.IsTrue(menu.Selected.HasDifficulty, $"{menu.Selected.Title} tiene dificultad");
                menu.ChangeDifficulty(-9);
                Assert.AreEqual(0, menu.Difficulty, "Tope inferior");
                menu.ChangeDifficulty(1);
                Assert.AreEqual(1, menu.Difficulty);
                menu.ChangeDifficulty(5);
                Assert.AreEqual(ScenarioSession.DifficultyCount - 1, menu.Difficulty, "Tope superior");
            }
        }

        [Test]
        public void Catalog_IsAvailable_IsFalseForASceneThatIsNotInTheBuild()
        {
            var missing = new ScenarioInfo("x", "X", "CU1", "Escena_Que_No_Existe", false, "x");
            Assert.IsFalse(ScenarioCatalog.IsAvailable(missing));
            Assert.IsFalse(ScenarioCatalog.IsAvailable(null));
        }

        // ---------------- Hueco por dificultad ----------------
        [Test]
        public void Course_GapShrinksWithDifficulty_AndFitsTheCar()
        {
            const float carLength = 4.255f;
            float easy = ParallelParkingCourse.GapFor(0), medium = ParallelParkingCourse.GapFor(1), hard = ParallelParkingCourse.GapFor(2);
            Assert.Greater(easy, medium); Assert.Greater(medium, hard);
            Assert.Greater(hard, carLength + 1f, "Aun el difícil deja más de 1 m de holgura total");
            Assert.AreEqual(hard, ParallelParkingCourse.GapFor(99), "Fuera de rango usa el más difícil");
            Assert.AreEqual(easy, ParallelParkingCourse.GapFor(-1));
        }

        [Test]
        public void Course_Apply_MovesTheRearCarsTheSpotAndTheLines()
        {
            go = new GameObject("Street");
            Transform Child(string n) { var t = new GameObject(n).transform; t.SetParent(go.transform, false); return t; }
            var rear = Child("Rear"); var spot = Child("Spot");
            var outer = new GameObject("Outer").transform; outer.SetParent(spot, false);
            var front = new GameObject("Front").transform; front.SetParent(spot, false);
            var back = new GameObject("Back").transform; back.SetParent(spot, false);
            var course = go.AddComponent<ParallelParkingCourse>();
            course.Configure(rear, spot, outer, front, back, -5f, 2.3f);

            foreach (int d in new[] { 0, 1, 2 })
            {
                course.Apply(d);
                float gap = ParallelParkingCourse.GapFor(d);
                Assert.AreEqual(gap, course.CurrentGap, 1e-4f);
                Assert.AreEqual(-gap, rear.localPosition.x, 1e-4f, "El grupo de atrás parte donde termina el hueco");
                Assert.AreEqual(-gap / 2f, spot.localPosition.x, 1e-4f, "El espacio queda centrado en el hueco");
                Assert.AreEqual(-5f, spot.localPosition.z, 1e-4f, "Sobre la cara de la solera");
                Assert.AreEqual(gap / 2f - 0.05f, front.localPosition.z, 1e-4f);
                Assert.AreEqual(-(gap / 2f - 0.05f), back.localPosition.z, 1e-4f);
                Assert.AreEqual(gap - 0.1f, outer.localScale.z, 1e-4f);
            }
        }

        [Test]
        public void Course_Apply_PassesTheLevelToTheJudgment()
        {
            go = new GameObject("Street");
            var course = go.AddComponent<ParallelParkingCourse>();
            var judgment = go.AddComponent<ParallelParkingJudgment>();
            course.SetJudgment(judgment);
            course.Apply(2);
            Assert.AreEqual(ParallelParkingCourse.GapFor(2), judgment.GapLength, 1e-4f);
        }
    }
}
