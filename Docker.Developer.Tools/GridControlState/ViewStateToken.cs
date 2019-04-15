using DevExpress.XtraGrid.Views.Grid;
using System;

namespace Docker.Developer.Tools.GridControlState
{
  /// <summary>
  /// Represents a saved view state.
  /// </summary>
  //TODO: Implementer discard metode.
  public sealed class ViewStateToken : IDisposable
  {
    // Flag: Has Dispose already been called?
    private bool _disposed = false;
    private bool _stateRestored = false;

    internal ViewStateToken(GridControlState controlState, GridView view)
    {
      IsDisposed = false;
      ControlState = controlState;
      GridView = view;
    }

    /// <summary>
    /// Gets wether the instance is disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets whether the state has been discarded.
    /// </summary>
    public bool StateDiscarded { get; private set; } = false;

    /// <summary>
    /// Gets the <see cref="GridControlState"/> that created the token.
    /// </summary>
    public GridControlState ControlState { get; private set; }

    /// <summary>
    /// Gets the <see cref="GridView"/> that is associated with the token.
    /// </summary>
    public GridView GridView { get; private set; }

    /// <summary>
    /// Restores the saved state to the <see cref="GridView"/>.
    /// </summary>
    public void RestoreState()
    {
      if (StateDiscarded) throw new InvalidOperationException($"Cannot call \"{nameof(RestoreState)}\" when state has been discarded!");
      if (!_stateRestored && ControlState != null)
        ControlState.RestoreViewState(GridView);

      _stateRestored = true;
    }

    /// <summary>
    /// Discards the state thereby preventing an automatic restore during dispose.
    /// </summary>
    public void DiscardState()
    {
      ControlState.DiscardViewState(this);
      StateDiscarded = true;
    }

    /// <summary>
    /// Disposes the token thereby restoring the saved state.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the token thereby restoring the saved state.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources;
    /// false to release only unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
      if (_disposed) return;

      if (disposing)
      {
        if (!StateDiscarded) RestoreState();
        ControlState = null;
        GridView = null;
        IsDisposed = true;
      }

      _disposed = true;
    }
  }
}
