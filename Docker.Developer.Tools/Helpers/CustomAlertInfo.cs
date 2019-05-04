using System;
using DevExpress.XtraBars.Alerter;

namespace Docker.Developer.Tools.Helpers
{
  internal class CustomAlertInfo : AlertInfo
  {
    public CustomAlertInfo(string caption, string text)
      : base(caption, text)
    {
    }

    public CustomAlertInfo(string caption, string text, string hotTrackedText)
      : base(caption, text, hotTrackedText)
    {
    }

    public CustomAlertInfo(string caption, string text, System.Drawing.Image image)
      : base(caption, text, image)
    {
    }

    public Action ClickAction
    {
      get;
      set;
    }
  }
}
