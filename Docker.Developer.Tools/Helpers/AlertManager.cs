using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Utils.Svg;
using DevExpress.XtraBars.Alerter;

namespace Docker.Developer.Tools.Helpers
{
  /// <summary>
  /// The AlertManager simplifies showing alert windows.
  /// </summary>
  public static class AlertManager
  {
    private static Form _form;
    private static AlertControl _alertControl;
    private static double _alertWindowOpacityLevel;
    private static readonly object _alertSync;

    static AlertManager()
    {
      EnforceImageSize = true;
      ImageWidth = 48;
      ImageHeight = 48;
      _alertWindowOpacityLevel = 1;
      _alertSync = new object();
      _alertControl = new AlertControl
      {
        ShowPinButton = false
      };

      _alertControl.AlertClick += new AlertClickEventHandler(mAlertControl_AlertClick);
      _alertControl.BeforeFormShow += new AlertFormEventHandler(mAlertControl_BeforeFormShow);
      _alertControl.FormClosing += new AlertFormClosingEventHandler(mAlertControl_FormClosing);
      //Set the default location of the alert windows based on the location of the taskbar.
      _alertControl.FormLocation = GetAlertFormLocation();

      AlertFormLocation GetAlertFormLocation()
      {
        switch (TaskbarHelper.GetTaskbarLocation())
        {
          case TaskbarLocation.Top:
            return AlertFormLocation.TopRight;
          case TaskbarLocation.Left:
            return AlertFormLocation.BottomLeft;
          case TaskbarLocation.Right:
          case TaskbarLocation.Bottom:
          default:
            return AlertFormLocation.BottomRight;
        }
      }
    }

    /// <summary>
    /// Gets or sets whether or not the size of images should be forced to be the size specified in 
    /// <see cref="ImageWidth" /> and <see cref="ImageHeight" />.
    /// Default is true.
    /// </summary>
    public static bool EnforceImageSize { get; set; }

    /// <summary>
    /// Gets or sets the exact width that images are required to have when <see cref="EnforceImageSize"/> is set to true.
    /// Default is 48
    /// </summary>
    public static int ImageWidth { get; set; }

    /// <summary>
    /// Gets or sets the exact height that images are required to have when <see cref="EnforceImageSize"/> is set to true.
    /// Default is 48
    /// </summary>
    public static int ImageHeight { get; set; }

    /// <summary>
    /// Gets or sets the opacity level of the alert windows.
    /// Can be any value between 0 and 1, both included.
    /// Default is 1.
    /// </summary>
    public static double AlertWindowOpacityLevel
    {
      get
      {
        return _alertWindowOpacityLevel;
      }
      set
      {
        if (value > 1)
          _alertWindowOpacityLevel = 1;
        else if (value < 0)
          _alertWindowOpacityLevel = 0;
        else
          _alertWindowOpacityLevel = value;
      }
    }

    /// <summary>
    /// Gets or sets the location where alert windows are displayed.
    /// </summary>
    public static AlertFormLocation AlertFormLocation
    {
      get
      {
        return _alertControl.FormLocation;
      }
      set
      {
        _alertControl.FormLocation = value;
      }
    }

    /// <summary>
    /// Initializes the AlertManager.
    /// </summary>
    /// <param name="form">A form used for syncronizing the alert windows.</param>
    /// <remarks>Should only be called once using the main form of the application.</remarks>
    public static void Initialze(Form form)
    {
      if (_form != null) throw new InvalidOperationException($"Cannot call \"{nameof(Initialze)}\" more than once.");
      _form = form ?? throw new ArgumentNullException("form");
    }

    private static void mAlertControl_AlertClick(object sender, AlertClickEventArgs e)
    {
      var alertInfo = (CustomAlertInfo)e.Info;
      alertInfo.ClickAction?.Invoke();
    }

    private static void mAlertControl_FormClosing(object sender, AlertFormClosingEventArgs e)
    {
      //Remove the MouseMove event handler when the alert form is closed.
      e.AlertForm.MouseMove -= new MouseEventHandler(AlertForm_MouseMove);
    }

    private static void mAlertControl_BeforeFormShow(object sender, AlertFormEventArgs e)
    {
      var alertInfo = (CustomAlertInfo)e.AlertForm.AlertInfo;
      //Set opacity level when showing the alert form.
      e.AlertForm.OpacityLevel = AlertWindowOpacityLevel;
      //Attach an event handler to the alert form's MouseMove event, when no action is present.
      if (alertInfo.ClickAction == null)
        e.AlertForm.MouseMove += new MouseEventHandler(AlertForm_MouseMove);
    }

    private static void AlertForm_MouseMove(object sender, MouseEventArgs e)
    {
      //Force the mouse cursor to always be the default cursor when moving the mouse inside the alert form.
      ((Form)sender).Cursor = Cursors.Default;
    }

    /// <summary>
    /// Shows an alert window.
    /// </summary>
    /// <param name="caption">The caption of the alert window.</param>
    /// <param name="message">The message to display in the alert window.</param>
    /// <remarks>Will invoke on the main thread if needed.</remarks>
    public static void ShowAlert(string caption, string message)
    {
      ShowAlert(caption, message, null, null);
    }

    /// <summary>
    /// Shows an alert window.
    /// </summary>
    /// <param name="caption">The caption of the alert window.</param>
    /// <param name="message">The message to display in the alert window.</param>
    /// <param name="image">The image to display in the alert window.
    /// Must meet the size specified in <see cref="ImageWidth"/> and <see cref="ImageHeight"/> if 
    /// <see cref="EnforceImageSize"/> is true.
    /// Can be null.</param>
    /// <remarks>Will invoke on the main thread if needed.</remarks>
    public static void ShowAlert(string caption, string message, Bitmap image)
    {
      ShowAlert(caption, message, image, null);
    }

