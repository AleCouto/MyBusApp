let visibilityHandler;

export function observeVisibility(dotNetReference) {
    stopObservingVisibility();
    visibilityHandler = () => {
        dotNetReference.invokeMethodAsync(
            "SetDocumentVisibilityAsync",
            document.visibilityState === "visible");
    };
    document.addEventListener("visibilitychange", visibilityHandler);
    return document.visibilityState === "visible";
}

export function stopObservingVisibility() {
    if (visibilityHandler) {
        document.removeEventListener("visibilitychange", visibilityHandler);
        visibilityHandler = undefined;
    }
}
