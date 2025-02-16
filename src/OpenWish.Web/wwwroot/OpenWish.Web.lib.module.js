export function afterStarted(blazor) {
    blazor.registerCustomEventType('openwishpaste', {
        browserEventName: 'paste',
        createEventArgs: event => {
            return {
                eventTimestamp: new Date(),
                pastedData: event.clipboardData.getData('text')
            };
        }
    });
}