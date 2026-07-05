# Improvised EOSL

> 改修は間に合わない。予算もない。それでも `showModalDialog()` は残っている。

古い業務Webアプリケーションの一部を、Microsoft Edge WebView2上で動かすための実験的なWindowsデスクトップブラウザーです。最初の技術MVPでは、廃止された `window.showModalDialog()` の同期動作を中心に成立性を検証しました。現在は、明示的なorigin許可の下で `window.open()` の一部のIE固有featureも限定的に補完します。

これはInternet Explorerではなく、実運用を保証する製品でもありません。まずは隔離した検証環境で使用してください。

## まず動かす

### ZIP版（いちばん簡単）

[GitHub Releases](https://github.com/kneco/improvised-eosl/releases)からWindows ZIPを取得し、ZIP全体を展開して `ImprovisedEosl.Spike.SyncModal.exe` をダブルクリックします。.NET SDKは不要です。ZIP内から直接EXEを開かないでください。隣接する `config` と `pages` フォルダーが必要です。

ZIP版にもMicrosoft Edge WebView2 Runtimeは必要です。通常のMicrosoft Edgeが導入されたWindows環境では、すでに利用できることがあります。

### 必要なもの

- Windows 10またはWindows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)

通常のMicrosoft Edgeが導入されたWindows環境では、WebView2 Runtimeもすでに利用できることがあります。

### 起動

PowerShellで次を実行します。

```powershell
git clone https://github.com/kneco/improvised-eosl.git
cd improvised-eosl
dotnet restore
dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj
```

すでにリポジトリを取得済みなら、最後の2行だけで起動できます。

起動すると、アドレスバーを備えたブラウザー画面とホームページが表示されます。画面下部の診断ログは通常は隠れています。

ツールバーの `設定` では、次回の通常起動時に開くHTTP(S) URLと、ユーザーが判断した互換機能の許可・拒否をまとめて管理できます。初期URLを空欄に戻すと組み込みホームを使用します。保存時に現在のページは移動しません。これらのユーザー設定はJSONへエクスポートでき、同じ画面のインポート操作または表示されたドロップ領域へのD&Dで復元できます。信頼済み互換プロファイル、Cookie、WebView2データは含まれません。

## 最初の動作確認

1. ホームページで `互換機能テスト` を開きます。
2. `Open child dialog` を押します。
3. 初回に許可確認が表示されたら `許可してリロード` を選び、もう一度 `Open child dialog` を押します。
4. 子ダイアログの入力欄へ任意の文字を入力します。
5. 子画面を操作できる一方、親ページの更新が止まっていることを確認します。
6. `Return value and close` を押します。
7. 子画面が閉じ、親ページへJSONの戻り値が同期的に表示されれば成功です。

`Open Google` を押すか、アドレスバーへURLを入力すると、互換機能を使わない一般サイトも閲覧できます。

