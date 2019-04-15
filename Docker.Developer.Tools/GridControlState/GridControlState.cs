using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using Docker.Developer.Tools.GridControlState.Design;

namespace Docker.Developer.Tools.GridControlState
{
  /// <summary>
  /// Stores and restores state in <see cref="GridView"/> controls.
  /// </summary>
  // TODO: Sørg for den understøtter de andre view typer (WinExplorerView, CardView, LayoutView.
  [ProvideProperty("KeyColumn", typeof(GridView))]
  public class GridControlState : Component, IExtenderProvider
  {
    #region Internal Classes

    /// <summary>
    /// Describes stored row info.
    /// </summary>
    protected struct RowInfo
    {
      public RowInfo(int level, SelectionType selectionType, object value)
      {
        Level = level;
        SelectionType = selectionType;
        Value = value;
      }

      /// <summary>
      /// Gets the stored value.
      /// </summary>
      public object Value;

      /// <summary>
      /// Gets the level of the row.
      /// </summary>
      public int Level;

      /// <summary>
      /// Gets the selection type.
      /// </summary>
      public SelectionType SelectionType;
    }

    [Flags]
    protected enum SelectionType
    {
      None = 0,
      Selection = 1,
      Focus = 2
    }

    /// <summary>
    /// Describes a visible row.
    /// </summary>
    protected struct VisibleRowInfo
    {
      /// <summary>
      /// Gets or sets the value of the visible row.
      /// </summary>
      public object Value;

      /// <summary>
      /// Gets or sets the level of the visible row.
      /// </summary>
      public int Level;

      /// <summary>
      /// Gets or sets if the row is a group row.
      /// </summary>
      public bool IsGroupRow;
    }

    /// <summary>
    /// Describes a view based on a level and a key column.
    /// </summary>
    protected class ViewDescriptor
    {
      /// <summary>
      /// Creates a new <see cref="ViewDescriptor"/> instance without a key column.
      /// </summary>
      /// <param name="levelName">The level name associated with the descriptor.</param>
      public ViewDescriptor(string levelName)
        : this(levelName, null)
      {
      }

      /// <summary>
      /// Creates a new <see cref="ViewDescriptor"/> instance with a key column.
      /// </summary>
      /// <param name="levelName">The level name associated with the descriptor.</param>
      /// <param name="keyFieldName">The key column associated with the level.</param>
      public ViewDescriptor(string levelName, GridColumn keyFieldName)
      {
        LevelName = LevelName;
        KeyColumn = KeyColumn;
      }

      /// <summary>
      /// The level name.
      /// </summary>
      public string LevelName { get; set; }

      /// <summary>
      /// The key column.
      /// </summary>
      public GridColumn KeyColumn { get; set; }
    }

    /// <summary>
    /// State object for a saved view state.
    /// </summary>
    protected class ViewState
    {
      private ViewDescriptor _descriptor;
      private List<RowInfo> _savedExpandedGroupsList;
      private List<RowInfo> _savedSelectionList;
      private ArrayList _savedMasterRowsList;
      private VisibleRowInfo _visibleRowInfo;
      private IDictionary<object, ViewState> _detailViews;
      private int _horzScrollPos;
      private IDictionary<object, string[]> _cellSelection;
      private bool _viewFocused;

      #region Constructors and Creators

      /// <summary>
      /// Creates a new root <see cref="ViewState"/> instance with no parent ViewState.
      /// </summary>
      /// <param name="gridState">The GridControlState instance that this ViewState is created for.</param>
      /// <param name="descriptor">View descriptor instance.</param>
      private ViewState(GridControlState gridState, ViewDescriptor descriptor)
      {
        GridState = gridState;
        _descriptor = descriptor;
      }

      /// <summary>
      /// Creates a new <see cref="ViewState"/> instance with a parent ViewState.
      /// </summary>
      /// <param name="parent">The parent <see cref="ViewState"/> that this ViewState is based on.</param>
      /// <param name="descriptor">View descriptor instance.</param>
      private ViewState(ViewState parent, ViewDescriptor descriptor)
      {
        GridState = parent.GridState;
        _descriptor = descriptor;
      }

