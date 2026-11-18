using Microsoft.AspNetCore.Components;

namespace Server.UI.Components.Checkbox;

public class CheckboxBase : ComponentBase
{
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public bool Value { get; set; }
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }

    protected async Task OnClick()
    {
        Value = !Value;
        await ValueChanged.InvokeAsync(Value);
    }
}