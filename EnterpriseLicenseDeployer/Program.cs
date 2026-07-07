using System;
using System.Windows.Forms;
using EnterpriseLicenseDeployer.Services;

namespace EnterpriseLicenseDeployer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Global exception handlers so unexpected errors are always audited
            Application.ThreadException += (s, e) =>
            {
                try { AuditLogger.Instance.Log("FATAL", $"UI thread exception: {e.Exception}"); } catch { }
                MessageBox.Show(e.Exception.Message, "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { AuditLogger.Instance.Log("FATAL", $"Unhandled exception: {e.ExceptionObject}"); } catch { }
            };

            Application.Run(new MainForm());
        }
    }
}
