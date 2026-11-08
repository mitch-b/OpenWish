export function afterWebStarted(blazor) {
    blazor.addEventListener('enhancedload', () => {
        globalThis.openWishInitializeIntegrationPickers();
        globalThis.openWishInitializePasswordToggles();
    });
    globalThis.openWishInitializeIntegrationPickers();
    globalThis.openWishInitializePasswordToggles();

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