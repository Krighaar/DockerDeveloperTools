using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms.Design;

namespace Docker.Developer.Tools.GridControlState.Design
{
  /// <summary>
  /// GridControlStateEditor is used to select the key column for each GridView used in the GridControlState component.
  /// </summary>
  internal class GridControlStateEditor : UITypeEditor
  {
    IWindowsFormsEditorService _editorService;

    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
    {
      return UITypeEditorEditStyle.DropDown;
    }

    public override bool IsDropDownResizable
    {
      get
      {
        return true;
      }
    }

    public override object EditValue(ITypeDescriptorContext context, System.IServiceProvider provider, object value)
    {
      if (context.Instance is GridView)
        _editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

      if (_editorService != null)
      {
        var listBox = BuildListBox(value, context);
        listBox.SelectedIndexChanged += (s, e) => { if (_editorService != null) _editorService.CloseDropDown(); };
        _editorService.DropDownControl(listBox);
        value = listBox.SelectedItem;
      }

      return value;
    }

    private ListBoxControl BuildListBox(object selectedValue, ITypeDescriptorContext context)
    {
      //Create a new list box control.
      var listBox = new ListBoxControl
      {
        Width = 250
      };
      //Handle DrawItem for drawing the null value.
      listBox.DrawItem += (s, e) =>
      {
        var itemInfo = e.GetItemInfo() as DevExpress.XtraEditors.ViewInfo.BaseListBoxViewInfo.ItemInfo;
        if (e.Item == null)
          itemInfo.Text = string.Format("(none)");
      };
      listBox.DisplayMember = "Name";
      listBox.HotTrackItems = true;
      if (context.Instance is GridView gridView)
      {
        //Get the column list.
        var columnList = gridView.Columns.OrderBy(l => l.Name).ToList();
        //Add null value column
        columnList.Insert(0, null);
        //Set data source
        listBox.DataSource = columnList;
        //Set the current selection.
        if (selectedValue != null && gridView.Columns.Contains(selectedValue))
          listBox.SelectedItem = selectedValue;
        else
          listBox.SelectedItem = null;
      }

      return listBox;
    }
  }
}