      /// <summary>
      /// Creates a new root <see cref="ViewState"/> instance with no parent ViewState.
      /// </summary>
      /// <param name="gridState">The GridControlState instance that this ViewState is created for.</param>
      /// <param name="view">The <see cref="GridView"/> to create the view state for.</param>
      /// <returns></returns>
      public static ViewState Create(GridControlState gridState, GridView view)
      {
        if (!gridState.ViewDescriptors.ContainsKey(view)) return null;
        var state = new ViewState(gridState, gridState.ViewDescriptors[view]);
        return state;
      }

      /// <summary>
      /// Creates a new <see cref="ViewState"/> instance with a parent ViewState.
      /// </summary>
      /// <param name="parent">The parent <see cref="ViewState"/> that this ViewState is based on.</param>
      /// <param name="view">The <see cref="GridView"/> to create the view state for.</param>
      /// <returns></returns>
      private static ViewState Create(ViewState parent, GridView view)
      {
        if (!parent.GridState.ViewDescriptors.ContainsKey(view)) return null;
        var state = new ViewState(parent, parent.GridState.ViewDescriptors[view]);
        return state;
      }

      #endregion

      #region Properties

      /// <summary>
      /// Gets or Sets the grid control state that this ViewState belongs to.
      /// </summary>
      private GridControlState GridState
      {
        get;
        set;
      }

      /// <summary>
      /// Gets the level name of the <see cref="ViewState"/>.
      /// </summary>
      public string LevelName { get { return _descriptor.LevelName; } }

      /// <summary>
      /// Gets a list of RowInfo objects representing expanded groups.
      /// </summary>
      private List<RowInfo> SavedExpandedGroupsList
      {
        get
        {
          if (_savedExpandedGroupsList == null)
            _savedExpandedGroupsList = new List<RowInfo>();

          return _savedExpandedGroupsList;
        }
      }

      /// <summary>
      /// Gets a list of RowInfo objects representing selected rows.
      /// </summary>
      private List<RowInfo> SavedSelectionList
      {
        get
        {
          if (_savedSelectionList == null)
            _savedSelectionList = new List<RowInfo>();

          return _savedSelectionList;
        }
      }

      /// <summary>
      /// Gets a list of values representing expanded master rows.
      /// </summary>
      private ArrayList SavedExpandedMasterRowsList
      {
        get
        {
          if (_savedMasterRowsList == null)
            _savedMasterRowsList = new ArrayList();

          return _savedMasterRowsList;
        }
      }

      /// <summary>
      /// Gets a dictionary containing keyValues and their related ViewStates.
      /// </summary>
      private IDictionary<object, ViewState> DetailViews
      {
        get
        {
          if (_detailViews == null)
            _detailViews = new ConcurrentDictionary<object, ViewState>();
          return _detailViews;
        }
      }

      /// <summary>
      /// Gets a dictionary containing keyValues and their selected column names.
      /// </summary>
      private IDictionary<object, string[]> CellSelection
      {
        get
        {
          if (_cellSelection == null)
            _cellSelection = new ConcurrentDictionary<object, string[]>();
          return _cellSelection;
        }
      }

      #endregion

      #region General Helper Methods

      /// <summary>
      /// Finds the handle of the row containing the specified value.
      /// </summary>
      /// <param name="view">The view to look through.</param>
      /// <param name="value">The value to search for.</param>
      /// <returns>Returns the row handle of the matching row.
      /// Returns GridControl.InvalidRowHandle when no matching row was found.</returns>
      protected int FindRowHandleByKeyValue(GridView view, object value)
      {
        //If the grid is in server mode, then get the row handle from the DataController.
        if (view.IsServerMode)
          return view.DataController.FindRowByValue(_descriptor.KeyColumn.FieldName, value);

        //Go through each row until we find the row with the saved key value.
        for (var index = 0; index < view.DataRowCount; index++)
        {
          if (Equals(value, view.GetRowCellValue(index, _descriptor.KeyColumn)))
            return index;
        }

        //If the row was not was found, then return InvalidRowHandle.
        return GridControl.InvalidRowHandle;
      }

