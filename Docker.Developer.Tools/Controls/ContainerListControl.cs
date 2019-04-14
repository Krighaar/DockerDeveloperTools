using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Docker.Developer.Tools.Controls
{
  public partial class ContainerListControl : XtraUserControl, IControlSupportsRibbonMerge
  {
    private DockerClient _dockerClient;

    public ContainerListControl()
    {
      InitializeComponent();
      gridViewContainerList.OptionsView.ShowColumnHeaders = false; // Hide here to allow easy design-time modification.
      UpdateButtons();
    }

    public void Initialize(DockerClient dockerClient)
    {
      _dockerClient = dockerClient;
    }

    public void MergeRibbon(RibbonControl parent)
    {
      if (parent == null) throw new ArgumentNullException(nameof(parent));
      parent.MergeRibbon(ribbonControl);
    }

    public void MergeStatusBar(RibbonStatusBar parent)
    {
      // No status bar to merge.
    }

    private void timer_Tick(object sender, EventArgs e)
    {
      virtualServerModeSource.Refresh();
    }

    #region < GridViewList >

    private void virtualServerModeSource_ConfigurationChanged(object sender, DevExpress.Data.VirtualServerModeRowsEventArgs e)
    {
      var listContainerParameters = new ContainersListParameters() { All = true };
      e.RowsTask = _dockerClient.Containers.ListContainersAsync(listContainerParameters, e.CancellationToken).ContinueWith(result =>
      {
        return new DevExpress.Data.VirtualServerModeRowsTaskResult(result.Result.OrderBy(l => l.ID).ToList(), false, null);
      });

      if (!timer.Enabled) timer.Start();
    }

    private void gridViewContainerList_AsyncCompleted(object sender, EventArgs e)
    {
      barButtonItemStopAllContainers.Enabled = gridViewContainerList.DataRowCount > 0;
      barButtonItemDeleteAllContainersMenu.Enabled = gridViewContainerList.DataRowCount > 0;
    }

    private void gridViewContainerList_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      if (e.IsGetData && e.Row is ContainerListResponse container)
      {
        if (e.Column == colName)
        {
          e.Value = container.Names.FirstOrDefault();
        }
        else if (e.Column == colState)
        {
          var state = (ContainerStatus)Enum.Parse(typeof(ContainerStatus), container.State, true);
          e.Value = (int)state;
        }
        else if (e.Column == colImage)
        {
          e.Value = $"\t{container.Image}";
        }
      }
    }

    private void gridViewContainerList_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
    {
      var row = gridViewContainerList.GetFocusedRow() as ContainerListResponse;
      textContainerId.Text = row != null ? row.ID : string.Empty;
      textContainerName.Text = row != null ? row.Names.FirstOrDefault() : string.Empty;
      textImageId.Text = row != null ? row.ImageID : string.Empty;
      textImageName.Text = row != null ? row.Image : string.Empty;
      textCreatedDate.Text = row != null ? row.Created.ToShortDateString() : string.Empty;
      textCommand.Text = row != null ? row.Command : string.Empty;
      textSizeRootFS.Text = row != null ? HelperFunctions.GetSizeString(row.SizeRootFs) : string.Empty;
      textSizeRW.Text = row != null ? HelperFunctions.GetSizeString(row.SizeRw) : string.Empty;
      textState.Text = row != null ? row.State : string.Empty;
      textStatus.Text = row != null ? row.Status : string.Empty;
      gridVolumes.DataSource = row?.Mounts;
      gridPorts.DataSource = row?.Ports;
      gridNetworkSettings.DataSource = row?.NetworkSettings?.Networks;
      gridLabels.DataSource = row?.Labels;
      UpdateButtons();
    }

    private void UpdateButtons()
    {
      var row = gridViewContainerList.GetFocusedRow() as ContainerListResponse;
      barButtonItemStopContainer.Enabled = row != null;
      barButtonItemDeleteContainerMenu.Enabled = row != null;
      barButtonItemAttachToContainer.Enabled = row != null && row.State == ContainerStatus.running.ToString();
      barButtonItemShowLogs.Enabled = row != null;
      barButtonItemOpenMappedFolder.Enabled = row != null && row.Mounts.Any();
      barButtonItemOpenUrlMenu.Enabled = row != null && row.Ports.Any();
    }

    private void gridViewContainerList_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.KeyCode == Keys.Down)
      {
        gridViewContainerList.MoveNext();
        e.Handled = true;
      }
      else if (e.KeyCode == Keys.Up)
      {
        gridViewContainerList.MovePrev();
        e.Handled = true;
      }
    }

    private void gridViewContainerList_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
    {
      if (e.Column == colImage)
      {
        e.Appearance.ForeColor = HelperFunctions.GetDisabledColor();
      }
    }

    #endregion

    #region < Details >

    private void gridViewVolumes_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      if (e.IsGetData && e.Row is MountPoint mountPoint)
      {
        if (e.Column == colVolumesId)
        {
          e.Value = mountPoint.GetHashCode();
        }
        else if (e.Column == colVolumesMode)
        {
          e.Value = mountPoint.RW ? "Read/Write" : "Read-only";
        }
      }
    }

    private void gridViewNetworkSettings_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      if (e.IsGetData && e.Row is KeyValuePair<string, EndpointSettings> networkSettings)
      {
        if (e.Column == colNetworkLinks)
        {
          e.Value = networkSettings.Value.Links != null ? string.Join(", ", networkSettings.Value.Links) : string.Empty;
        }
        else if (e.Column == colNetworkAliases)
        {
          e.Value = networkSettings.Value.Aliases != null ? string.Join(", ", networkSettings.Value.Aliases) : string.Empty;
        }
        else if (e.Column == colNetworkLinkLocalIPs)
        {
          e.Value = networkSettings.Value.IPAMConfig?.LinkLocalIPs != null ? string.Join(", ", networkSettings.Value.IPAMConfig.LinkLocalIPs) : string.Empty;
        }
      }
    }

    private void gridViewNetworkSettings_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
      if (e.Column == colNetworkIPPrefixLen || e.Column == colNetworkGlobalIPv6PrefixLen)
      {
        var value = (long?)e.Value;
        e.DisplayText = value.HasValue && value != 0 ? value.ToString() : string.Empty;
      }
    }

    #endregion

    #region < Ribbon >

    private async void barButtonItemStopContainer_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (gridViewContainerList.GetFocusedRow() is ContainerListResponse container)
      {
        if (XtraMessageBox.Show($"Do you want to stop the container '{container.Names.First()}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
          return;

        try
        {
          await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
        }
        catch
        {
          XtraMessageBox.Show("Could not stop the container!");
        }
      }
    }

    private async void barButtonItemStopAllContainers_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (XtraMessageBox.Show($"Do you want to stop all running containers?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        return;

      var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
      var taskList = new List<Task>();
      foreach (var container in containers.Where(l => l.State.ToLowerInvariant() == "running"))
        taskList.Add(_dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters()));

      try
      {
        if (taskList.Any()) await Task.WhenAll(taskList.ToArray());
      }
      catch
      {
        XtraMessageBox.Show("Could not stop one or more of the containers!");
      }
    }

    private async void barButtonItemDeleteContainer_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (gridViewContainerList.GetFocusedRow() is ContainerListResponse container)
      {
        if (XtraMessageBox.Show($"Do you want to delete the container '{container.Names.First()}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
          return;

        var state = (ContainerStatus)Enum.Parse(typeof(ContainerStatus), container.State, true);
        try
        {
          if (state != ContainerStatus.running) await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters() { Force = false });
        }
        catch
        {
          XtraMessageBox.Show("Could not delete the container!");
        }
      }
    }

    private async void barButtonItemDeleteContainerAndImage_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (gridViewContainerList.GetFocusedRow() is ContainerListResponse container)
      {
        if (XtraMessageBox.Show($"Do you want to delete the container '{container.Names}'{Environment.NewLine}and image '{container.Image}'?",
          "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
          return;
        }

        bool deleteContainerError = false;
        try
        {
          deleteContainerError = await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters() { Force = false }).ContinueWith(task =>
          {
            if (task.IsFaulted) return true;
            _dockerClient.Images.DeleteImageAsync(container.ImageID, new ImageDeleteParameters() { Force = false, PruneChildren = true });
            return false;
          });
        }
        catch
        {
          if (deleteContainerError)
            XtraMessageBox.Show("Could not delete the container!");
          else
            XtraMessageBox.Show("Could not delete the image!");
        }
      }
    }

    private async void barButtonItemDeleteAllContainers_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (XtraMessageBox.Show($"Do you want to delete all containers?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        return;

      var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
      var taskList = new List<Task>();
      foreach (var container in containers)
        taskList.Add(_dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters() { Force = false }));

      try
      {
        if (taskList.Any()) await Task.WhenAll(taskList.ToArray());
      }
      catch
      {
        XtraMessageBox.Show("Could not delete one or more of the containers!");
      }
    }

    private async void barButtonItemDeleteAllContainersAndImages_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (XtraMessageBox.Show($"Do you want to delete all containers and images?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        return;

      var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
      var taskList = new List<Task>();
      foreach (var container in containers)
      {
        taskList.Add(_dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters() { Force = false }).ContinueWith(t =>
          _dockerClient.Images.DeleteImageAsync(container.ImageID, new ImageDeleteParameters() { Force = false, PruneChildren = true })));
      }

      try
      {
        if (taskList.Any()) await Task.WhenAll(taskList.ToArray());
      }
      catch
      {
        XtraMessageBox.Show("Could not delete one or more of the containers and/or images!");
      }
    }

    private void barButtonItemAttachToContainer_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (gridViewContainerList.GetFocusedRow() is ContainerListResponse row)
      {
        Process.Start("docker.exe", $"attach {row.ID}");
      }
    }

    private void barButtonItemShowLogs_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (gridViewContainerList.GetFocusedRow() is ContainerListResponse row)
      {
        var startInfo = new ProcessStartInfo
        {
          FileName = "cmd.exe",
          Arguments = $"/K docker.exe logs {row.ID}"
        };

        Process.Start(startInfo);
      }
    }

    private void barButtonItemOpenUrlHttp_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (gridViewPorts.GetFocusedRow() is Port port)
        Process.Start($"http://localhost:{port.PublicPort}");
    }

    private void barButtonItemOpenUrlHttps_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (gridViewPorts.GetFocusedRow() is Port port)
        Process.Start($"https://localhost:{port.PublicPort}");
    }

    #endregion
  }
}