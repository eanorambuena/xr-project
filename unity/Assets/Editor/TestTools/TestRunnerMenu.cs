using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace VRCar.EditorTools
{
    /// <summary>
    /// Corre los tests EditMode y escribe un resumen legible en TestResults/editmode.txt
    /// Menú: VRCar → Run EditMode Tests  (atajo: Ctrl+Shift+T)
    /// </summary>
    public static class TestRunnerMenu
    {
        const string OutputPath = "TestResults/editmode.txt";

        [MenuItem("VRCar/Run EditMode Tests %#t")]
        public static void RunEditModeTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultWriter());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
            Debug.Log("[VRCar Tests] Ejecutando tests EditMode...");
        }

        class ResultWriter : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Passed: {result.PassCount}  Failed: {result.FailCount}  Skipped: {result.SkipCount}  Inconclusive: {result.InconclusiveCount}");
                sb.AppendLine();
                Collect(result, sb);
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, sb.ToString());
                Debug.Log($"[VRCar Tests] Passed {result.PassCount}, Failed {result.FailCount}. Detalle en {OutputPath}");
            }

            static void Collect(ITestResultAdaptor r, StringBuilder sb)
            {
                if (r.HasChildren)
                {
                    foreach (var c in r.Children) Collect(c, sb);
                    return;
                }
                sb.AppendLine($"[{r.TestStatus}] {r.Test.FullName}");
                if (r.TestStatus == TestStatus.Failed)
                {
                    sb.AppendLine("    " + (r.Message ?? "").Trim().Replace("\n", "\n    "));
                    var st = (r.StackTrace ?? "").Trim();
                    if (st.Length > 0) sb.AppendLine("    at " + st.Split('\n')[0].Trim());
                }
            }
        }
    }
}
