using System;
using System.Globalization;
using System.Windows.Forms;
using Docker.Developer.Tools.Helpers;

namespace Docker.Developer.Tools
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      var culture = new CultureInfo("en-US");
      //Culture for any thread
      CultureInfo.DefaultThreadCurrentCulture = culture;
      //Culture for UI in any thread
      CultureInfo.DefaultThreadCurrentUICulture = culture;

      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      var form = new MainForm();
      AlertManager.Initialze(form);
      Application.Run(form);
    }
  }
}