      /// <summary>
      /// Finds a GroupRowHandle by value.
      /// </summary>
      /// <param name="view">The <see cref="GridView"/> to search.</param>
      /// <param name="value">The value to look for.</param>
      /// <returns></returns>
      protected int FindGroupRowHandleByValue(GridView view, object value)
      {
        for (var index = -1; index > int.MinValue; index--)
        {
          //Break the loop when there are no more group rows.
          if (!view.IsValidRowHandle(index)) break;

          var groupRowValue = view.GetGroupRowValue(index);
          if (groupRowValue == null) continue;

          if (groupRowValue.Equals(value)) return index;
        }

        //If the row was no row was found, then return InvalidRowHandle.
        return GridControl.InvalidRowHandle;
      }

      /// <summary>
      /// Finds the parent row handle matching the level.
      /// </summary>
      /// <param name="view">The view to look through.</param>
      /// <param name="groupLevel">The groups level to look for.</param>
      /// <param name="rowHandle">The row handle to find for.</param>
      /// <returns>Returns the row handle of the parent group.</returns>
      protected int FindParentGroupRowHandle(GridView view, int groupLevel, int rowHandle)
      {
        //Get the parent handle for the row handle.
        var parentRowHandle = view.GetParentRowHandle(rowHandle);
        //While the level of the parent row handle does not match the expected group level.
        while (view.GetRowLevel(parentRowHandle) != groupLevel)
          //Get the parent handle for the parent row handle.
          parentRowHandle = view.GetParentRowHandle(parentRowHandle);

        return parentRowHandle;
      }

      /// <summary>
      /// Finds the row handle to select.
      /// </summary>
      /// <param name="view">The grid view to find the handle from.</param>
      /// <param name="rowInfo">Row info to find the row handle for.</param>
      /// <returns>Returns the row handle to select.</returns>
      protected int GetRowHandleFromRowInfo(GridView view, RowInfo rowInfo)
      {
        //Find the row handle by key value.
        var dataRowHandle = FindRowHandleByKeyValue(view, rowInfo.Value);
        //If the row handle is valid and the row handle level does not match the row info level
        //  then find and return the parent group row handle.
        if (dataRowHandle != GridControl.InvalidRowHandle && view.GetRowLevel(dataRowHandle) != rowInfo.Level)
          return FindParentGroupRowHandle(view, rowInfo.Level, dataRowHandle);

        //Return the row handle.
        return dataRowHandle;
      }

      #endregion

      #region StoreState

      /// <summary>
      /// Stores the specified grid views current state, as well as the state of all the details views.
      /// </summary>
      /// <param name="view">The grid view to store state for.</param>
      public void StoreState(GridView view)
      {
        //Is this view the grid control's focused view?
        _viewFocused = ReferenceEquals(view.GridControl.FocusedView, view);
        StoreExpandedMasterRows(view);
        StoreExpandedGroupRows(view);
        StoreSelection(view);
        StoreVisibleIndex(view);
        //Store horizontal scroll position.
        _horzScrollPos = view.LeftCoord;
      }

      /// <summary>
      /// Stores the expanded master rows.
      /// </summary>
      /// <param name="view">The grid view to store expanded master rows for.</param>
      public void StoreExpandedMasterRows(GridView view)
      {
        //If there are no master details in the grid control then return to caller.
        if (view.GridControl.Views.Count == 1) return;
        //Clear any previously stored detail views.
        DetailViews.Clear();
        //Clear any previously saved rows.
        SavedExpandedMasterRowsList.Clear();
        //Go through each row in the grid view.
        for (var index = 0; index < view.DataRowCount; index++)
        {
          //Is the master row is expanded?
          if (view.GetMasterRowExpanded(index))
          {
            //Get the key value of the row and add it to the expanded master rows list.
            var keyValue = view.GetRowCellValue(index, _descriptor.KeyColumn);
            SavedExpandedMasterRowsList.Add(keyValue);
            //Try and get the visible details view of the grid view.
            if (view.GetVisibleDetailView(index) is GridView detail)
            {
              //Create a new view state for the details view.
              var state = Create(this, detail);
              if (state != null)
              {
                //Save the state with it's key value.
                DetailViews[keyValue] = state;
                //Store the state of the details view.
                state.StoreState(detail);
              }
            }
          }
        }
      }

