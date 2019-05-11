using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using Docker.Developer.Tools.Helpers;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Docker.Developer.Tools.Controls
{
  public partial class NetworkListControl : XtraUserControl, IControlSupportsRibbonMerge
  {
    private DockerClient _dockerClient;
    // Prevents running UpdateDetails when the container list data source is changed.
    private bool _updatingDataSource = false;

    public NetworkListControl()
    {
      InitializeComponent();
      gridViewNetworkList.OptionsView.ShowColumnHeaders = false; // Hide here to allow easy design-time modification.
      UpdateButtons();
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (!DesignMode && _dockerClient == null)
        throw new InvalidOperationException($"Cannot load control when {nameof(_dockerClient)} has not been initialized!");
    }

    public async void Initialize(DockerClient dockerClient)
    {
      _dockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));
      await RefreshData();
      timer.Start();
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

    private async void timer_Tick(object sender, EventArgs e)
    {
      timer.Stop();
      try
      {
        gridViewNetworkList.ShowLoadingPanel();
        await RefreshData();
      }
      finally
      {
        gridViewNetworkList.HideLoadingPanel();
        timer.Start();
      }
    }

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }

      if (disposing && timer != null)
        timer.Tick -= timer_Tick;

      if (disposing && _dockerClient != null)
      {
        _dockerClient.Dispose();
        _dockerClient = null;
      }

      base.Dispose(disposing);
    }

    #region < GridViewList >

    private async Task RefreshData()
    {
      using (var token = gridControlState.StoreViewState(gridViewNetworkList))
      {
        var listContainerParameters = new NetworksListParameters();
        var result = await _dockerClient.Networks.ListNetworksAsync(listContainerParameters);
        _updatingDataSource = true;
        try
        {
          // Triggers FocusedRowChanged
          gridNetworkList.DataSource = result.ToList();
        }
        finally
        {
          _updatingDataSource = false;
        }
      }
    }

    private void gridViewNetworkList_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      if (e.IsGetData && e.Row is NetworkResponse network)
      {
        if (e.Column == colIDShort)
        {
          e.Value = network.ID.Substring(0, 8);
        }
      }
    }

    private void gridViewNetworkList_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
    {
      UpdateDetails();
    }

    private void UpdateDetails()
    {
      if (_updatingDataSource) return;

      var row = gridViewNetworkList.GetFocusedRow() as NetworkResponse;
      textId.Text = row != null ? row.ID : string.Empty;
      textName.Text = row != null ? row.Name : string.Empty;
      textCreatedDate.Text = row != null ? row.Created.ToShortDateString() : string.Empty;
      var driver = row != null ? row.Driver : string.Empty;
      textDriver.Text = driver != "null" ? driver : string.Empty;
      textScope.Text = row != null ? row.Scope : string.Empty;
      textAttachable.Text = row != null ? HelperFunctions.BooleanToText(row.Attachable) : string.Empty;
      textIPv6Enabled.Text = row != null ? HelperFunctions.BooleanToText(row.EnableIPv6) : string.Empty;
      textInternal.Text = row != null ? HelperFunctions.BooleanToText(row.Internal) : string.Empty;

      textIPAMConfig.Text = row != null ? row.IPAM.Driver : string.Empty;
      using (var token = gridControlState.StoreViewState(gridViewIPAMConfig))
        gridIPAMConfig.DataSource = row?.IPAM.Config?.ToList();

      using (var token = gridControlState.StoreViewState(gridViewIPAMOptions))
        gridIPAMOptions.DataSource = row?.IPAM.Options?.ToList();

      using (var token = gridControlState.StoreViewState(gridViewOptions))
        gridOptions.DataSource = row?.Options?.ToList();

      using (var token = gridControlState.StoreViewState(gridViewLabels))
        gridLabels.DataSource = row?.Labels?.ToList();

      // Not yet setup - No known way of getting sample data yet!
      // Containers
      // Peers

      UpdateButtons();
    }

    private void UpdateButtons()
    {
      var row = gridViewNetworkList.GetFocusedRow() as NetworkResponse;
      barButtonDeleteNetwork.Enabled = row != null;
      barButtonPruneNetworks.Enabled = row != null;
    }

    private void gridViewNetworkList_KeyDown(object sender, KeyEventArgs e)
    {
      // Skip moving through cells when moving focus up and down with the keys.
      if (e.KeyCode == Keys.Down)
      {
        gridViewNetworkList.MoveNext();
        e.Handled = true;
      }
      else if (e.KeyCode == Keys.Up)
      {
        gridViewNetworkList.MovePrev();
        e.Handled = true;
      }
    }

    private void gridViewNetworkList_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
    {
      if (e.Column == colIDShort)
      {
        e.Appearance.ForeColor = HelperFunctions.GetDisabledColor();
      }
    }

    #endregion

    #region < Details >

    private void gridViewIPAMConfig_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      var row = e.Row as IPAMConfig;
      if (e.IsGetData && row != null && e.Column == colIPAMId)
      {
        e.Value = row.IPRange + row.Subnet + row.Gateway;
      }
    }

    #endregion

    #region < Ribbon >

    private async void barButtonDeleteNetwork_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (gridViewNetworkList.GetFocusedRow() is NetworkResponse network)
      {
        if (XtraMessageBox.Show($"Do you want to delete the network '{network.Name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
          return;

        try
        {
          await _dockerClient.Networks.DeleteNetworkAsync(network.ID);
          AlertManager.ShowAlert("", "Network deleted!", Properties.Resources.DeleteNetwork);
        }
        catch
        {
          XtraMessageBox.Show("Could not delete the network!");
        }
      }
    }

    private async void barButtonPruneNetworks_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (XtraMessageBox.Show($"Do you want to delete all unused networks?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        return;

      try
      {
        await _dockerClient.Networks.PruneNetworksAsync();
        AlertManager.ShowAlert("", "Network pruning complete!", Properties.Resources.DeleteNetwork);
      }
      catch
      {
        XtraMessageBox.Show("An error occured while pruning the networks!");
      }
    }

    #endregion
  }
}
