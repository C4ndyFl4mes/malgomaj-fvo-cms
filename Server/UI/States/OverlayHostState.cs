using System.Collections.ObjectModel;
using System.ComponentModel;
using Server.Enums;
using Server.Models;

namespace Server.UI.States;

public class OverlayHostState : INotifyPropertyChanged
{
    private readonly List<OverlayModel> _stack = [];
    private readonly ReadOnlyCollection<OverlayModel> _stackView;

    public OverlayHostState()
    {
        _stackView = _stack.AsReadOnly();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<OverlayModel> Stack => _stackView;

    public void Append(OverlayModel overlay)
    {
        if (!_stack.Any(stackOverlay => stackOverlay.Id == overlay.Id))
        {
            _stack.Add(overlay);
            PropertyChanged?.Invoke(this, new(nameof(Stack)));
        }
    }

    public void Detach(Guid id)
    {
        if (_stack.RemoveAll(overlay => overlay.Id == id) > 0)
        {
            PropertyChanged?.Invoke(this, new(nameof(Stack)));
        }
    } 
}

public sealed class OverlayCloseContext(Func<OverlayCloseReason, Task> requestClose)
{
    public Task CloseAsync(OverlayCloseReason reason = OverlayCloseReason.Closed)
        => requestClose(reason);
}