      /// <summary>
      /// Stores the expanded group rows.
      /// </summary>
      /// <param name="view">The grid view to store expanded group rows for.</param>
      public void StoreExpandedGroupRows(GridView view)
      {
        //If there are no grouped columns in the grid view then return to caller.
        if (view.GroupedColumns.Count == 0) return;
        //Clear any previously saved groups.
        SavedExpandedGroupsList.Clear();
        //Go through each group row in the grid view.
        for (var index = -1; index > int.MinValue; index--)
        {
          //Break the loop when there are no more group rows.
          if (!view.IsValidRowHandle(index)) break;
          //Is the group is expanded.
          if (view.GetRowExpanded(index))
          {
            //Create row info object with value and level.
            RowInfo rowInfo;
            rowInfo.Level = view.GetRowLevel(index);
            rowInfo.SelectionType = SelectionType.None;
            rowInfo.Value = view.GetGroupRowValue(index);
            //Add the row info to the selection list.
            SavedExpandedGroupsList.Add(rowInfo);
          }
        }
      }

      /// <summary>
      /// Stores the selected rows.
      /// </summary>
      /// <param name="view">The grid view to store the selection for.</param>
      /// <remarks>This method will also store the selected cells when GridMultiSelectMode is set to CellSelect.</remarks>
      public void StoreSelection(GridView view)
      {
        //Clear any previously selected rows.
        SavedSelectionList.Clear();
        //Clear any previously selected cells.
        CellSelection.Clear();
        //Get the selected rows.
        var selectedRows = view.GetSelectedRows();
        //Are there any selected rows? If not, then we just have a single focused but not selected row.
        var focusedRowStored = false;
        if (selectedRows != null)
        {
          //Go through each selected row in the grid view.
          foreach (var selectedRowHandle in selectedRows)
          {
            //Add the selected row to the selection list.
            var dataRowHandle = StoreSelectedRow(view, selectedRowHandle, selectedRowHandle == view.FocusedRowHandle, true);
            // If focused row have been stored
            if (!focusedRowStored && selectedRowHandle == view.FocusedRowHandle) focusedRowStored = true;

            //If GridMultiSelectMode is set to CellSelect then store selected cells.
            if (view.OptionsSelection.MultiSelectMode == GridMultiSelectMode.CellSelect)
            {
              //Get the selected columns for the row handle.
              var columns = view.GetSelectedCells(dataRowHandle);
              var columnNames = new string[columns.Length];
              //Add the column names to the array.
              for (var index = 0; index < columns.Length; index++)
                columnNames[index] = columns[index].FieldName;
              //Add or update the the cell selection for the row.
              CellSelection[view.GetRowCellValue(dataRowHandle, _descriptor.KeyColumn)] = columnNames;
            }
          }
        }

        // Add the focused row to the selection.
        if (!focusedRowStored) StoreSelectedRow(view, view.FocusedRowHandle, true, false);
      }

      /// <summary>
      /// Adds the specified row to the selection list.
      /// </summary>
      /// <param name="view">The grid view that the row belongs to.</param>
      /// <param name="rowHandle">The row handle of the row to add.</param>
      /// <returns>Returns the row handle that was added.</returns>
      private int StoreSelectedRow(GridView view, int rowHandle, bool focused, bool selected)
      {
        // Create row info object.
        RowInfo rowInfo;
        rowInfo.Level = view.GetRowLevel(rowHandle);
        rowInfo.SelectionType = focused ? SelectionType.Focus : SelectionType.None;
        rowInfo.SelectionType |= selected ? SelectionType.Selection : SelectionType.None;
        // If the handle is a group row handle then replace rowHandle with the group row handles data row handle.
        if (rowHandle < 0) // group row
          rowHandle = view.GetDataRowHandleByGroupRowHandle(rowHandle);

        // Store the value of the row handle in the row info object.
        rowInfo.Value = view.GetRowCellValue(rowHandle, _descriptor.KeyColumn);
        // Add the row info to the selection list.
        SavedSelectionList.Add(rowInfo);
        // Return the row handle.
        return rowHandle;
      }

