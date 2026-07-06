using System.Globalization;

namespace ImprovisedEosl.Spike.SyncModal;

public static class UiText
{
    public const string ApplicationSettingsButton = nameof(ApplicationSettingsButton);
    public const string ApplicationSettingsTitle = nameof(ApplicationSettingsTitle);
    public const string ApplicationSettingsIntro = nameof(ApplicationSettingsIntro);
    public const string InitialUrlLabel = nameof(InitialUrlLabel);
    public const string UseHomeButton = nameof(UseHomeButton);
    public const string InitialUrlNextLaunchNote = nameof(InitialUrlNextLaunchNote);
    public const string InitialUrlInvalidTitle = nameof(InitialUrlInvalidTitle);
    public const string InitialUrlInvalidBody = nameof(InitialUrlInvalidBody);
    public const string ApplicationSettingsSaveErrorTitle = nameof(ApplicationSettingsSaveErrorTitle);
    public const string ApplicationSettingsSaveErrorBody = nameof(ApplicationSettingsSaveErrorBody);
    public const string ImportButton = nameof(ImportButton);
    public const string ExportButton = nameof(ExportButton);
    public const string ImportDialogTitle = nameof(ImportDialogTitle);
    public const string ExportDialogTitle = nameof(ExportDialogTitle);
    public const string SettingsJsonFilter = nameof(SettingsJsonFilter);
    public const string PortableDecisionSummary = nameof(PortableDecisionSummary);
    public const string ImportConfirmationTitle = nameof(ImportConfirmationTitle);
    public const string ImportConfirmationBody = nameof(ImportConfirmationBody);
    public const string ImportFailedTitle = nameof(ImportFailedTitle);
    public const string ImportFailedBody = nameof(ImportFailedBody);
    public const string ImportSingleJsonBody = nameof(ImportSingleJsonBody);
    public const string ExportSucceededTitle = nameof(ExportSucceededTitle);
    public const string ExportSucceededBody = nameof(ExportSucceededBody);
    public const string ExportFailedTitle = nameof(ExportFailedTitle);
    public const string ExportFailedBody = nameof(ExportFailedBody);
    public const string HomeValue = nameof(HomeValue);
    public const string CompatibilityDecisionsLabel = nameof(CompatibilityDecisionsLabel);
    public const string SettingsDropTarget = nameof(SettingsDropTarget);
    public const string OriginColumn = nameof(OriginColumn);
    public const string ApiColumn = nameof(ApiColumn);
    public const string DecisionColumn = nameof(DecisionColumn);
    public const string DecisionAllowed = nameof(DecisionAllowed);
    public const string DecisionDenied = nameof(DecisionDenied);
    public const string RevokeButton = nameof(RevokeButton);
    public const string SaveButton = nameof(SaveButton);
    public const string CancelButton = nameof(CancelButton);
    public const string ConsentTitle = nameof(ConsentTitle);
    public const string ConsentBody = nameof(ConsentBody);
    public const string ConsentReloadNote = nameof(ConsentReloadNote);
    public const string ConsentAllow = nameof(ConsentAllow);
    public const string ConsentDeny = nameof(ConsentDeny);
    public const string ConsentAllowSelected = nameof(ConsentAllowSelected);
    public const string ConsentAllowAll = nameof(ConsentAllowAll);
    public const string ConsentDenyAll = nameof(ConsentDenyAll);
    public const string ConsentKnownApisOnly = nameof(ConsentKnownApisOnly);
    public const string DiagnosticsShow = nameof(DiagnosticsShow);
    public const string DiagnosticsHide = nameof(DiagnosticsHide);
    public const string StartupProfileErrorTitle = nameof(StartupProfileErrorTitle);
    public const string StartupProfileMissingId = nameof(StartupProfileMissingId);
    public const string StartupProfileMultiple = nameof(StartupProfileMultiple);
    public const string StartupProfileUnknown = nameof(StartupProfileUnknown);
    public const string StartupProfileAutoConflict = nameof(StartupProfileAutoConflict);
    public const string LocalOpenErrorTitle = nameof(LocalOpenErrorTitle);
    public const string LocalOpenSingleFile = nameof(LocalOpenSingleFile);
    public const string LocalOpenHtmlOnly = nameof(LocalOpenHtmlOnly);
    public const string LocalOpenFailed = nameof(LocalOpenFailed);
    public const string CompatibilityStatusUndecided = nameof(CompatibilityStatusUndecided);
    public const string CompatibilityStatusDetected = nameof(CompatibilityStatusDetected);
    public const string CompatibilityStatusEnabled = nameof(CompatibilityStatusEnabled);
    public const string CompatibilityStatusDenied = nameof(CompatibilityStatusDenied);
    public const string CompatibilityStatusBlocked = nameof(CompatibilityStatusBlocked);
    public const string CompatibilityStatusDetail = nameof(CompatibilityStatusDetail);
    public const string CompatibilityStatusNoApis = nameof(CompatibilityStatusNoApis);
    public const string CompatibilityStatusChecking = nameof(CompatibilityStatusChecking);
    public const string CompatibilityStatusError = nameof(CompatibilityStatusError);
    public const string CompatibilityStatusInitializingDetail = nameof(CompatibilityStatusInitializingDetail);
    public const string CompatibilityStatusRecoveringDetail = nameof(CompatibilityStatusRecoveringDetail);
    public const string CompatibilityStatusRecoveryFailedDetail = nameof(CompatibilityStatusRecoveryFailedDetail);
    public const string CompatibilityStatusDetailTitle = nameof(CompatibilityStatusDetailTitle);
    public const string CloseButton = nameof(CloseButton);

    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        [ApplicationSettingsButton] = "Settings",
        [ApplicationSettingsTitle] = "Settings",
        [ApplicationSettingsIntro] = "Manage the next-launch page and user decisions for legacy compatibility. Trusted configured profiles are separate and cannot be changed here.",
        [InitialUrlLabel] = "Initial URL (HTTP or HTTPS)",
        [UseHomeButton] = "Use home",
        [InitialUrlNextLaunchNote] = "The change takes effect on the next normal launch. The current page will not be moved.",
        [InitialUrlInvalidTitle] = "Invalid initial URL",
        [InitialUrlInvalidBody] = "Enter an absolute HTTP or HTTPS URL without user information, or leave the field empty to use home.",
        [ApplicationSettingsSaveErrorTitle] = "Could not save application settings",
        [ApplicationSettingsSaveErrorBody] = "The application settings could not be saved. No change was applied.\n\n{0}",
        [ImportButton] = "Import",
        [ExportButton] = "Export",
        [ImportDialogTitle] = "Import user settings",
        [ExportDialogTitle] = "Export user settings",
        [SettingsJsonFilter] = "JSON settings (*.json)|*.json",
        [PortableDecisionSummary] = "Staged compatibility decisions: {0} allowed, {1} denied. Import replaces this user-managed list only when Save is chosen.",
        [ImportConfirmationTitle] = "Import user settings?",
        [ImportConfirmationBody] = "Initial URL: {0}\nAllowed decisions: {1}\nDenied decisions: {2}\n\nReplace the staged user settings with this file? Trusted configured profiles are not affected.",
        [ImportFailedTitle] = "Could not import user settings",
        [ImportFailedBody] = "The settings file was rejected. No changes were staged.\n\n{0}",
        [ImportSingleJsonBody] = "Drop one .json settings file at a time.",
        [ExportSucceededTitle] = "User settings exported",
        [ExportSucceededBody] = "The staged user settings were exported. Trusted configured profiles and browser data were not included.",
        [ExportFailedTitle] = "Could not export user settings",
        [ExportFailedBody] = "The settings file could not be written.\n\n{0}",
        [HomeValue] = "Built-in home",
        [CompatibilityDecisionsLabel] = "Legacy compatibility decisions",
        [SettingsDropTarget] = "Drop one exported settings JSON file here to import",
        [OriginColumn] = "Origin",
        [ApiColumn] = "API",
        [DecisionColumn] = "Decision",
        [DecisionAllowed] = "Allowed",
        [DecisionDenied] = "Denied",
        [RevokeButton] = "Revoke selected",
        [SaveButton] = "Save",
        [CancelButton] = "Cancel",
        [ConsentTitle] = "Legacy API detected",
        [ConsentBody] = "This site attempted to use {0}. Allow compatibility mode for this origin?\n\n{1}",
        [ConsentReloadNote] = "If allowed, the page will reload. Depending on the site, you may need to start the operation again from the top page.",
        [ConsentAllow] = "Allow and reload",
        [ConsentDeny] = "Deny",
        [ConsentAllowSelected] = "Allow selected",
        [ConsentAllowAll] = "Allow all known",
        [ConsentDenyAll] = "Deny all",
        [ConsentKnownApisOnly] = "Allow all applies only to the compatibility features listed above. Future features require new consent.",
        [DiagnosticsShow] = "Diagnostic log",
        [DiagnosticsHide] = "Hide log",
        [StartupProfileErrorTitle] = "Could not select startup profile",
        [StartupProfileMissingId] = "Specify a profile ID after --profile or use --profile=<id>.",
        [StartupProfileMultiple] = "Specify only one startup profile.",
        [StartupProfileUnknown] = "Startup profile '{0}' was not found. Check the compatibility profile file.",
        [StartupProfileAutoConflict] = "Startup profile selection cannot be combined with an automatic validation mode.",
        [LocalOpenErrorTitle] = "Could not open local HTML",
        [LocalOpenSingleFile] = "Drop one local HTML file at a time.",
        [LocalOpenHtmlOnly] = "Only local .html and .htm files can be opened.",
        [LocalOpenFailed] = "The local HTML file could not be opened. Check that the file still exists and is readable.",
        [CompatibilityStatusUndecided] = "Compatibility: undecided",
        [CompatibilityStatusDetected] = "Compatibility: detected",
        [CompatibilityStatusEnabled] = "Compatibility: enabled",
        [CompatibilityStatusDenied] = "Compatibility: denied",
        [CompatibilityStatusBlocked] = "Compatibility: blocked",
        [CompatibilityStatusDetail] = "{0}. Origin: {1}. Enabled APIs: {2}. Denied APIs: {3}. Detected APIs: {4}.",
        [CompatibilityStatusNoApis] = "none",
        [CompatibilityStatusChecking] = "Compatibility: checking",
        [CompatibilityStatusError] = "Compatibility: error",
        [CompatibilityStatusInitializingDetail] = "Compatibility status is not available while the browser initializes.",
        [CompatibilityStatusRecoveringDetail] = "Compatibility status is not available while the browser recovers.",
        [CompatibilityStatusRecoveryFailedDetail] = "Browser recovery failed. This is an operational error, not a compatibility permission decision.",
        [CompatibilityStatusDetailTitle] = "Compatibility status",
        [CloseButton] = "Close"
    };

    private static readonly IReadOnlyDictionary<string, string> Japanese = new Dictionary<string, string>
    {
        [ApplicationSettingsButton] = "設定",
        [ApplicationSettingsTitle] = "設定",
        [ApplicationSettingsIntro] = "次回起動時のページとレガシー互換機能のユーザー判断を管理します。信頼済み互換プロファイルは別管理のため、ここでは変更できません。",
        [InitialUrlLabel] = "初期表示URL（HTTPまたはHTTPS）",
        [UseHomeButton] = "ホームを使用",
        [InitialUrlNextLaunchNote] = "変更は次回の通常起動から有効になります。現在のページは移動しません。",
        [InitialUrlInvalidTitle] = "初期表示URLが正しくありません",
        [InitialUrlInvalidBody] = "ユーザー情報を含まないHTTPまたはHTTPSの絶対URLを入力するか、ホームを使用する場合は空欄にしてください。",
        [ApplicationSettingsSaveErrorTitle] = "設定を保存できませんでした",
        [ApplicationSettingsSaveErrorBody] = "設定を保存できなかったため、変更は適用されていません。\n\n{0}",
        [ImportButton] = "インポート",
        [ExportButton] = "エクスポート",
        [ImportDialogTitle] = "ユーザー設定をインポート",
        [ExportDialogTitle] = "ユーザー設定をエクスポート",
        [SettingsJsonFilter] = "JSON設定 (*.json)|*.json",
        [PortableDecisionSummary] = "編集中の互換機能設定: 許可 {0} 件、拒否 {1} 件。インポート内容は「保存」を選ぶまで反映されません。",
        [ImportConfirmationTitle] = "ユーザー設定をインポートしますか？",
        [ImportConfirmationBody] = "初期表示URL: {0}\n許可: {1} 件\n拒否: {2} 件\n\n編集中のユーザー設定をこの内容で置き換えますか？信頼済み互換プロファイルは変更されません。",
        [ImportFailedTitle] = "ユーザー設定をインポートできませんでした",
        [ImportFailedBody] = "設定ファイルを受け入れられませんでした。変更は適用されていません。\n\n{0}",
        [ImportSingleJsonBody] = ".json設定ファイルを1つだけドロップしてください。",
        [ExportSucceededTitle] = "ユーザー設定をエクスポートしました",
        [ExportSucceededBody] = "編集中のユーザー設定をエクスポートしました。信頼済み互換プロファイルとブラウザーデータは含まれません。",
        [ExportFailedTitle] = "ユーザー設定をエクスポートできませんでした",
        [ExportFailedBody] = "設定ファイルを書き込めませんでした。\n\n{0}",
        [HomeValue] = "組み込みホーム",
        [CompatibilityDecisionsLabel] = "レガシー互換機能の許可・拒否",
        [SettingsDropTarget] = "エクスポートした設定JSONをここにドロップしてインポート",
        [OriginColumn] = "Origin",
        [ApiColumn] = "API",
        [DecisionColumn] = "決定",
        [DecisionAllowed] = "許可",
        [DecisionDenied] = "拒否",
        [RevokeButton] = "選択した許可を取り消す",
        [SaveButton] = "保存",
        [CancelButton] = "キャンセル",
        [ConsentTitle] = "レガシー互換機能を検出しました",
        [ConsentBody] = "このサイトが {0} を使用しようとしました。このオリジンで使用する互換機能を選択してください。\n\n{1}",
        [ConsentReloadNote] = "showModalDialog を許可した場合はページを再読み込みします。サイトによってはトップページから操作し直してください。",
        [ConsentAllow] = "許可してリロード",
        [ConsentDeny] = "許可しない",
        [ConsentAllowSelected] = "選択した機能を許可",
        [ConsentAllowAll] = "表示中の機能をすべて許可",
        [ConsentDenyAll] = "すべて許可しない",
        [ConsentKnownApisOnly] = "「すべて許可」は上に表示された互換機能だけが対象です。将来追加される機能は改めて確認します。",
        [DiagnosticsShow] = "診断ログ",
        [DiagnosticsHide] = "ログを閉じる",
        [StartupProfileErrorTitle] = "起動プロファイルを選択できません",
        [StartupProfileMissingId] = "--profile の後にプロファイルIDを指定するか、--profile=<id> を使用してください。",
        [StartupProfileMultiple] = "起動プロファイルは1つだけ指定してください。",
        [StartupProfileUnknown] = "起動プロファイル「{0}」が見つかりません。互換プロファイル設定を確認してください。",
        [StartupProfileAutoConflict] = "起動プロファイル選択と自動検証モードは同時に使用できません。",
        [LocalOpenErrorTitle] = "ローカルHTMLを開けませんでした",
        [LocalOpenSingleFile] = "ローカルHTMLファイルを1つだけドロップしてください。",
        [LocalOpenHtmlOnly] = "開けるローカルファイルは .html または .htm だけです。",
        [LocalOpenFailed] = "ローカルHTMLを開けませんでした。ファイルが存在し、読み取り可能か確認してください。",
        [CompatibilityStatusUndecided] = "互換: 未決定",
        [CompatibilityStatusDetected] = "互換: 検出済み",
        [CompatibilityStatusEnabled] = "互換: 有効",
        [CompatibilityStatusDenied] = "互換: 拒否",
        [CompatibilityStatusBlocked] = "互換: ブロック",
        [CompatibilityStatusDetail] = "{0}。Origin: {1}。有効なAPI: {2}。拒否したAPI: {3}。検出したAPI: {4}。",
        [CompatibilityStatusNoApis] = "なし",
        [CompatibilityStatusChecking] = "互換: 確認中",
        [CompatibilityStatusError] = "互換: エラー",
        [CompatibilityStatusInitializingDetail] = "ブラウザーの初期化中は互換状態を確認できません。",
        [CompatibilityStatusRecoveringDetail] = "ブラウザーの復旧中は互換状態を確認できません。",
        [CompatibilityStatusRecoveryFailedDetail] = "ブラウザーの復旧に失敗しました。これは互換機能の許可・拒否を表す状態ではありません。",
        [CompatibilityStatusDetailTitle] = "互換状態",
        [CloseButton] = "閉じる"
    };

    public static string Get(string key)
    {
        return Japanese.TryGetValue(key, out var value)
            ? value
            : English[key];
    }

    public static string Format(string key, params object[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
    }
}
