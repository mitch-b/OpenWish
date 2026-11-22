using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OpenWish.Shared.Models;

namespace OpenWish.Web.Client.Components.Wishlist;

/// <summary>
/// Checks the fields an idea needs before it is saved, so problems appear next to the field instead of as a failed save.
/// </summary>
public sealed class IdeaFieldsValidator : ComponentBase, IDisposable
{
    public const string NameRequiredMessage = "Enter a name for this idea.";
    public const string NegativePriceMessage = "Estimated price can't be negative.";

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
            $"{nameof(IdeaFieldsValidator)} must be placed inside an EditForm.");
        _messages = new ValidationMessageStore(_editContext);
        _editContext.OnValidationRequested += HandleValidationRequested;
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        if (_editContext?.Model is not WishlistItemModel item || _messages is null)
        {
            return;
        }

        _messages.Clear();
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _messages.Add(_editContext.Field(nameof(WishlistItemModel.Name)), NameRequiredMessage);
        }

        if (item.Price < 0)
        {
            _messages.Add(_editContext.Field(nameof(WishlistItemModel.Price)), NegativePriceMessage);
        }
    }

    // Clear a field's message as soon as it is edited; the next save checks it again.
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