      /// <summary>
      /// Stores the visibile row.
      /// </summary>
      /// <param name="view">The grid view to store visibility for.</param>
      public void StoreVisibleIndex(GridView view)
      {
        //Get the rowhandle of the top visible row.
        var topVisibleRowHandle = view.GetVisibleRowHandle(view.TopRowIndex);
        VisibleRowInfo rowInfo;
        //If the rowHandle is a group row, then replace topVisibleRowHandle with the first
        if (view.IsGroupRow(topVisibleRowHandle))
        {
          //Create row info object with value and level.
          rowInfo.Value = view.GetGroupRowValue(topVisibleRowHandle);
          rowInfo.Level = view.GetRowLevel(topVisibleRowHandle);
          rowInfo.IsGroupRow = true;
        }
        else
        {
          //Create row info object with value and level.
          rowInfo.Value = view.GetRowCellValue(topVisibleRowHandle, _descriptor.KeyColumn);
          rowInfo.Level = view.GetRowLevel(topVisibleRowHandle);
          rowInfo.IsGroupRow = false;
        }

        _visibleRowInfo = rowInfo;
      }

      #endregion

      #region StoreState

      /// <summary>
      /// Restores the state of the specified grid view, as well as the state of all the details views.
      /// </summary>
      /// <param name="view">The grid view to restore state for.</param>
      public void RestoreState(GridView view)
      {
        //Restore the focused view.
        if (_viewFocused) view.GridControl.FocusedView = view;
        RestoreExpandedMasterRows(view);
        RestoreExpandedGroupRows(view);
        RestoreSelection(view);
        RestoreVisibleIndex(view);
        //Restore horizontal scroll position.
        view.LeftCoord = _horzScrollPos;
      }

      /// <summary>
      /// Restores the previously expanded master rows.
      /// </summary>
      /// <param name="view">The grid view to expand in.</param>
      public void RestoreExpandedMasterRows(GridView view)
      {
        //TODO: Does including BeginUpdate and EndUpdate have any effect here?
        //  A potential problem with adding BeginUpdate and EndUpdate might be that GetVisibleDetailView
        //  does not get the correct/expected value when the grid is not updating. This needs verifying.
        //view.BeginUpdate();
        //try
        //{
        //Collapse all master rows.
        view.CollapseAllDetails();
        //Foreach saved key value
        foreach (var value in SavedExpandedMasterRowsList)
        {
          //Find the row handle matching the value.
          var rowHandle = FindRowHandleByKeyValue(view, value);
          //If no details ViewState exist for this value.
          if (!DetailViews.TryGetValue(value, out var state))
          {
            //Expand the master row.
            view.SetMasterRowExpanded(rowHandle, true);
          }
          else
          {
            //Expand the master row and make the specified relation index the visible detail.
            view.SetMasterRowExpandedEx(rowHandle, view.GetRelationIndex(rowHandle, state._descriptor.LevelName), true);
            //Get the visible details view for the row.
            //If a details view is found then load the state for this details view.
            if (view.GetVisibleDetailView(rowHandle) is GridView detail) state.RestoreState(detail);
          }
        }
        //}
        //finally
        //{
        //  view.EndUpdate();
        //}
      }

      /// <summary>
      /// Restores the previously expanded group rows.
      /// </summary>
      /// <param name="view">The grid view to expand in.</param>
      public void RestoreExpandedGroupRows(GridView view)
      {
        //Are there any grouped columns?
        if (view.GroupedColumns.Count == 0) return;

        view.BeginUpdate();
        try
        {
          //Collapse all groups.
          view.CollapseAllGroups();
          //Expand the rows that was saved.
          foreach (var info in SavedExpandedGroupsList)
            ExpandGroupRow(view, info);
        }
        finally
        {
          view.EndUpdate();
        }
      }

      /// <summary>
      /// Expands the specified group row.
      /// </summary>
      /// <param name="view">The grid view that the row belongs to.</param>
      /// <param name="rowInfo">The row info to expand.</param>
      protected void ExpandGroupRow(GridView view, RowInfo rowInfo)
      {
        //Get the row handle from the value.
        var groupRowHandle = FindGroupRowHandleByValue(view, rowInfo.Value);
        //Only try to expand the group if it is valid.
        if (groupRowHandle != GridControl.InvalidRowHandle)
          view.SetRowExpanded(groupRowHandle, true, false);
      }

