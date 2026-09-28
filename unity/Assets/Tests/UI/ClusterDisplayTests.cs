using NUnit.Framework;
using VRCar.UI;

namespace VRCar.Tests.UI
{
    public class ClusterDisplayTests
    {
        [Test] public void Gear_DriveOrReverse() { Assert.AreEqual("D", ClusterDisplay.GearLabel(false)); Assert.AreEqual("R", ClusterDisplay.GearLabel(true)); }
        [Test] public void Speed_RoundedAndPositive() { Assert.AreEqual("7", ClusterDisplay.SpeedLabel(6.6f)); Assert.AreEqual("3", ClusterDisplay.SpeedLabel(-3.2f)); }
    }
}
