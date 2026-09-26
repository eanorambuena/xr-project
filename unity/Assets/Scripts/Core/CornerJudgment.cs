using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using VRCar.Obstacles;
using VRCar.UI;

namespace VRCar.Core
{
    /// <summary>
    /// CU1 - Juicio de Esquinas: evalúa cada puerta de conos que cruza el vehículo.
    /// - Holgura = espacio libre entre el costado del auto y el cono más cercano (m)
    /// - Una puerta es "limpia" si no se tocó ningún obstáculo mientras se cruzaba
    /// - Al terminar muestra puertas limpias, holgura mínima y tiempo
    /// Supone un circuito recto a lo largo de +Z (como la escena CU1).
    /// </summary>
    public class CornerJudgment : MonoBehaviour
    {
        [Serializable]
        public class GateDefinition
        {
            public Transform leftCone;
            public Transform rightCone;
        }

        [SerializeField] private List<GateDefinition> gates = new List<GateDefinition>();
        [SerializeField] private float coneRadius = 0.175f;
        [SerializeField] private Collider vehicleCollider;
        [SerializeField] private CollisionFeedback collisionFeedback;
        [SerializeField] private HUDManager hud;

        private ScoreSheet score;
        private int nextGate;
        private bool hitSinceLastGate;
        private float startTime = -1f;

        public ScoreSheet Score => score;

        public void Configure(List<GateDefinition> gateList, Collider vehicle, CollisionFeedback feedback, HUDManager hudManager)
        {
            gates = gateList;
            vehicleCollider = vehicle;
            collisionFeedback = feedback;
            hud = hudManager;
        }

        private void Start()
        {
            score = new ScoreSheet(gates.Count);
            if (collisionFeedback != null) collisionFeedback.ObstacleHit += OnObstacleHit;
            hud?.ShowMessage($"Pasa por las {gates.Count} puertas sin tocar los conos");
        }

        private void OnDestroy()
        {
            if (collisionFeedback != null) collisionFeedback.ObstacleHit -= OnObstacleHit;
        }

        private void OnObstacleHit()
        {
            hitSinceLastGate = true;
        }

        private void Update()
        {
            if (score == null || vehicleCollider == null || score.IsComplete || nextGate >= gates.Count) return;

            Bounds b = vehicleCollider.bounds;
            // El cronómetro parte cuando el auto empieza a moverse
            var rb = vehicleCollider.attachedRigidbody;
            if (startTime < 0f && rb != null && rb.linearVelocity.sqrMagnitude > 0.01f) startTime = Time.time;

            var gate = gates[nextGate];
            if (gate.leftCone == null || gate.rightCone == null) { nextGate++; return; }

            // La puerta se evalúa cuando el centro del auto la cruza
            float gateZ = (gate.leftCone.position.z + gate.rightCone.position.z) / 2f;
            if (b.center.z < gateZ) return;

            float clearance = ComputeClearance(b.min.x, b.max.x, gate.leftCone.position.x, gate.rightCone.position.x, coneRadius);
            bool hit = hitSinceLastGate || clearance < 0f;
            score.RegisterGate(clearance, hit);
            hitSinceLastGate = false;

            if (score.IsComplete)
            {
                float elapsed = startTime < 0f ? 0f : Time.time - startTime;
                hud?.ShowMessage(score.Summary(elapsed));
            }
            else
            {
                hud?.ShowMessage(GateMessage(nextGate, gates.Count, clearance, hit));
            }
            nextGate++;
        }

        // ------------------------------------------------------------------
        // Lógica pura (testeable)
        // ------------------------------------------------------------------

        /// <summary>
        /// Holgura mínima entre el auto (extensión en X) y los conos de la puerta.
        /// Negativa si el auto se superpone con un cono.
        /// </summary>
        public static float ComputeClearance(float vehicleMinX, float vehicleMaxX, float leftConeX, float rightConeX, float coneRadius)
        {
            float left = vehicleMinX - (leftConeX + coneRadius);
            float right = (rightConeX - coneRadius) - vehicleMaxX;
            return Mathf.Min(left, right);
        }

        public static string GateMessage(int index, int total, float clearance, bool hit)
        {
            string gate = $"Puerta {index + 1}/{total}";
            if (hit) return $"{gate}: ¡tocaste un cono!";
            int cm = Mathf.RoundToInt(clearance * 100f);
            return $"{gate} OK - holgura {cm} cm";
        }

        /// <summary>
        /// Registro de puntuación de una pasada por el circuito
        /// </summary>
        public class ScoreSheet
        {
            public int TotalGates { get; }
            public int GatesPassed { get; private set; }
            public int CleanGates { get; private set; }
            public float MinCleanClearance { get; private set; } = float.MaxValue;
            public bool IsComplete => GatesPassed >= TotalGates;

            public ScoreSheet(int totalGates)
            {
                TotalGates = totalGates;
            }

            public void RegisterGate(float clearance, bool hit)
            {
                GatesPassed++;
                if (!hit)
                {
                    CleanGates++;
                    MinCleanClearance = Mathf.Min(MinCleanClearance, clearance);
                }
            }

            public string Summary(float elapsedSeconds)
            {
                string minClear = CleanGates > 0
                    ? MinCleanClearance.ToString("F2", CultureInfo.InvariantCulture) + " m"
                    : "--";
                return $"¡Completado! Puertas limpias {CleanGates}/{TotalGates}\n" +
                       $"Holgura mínima {minClear} - Tiempo {Mathf.RoundToInt(elapsedSeconds)} s";
            }
        }
    }
}
