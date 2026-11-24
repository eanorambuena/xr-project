using NUnit.Framework;
using VRCar.UI;
using VRCar.Vehicle;

namespace VRCar.Tests.UI
{
    public class ClusterDisplayTests
    {
        [Test] public void Gear_DriveOrReverse() { Assert.AreEqual("D", ClusterDisplay.GearLabel(false)); Assert.AreEqual("R", ClusterDisplay.GearLabel(true)); }
        [Test] public void Gear_ShowsR_N_D() { Assert.AreEqual("R", ClusterDisplay.GearLabel(GearMode.Reverse)); Assert.AreEqual("N", ClusterDisplay.GearLabel(GearMode.Neutral)); Assert.AreEqual("D", ClusterDisplay.GearLabel(GearMode.Drive)); }
        [Test] public void Speed_RoundedAndPositive() { Assert.AreEqual("7", ClusterDisplay.SpeedLabel(6.6f)); Assert.AreEqual("3", ClusterDisplay.SpeedLabel(-3.2f)); }
    }
}
