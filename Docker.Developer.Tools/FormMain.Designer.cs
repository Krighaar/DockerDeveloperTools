namespace Docker.Developer.Tools
{
  partial class MainForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

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
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this.virtualServerModeSourceContainers = new DevExpress.Data.VirtualServerModeSource(this.components);
      this.ribbonControl = new DevExpress.XtraBars.Ribbon.RibbonControl();
      this.ribbonPageContainers = new DevExpress.XtraBars.Ribbon.RibbonPage();
      this.ribbonPageImages = new DevExpress.XtraBars.Ribbon.RibbonPage();
      this.ribbonPageNetworks = new DevExpress.XtraBars.Ribbon.RibbonPage();
      this.ribbonStatusBar = new DevExpress.XtraBars.Ribbon.RibbonStatusBar();
      this.tabControl = new DevExpress.XtraTab.XtraTabControl();
      this.tabPageContainers = new DevExpress.XtraTab.XtraTabPage();
      this.containerListControl = new Docker.Developer.Tools.Controls.ContainerListControl();
      this.tabPageImages = new DevExpress.XtraTab.XtraTabPage();
      this.imagesListControl = new Docker.Developer.Tools.Controls.ImagesListControl();
      this.tabPageNetworks = new DevExpress.XtraTab.XtraTabPage();
      this.gridNetworks = new DevExpress.XtraGrid.GridControl();
      this.gridViewNetworks = new DevExpress.XtraGrid.Views.Grid.GridView();
      ((System.ComponentModel.ISupportInitialize)(this.virtualServerModeSourceContainers)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.ribbonControl)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.tabControl)).BeginInit();
      this.tabControl.SuspendLayout();
      this.tabPageContainers.SuspendLayout();
      this.tabPageImages.SuspendLayout();
      this.tabPageNetworks.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.gridNetworks)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.gridViewNetworks)).BeginInit();
      this.SuspendLayout();
      // 
      // virtualServerModeSourceContainers
      // 
      this.virtualServerModeSourceContainers.RowType = typeof(Docker.DotNet.Models.ImagesListResponse);
      // 
      // ribbonControl
      // 
      this.ribbonControl.ExpandCollapseItem.Id = 0;
      this.ribbonControl.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.ribbonControl.ExpandCollapseItem});
      this.ribbonControl.Location = new System.Drawing.Point(0, 0);
      this.ribbonControl.MaxItemId = 2;
      this.ribbonControl.Name = "ribbonControl";
      this.ribbonControl.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
            this.ribbonPageContainers,
            this.ribbonPageImages,
            this.ribbonPageNetworks});
      this.ribbonControl.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
      this.ribbonControl.ShowExpandCollapseButton = DevExpress.Utils.DefaultBoolean.False;
      this.ribbonControl.Size = new System.Drawing.Size(1546, 114);
      this.ribbonControl.StatusBar = this.ribbonStatusBar;
      this.ribbonControl.ToolbarLocation = DevExpress.XtraBars.Ribbon.RibbonQuickAccessToolbarLocation.Hidden;
      this.ribbonControl.SelectedPageChanging += new DevExpress.XtraBars.Ribbon.RibbonPageChangingEventHandler(this.ribbonControl_SelectedPageChanging);
      // 
      // ribbonPageContainers
      // 
      this.ribbonPageContainers.Name = "ribbonPageContainers";
      this.ribbonPageContainers.Text = "Containers";
      // 
      // ribbonPageImages
      // 
      this.ribbonPageImages.Name = "ribbonPageImages";
      this.ribbonPageImages.Text = "Images";
      // 
      // ribbonPageNetworks
      // 
      this.ribbonPageNetworks.Name = "ribbonPageNetworks";
      this.ribbonPageNetworks.Text = "Networks";
      // 
      // ribbonStatusBar
      // 
      this.ribbonStatusBar.Location = new System.Drawing.Point(0, 857);
      this.ribbonStatusBar.Name = "ribbonStatusBar";
      this.ribbonStatusBar.Ribbon = this.ribbonControl;
      this.ribbonStatusBar.Size = new System.Drawing.Size(1546, 25);
      // 
      // tabControl
      // 
      this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tabControl.Location = new System.Drawing.Point(0, 114);
      this.tabControl.Name = "tabControl";
      this.tabControl.SelectedTabPage = this.tabPageContainers;
      this.tabControl.Size = new System.Drawing.Size(1546, 743);
      this.tabControl.TabIndex = 0;
      this.tabControl.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
            this.tabPageContainers,
            this.tabPageImages,
            this.tabPageNetworks});
      // 
      // tabPageContainers
      // 
      this.tabPageContainers.Controls.Add(this.containerListControl);
      this.tabPageContainers.Name = "tabPageContainers";
      this.tabPageContainers.Size = new System.Drawing.Size(1540, 715);
      this.tabPageContainers.Text = "Containers";
      // 
      // containerListControl
      // 
      this.containerListControl.Dock = System.Windows.Forms.DockStyle.Fill;
      this.containerListControl.Location = new System.Drawing.Point(0, 0);
      this.containerListControl.Name = "containerListControl";
      this.containerListControl.Size = new System.Drawing.Size(1540, 715);
      this.containerListControl.TabIndex = 1;
      // 
      // tabPageImages
      // 
      this.tabPageImages.Controls.Add(this.imagesListControl);
      this.tabPageImages.Name = "tabPageImages";
      this.tabPageImages.Size = new System.Drawing.Size(1540, 715);
      this.tabPageImages.Text = "Images";
      // 
      // imagesListControl
      // 
      this.imagesListControl.Dock = System.Windows.Forms.DockStyle.Fill;
      this.imagesListControl.Location = new System.Drawing.Point(0, 0);
      this.imagesListControl.Name = "imagesListControl";
      this.imagesListControl.Size = new System.Drawing.Size(1540, 715);
      this.imagesListControl.TabIndex = 1;
      // 
      // tabPageNetworks
      // 
      this.tabPageNetworks.Controls.Add(this.gridNetworks);
      this.tabPageNetworks.Name = "tabPageNetworks";
      this.tabPageNetworks.Size = new System.Drawing.Size(1540, 715);
      this.tabPageNetworks.Text = "Networks";
      // 
      // gridNetworks
      // 
      this.gridNetworks.Dock = System.Windows.Forms.DockStyle.Fill;
      this.gridNetworks.Location = new System.Drawing.Point(0, 0);
      this.gridNetworks.MainView = this.gridViewNetworks;
      this.gridNetworks.MenuManager = this.ribbonControl;
      this.gridNetworks.Name = "gridNetworks";
      this.gridNetworks.Size = new System.Drawing.Size(1540, 715);
      this.gridNetworks.TabIndex = 0;
      this.gridNetworks.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewNetworks});
      // 
      // gridViewNetworks
      // 
      this.gridViewNetworks.GridControl = this.gridNetworks;
      this.gridViewNetworks.Name = "gridViewNetworks";
      this.gridViewNetworks.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDownFocused;
      this.gridViewNetworks.OptionsBehavior.ReadOnly = true;
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(1546, 882);
      this.Controls.Add(this.tabControl);
      this.Controls.Add(this.ribbonStatusBar);
      this.Controls.Add(this.ribbonControl);
      this.Name = "MainForm";
      this.Text = "Docker developer tools";
      this.Load += new System.EventHandler(this.FormMain_Load);
      ((System.ComponentModel.ISupportInitialize)(this.virtualServerModeSourceContainers)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.ribbonControl)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.tabControl)).EndInit();
      this.tabControl.ResumeLayout(false);
      this.tabPageContainers.ResumeLayout(false);
      this.tabPageImages.ResumeLayout(false);
      this.tabPageNetworks.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.gridNetworks)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.gridViewNetworks)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private DevExpress.XtraTab.XtraTabControl tabControl;
    private DevExpress.XtraTab.XtraTabPage tabPageContainers;
    private DevExpress.XtraTab.XtraTabPage tabPageImages;
    private DevExpress.XtraBars.Ribbon.RibbonControl ribbonControl;
    private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPageContainers;
    private DevExpress.XtraTab.XtraTabPage tabPageNetworks;
    private DevExpress.XtraGrid.GridControl gridNetworks;
    private DevExpress.XtraGrid.Views.Grid.GridView gridViewNetworks;
    private DevExpress.Data.VirtualServerModeSource virtualServerModeSourceContainers;
    private Developer.Tools.Controls.ContainerListControl containerListControl;
    private DevExpress.XtraBars.Ribbon.RibbonStatusBar ribbonStatusBar;
    private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPageImages;
    private Controls.ImagesListControl imagesListControl;
    private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPageNetworks;
  }
}

