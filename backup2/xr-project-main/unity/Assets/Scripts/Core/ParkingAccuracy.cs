using System.Collections.Generic;
using UnityEngine;

namespace VRCar.Core
{
    /// <summary>Un escenario que sabe qué tan bien está estacionado el auto ahora mismo</summary>
    public interface IParkingScore
    {
        /// <summary>Fracción de la superficie del auto que está dentro del rectángulo de estacionamiento (0 = afuera, 1 = todo adentro)</summary>
        float Accuracy { get; }
        /// <summary>Desalineación del auto respecto al espacio, en grados (0 = paralelo a las líneas)</summary>
        float AngleDegrees { get; }
        /// <summary>Cuánto está descentrado el auto en su espacio, en metros (0 = centrado). Batería: de lado a lado; paralelo: entre el auto de adelante y el de atrás.</summary>
        float CenterOffset { get; }
    }

    /// <summary>Cálculo de la "precisión" del estacionamiento: qué parte del auto quedó dentro del rectángulo</summary>
    public static class ParkingAccuracy
    {
        /// <summary>
        /// Fracción del área del auto (visto desde arriba) que cae dentro del rectángulo
        /// x ∈ [minX, maxX], z ∈ [minZ, maxZ], expresado en coordenadas locales de "spot".
        /// corners = esquinas del auto en el orden de ParkingJudgment.VehicleCorners
        /// (delantera izq., delantera der., trasera izq., trasera der.).
        /// </summary>
        public static float InsideFraction(Vector3[] corners, Transform spot, float minX, float maxX, float minZ, float maxZ)
        {
            if (corners == null || corners.Length != 4 || spot == null) return 0f;
            // Contorno en orden: FL, FR, RR, RL
            int[] order = { 0, 1, 3, 2 };
            var poly = new List<Vector2>(4);
            foreach (int i in order)
            {
                Vector3 l = spot.InverseTransformPoint(corners[i]);
                poly.Add(new Vector2(l.x, l.z));
            }
            float carArea = Area(poly);
            if (carArea < 1e-6f) return 0f;

            poly = Clip(poly, 0, minX, true);
            poly = Clip(poly, 0, maxX, false);
            poly = Clip(poly, 1, minZ, true);
            poly = Clip(poly, 1, maxZ, false);
            return Mathf.Clamp01(Area(poly) / carArea);
        }

        /// <summary>Área de un polígono (valor absoluto de la fórmula del cordón)</summary>
        public static float Area(List<Vector2> p)
        {
            if (p.Count < 3) return 0f;
            float a = 0f;
            for (int i = 0; i < p.Count; i++)
            {
                var u = p[i]; var v = p[(i + 1) % p.Count];
                a += u.x * v.y - v.x * u.y;
            }
            return Mathf.Abs(a) * 0.5f;
        }

        /// <summary>Recorta un polígono contra un semiplano (Sutherland–Hodgman). axis 0 = x, 1 = y; keepGreater: se queda con los valores ≥ bound</summary>
        private static List<Vector2> Clip(List<Vector2> input, int axis, float bound, bool keepGreater)
        {
            var output = new List<Vector2>();
            if (input.Count == 0) return output;
            float Side(Vector2 v) => (axis == 0 ? v.x : v.y) - bound;
            bool Inside(Vector2 v) => keepGreater ? Side(v) >= 0f : Side(v) <= 0f;
            for (int i = 0; i < input.Count; i++)
            {
                var cur = input[i]; var prev = input[(i + input.Count - 1) % input.Count];
                bool curIn = Inside(cur), prevIn = Inside(prev);
                if (curIn != prevIn)
                {
                    float t = Side(prev) / (Side(prev) - Side(cur));
                    output.Add(prev + (cur - prev) * t);
                }
                if (curIn) output.Add(cur);
            }
            return output;
        }
    }
}
