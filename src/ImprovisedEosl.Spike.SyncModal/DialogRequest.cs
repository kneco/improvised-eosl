using ImprovisedEosl.Core;
using ImprovisedEosl.ModalDialog;

namespace ImprovisedEosl.Spike.SyncModal;

public sealed record DialogRequest(
    Uri Url,
    string SerializedArguments,
    string Features,
    DialogWindowOptions WindowOptions,
    string UserDataFolder,
    CompatibilityOriginPolicy CompatibilityPolicy,
    int Depth,
    nint OwnerWindowHandle,
    Action<string> Log,
    TimeSpan? NativeCloseDelay = null,
    bool CrashRendererAfterNavigation = false,
    bool CrashBrowserAfterNavigation = false,
    bool HangRendererAfterNavigation = false);
