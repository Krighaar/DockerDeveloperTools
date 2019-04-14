using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Docker.Developer.Tools.Controls
{
  public partial class ImagesListControl : XtraUserControl, IControlSupportsRibbonMerge
  {
    private DockerClient _dockerClient;

    public ImagesListControl()
    {
      InitializeComponent();
      gridViewImageList.OptionsView.ShowColumnHeaders = false; // Hide here to allow easy design-time modification.
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
      e.RowsTask = _dockerClient.Images.ListImagesAsync(new ImagesListParameters() { All = true }, e.CancellationToken).ContinueWith(task =>
      {
        // We add an order by here as the grid seems to have trouble sorting on its own.
        return new DevExpress.Data.VirtualServerModeRowsTaskResult(task.Result.OrderBy(l => l.ID).ToList(), false, null);
      });

      if (!timer.Enabled) timer.Start();
    }

    private void gridViewImageList_AsyncCompleted(object sender, EventArgs e)
    {
      barButtonItemDeleteAllImages.Enabled = gridViewImageList.DataRowCount > 0;
    }

    private void gridViewImageList_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
    {
      if (e.IsGetData && e.Row is ImagesListResponse imagesListResponse)
      {
        if (e.Column == colID)
          e.Value = imagesListResponse.ID.Substring(imagesListResponse.ID.IndexOf(":") + 1, 12);
        else if (e.Column == colRepositoryName)
          e.Value = imagesListResponse.RepoTags.FirstOrDefault();
      }
    }

    private void gridViewImageList_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
    {
      var imageResponse = gridViewImageList.GetFocusedRow() as ImagesListResponse;
      textImageId.Text = imageResponse != null ? imageResponse.ID : string.Empty;
      textParentId.Text = imageResponse != null ? imageResponse.ParentID : string.Empty;
      textCreatedDate.Text = imageResponse != null ? imageResponse.Created.ToShortDateString() : string.Empty;
      textVirtualSize.Text = imageResponse != null ? HelperFunctions.GetSizeString(imageResponse.VirtualSize) : string.Empty;
      textSize.Text = imageResponse != null ? HelperFunctions.GetSizeString(imageResponse.Size) : string.Empty;
      textSharedSize.Text = imageResponse != null && imageResponse.SharedSize > -1 ? HelperFunctions.GetSizeString(imageResponse.SharedSize) : string.Empty;
      textContainers.Text = imageResponse != null && imageResponse.Containers > -1 ? imageResponse.Containers.ToString() : string.Empty;
      gridRepositoryDigests.DataSource = imageResponse?.RepoDigests;
      gridRepositoryTags.DataSource = imageResponse?.RepoTags;
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
      if (e.Column == colRepositoryName)
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