      /// <summary>
      /// Restores the previously selected rows.
      /// </summary>
      /// <param name="view">The grid view to select in.</param>
      /// <remarks>This method will also restore the selected cells when GridMultiSelectMode is set to CellSelect.</remarks>
      public void RestoreSelection(GridView view)
      {
        view.BeginSelection();
        try
        {
          //Clear current selection
          view.ClearSelection();
          //Select each saved row. The last row in the stored selection is also given focus.
          //The last RowInfo added to the list during StoreSelection is always the focused row.
          for (var index = 0; index < SavedSelectionList.Count; index++)
            RestoreSelectedRow(view, SavedSelectionList[index]);
        }
        finally
        {
          view.EndSelection();
        }
      }

      /// <summary>
      /// Restores selection to the specified row.
      /// </summary>
      /// <param name="view">The grid view that the row should belong to.</param>
      /// <param name="rowInfo">Row info of the row to select.</param>
      protected void RestoreSelectedRow(GridView view, RowInfo rowInfo)
      {
        // Get the row handle.
        var rowHandle = GetRowHandleFromRowInfo(view, rowInfo);
        // Should the row be focused?
        if (rowInfo.SelectionType.HasFlag(SelectionType.Focus))
        {
          view.FocusedRowHandle = rowHandle;
          return;
        }

        // If GridMultiSelectMode is set to CellSelect then restore selected cells.
        if (view.OptionsSelection.MultiSelectMode == GridMultiSelectMode.CellSelect)
        {
          // Try to get the selected cell columns.
          if (CellSelection.TryGetValue(rowInfo.Value, out var columnNames))
          {
            // Make sure we did not store null for this value.
            if (columnNames != null)
            {
              // Go through each column and select the corresponding cell.
              foreach (var columnName in columnNames)
                view.SelectCell(rowHandle, view.Columns[columnName]);
            }
          }
        }
        // else select the entire row.
        else
        {
          view.SelectRow(rowHandle);
        }
      }

      /// <summary>
      /// Restores the visible row.
      /// </summary>
      /// <param name="view">The grid view to restore visibility for.</param>
      public void RestoreVisibleIndex(GridView view)
      {
        view.MakeRowVisible(view.FocusedRowHandle, true);
        if (_visibleRowInfo.Value != null)
        {
          int rowHandle;
          if (_visibleRowInfo.IsGroupRow)
          {
            rowHandle = FindGroupRowHandleByValue(view, _visibleRowInfo.Value);
          }
          else
          {
            rowHandle = GetRowHandleFromRowInfo(view, new RowInfo()
            {
              Level = _visibleRowInfo.Level,
              Value = _visibleRowInfo.Value
            });
          }

          view.TopRowIndex = view.GetVisibleIndex(rowHandle);
        }
      }

