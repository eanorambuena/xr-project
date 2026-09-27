using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRCar.Obstacles;
using VRCar.Vehicle;

namespace VRCar.UI
{
    /// <summary>
    /// Sensor de estacionamiento en la pantalla central: al ir en reversa muestra la distancia del
    /// parachoques trasero al obstáculo con 4 barras (verde → rojo) y pitidos cada vez más rápidos.
    /// </summary>
    public class ParkingSensor : MonoBehaviour
    {
        public const float MaxRange = 1.5f, StopDistance = 0.30f;
        static readonly float[] Thresholds = { 1.5f, 1.0f, 0.6f, StopDistance };
        static readonly Color[] BarColors =
        {
            new Color(0.2f, 0.85f, 0.3f), new Color(0.95f, 0.85f, 0.2f),
            new Color(1f, 0.55f, 0.1f), new Color(0.95f, 0.2f, 0.15f)
        };
        static readonly Color BarOff = new Color(1f, 1f, 1f, 0.08f);

        public ObstacleDetector detector;
        public VehiclePhysics physics;
        public Image[] bars = new Image[4];
        public TextMeshProUGUI label;
        public GameObject panel;            // se muestra solo en reversa
        public AudioSource audioSource;

        private AudioClip beep;
        private float nextBeep;

        /// <summary>Nivel 0 (nada) a 4 (detenerse) según la distancia en metros.</summary>
        public static int Level(float distance)
        {
            int level = 0;
            foreach (var t in Thresholds) if (distance < t) level++;
            return level;
        }

        /// <summary>Segundos entre pitidos: infinito si está lejos, 0 = tono continuo.</summary>
        public static float BeepInterval(float distance)
        {
            if (distance >= MaxRange) return float.PositiveInfinity;
            if (distance < StopDistance) return 0f;
            float t = (distance - StopDistance) / (MaxRange - StopDistance);
            return Mathf.Lerp(0.12f, 0.8f, t);
        }

        public static string Label(float distance)
        {
            if (distance >= MaxRange) return "--";
            if (distance < StopDistance) return "STOP";
            return Mathf.RoundToInt(distance * 100f).ToString(System.Globalization.CultureInfo.InvariantCulture) + " cm";
        }

        private void Update()
        {
            if (physics == null || detector == null) return;
            bool reversing = physics.IsReversing;
            if (panel != null && panel.activeSelf != reversing) panel.SetActive(reversing);
            if (!reversing) return;

            float d = detector.GetClosestDistance();
            int level = Level(d);
            for (int i = 0; i < bars.Length; i++)
                if (bars[i] != null) bars[i].color = i < level ? BarColors[i] : BarOff;
            if (label != null) label.text = Label(d);

            float interval = BeepInterval(d);
            if (audioSource == null || float.IsPositiveInfinity(interval)) return;
            if (beep == null) beep = CreateBeep();
            if (Time.time >= nextBeep)
            {
                audioSource.PlayOneShot(beep, 0.6f);
                nextBeep = Time.time + Mathf.Max(interval, beep.length);
            }
        }

        private static AudioClip CreateBeep()
        {
            const int rate = 44100; int n = rate / 12;
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Sin(2 * Mathf.PI * 1800f * i / rate) * (1f - (float)i / n) * 0.5f;
            var clip = AudioClip.Create("ParkingBeep", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
