Improvised EOSL - 実験版MVP
==========================

このアプリケーションは、window.showModalDialog() を使用する古い業務Web
アプリケーションのための、実験的なWindows互換ブラウザーです。

起動方法
--------

1. ZIPファイル全体を、書き込み可能なフォルダーへ展開します。
2. ImprovisedEosl.Spike.SyncModal.exe をダブルクリックします。
3. ホームページで「互換機能テスト」を開きます。
4. 「Open child dialog」を押します。
5. 初回の許可確認で「許可してリロード」を選び、もう一度同じ操作をします。
6. 子画面へ文字を入力し、「Return value and close」を押します。
7. 親ページへ同期的なJSON戻り値が表示されれば成功です。

ホームの「レガシーECサンプル」では、商品追加、数量ダイアログ、同期戻り値の
カート反映を一連の画面で確認できます。

ZIPの中からEXEを直接開かないでください。EXEと同じ場所にあるconfig、pages
などのファイルも動作に必要です。

動作環境
--------

- 64-bit版Windows 10またはWindows 11
- Microsoft Edge WebView2 Runtime
  https://developer.microsoft.com/microsoft-edge/webview2/

.NET 8 Runtimeはこの配布物に含まれているため、別途導入する必要はありません。

ローカルHTML
-------------

file://で直接開いたページは表示できますが、互換機能は有効になりません。
showModalDialogを使う親ページと子ページはHTTPまたはHTTPSで配信してください。
子画面のdata:、file:、javascript: URLは安全のため拒否されます。

既存サイトで試す
----------------

アドレスバーへ対象サイトのURLを入力します。サイトが廃止済みAPIを呼び出すと、
そのoriginでshowModalDialog互換を有効にするか確認画面が表示されます。
「許可してリロード」を選択し、元の操作をもう一度行ってください。

許可は画面に表示されたスキーム、ホスト、ポートだけに適用されます。ツールバーの
「設定」から、ユーザーが許可・拒否したoriginを確認・解除できます。

重要な制限
----------

これはInternet Explorerではありません。Trident、ActiveX、古いTLS、完全なIE DOM
は再現しません。本番運用や業務継続を保証する製品ではないため、隔離した検証環境
だけで使用してください。

診断ログにはサイトのパスやエラー情報が含まれる場合があります。ログは
アプリケーションと同じ場所のartifacts\sync-modal-poc.logへ保存されます。

Project: https://github.com/kneco/improvised-eosl

ライセンス
----------

プロジェクト独自部分はMIT Licenseです。LICENSE.txtを参照してください。
第三者コンポーネントの条件はTHIRD-PARTY-NOTICES.txtを参照してください。
