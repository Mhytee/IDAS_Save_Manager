using System;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace IDAS_Save_System
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Single-instance guard: a second copy would race this one for the
            // card files in TeknoParrot's folder and the updater's swap files.
            // Wait briefly instead of failing immediately — during a self-update
            // the new exe starts while the old one is still shutting down.
            using var singleInstance = new Mutex(false, "IDAS_Save_Manager_SingleInstance");
            bool owned = false;
            try
            {
                owned = singleInstance.WaitOne(TimeSpan.FromSeconds(3), false);
            }
            catch (AbandonedMutexException)
            {
                owned = true; // previous instance died without releasing — safe to proceed
            }
            if (!owned)
            {
                MessageBox.Show("IDAS Save Manager is already running.", "IDAS Save Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Register Shift-JIS and other legacy code pages
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // A missed exception should show a readable message, not the raw
            // .NET crash dialog (or a silent vanish) mid-file-operation.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
                MessageBox.Show(
                    "Something went wrong:\n" + e.Exception.Message +
                    "\n\nThe app will keep running. If this happened while saving, check " +
                    @"AppData\TeknoParrot and AppData\IDAS_Save_Manager\IDAS_Backups before launching again.",
                    "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show(
                    "A fatal error occurred:\n" + (e.ExceptionObject as Exception)?.Message,
                    "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form2());
        }
    }
}
