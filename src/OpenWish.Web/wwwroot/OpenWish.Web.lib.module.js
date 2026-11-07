export function afterWebStarted(blazor) {
    blazor.addEventListener('enhancedload', () => globalThis.openWishInitializeIntegrationPickers());
    globalThis.openWishInitializeIntegrationPickers();

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