      #endregion
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="GridControlState"/> class.
    /// </summary>
    public GridControlState()
    {
      ViewDescriptors = new ConcurrentDictionary<ColumnView, ViewDescriptor>();
      ViewStates = new ConcurrentDictionary<GridView, ViewState>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GridControlState"/> class with the given <see cref="System.ComponentModel.IContainer"/>.
    /// </summary>
    /// <param name="container">The <see cref="System.ComponentModel.IContainer"/> to be added to this components container.</param>
    public GridControlState(IContainer container)
      : this()
    {
      container.Add(this);
    }

    /// <summary>
    /// Stores <see cref="ColumnView"/> and associated <see cref="ViewDescriptor"/>.
    /// </summary>
    private IDictionary<ColumnView, ViewDescriptor> ViewDescriptors
    {
      get;
      set;
    }

    /// <summary>
    /// Stores <see cref="GridView"/> and current <see cref="ViewState"/>.
    /// </summary>
    private IDictionary<GridView, ViewState> ViewStates
    {
      get;
      set;
    }

    //Design-Time implementation for easy setup.
    #region Design-Time provider implementation

    /// <summary>
    /// Specifies whether this object can provide its extender properties to the specified object.
    /// </summary>
    /// <param name="extendee">The object to extend.</param>
    /// <returns></returns>
    public bool CanExtend(object extendee)
    {
      //We only support the GridView - for now at least.
      return extendee is GridView;
    }

    /// <summary>
    /// Gets the key column for the specified <see cref="GridView"/>.
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    [DefaultValue(null)]
    [Editor(typeof(GridControlStateEditor), typeof(UITypeEditor))]
    public GridColumn GetKeyColumn(GridView view)
    {
      return EnsureViewExists(view).KeyColumn;
    }

    /// <summary>
    /// Sets the key column for the specified <see cref="GridView"/>.
    /// </summary>
    /// <param name="view">The view to update key column for.</param>
    /// <param name="value">The key column to associate with the <see cref="GridView"/>.</param>
    [Editor(typeof(GridControlStateEditor), typeof(UITypeEditor))]
    public void SetKeyColumn(GridView view, GridColumn value)
    {
      EnsureViewExists(view).KeyColumn = value;
    }

    /// <summary>
    /// Ensures the view exists in the <see cref="ViewDescriptors"/> dictionary and returns the associated <see cref="ViewDescriptor"/>.
    /// </summary>
    /// <param name="view">The view to get the </param>
    /// <returns></returns>
    private ViewDescriptor EnsureViewExists(ColumnView view)
    {
      if (!ViewDescriptors.ContainsKey(view))
      {
        //Creates a new view descriptor with the default key value.
        var descriptor = new ViewDescriptor(view.LevelName);
        ViewDescriptors.Add(view, descriptor);
      }

      return ViewDescriptors[view];
    }

    #endregion

    //AddView methods for manually adding a view to the GridControlState in code
    #region AddView

    /// <summary>
    /// Adds a <see cref="GridView"/> and key column to the <see cref="GridControlState"/>.
    /// </summary>
    /// <param name="view">The <see cref="GridView"/> to add.</param>
    /// <param name="keyColumn">The key column associated with the view.</param>
    public void AddView(GridView view, GridColumn keyColumn)
    {
      if (view == null) throw new ArgumentNullException(nameof(view));
      if (keyColumn == null) throw new ArgumentNullException(nameof(keyColumn));

      SetKeyColumn(view, keyColumn);
    }

    #endregion

    #region Store/Restore state methods

    /// <summary>
    /// Stores the view state of the active view on a <see cref="GridControl"/>.
    /// </summary>
    /// <param name="control">The control to store view state for.</param>
    /// <returns></returns>
    public ViewStateToken StoreViewState(GridControl control)
    {
      if (!(control.MainView is GridView gridView))
        throw new ArgumentException("The control.MainView must be of type GridView.");

      return StoreViewState(gridView);
    }

    /// <summary>
    /// Stores the view state of the specified <see cref="GridView"/>.
    /// </summary>
    /// <param name="view">The view to store view state for.</param>
    /// <returns></returns>
    public ViewStateToken StoreViewState(GridView view)
    {
      if (view == null) throw new ArgumentNullException(nameof(view));

      //Create a new ViewState.
      var viewState = ViewState.Create(this, view);
      //Store the state of the view.
      viewState.StoreState(view);
      //Add or update the ViewState for the GridView.
      if (!ViewStates.ContainsKey(view))
        ViewStates.Add(view, viewState);
      else
        ViewStates[view] = viewState;
      //Create a new StateToken for this GridControlState and GridView.
      var token = new ViewStateToken(this, view);
      return token;
    }

    /// <summary>
    /// Restores the saved state for the specified <see cref="GridView"/> if one is stored.
    /// </summary>
    /// <param name="view">The view to store view state for.</param>
    public void RestoreViewState(GridView view)
    {
      if (view == null) throw new ArgumentNullException(nameof(view));
      //Get the ViewState for the GridView.
      if (ViewStates.TryGetValue(view, out var state))
      {
        //If the state is not set, then remove it.
        if (state == null)
        {
          ViewStates.Remove(view);
          return;
        }

        //Restore state.
        state.RestoreState(view);
        ViewStates.Remove(view);
      }
    }

    /// <summary>
    /// Discards the view state associated with the <see cref="ViewStateToken"/>.
    /// </summary>
    /// <param name="token">The token whose view state should be discarded.</param>
    internal void DiscardViewState(ViewStateToken token)
    {
      if (token == null) throw new ArgumentNullException(nameof(token));
      DiscardViewState(token.GridView);
    }

    /// <summary>
    /// Discards the view state associated with the <see cref="GridView"/>.
    /// </summary>
    /// <param name="view">The <see cref="GridView"/> whose view state should be discarded.</param>
    public void DiscardViewState(GridView view)
    {
      if (view == null) throw new ArgumentNullException(nameof(view));
      if (ViewStates.ContainsKey(view))
        ViewStates.Remove(view);
    }

    #endregion
  }
}
