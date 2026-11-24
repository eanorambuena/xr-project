using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Radio del vehículo con soporte para 3 pistas de audio (MP3/WAV en Resources/Radio o asignadas en Inspector),
    /// encendido/apagado, cambio de canción y volumen discreto de 0 a 20.
    /// </summary>
    public class CarRadio : MonoBehaviour
    {
        public const int MinVolume = 0;
        public const int MaxVolume = 20;
        public const int MaxTracks = 3;
        public const string ResourcesFolder = "Radio";

        [Header("Pistas (hasta 3; si está vacío carga desde Resources/Radio)")]
        [SerializeField] private List<AudioClip> tracks = new List<AudioClip>();

        [Header("Estado inicial")]
        [SerializeField] private bool startOn = false;
        [Range(MinVolume, MaxVolume)]
        [SerializeField] private int initialVolume = 10;

        [Header("Sensibilidad de joystick")]
        [SerializeField] private float axisThreshold = 0.65f;
        [SerializeField] private float stepCooldown = 0.28f;

        private AudioSource audioSource;
        private int currentTrackIndex;
        private int volumeLevel = 10;
        private bool isOn;
        private float nextTrackTime;
        private float nextVolumeTime;

        public event System.Action<string> OnRadioFeedback;

        public bool IsOn => isOn;
        public int VolumeLevel => volumeLevel;
        public int TrackCount => Mathf.Min(tracks.Count, MaxTracks);
        public int CurrentTrackNumber => TrackCount == 0 ? 0 : (currentTrackIndex % TrackCount) + 1;
        public string CurrentTrackName => TrackCount == 0
            ? "Sin pistas"
            : (tracks[currentTrackIndex % TrackCount] != null ? tracks[currentTrackIndex % TrackCount].name : $"Pista {CurrentTrackNumber}");

        private void Awake()
        {
            EnsureAudioSource();
            LoadTracksIfNeeded();
            volumeLevel = Mathf.Clamp(initialVolume, MinVolume, MaxVolume);
            ApplyVolume();
            SetPower(startOn, notify: false);
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null) return;
            var child = transform.Find("RadioAudio");
            if (child != null)
            {
                audioSource = child.GetComponent<AudioSource>();
            }
            if (audioSource == null)
            {
                var go = new GameObject("RadioAudio");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.9f, 0.4f);
                audioSource = go.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0.35f;
        }

        public void LoadTracksIfNeeded()
        {
            tracks.RemoveAll(t => t == null);
            if (tracks.Count == 0)
            {
                var loaded = Resources.LoadAll<AudioClip>(ResourcesFolder)
                    .OrderBy(c => c.name)
                    .Take(MaxTracks)
                    .ToArray();
                tracks.AddRange(loaded);
            }
            else if (tracks.Count > MaxTracks)
            {
                tracks = tracks.Take(MaxTracks).ToList();
            }

            if (TrackCount > 0)
                currentTrackIndex = Mathf.Clamp(currentTrackIndex, 0, TrackCount - 1);
            else
                currentTrackIndex = 0;
        }

        public void TogglePower()
        {
            SetPower(!isOn, notify: true);
        }

        public void SetPower(bool on, bool notify = true)
        {
            EnsureAudioSource();
            LoadTracksIfNeeded();
            isOn = on;

            if (!isOn)
            {
                audioSource.Stop();
                if (notify) EmitStatus("Radio apagada");
                return;
            }

            if (TrackCount == 0)
            {
                if (notify) EmitStatus("Radio ON (sin MP3 en Resources/Radio)");
                return;
            }

            PlayCurrentTrack(restart: false);
            if (notify) EmitStatus($"Radio ON · {CurrentTrackNumber}/{TrackCount}: {CurrentTrackName} · Vol {volumeLevel}/{MaxVolume}");
        }

        public void NextTrack()
        {
            LoadTracksIfNeeded();
            if (TrackCount == 0) return;
            currentTrackIndex = (currentTrackIndex + 1) % TrackCount;
            if (isOn) PlayCurrentTrack(restart: true);
            EmitStatus($"Pista {CurrentTrackNumber}/{TrackCount}: {CurrentTrackName}");
        }

        public void PreviousTrack()
        {
            LoadTracksIfNeeded();
            if (TrackCount == 0) return;
            currentTrackIndex = (currentTrackIndex - 1 + TrackCount) % TrackCount;
            if (isOn) PlayCurrentTrack(restart: true);
            EmitStatus($"Pista {CurrentTrackNumber}/{TrackCount}: {CurrentTrackName}");
        }

        public void SetVolumeLevel(int level)
        {
            int clamped = Mathf.Clamp(level, MinVolume, MaxVolume);
            if (clamped == volumeLevel) return;
            volumeLevel = clamped;
            ApplyVolume();
            EmitStatus($"Volumen radio: {volumeLevel}/{MaxVolume}");
        }

        public void AdjustVolumeStep(int delta)
        {
            if (delta == 0) return;
            SetVolumeLevel(volumeLevel + delta);
        }

        /// <summary>
        /// Procesa la inclinación continua del joystick derecho (X: izq/der canción, Y: arriba/abajo volumen).
        /// </summary>
        public void HandleStickAxis(Vector2 stick, float now)
        {
            if (Mathf.Abs(stick.x) >= axisThreshold && now >= nextTrackTime)
            {
                nextTrackTime = now + stepCooldown;
                if (stick.x > 0f) NextTrack();
                else PreviousTrack();
            }

            if (Mathf.Abs(stick.y) >= axisThreshold && now >= nextVolumeTime)
            {
                nextVolumeTime = now + stepCooldown;
                if (stick.y > 0f) AdjustVolumeStep(1);
                else AdjustVolumeStep(-1);
            }
        }

        private void PlayCurrentTrack(bool restart)
        {
            if (TrackCount == 0) return;
            var clip = tracks[currentTrackIndex];
            if (clip == null) return;

            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else if (restart)
            {
                audioSource.time = 0f;
                audioSource.Play();
            }
            else if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void ApplyVolume()
        {
            if (audioSource == null) return;
            // Curva suave perceptiva para el rango 0..20
            float normalized = volumeLevel / (float)MaxVolume;
            audioSource.volume = normalized * normalized;
        }

        private void EmitStatus(string msg)
        {
            OnRadioFeedback?.Invoke(msg);
        }
    }
}