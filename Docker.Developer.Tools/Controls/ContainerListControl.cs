using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraRichEdit;
using Docker.Developer.Tools.GridControlState;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Docker.Developer.Tools.Controls
{
  public partial class ContainerListControl : XtraUserControl, IControlSupportsRibbonMerge
  {
    private DockerClient _dockerClient;
    private ViewStateToken _containerViewToken;
    private CancellationTokenSource _containerLogsTokenSource;

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
      _containerViewToken = gridControlState.StoreViewState(gridViewContainerList);
      virtualServerModeSource.Refresh();
    }

    #region < GridViewList >

    private void virtualServerModeSource_ConfigurationChanged(object sender, DevExpress.Data.VirtualServerModeRowsEventArgs e)
    {
      var listContainerParameters = new ContainersListParameters() { All = true };
      e.RowsTask = _dockerClient.Containers.ListContainersAsync(listContainerParameters, e.CancellationToken).ContinueWith(result =>
      {
        return new DevExpress.Data.VirtualServerModeRowsTaskResult(result.Result.ToList(), false, null);
      });
    }

    private void gridViewContainerList_AsyncCompleted(object sender, EventArgs e)
    {
      // Store token in local variable before nulling the member.
      // See gridViewContainerList_FocusedRowChanged for reason.
      var token = _containerViewToken;
      _containerViewToken = null;
      token?.RestoreState();

      barButtonItemStopAllContainers.Enabled = gridViewContainerList.DataRowCount > 0;
      barButtonItemDeleteAllContainersMenu.Enabled = gridViewContainerList.DataRowCount > 0;
      // Manual trigger UpdateDetails since FocusedRowChanged wont trigger in this case.
      if (gridViewContainerList.FocusedRowHandle == 0) UpdateDetails();
      if (!timer.Enabled) timer.Start(); // Start refresh timer if not already started.
    }

    private void gridViewContainerList_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      if (e.IsGetData && e.Row is ContainerListResponse container)
      {
        if (e.Column == colName)
        {
          var name = container.Names.FirstOrDefault();
          e.Value = name.StartsWith("/") ? name.Substring(1) : name;
        }
        else if (e.Column == colContainerIdString)
        {
          e.Value = container.ID.Substring(0, 8);
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
      // If state is stored then this FocusedRowChanged was triggered by the data source being updated.
      // We ignore this as another FocusedRowChanged event will be triggered when state is restored.
      if (_containerViewToken != null) return;
      UpdateDetails();
    }

    private async void UpdateDetails()
    {
      var row = gridViewContainerList.GetFocusedRow() as ContainerListResponse;
      //UpdateLogs(row);

      textContainerId.Text = row != null ? row.ID : string.Empty;
      var containerName = row != null ? row.Names.FirstOrDefault() : string.Empty;
      textContainerName.Text = containerName.StartsWith("/") ? containerName.Substring(1) : containerName;
      var imageId = row != null ? row.ImageID : string.Empty;
      textImageId.Text = imageId.Contains(":") ? imageId.Substring(imageId.IndexOf(":") + 1) : imageId;
      textImageName.Text = row != null ? row.Image : string.Empty;
      textCreatedDate.Text = row != null ? row.Created.ToShortDateString() : string.Empty;
      textCommand.Text = row != null ? row.Command : string.Empty;
      textSizeRootFS.Text = row != null ? HelperFunctions.GetSizeString(row.SizeRootFs) : string.Empty;
      textSizeRW.Text = row != null ? HelperFunctions.GetSizeString(row.SizeRw) : string.Empty;
      textState.Text = row != null ? row.State : string.Empty;
      textStatus.Text = row != null ? row.Status : string.Empty;
      using (var token = gridControlState.StoreViewState(gridViewVolumes))
        gridVolumes.DataSource = row?.Mounts.ToList();

      using (var token = gridControlState.StoreViewState(gridViewPorts))
        gridPorts.DataSource = row?.Ports;

      using (var token = gridControlState.StoreViewState(gridViewNetworkSettings))
        gridNetworkSettings.DataSource = row?.NetworkSettings?.Networks;

      using (var token = gridControlState.StoreViewState(gridViewLabels))
        gridLabels.DataSource = row?.Labels;

      UpdateButtons();
    }

    private async void UpdateLogs(ContainerListResponse row)
    {
      // Cancel current token.
      if(_containerLogsTokenSource != null && !_containerLogsTokenSource.IsCancellationRequested) _containerLogsTokenSource.Cancel();
      if (row != null)
      {
        _containerLogsTokenSource = new CancellationTokenSource(100);
        var token = _containerLogsTokenSource.Token;
        token.Register(() => _containerLogsTokenSource = null);
        var logString = string.Empty;
        try
        {
          var stream = await _dockerClient.Containers.GetContainerLogsAsync(row.ID, new ContainerLogsParameters()
          {
            //Follow = true,
            ShowStderr = true,
            ShowStdout = true,
            Timestamps = false,
          }, token);

          // TODO: Resulting string cannot be parsed by text controls - or any controls it seems.
          // A way to parse it must be found for this to work.
          logString = new StreamReader(stream).ReadToEnd();
        }
        catch(OperationCanceledException)
        {
          logString = string.Empty;
        }
        finally
        {
          memoLogsViewer.EditValue = logString;
        }
      }
      else
      {
        memoLogsViewer.Text = string.Empty;
      }
    }

    private void UpdateButtons()
    {
      var row = gridViewContainerList.GetFocusedRow() as ContainerListResponse;
      barButtonItemStopContainer.Enabled = row != null;
      barButtonItemDeleteContainerMenu.Enabled = row != null;
      barButtonItemAttachToContainer.Enabled = row != null && row.State == ContainerStatus.running.ToString();
      barButtonItemShowLogs.Enabled = row != null;
      //barButtonItemOpenMappedFolder.Enabled = row != null && row.Mounts.Any();
      barButtonItemOpenUrlMenu.Enabled = row != null && row.Ports.Any();
    }

    private void gridViewContainerList_KeyDown(object sender, KeyEventArgs e)
    {
      // Skip moving through cells when moving focus up and down with the keys.
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
      if (e.Column == colImage || e.Column == colContainerIdString)
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
        if (e.Column == colVolumesMode)
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