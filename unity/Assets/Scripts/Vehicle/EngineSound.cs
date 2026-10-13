using UnityEngine;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Sonido de motor procedural (ralentí → marcha lenta): tono y volumen según velocidad y acelerador.
    /// En un estacionamiento se maneja a 0-10 km/h, así que el rango útil es el ralentí.
    /// </summary>
    [RequireComponent(typeof(VehiclePhysics))]
    public class EngineSound : MonoBehaviour
    {
        public float idlePitch = 0.8f, maxPitch = 1.6f, idleVolume = 0.18f, maxVolume = 0.35f;
        private AudioSource source;
        private VehiclePhysics physics;
        private float throttleSmoothed;

        /// <summary>Tono (pitch) para una velocidad (km/h) y acelerador (0-1).</summary>
        public static float Pitch(float speedKmh, float throttle, float idle = 0.8f, float max = 1.6f)
        {
            float s = Mathf.Clamp01(Mathf.Abs(speedKmh) / 30f);
            return Mathf.Lerp(idle, max, Mathf.Clamp01(0.7f * s + 0.3f * Mathf.Clamp01(throttle)));
        }

        public static float Volume(float throttle, float idle = 0.18f, float max = 0.35f)
            => Mathf.Lerp(idle, max, Mathf.Clamp01(throttle));

        private void Start()
        {
            physics = GetComponent<VehiclePhysics>();
            var go = new GameObject("EngineAudio");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 1.5f); // motor adelante
            source = go.AddComponent<AudioSource>();
            source.clip = CreateEngineLoop();
            source.loop = true;
            source.spatialBlend = 0.6f;
            source.volume = idleVolume;
            source.pitch = idlePitch;
            source.Play();
        }

        private void Update()
        {
            if (source == null || physics == null) return;
            float throttle = physics.IsReversing ? physics.brakeInput : physics.accelerationInput;
            throttleSmoothed = Mathf.MoveTowards(throttleSmoothed, throttle, Time.deltaTime * 3f);
            source.pitch = Pitch(physics.GetCurrentSpeedKmh(), throttleSmoothed, idlePitch, maxPitch);
            source.volume = Volume(throttleSmoothed, idleVolume, maxVolume);
        }

        /// <summary>Loop de 1 s: armónicos de un 4 cilindros a ~800 rpm (27 Hz de encendido) + ruido suave.</summary>
        private static AudioClip CreateEngineLoop()
        {
            const int rate = 22050; int n = rate;
            var data = new float[n];
            var rng = new System.Random(7);
            float f0 = 27f; // debe ser entero de ciclos en 1 s para que el loop no haga clic
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float v = 0.55f * Mathf.Sin(2 * Mathf.PI * f0 * t)
                        + 0.30f * Mathf.Sin(2 * Mathf.PI * f0 * 2 * t + 0.5f)
                        + 0.15f * Mathf.Sin(2 * Mathf.PI * f0 * 4 * t + 1.1f)
                        + 0.08f * Mathf.Sin(2 * Mathf.PI * f0 * 7 * t);
                // pulsos de encendido
                v *= 0.75f + 0.25f * Mathf.Sin(2 * Mathf.PI * f0 * 2 * t);
                v += 0.04f * (float)(rng.NextDouble() * 2 - 1);
                data[i] = v * 0.5f;
            }
            var clip = AudioClip.Create("EngineLoop", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
