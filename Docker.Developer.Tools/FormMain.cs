using System;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraTab;
using Docker.Developer.Tools.Controls;
using Docker.DotNet;

namespace Docker.Developer.Tools
{
  public partial class MainForm : XtraForm
  {
    private DockerClient _dockerClient;

    public MainForm()
    {
      InitializeComponent();
      _dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine"))
        .CreateClient();

      containerListControl.Initialize(_dockerClient);
      imagesListControl.Initialize(_dockerClient);
      networkListControl.Initialize(_dockerClient);
      RibbonTabChanging(ribbonPageContainers);
#if !DEBUG
      AutoUpdaterDotNET.AutoUpdater.Start("http://docker-developer-tools.net/UpdateManifest.xml");
#endif
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      // Do not show TabHeaders in runtime.
      if (!DesignMode) tabControl.ShowTabHeader = DevExpress.Utils.DefaultBoolean.False;
    }

    private void ribbonControl_SelectedPageChanging(object sender, RibbonPageChangingEventArgs e)
    {
      e.Cancel = RibbonTabChanging(e.Page);
    }

    private bool RibbonTabChanging(RibbonPage page)
    {
      var tabPage = ConvertRibbonPageToTabPage(page);
      return TabPageChanging(tabPage);
    }

    private bool TabPageChanging(XtraTabPage tabPage)
    {
      var control = ConvertTabPageToControl(tabPage);
      tabControl.SelectedTabPage = tabPage;
      var controlName = tabPage.Name;
      if (tabControl.SelectedTabPage.Name != controlName)
        return true; // cancel change

      MergeRibbon(control);
      return false; // accept change
    }

    private void MergeRibbon(XtraUserControl control)
    {
      ribbonControl.UnMergeRibbon();
      ribbonStatusBar.UnMergeStatusBar();
      if (control is IControlSupportsRibbonMerge mergeControl)
      {
        mergeControl.MergeRibbon(ribbonControl);
        mergeControl.MergeStatusBar(ribbonStatusBar);
      }
    }

    private XtraTabPage ConvertRibbonPageToTabPage(RibbonPage ribbonPage)
    {
      if (ribbonPage == ribbonPageContainers)
        return tabPageContainers;
      else if (ribbonPage == ribbonPageImages)
        return tabPageImages;
      else if (ribbonPage == ribbonPageNetworks)
        return tabPageNetworks;

      throw new NotImplementedException($"RibbonPage \"{ribbonPage.Name}\" is not implemented!");
    }

    private XtraUserControl ConvertTabPageToControl(XtraTabPage tabPage)
    {
      if (tabPage == tabPageContainers)
        return containerListControl;
      else if (tabPage == tabPageImages)
        return imagesListControl;
      else if (tabPage == tabPageNetworks)
        return networkListControl;

      throw new NotImplementedException($"TabPage \"{tabPage.Name}\" is not implemented!");
    }
  }
}
