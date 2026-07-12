# Improvised EOSL

> 改修は間に合わない。予算もない。それでも `showModalDialog()` は残っている。

Improvised EOSL は、古い業務 Web アプリケーションを Microsoft Edge WebView2 上で検証するための実験的な Windows デスクトップブラウザーです。

現在の主な到達点は次の 2 つです。

- 廃止された `window.showModalDialog()` の同期呼び出しを、明示的に許可した HTTP(S) origin で再現する。
- 業務アプリの運用で問題になりやすい wrapper 側の戻る、進む、リロード、直接 URL 入力などを、管理用 JSON policy で非表示または一部キーボード抑制できるようにする。

これは Internet Explorer ではありません。Trident の再現、ActiveX、古い TLS、完全な IE DOM 互換、本番サポート、企業配布やロックダウン製品としての保証は対象外です。まずは隔離した検証環境で使ってください。

## まず使う

### ZIP 版

[GitHub Releases](https://github.com/kneco/improvised-eosl/releases) から Windows ZIP を取得し、新しいフォルダーへ ZIP 全体を展開します。展開先のルートにある `ImprovisedEosl.Spike.SyncModal.exe` を起動します。

ZIP 版は .NET SDK 不要ですが、Microsoft Edge WebView2 Runtime は必要です。ZIP 内から直接 EXE を起動したり、EXE だけを別の場所へ移動したりしないでください。隣接する DLL、`config`、`pages` なども実行に必要です。

### ソースから起動

必要なもの:

- Windows 10 または Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)

PowerShell で実行します。

```powershell
git clone https://github.com/kneco/improvised-eosl.git
cd improvised-eosl
dotnet restore
dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj
```

起動すると、アドレスバー付きのブラウザー画面と組み込みホームページが表示されます。一般の HTTP(S) サイトも通常の WebView2 ブラウザーとして開けます。

## 何ができるか

### `showModalDialog()` 互換

中核機能は `window.showModalDialog()` です。許可済み origin で呼び出されたとき、親 JavaScript の呼び出しを同期的にブロックし、別 STA スレッド上の子 WebView2 ダイアログを操作可能な状態で開き、`window.returnValue` を呼び出し元へ返します。

実装済みの範囲:

- `window.dialogArguments` と `window.returnValue`
- Cookie と `localStorage` を含むセッション共有
- 最大 4 階層までの入れ子ダイアログ
- IE モードで測定したダイアログ feature 文字列の一部
- origin 単位の明示的な互換許可と拒否
- 診断ログと、許可済み互換設定の UI からの確認・取り消し

最初の確認は、ホームページの `互換機能テスト` から `Open child dialog` を押して行えます。初回は origin/API の許可確認が表示されるため、`許可してリロード` を選び、もう一度操作してください。詳しい手順は [Sync modal PoC](docs/sync-modal-poc.md) と [MVP readiness](docs/mvp-readiness.md) に記録しています。

### `window.open()` の限定補完

明示的に許可された origin では、`window.open()` の IE 固有 feature のうち、計測済みの一部を限定的に補完します。`scrollbars` と `status` は表示上の判断材料として扱います。`resizable`、`menubar`、`toolbar` は近似または host 依存です。`location`、`fullscreen`、`channelmode` は未対応です。

詳細は [`window.open()` feature reference](docs/window-open-feature-reference-checklist.md) と [dialog feature compatibility](docs/dialog-feature-compatibility.md) を参照してください。

### 管理用 browser shell policy

管理者や検証担当者は JSON policy で wrapper のブラウザー操作面を制御できます。これは互換 API の許可ではなく、運用上の表示・操作 policy です。

主な操作:

```powershell
dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --shell-policy path\to\browser-shell-policy.json

dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --export-shell-policy path\to\visible-policy.json

dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --apply-shell-policy path\to\source.json --shell-policy path\to\target.json

dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --reset-user-settings
```

既定の policy パスは、実行ファイルから見た `config/browser-shell-policy.json` です。`--shell-policy <path>` を指定すると、その実行だけ指定ファイルを使います。

Version 1 policy では、次のような boolean key を使います。

```json
{
  "version": 1,
  "browserShell": {
    "toolbar-primary-toolbar-hidden": false,
    "toolbar-address-entry-hidden": false,
    "toolbar-history-command-hidden": false,
    "toolbar-reload-command-hidden": false,
    "toolbar-go-command-hidden": false,
    "toolbar-settings-command-hidden": false,
    "toolbar-diagnostics-command-hidden": false,
    "keyboard-history-command-disabled": false,
    "keyboard-reload-command-disabled": false
  }
}
```

実装済みの範囲:

- wrapper の primary toolbar 全体の非表示
- address entry、Back/Forward、Reload、Settings、Diagnostics の個別非表示
- `toolbar-go-command-hidden` は version 1 policy 互換のため受け入れられますが、現在の shell では typed address navigation は address entry で Enter を押して実行します
- `Alt+Left`、`Alt+Right`、`Ctrl+R`、`F5` の対象別抑制
- `Ctrl+F` と `F3` の WebView2 find-in-page 維持
- invalid policy の fail-safe standard shell
- policy export/apply と、user-managed 設定だけを戻す reset

