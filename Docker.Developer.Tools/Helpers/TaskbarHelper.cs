using System.Windows.Forms;

namespace Docker.Developer.Tools.Helpers
{
  /// <summary>
  /// Helper class for Windows Taskbar interaction.
  /// </summary>
  public class TaskbarHelper
  {
    /// <summary>
    /// Gets the location of the Windows Taskbar.
    /// </summary>
    /// <returns>Returns a <see cref="TaskbarLocation"/> value.</returns>
    public static TaskbarLocation GetTaskbarLocation()
    {
      //Hvis bounds er samme bredde som working area så ligger Taskbaren enten i toppen eller i bunden.
      if (Screen.PrimaryScreen.Bounds.Width == Screen.PrimaryScreen.WorkingArea.Width)
      {
        //Hvis toppen af working area starter på location 0 ligger taskbaren i bunden.
        if (Screen.PrimaryScreen.WorkingArea.Top == 0)
          return TaskbarLocation.Bottom;
        else
          return TaskbarLocation.Top;
      }
      else
      {
        //Hvis venstre side af working area starter på location 0 ligger taskbaren i højre side.
        if (Screen.PrimaryScreen.WorkingArea.Left == 0)
          return TaskbarLocation.Right;
        else
          return TaskbarLocation.Left;
      }
    }
  }
}
