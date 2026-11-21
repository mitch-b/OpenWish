export function afterWebStarted(blazor) {
    function initializePage() {
        globalThis.openWishInitializeIntegrationPickers();
        globalThis.openWishInitializePasswordToggles();
        globalThis.openWishInitializeGuestAccess();
        globalThis.openWishInitializeAppInstall();
    }
    blazor.addEventListener('enhancedload', initializePage);
    initializePage();

    if (globalThis.openWishPasteRegistered) {
        return;
    }

    blazor.registerCustomEventType('openwishpaste', {
        browserEventName: 'paste',
        createEventArgs: event => {
            return {
                eventTimestamp: new Date(),
                pastedData: event.clipboardData.getData('text')
            };
        }
    });
    globalThis.openWishPasteRegistered = true;
}