    /// <summary>
    /// Shows an alert window.
    /// </summary>
    /// <param name="caption">The caption of the alert window.</param>
    /// <param name="message">The message to display in the alert window.</param>
    /// <param name="image">The image to display in the alert window.
    /// Must meet the size specified in <see cref="ImageWidth"/> and <see cref="ImageHeight"/> if 
    /// <see cref="EnforceImageSize"/> is true.
    /// Can be null.</param>
    /// <remarks>Will invoke on the main thread if needed.</remarks>
    public static void ShowAlert(string caption, string message, Image image)
    {
      var bitmap = new Bitmap(image);
      ShowAlert(caption, message, bitmap, null);
    }

    /// <summary>
    /// Shows an alert window.
    /// </summary>
    /// <param name="caption">The caption of the alert window.</param>
    /// <param name="message">The message to display in the alert window.</param>
    /// <param name="svgImage">The image to display in the alert window.
    /// Must meet the size specified in <see cref="ImageWidth"/> and <see cref="ImageHeight"/> if 
    /// <see cref="EnforceImageSize"/> is true.
    /// Can be null.</param>
    /// <remarks>Will invoke on the main thread if needed.</remarks>
    public static void ShowAlert(string caption, string message, SvgImage svgImage)
    {
      var svgBitmap = new SvgBitmap(svgImage);
      var image = svgBitmap.Render(new Size(ImageWidth, ImageHeight), null);
      var bitmap = new Bitmap(image);
      ShowAlert(caption, message, bitmap, null);
    }

    /// <summary>
    /// Shows an alert window.
    /// </summary>
    /// <param name="caption">The caption of the alert window.</param>
    /// <param name="message">The message to display in the alert window.</param>
    /// <param name="svgBitmap">The image to display in the alert window.
    /// Must meet the size specified in <see cref="ImageWidth"/> and <see cref="ImageHeight"/> if 
    /// <see cref="EnforceImageSize"/> is true.
    /// Can be null.</param>
    /// <remarks>Will invoke on the main thread if needed.</remarks>
    public static void ShowAlert(string caption, string message, SvgBitmap svgBitmap)
    {
      var image = svgBitmap.Render(new Size(ImageWidth, ImageHeight), null);
      var bitmap = new Bitmap(image);
      ShowAlert(caption, message, bitmap, null);
    }

    /// <summary>
    /// Shows an alert window.
    /// </summary>
    /// <param name="caption">The caption of the alert window.</param>
    /// <param name="message">The message to display in the alert window.</param>
    /// <param name="actionClick">An action to invoke when the alert window is clicked.
    /// The input object is the value supplied in <paramref name="message"/>.</param>
    /// <remarks>Will invoke on the main thread if needed.</remarks>
    public static void ShowAlert(string caption, string message, Action actionClick)
    {
      ShowAlert(caption, message, null, actionClick);
    }

    /// <summary>
    /// Shows an alert window.
    /// </summary>
    /// <param name="caption">The caption of the alert window.</param>
    /// <param name="message">The message to display in the alert window.</param>
    /// <param name="image">The image to display in the alert window.
    /// Must meet the size specified in <see cref="ImageWidth"/> and <see cref="ImageHeight"/> if 
    /// <see cref="EnforceImageSize"/> is true.
    /// Can be null.</param>
    /// <param name="actionClick">An action to invoke when the alert window is clicked.
    /// The input object is the value supplied in <paramref name="message"/>.</param>
    /// <remarks>Will invoke on the main thread if needed.</remarks>
    public static void ShowAlert(string caption, string message, Bitmap image, Action actionClick)
    {
      if (_form == null) throw new Exception("AlertManager have not been initialized yet.");

      if (string.IsNullOrWhiteSpace(message))
        throw new ArgumentException("The message cannot be empty or consist only of white-space characters.", nameof(message));

      //Should image size be enforced?
      if (image != null && EnforceImageSize && (image.Size.Width != ImageWidth || image.Size.Height != ImageHeight))
        throw new ArgumentException(string.Format("Alert images must be of size {0}x{1}", ImageWidth, ImageHeight), nameof(image));

      var info = new CustomAlertInfo(caption, message, image)
      {
        ClickAction = actionClick
      };

      //If the call to ShowAlert originated on a thread other than the main thread,
      // we invoke the call to ShowAlertInternal on the main thread.
      if (_form.InvokeRequired)
        _form.Invoke(new Action(() => ShowAlertInternal(info)));
      else
        ShowAlertInternal(info);
    }

    private static void ShowAlertInternal(CustomAlertInfo alertInfo)
    {
      //Lock the sync object to prevent multiple threads calling mAlertControl.Show() at the same time.
      //This should help prevent the hot tracking problem explained below.
      lock (_alertSync)
      {
        //If no click action have been set then we disable hot tracking to prevent text from appearing like a link.
        //note: This might cause problems if two items are shown right after each other i.e.
        // Alert1 is shown with AllowHotTrack set to false; but before Alert1 is shown to the user,
        // Alert2 changes AllowHotTrack to true when it is shown.
        //This have not been tested.
        if (alertInfo.ClickAction == null)
          _alertControl.AllowHotTrack = false;
        else
          _alertControl.AllowHotTrack = true;

        _alertControl.Show(_form, alertInfo);
      }
    }
  }
}
