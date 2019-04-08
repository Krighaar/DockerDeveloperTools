using DevExpress.XtraBars.Ribbon;

namespace Docker.Developer.Tools.Controls
{
  /// <summary>
  /// Interface for supporting merging ribbons in Controls.
  /// </summary>
  public interface IControlSupportsRibbonMerge
  {
    /// <summary>
    /// Merges a <see cref="RibbonControl"/> into the <paramref name="parent"/> ribbon.
    /// </summary>
    /// <param name="parent">The parent <see cref="RibbonControl"/> to merge into.</param>
    void MergeRibbon(RibbonControl parent);

    /// <summary>
    /// Merges a <see cref="RibbonStatusBar"/> into the <paramref name="parent"/> ribbon.
    /// </summary>
    /// <param name="parent">The parent <see cref="RibbonStatusBar"/> to merge into.</param>
    void MergeStatusBar(RibbonStatusBar parent);
  }
}