より詳しい確認項目は[手動検証手順](docs/sync-modal-poc.md#manual-validation-steps)を参照してください。

## 既存サイトで試す

1. アドレスバーから対象サイトを開きます。
2. サイトが `window.showModalDialog()` を呼ぶと、互換機能を許可するか確認画面が表示されます。
3. `許可してリロード` を選びます。
4. ページの再読み込み後、対象の操作をもう一度行います。

許可は `スキーム + ホスト + ポート` が完全に一致するoriginと、互換APIの組み合わせにだけ適用されます。ワイルドカードは使用しません。許可・拒否はツールバーの `設定` から確認・解除できます。

最初の呼び出しは許可確認と再読み込みによって中断されます。サイトによっては、トップページから操作し直す必要があります。

### ローカルHTMLについて

`file://`で直接開いたページは表示できますが、opaque originになるため互換機能の許可対象にはなりません。`showModalDialog()`を検証する親ページと子ページはHTTP(S)で配信してください。子URLの`data:`、`file:`、`javascript:`は安全境界として拒否します。

中核となる互換APIは`window.showModalDialog()`です。加えて、`window.open()`のIE固有featureのうち、計測済みの`scrollbars`と`status`を明示的なorigin許可の下で補完します。dummy launcher向けの`window.close handoff`は別の互換判断であり、許可しても任意のページがアプリを終了できるわけではありません。同一originの第一子候補がある場合だけ、既存画面をそのURLへ遷移させます。`resizable`、`menubar`、`toolbar`は近似またはホスト依存、`location`、`fullscreen`、`channelmode`は未対応です。ActiveXとTridentの表示互換は対象外です。

ホームの`レガシーECサンプル`は、親ページと数量入力ダイアログを同一originのHTTPページとして分離した比較デモです。初回のカート追加では互換許可とリロードが入り、その後の操作で数量が同期的にカートへ返ります。

## 管理用プロファイル

許可確認を出さず、指定したURLを起動時に開くには、次のファイルへプロファイルを追加します。

```text
src/ImprovisedEosl.Spike.SyncModal/config/compatibility-profiles.json
```

例:

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

プロファイルIDを指定して起動します。

```powershell
dotnet run --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --profile=legacy-order-system
```

設定ファイルは信頼済みの管理設定として扱われます。originにパスやワイルドカードは指定できません。詳細は[互換プロファイル仕様](docs/compatibility-profiles.md)を参照してください。

## 診断ログ

- ツールバーの `診断ログ` で画面下部のログ表示を切り替えられます。
- `--show-diagnostics` を付けると、起動時からログを表示します。
- ファイルログは実行ファイルと同じディレクトリの `artifacts/sync-modal-poc.log` に出力されます。

ログにはURLパスやエラー情報などが含まれる場合があります。取り扱いは[診断ログ仕様](docs/diagnostics.md)を確認してください。

## テスト

UIに依存しないポリシーテスト:

```powershell
dotnet run --project tests/ImprovisedEosl.Spike.Tests/ImprovisedEosl.Spike.Tests.csproj
```

WebView2を使う基本自動検証:

```powershell
dotnet run --no-build --project src/ImprovisedEosl.Spike.SyncModal/ImprovisedEosl.Spike.SyncModal.csproj -- --auto
```

すべての自動検証モードは[同期モーダルPoC](docs/sync-modal-poc.md#automatic-validation)に記載しています。

## 配布ZIPを作る

自己完結型の64-bit Windows版を生成します。受け取る側に.NET SDKは不要です。

```powershell
powershell -ExecutionPolicy Bypass -File scripts/publish-dist.ps1 -Version 0.1.7-mvp
```

生成物:

```text
dist/ImprovisedEosl-0.1.7-mvp-win-x64.zip
```

これは単一EXEではなく、WebView2のネイティブローダー、HTML、設定ファイルを含むフォルダー形式です。ZIPを展開した後はEXEを直接起動できます。

## MVPの範囲

実装済みの中心機能:

- 同期的な `window.showModalDialog()` 呼び出し
- 別STAスレッド上で動作する子WebView2
- `window.dialogArguments` と `window.returnValue`
- Cookieと `localStorage` を含むセッション共有
- IEモードで測定したダイアログfeature文字列の一部
- origin単位の明示的な互換許可

対象外:

- Internet ExplorerやTridentの完全再現
- ActiveX、NPAPI、古いTLSや暗号スイート
- 完全なIE DOM互換
- 本番サポートや企業配布機能

技術的な成立性、既知の制約、検証結果は[MVP readiness decision](docs/mvp-readiness.md)と[v0.1.7-mvp release notes](docs/releases/v0.1.7-mvp.md)にまとめています。

## ライセンス

プロジェクト独自のコードと文書は[MIT License](LICENSE)で公開しています。配布物に含まれる第三者コンポーネントには、それぞれのライセンスが適用されます。詳細は[Third-party notices](THIRD-PARTY-NOTICES.md)を確認してください。

## 免責

このソフトウェアは実験段階であり、不完全です。互換性、可用性、データ保全、セキュリティ、法令適合性、本番利用への適合性を保証しません。利用に伴う損害、データ損失、サービス停止、セキュリティ事故、その他の結果について、作者は責任を負いません。
