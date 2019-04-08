using System.Drawing;
using System.Globalization;

namespace Docker.Developer.Tools
{
  public static class HelperFunctions
  {
    public static string GetSizeString(long size)
    {
      if (size <= 0)
        return string.Empty;

      var format = "###.0";
      decimal kbSize = size / 1000;
      decimal mbSize = kbSize / 1000;
      decimal gbSize = mbSize / 1000;
      if (gbSize >= 1)
        return $"{gbSize.ToString(format, CultureInfo.CurrentCulture)} GB";
      else if (mbSize >= 1)
        return $"{mbSize.ToString(format, CultureInfo.CurrentCulture)} MB";
      else if (kbSize >= 1)
        return $"{kbSize.ToString(format, CultureInfo.CurrentCulture)} KB";
      else
        return $"{size.ToString(format, CultureInfo.CurrentCulture)} B";
    }

    public static Color GetDisabledColor()
    {
      var currentSkin = DevExpress.Skins.CommonSkins.GetSkin(DevExpress.LookAndFeel.UserLookAndFeel.Default.ActiveLookAndFeel);
      var SkinElementName = DevExpress.Skins.CommonColors.DisabledText;
      Color DisabledColor = currentSkin.Colors[SkinElementName];
      return DisabledColor;
    }
  }
}
