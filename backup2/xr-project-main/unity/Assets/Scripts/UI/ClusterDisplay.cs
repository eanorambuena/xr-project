using TMPro;
using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.UI
{
    /// <summary>Tablero detrás del volante: velocidad en km/h y marcha (R / N / D)</summary>
    public class ClusterDisplay : MonoBehaviour
    {
        public VehiclePhysics physics;
        public TextMeshPro speedText;
        public TextMeshPro gearText;

        public static string GearLabel(bool reversing) => reversing ? "R" : "D";
        public static string GearLabel(GearMode gear) => gear == GearMode.Reverse ? "R" : gear == GearMode.Neutral ? "N" : "D";

        public static Color GearColor(GearMode gear) =>
            gear == GearMode.Reverse ? new Color(1f, 0.55f, 0.1f) : gear == GearMode.Neutral ? new Color(1f, 0.9f, 0.4f) : new Color(0.4f, 0.9f, 1f);
        public static string SpeedLabel(float kmh) => Mathf.RoundToInt(Mathf.Abs(kmh)).ToString(System.Globalization.CultureInfo.InvariantCulture);

        private void Update()
        {
            if (physics == null) physics = GetComponent<VehiclePhysics>();
            if (physics == null) return;
            if (speedText != null) speedText.text = SpeedLabel(physics.GetCurrentSpeedKmh());
            if (gearText != null)
            {
                gearText.text = GearLabel(physics.CurrentGear);
                gearText.color = GearColor(physics.CurrentGear);
            }
        }
    }
}
