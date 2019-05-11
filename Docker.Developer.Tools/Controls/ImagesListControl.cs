using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Docker.Developer.Tools.Controls
{
  public partial class ImagesListControl : XtraUserControl, IControlSupportsRibbonMerge
  {
    private DockerClient _dockerClient;
    // Prevents running UpdateDetails when the container list data source is changed.
    private bool _updatingDataSource = false;

    public ImagesListControl()
    {
      InitializeComponent();
      gridViewImageList.OptionsView.ShowColumnHeaders = false; // Hide here to allow easy design-time modification.
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
      if (parent == null) throw new ArgumentNullException(nameof(parent));
      parent.MergeStatusBar(ribbonStatusBar);
    }

    private async void timer_Tick(object sender, EventArgs e)
    {
      timer.Stop();
      try
      {
        gridViewImageList.ShowLoadingPanel();
        await RefreshData();
      }
      finally
      {
        gridViewImageList.HideLoadingPanel();
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
      using (var token = gridControlState.StoreViewState(gridViewImageList))
      {
        try
        {
          var imagesListParameters = new ImagesListParameters() { All = barButtonItemShowAllImages.Down };
          var result = await _dockerClient.Images.ListImagesAsync(imagesListParameters);
          if (barButtonItemHideNoneImages.Down)
            result = result.Where(l => l.RepoTags.FirstOrDefault() != null && l.RepoTags.First().ToLowerInvariant() != "<none>:<none>").ToList();

          _updatingDataSource = true;
          // Triggers FocusedRowChanged
          gridImageList.DataSource = result.ToList();
          barStaticItemDockerConnectionMissing.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
        }
        catch (Exception ex)
        {
          // The async call first throws a DockerApiException and a short while after a TimeoutException is throw as well.
          if (ex is DockerApiException || ex is TimeoutException)
            barStaticItemDockerConnectionMissing.Visibility = DevExpress.XtraBars.BarItemVisibility.Always;
          else
            throw;
        }
        finally
        {
          _updatingDataSource = false;
        }
      }
    }

    private void gridViewImageList_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      if (e.IsGetData && e.Row is ImagesListResponse imagesListResponse)
      {
        if (e.Column == colIDShort)
          e.Value = imagesListResponse.ID.Substring(imagesListResponse.ID.IndexOf(":") + 1, 10);
        else if (e.Column == colRepositoryName)
          e.Value = imagesListResponse.RepoTags.FirstOrDefault();
      }
    }

    private void gridViewImageList_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
    {
      UpdateDetails();
    }

    private void UpdateDetails()
    {
      if (_updatingDataSource) return;

      var imageResponse = gridViewImageList.GetFocusedRow() as ImagesListResponse;
      var imageId = imageResponse != null ? imageResponse.ID : string.Empty;
      textImageId.Text = imageId.Contains(":") ? imageId.Substring(imageId.IndexOf(":") + 1) : imageId;
      var parentImageId = imageResponse != null ? imageResponse.ParentID : string.Empty;
      textParentId.Text = parentImageId.Contains(":") ? parentImageId.Substring(parentImageId.IndexOf(":") + 1) : parentImageId;
      textCreatedDate.Text = imageResponse != null ? imageResponse.Created.ToShortDateString() : string.Empty;
      textVirtualSize.Text = imageResponse != null ? HelperFunctions.GetSizeString(imageResponse.VirtualSize) : string.Empty;
      textSize.Text = imageResponse != null ? HelperFunctions.GetSizeString(imageResponse.Size) : string.Empty;
      textSharedSize.Text = imageResponse != null && imageResponse.SharedSize > -1 ? HelperFunctions.GetSizeString(imageResponse.SharedSize) : string.Empty;
      textContainers.Text = imageResponse != null && imageResponse.Containers > -1 ? imageResponse.Containers.ToString() : string.Empty;
      using (var token = gridControlState.StoreViewState(gridViewRepositoryDigests))
        gridRepositoryDigests.DataSource = imageResponse?.RepoDigests;

      using (var token = gridControlState.StoreViewState(gridViewRepositoryTags))
        gridRepositoryTags.DataSource = imageResponse?.RepoTags;

      using (var token = gridControlState.StoreViewState(gridViewLabels))
        gridLabels.DataSource = imageResponse?.Labels;

      UpdateButtons();
    }

    private void UpdateButtons()
    {
      var row = gridViewImageList.GetFocusedRow() as ImagesListResponse;
      barButtonItemDeleteImage.Enabled = row != null;
    }

    private void gridViewImageList_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.KeyCode == Keys.Down)
      {
        gridViewImageList.MoveNext();
        e.Handled = true;
      }
      else if (e.KeyCode == Keys.Up)
      {
        gridViewImageList.MovePrev();
        e.Handled = true;
      }
    }

    private void gridViewImageList_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
    {
      if (e.Column == colIDShort)
      {
        e.Appearance.ForeColor = HelperFunctions.GetDisabledColor();
      }
    }

    #endregion

    #region < Details >

    private void gridViewRepositoryDigests_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      e.Value = e.Row;
    }

    private void gridViewRepositoryTags_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      e.Value = e.Row;
    }

    #endregion

    #region < Ribbon >

    private async void barButtonItemDeleteImage_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (gridViewImageList.GetFocusedRow() is ImagesListResponse image)
      {
        if (XtraMessageBox.Show($"Do you want to delete the image '{image.ID.Substring(image.ID.IndexOf(":") + 1, 12)}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
          return;

        try
        {
          await _dockerClient.Images.DeleteImageAsync(image.ID, new ImageDeleteParameters() { Force = false, PruneChildren = true });
        }
        catch
        {
          XtraMessageBox.Show("Could not delete the image!");
        }
      }
    }

    private async void barButtonItemDeleteAllImages_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
      if (XtraMessageBox.Show($"Do you want to delete all images?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        return;

      var images = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters() { All = true });
      var taskList = new List<Task>();
      foreach (var image in images)
        taskList.Add(_dockerClient.Images.DeleteImageAsync(image.ID, new ImageDeleteParameters() { Force = false, PruneChildren = true }));

      try
      {
        if (taskList.Any()) await Task.WhenAll(taskList.ToArray());
      }
      catch
      {
        XtraMessageBox.Show("Could not delete one or more of the images!");
      }
    }

    #endregion
  }
}