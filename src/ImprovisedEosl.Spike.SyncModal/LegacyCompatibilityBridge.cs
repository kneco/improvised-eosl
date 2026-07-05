using Microsoft.Web.WebView2.Core;

namespace ImprovisedEosl.Spike.SyncModal;

internal static class LegacyCompatibilityBridge
{
    public static async Task InstallAsync(CoreWebView2 core, CompatibilityBroker broker)
    {
        core.AddHostObjectToScript("compatibilityBroker", broker);
        await core.AddScriptToExecuteOnDocumentCreatedAsync(
            """
            (() => {
              const broker = chrome.webview.hostObjects.sync.compatibilityBroker;
              const apiName = "window.showModalDialog";
              const windowOpenApiName = "window.open features";
              const topLevelCloseApiName = "window.close handoff";
              const origin = location.origin;
              let stagedWindowOpen = null;

              const nativeWindowOpen = window.open;
              Object.defineProperty(window, "open", {
                configurable: true,
                writable: true,
                value: function open(url, target, features) {
                  const targetText = String(target ?? "");
                  const featuresText = String(features ?? "");
                  if (broker.IsWindowOpenFeaturesAllowed(origin)) {
                    broker.CaptureWindowOpenFeatures(
                      origin,
                      targetText,
                      featuresText
                    );
                  } else if (featuresText.length > 0) {
                    broker.DetectLegacyApi(origin, windowOpenApiName);
                    return null;
                  }

                  const urlText = String(url ?? "");
                  if (urlText.length > 0 &&
                      !broker.IsTopLevelCloseHandoffDenied(origin)) {
                    const absoluteUrl = new URL(urlText, location.href).href;
                    const token = broker.StageTopLevelCloseHandoff(
                      origin,
                      absoluteUrl,
                      targetText,
                      featuresText
                    );
                    if (token) {
                      const openArguments = Array.from(arguments);
                      openArguments[0] = "about:blank";
                      const child = nativeWindowOpen.apply(this, openArguments);
                      const timer = setTimeout(() => {
                        broker.ReleaseTopLevelCloseHandoff(origin, token, "normal-popup");
                        if (stagedWindowOpen?.token === token) {
                          stagedWindowOpen = null;
                        }
                        if (child && !child.closed) {
                          child.location.replace(absoluteUrl);
                        }
                      }, 0);
                      stagedWindowOpen = { token, child, timer };
                      return child;
                    }
                  }
                  return nativeWindowOpen.apply(this, arguments);
                }
              });

              const nativeWindowClose = window.close;
              Object.defineProperty(window, "close", {
                configurable: true,
                writable: true,
                value: function close() {
                  const staged = stagedWindowOpen;
                  if (staged) {
                    clearTimeout(staged.timer);
                    stagedWindowOpen = null;
                    try {
                      staged.child?.close();
                    } catch {
                      // The host also closes any discovery window before prompting.
                    }
                  }
                  if (broker.IsTopLevelCloseHandoffAllowed(origin)) {
                    return nativeWindowClose.apply(this, arguments);
                  }
                  if (staged) {
                    broker.ReleaseTopLevelCloseHandoff(origin, staged.token, "close-detection");
                  }
                  broker.DetectLegacyApi(origin, topLevelCloseApiName);
                  return undefined;
                }
              });

              if (typeof window.showModalDialog === "function") {
                return;
              }

              if (!broker.IsShowModalDialogAllowed(origin)) {
                Object.defineProperty(window, "showModalDialog", {
                  configurable: true,
                  writable: true,
                  value: function showModalDialog() {
                    broker.DetectLegacyApi(origin, apiName);
                    return undefined;
                  }
                });
                return;
              }

              Object.defineProperty(window, "showModalDialog", {
                configurable: true,
                writable: true,
                value: function showModalDialog(url, args, features) {
                  const absoluteUrl = new URL(url, location.href).href;
                  const serializedArguments = JSON.stringify(args ?? null);
                  if (serializedArguments === undefined) {
                    throw new TypeError("showModalDialog arguments must be JSON-compatible");
                  }
                  const serializedResult =
                    broker.ShowDialog(
                      origin,
                      absoluteUrl,
                      serializedArguments,
                      String(features ?? "")
                    );

                  if (serializedResult == null || serializedResult === "undefined") {
                    return undefined;
                  }

                  return JSON.parse(serializedResult);
                }
              });
            })();
            """);
    }
}