重要な境界:

- toolbar を隠しても、ページ内リンク、script navigation、redirect、form submit、アドレスバー経由の移動すべてを止めるわけではありません。
- `keyboard-...-disabled` は対象キーを WPF routed event で `Handled=true` にする実装です。より細かい `CoreWebView2Controller.AcceleratorKeyPressed` 制御は将来検討です。
- 専用 Browser Back/Forward hardware keys は、対象環境にそのキーがある場合の追加検証項目です。
- これは kiosk、DLP、origin allow-list、セキュリティ製品ではありません。

詳しくは [Browser shell policy](docs/browser-shell-policy.md) と [manual validation result](docs/browser-shell-policy-manual-test.md) を参照してください。

## 既存サイトで試す

1. アドレスバーから対象サイトを開きます。
2. サイトが `window.showModalDialog()` を呼ぶと、互換機能を許可するか確認画面が表示されます。
3. `許可してリロード` を選びます。
4. ページの再読み込み後、対象の操作をもう一度行います。

許可は `scheme + host + port` が一致する HTTP(S) origin と API の組み合わせにだけ適用されます。ワイルドカードは使いません。許可・拒否は toolbar の `設定` から確認・取り消しできます。

`file://` で直接開いたページは表示できますが、opaque origin になるため互換機能の許可対象にはなりません。`showModalDialog()` を検証する親ページと子ページは HTTP(S) で配信してください。子 URL の `data:`、`file:`、`javascript:` は安全境界として拒否します。

## 管理用 compatibility profile

許可確認を出さず、指定した URL を起動時に開くには、信頼済みの管理設定として `config/compatibility-profiles.json` に profile を追加します。

```json
{
  "version": 1,
  "profiles": [
    {
      "id": "legacy-order-system",
      "displayName": "Legacy order system",
      "startUrl": "https://orders.example.com/",
      "allowedOrigins": [
        "https://orders.example.com"
      ],
      "compatibility": {
        "showModalDialog": true
      }
    }
  ]
}
```

```powershell
dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --profile=legacy-order-system
```

profile は browser shell policy とは別物です。profile は互換 API の origin 許可を扱い、shell policy は wrapper chrome の表示や一部 accelerator 抑制を扱います。詳細は [Configured compatibility profiles](docs/compatibility-profiles.md) を参照してください。

## 診断ログ

- toolbar の `診断ログ` で画面下部のログ表示を切り替えられます。
- `--show-diagnostics` を付けると、起動時からログを表示します。
- ファイルログは実行ファイルと同じディレクトリの `artifacts/sync-modal-poc.log` に出力されます。

ログには origin、URL path、エラー情報、切り詰められた payload などが含まれる場合があります。取り扱いは [Diagnostics](docs/diagnostics.md) を確認してください。

## build と test

```powershell
dotnet build ImprovisedEosl.sln
```

UI に依存しない policy / compatibility tests:

```powershell
dotnet run --project tests/ImprovisedEosl.Spike.Tests/ImprovisedEosl.Spike.Tests.csproj
```

WebView2 を使う基本自動検証:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --auto
```

配布 ZIP:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/publish-dist.ps1 -Version 0.1.9-mvp
```

生成物は `dist/ImprovisedEosl-0.1.9-mvp-win-x64.zip` です。これは単一 EXE ではなく、WebView2 native loader、HTML、設定ファイルを含む folder-based package です。詳細は [v0.1.9-mvp release note](docs/releases/v0.1.9-mvp.md) を参照してください。

## 明示的な非目標

Improvised EOSL は次を提供しません。

- Internet Explorer や Trident の完全再現
- ActiveX、COM automation、Browser Helper Objects、NPAPI
- 古い TLS や暗号スイート
- 完全な IE DOM / keyboard event 互換
- 任意 origin の navigation allow-list
- kiosk、DLP、lockdown、native close suppression
- 署名、更新、enterprise deployment tooling
- production compatibility、operational support、security guarantee

互換機能、WebView2 制約、残リスクは [Risks and limitations](docs/risks-and-limitations.md)、[Technical feasibility](docs/technical-feasibility.md)、[Architecture](docs/architecture.md) に分けて記録しています。

## ライセンス

プロジェクト独自のコードと文書は [MIT License](LICENSE) で公開しています。配布物に含まれる第三者コンポーネントには、それぞれのライセンスが適用されます。詳細は [Third-party notices](THIRD-PARTY-NOTICES.md) を確認してください。

## 免責

このソフトウェアは実験段階であり、不完全です。互換性、可用性、データ保全、セキュリティ、法令適合性、本番利用への適合性を保証しません。利用に伴う損害、データ損失、サービス停止、セキュリティ事故、その他の結果について、作者は責任を負いません。
