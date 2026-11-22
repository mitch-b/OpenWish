using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OpenWish.Shared.Models;

namespace OpenWish.Web.Client.Components.Wishlist;

/// <summary>
/// Requires a wishlist name before saving, so a blank name is reported next to the field instead of creating an unnamed list.
/// </summary>
public sealed class WishlistFormValidator : ComponentBase, IDisposable
{
    public const string NameRequiredMessage = "Enter a name for this wishlist.";

    private EditContext? _editContext;
    private ValidationMessageStore? _messages;

    [CascadingParameter] public EditContext? CurrentEditContext { get; set; }

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(CurrentEditContext, _editContext))
        {
            return;
        }

        Detach();
        _editContext = CurrentEditContext ?? throw new InvalidOperationException(
            $"{nameof(WishlistFormValidator)} must be placed inside an EditForm.");
        _messages = new ValidationMessageStore(_editContext);
        _editContext.OnValidationRequested += HandleValidationRequested;
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        if (_editContext?.Model is not WishlistModel wishlist || _messages is null)
        {
            return;
        }

        _messages.Clear();
        if (string.IsNullOrWhiteSpace(wishlist.Name))
        {
            _messages.Add(_editContext.Field(nameof(WishlistModel.Name)), NameRequiredMessage);
        }
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        if (_messages is null || _editContext is null)
        {
            return;
        }

        _messages.Clear(e.FieldIdentifier);
        _editContext.NotifyValidationStateChanged();
    }

    private void Detach()
    {
        if (_editContext is null)
        {
            return;
        }

        _editContext.OnValidationRequested -= HandleValidationRequested;
        _editContext.OnFieldChanged -= HandleFieldChanged;
        _messages?.Clear();
        _editContext = null;
        _messages = null;
    }

    public void Dispose() => Detach();
}