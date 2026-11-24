using System.Globalization;
using UnityEngine;

namespace VRCar.Core
{
    /// <summary>Resultado de un intento: aprobado, regular (intermedio) o reprobado</summary>
    public enum ExperienceGrade { Failed, Regular, Passed }

    /// <summary>Lo que se muestra en la pantalla de resultado</summary>
    public struct ExperienceResult
    {
        public string ScenarioTitle;
        public string DifficultyName;   // vacío si el escenario no tiene dificultad
        public int Collisions;
        public float Seconds;
        public float Accuracy;          // 0..1
        public bool HasPose;            // false si el auto no llegó a entrar en el espacio (no hay ángulo ni centrado que mostrar)
        public float AngleDeg;          // desalineación respecto al espacio
        public float CenterOffsetM;     // descentrado en el espacio (m)
        public ExperienceGrade Grade;
        public bool Passed => Grade == ExperienceGrade.Passed;
    }

    /// <summary>
    /// Estadísticas de un intento: choques/topes, tiempo y aprobación (lógica pura, sin Unity en juego, testeable).
    /// </summary>
    public class ExperienceStats
    {
        /// <summary>Precisión mínima para aprobar (parte del auto que debe quedar dentro del rectángulo)</summary>
        public const float PassAccuracy = 0.90f;
        /// <summary>Máximo de choques/topes con el que todavía se aprueba</summary>
        public const int MaxCollisions = 1;
        /// <summary>Estado intermedio ("regular"): precisión mínima y máximo de choques. Por debajo de esto se reprueba.</summary>
        public const float RegularAccuracy = 0.75f;
        public const int MaxCollisionsRegular = 3;
        /// <summary>Alineación: hasta PassAngle° aprueba, hasta RegularAngle° es regular, más reprueba (PassAngle es el mismo 5° de "bien estacionado" de los juicios)</summary>
        public const float PassAngle = 5f, RegularAngle = 10f;
        /// <summary>Dos golpes separados por menos de esto cuentan como uno solo (un roce largo no suma muchos choques)</summary>
        public const float MinHitInterval = 0.75f;
        /// <summary>Velocidad (m/s) a partir de la cual se considera que el auto empezó a moverse y corre el tiempo</summary>
        public const float StartSpeed = 0.3f;

        public int Collisions { get; private set; }
        public float Elapsed { get; private set; }
        public bool Started { get; private set; }

        private float lastHitTime = float.NegativeInfinity;

        public void Reset()
        {
            Collisions = 0; Elapsed = 0f; Started = false; lastHitTime = float.NegativeInfinity;
        }

        /// <summary>El tiempo empieza a correr cuando el auto se mueve por primera vez</summary>
        public void Tick(float deltaTime, float speed)
        {
            if (!Started && speed >= StartSpeed) Started = true;
            if (Started) Elapsed += deltaTime;
        }

        /// <summary>Registra un golpe. Devuelve true si cuenta como un choque nuevo.</summary>
        public bool RegisterHit(float time)
        {
            bool isNew = time - lastHitTime >= MinHitInterval;
            lastHitTime = time;   // mientras siga rozando, el mismo choque
            if (isNew) Collisions++;
            return isNew;
        }

        /// <summary>Estado de la precisión (se compara el porcentaje redondeado, el que ve el usuario)</summary>
        public static ExperienceGrade AccuracyGrade(float accuracy)
        {
            int p = Percent(accuracy);
            return p >= Percent(PassAccuracy) ? ExperienceGrade.Passed : p >= Percent(RegularAccuracy) ? ExperienceGrade.Regular : ExperienceGrade.Failed;
        }

        /// <summary>Estado de los choques: hasta MaxCollisions aprueba, hasta MaxCollisionsRegular es regular</summary>
        public static ExperienceGrade CollisionGrade(int collisions) =>
            collisions <= MaxCollisions ? ExperienceGrade.Passed : collisions <= MaxCollisionsRegular ? ExperienceGrade.Regular : ExperienceGrade.Failed;

        /// <summary>Estado de la alineación (se compara el ángulo con un decimal, como se muestra)</summary>
        public static ExperienceGrade AngleGrade(float angleDeg)
        {
            float a = Mathf.Round(angleDeg * 10f) / 10f;
            return a <= PassAngle ? ExperienceGrade.Passed : a <= RegularAngle ? ExperienceGrade.Regular : ExperienceGrade.Failed;
        }

        /// <summary>El estado final es el peor de los tres: aprueba solo si los tres aprueban; reprueba si cualquiera reprueba</summary>
        public static ExperienceGrade GradeFor(float accuracy, int collisions, float angleDeg = 0f) =>
            (ExperienceGrade)Mathf.Min((int)AccuracyGrade(accuracy), Mathf.Min((int)CollisionGrade(collisions), (int)AngleGrade(angleDeg)));

        public static bool Passes(float accuracy, int collisions, float angleDeg = 0f) =>
            GradeFor(accuracy, collisions, angleDeg) == ExperienceGrade.Passed;

        public static int Percent(float accuracy) => Mathf.RoundToInt(Mathf.Clamp01(accuracy) * 100f);

        /// <summary>Tiempo como m:ss</summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (total / 60).ToString(CultureInfo.InvariantCulture) + ":" + (total % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        public ExperienceResult Build(float accuracy, string scenarioTitle, string difficultyName, float angleDeg = 0f, float centerOffsetMeters = 0f)
        {
            return new ExperienceResult
            {
                ScenarioTitle = scenarioTitle ?? "",
                DifficultyName = difficultyName ?? "",
                Collisions = Collisions,
                Seconds = Elapsed,
                Accuracy = Mathf.Clamp01(accuracy),
                HasPose = accuracy > 0f,
                AngleDeg = angleDeg,
                CenterOffsetM = centerOffsetMeters,
                Grade = GradeFor(accuracy, Collisions, angleDeg)
            };
        }
    }
}
