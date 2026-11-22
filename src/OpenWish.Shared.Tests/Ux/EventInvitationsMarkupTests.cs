using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class EventInvitationsMarkupTests
{
    private static readonly string[] _invitationsPath = ["OpenWish.Web.Client", "Components", "Event", "EventInvitations.razor"];
    private static readonly string[] _acceptPath = ["OpenWish.Web.Client", "Components", "Pages", "Events", "AcceptInvite.razor"];

    [Fact]
    public void CancelInvitation_AsksForConfirmationBeforeCanceling()
    {
        var markup = ReadComponent(_invitationsPath);

        Assert.Contains("<Dialog @ref=\"_cancelInvitationDialog\" Title=\"Cancel invitation?\"", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"() => PromptCancelInvitation(invitation)\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick=\"() => HandleCancelInvitation(", markup, StringComparison.Ordinal);
        Assert.Contains(">Keep invitation</button>", markup, StringComparison.Ordinal);
        Assert.Contains("$\"cancel-invitation-{_invitationPendingCancel.PublicId}\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void InvitationStatus_UsesWordsAndIconsNotOnlyColor()
    {
        var markup = ReadComponent(_invitationsPath);

        Assert.Contains("<span>@StatusLabel(invitation.Status)</span>", markup, StringComparison.Ordinal);
        Assert.Contains("<OpenWishIcon Name=\"@StatusIcon(invitation.Status)\" />", markup, StringComparison.Ordinal);
        Assert.Contains("\"Rejected\" => \"Declined\"", markup, StringComparison.Ordinal);
        Assert.Contains("\"Pending\" => \"Waiting\"", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Declined</span> <span class=\"invitation-filter-count\">", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("GetStatusBadgeClass", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Rejected (@", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void InvitationControls_WaitForInteractivity()
    {
        var markup = ReadComponent(_invitationsPath);

        Assert.Contains("private bool ActionsDisabled => IsInvitationActionBusy || !RendererInfo.IsInteractive;", markup, StringComparison.Ordinal);
        Assert.Contains("private bool ComposerDisabled => _isSending || !RendererInfo.IsInteractive;", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(DisableSendButton || !RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Equal(4, markup.Split("disabled=\"@(!RendererInfo.IsInteractive)\"", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void CopyLink_ConfirmsOnTheRowThatWasCopied()
    {
        var markup = ReadComponent(_invitationsPath);

        Assert.Contains("@if (_copiedInvitationId == invitation.Id)", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Link copied</span>", markup, StringComparison.Ordinal);
        Assert.Contains("@implements IDisposable", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptInvite_HasOnePrimaryActionAndReassurance()
    {
        var markup = ReadComponent(_acceptPath);
        var styles = ReadComponent([.. _acceptPath[..^1], "AcceptInvite.razor.css"]);

        Assert.Contains("class=\"btn btn-primary accept-invite-primary\"", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"btn btn-link accept-invite-decline\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("btn-success", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"What happens when you accept\"", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Hosted by @HostName</span>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("container py-5", markup, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: anywhere;", styles, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 576px)", styles, StringComparison.Ordinal);
    }

    private static string ReadComponent(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var solutionDirectory = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine([solutionDirectory, .. pathParts]));
    }
}