# miles-team/reqifsharp（STARIONGROUP/reqifsharp のフォーク）の運用

このリポジトリは、Next Design の ReqIF 連携向けに、ReqIFSharp へ最小限のパッチを当てたパッケージを作るためのフォークです。パッチは本家へも PR で提案しており、本家に取り込まれて公式版が出たら、このフォークのパッケージは使わなくなります。

## ブランチ

| ブランチ | 役割 |
|---|---|
| `development`、`master` | 本家のミラー。本家と同期するだけで、自前のコミットは入れない。本家へ PR を出すときの起点になる |
| `nd/patches` | 長命ブランチ。「本家のタグ + パッチのコミット」の線形の履歴を保つ。ブランチ名に版は入れない |

パッチを変えるときは、`nd/patches` から作業ブランチを切り、PR で `nd/patches` に取り込む。

パッチのコミットには、版に依存する記述（csproj の `<Version>`、改変表示の本家の版・コミット）を入れない。本家が進んだときの載せ替えで、書き換えが要らないようにするため。

## タグ

版はブランチ名ではなくタグで表す。nupkg を作ったコミットに、nupkg と同じ名前のタグ `<本家の版>-nd.<n>` を打つ。タグを見れば、どの版に何を当てて、どの nupkg を作ったかが分かる。

| タグ | 本家の版 | パッチの内容 |
|---|---|---|
| `10.0.1-nd.1` | `10.0.1`（`9a4f4b9`） | 接頭辞のない `<object>` 要素も外部オブジェクトとして検出する（`AttributeValueXHTML`）。reqifz 内のエントリを、先頭フォルダ・区切り文字・URL エンコードの違いを許して解決する（`ExternalObject.QueryLocalData`） |

- タグは、Next Design の法的通知の参照先（`https://github.com/miles-team/reqifsharp/tree/<タグ>`）や、3rdPartyLibs の README が指すので、公開した後は動かさない・消さない。
- nupkg には、作ったコミットの ID が入る（nuspec の `repository` の `commit`）。タグのコミットが `nd/patches` の履歴に残るように、PR は、コミットの SHA が変わらない方法（`git push origin <PR の先頭のコミット>:nd/patches` の早送り、または Merge commit）で取り込む。GitHub の「Rebase and merge」「Squash and merge」は SHA が変わるので、使った場合は、マージ後にタグを `nd/patches` の先頭に付け直し、nupkg を作り直す。

## パッケージの作り方

タグを checkout して、次を実行する。csproj には `<Version>` を書いていないので、`-p:Version` は必須。

```
dotnet pack ReqIFSharp/ReqIFSharp.csproj -c Release -p:Version=<本家の版>-nd.<n>
```

作った nupkg は 3rdPartyLibs の `nuget-packages` に格納し、NextDesign-ReqIF の PackageReference では `Version="[<版>]"` のように厳密に指定する。`Version="10.0.1-nd.1"` のような最小指定だと、パッチ版を取得できない環境で、警告（NU1603）だけで公式版 `10.0.1` に解決されてしまう。厳密指定なら、取得できない環境ではエラー（NU1102）で止まる。

## 本家が進んだとき

1. 本家を取り込む。`git fetch upstream --tags`（`upstream` は STARIONGROUP/reqifsharp）。`development` と `master` を本家に合わせる（早送りだけ。自前のコミットは入れない）。
2. パッチを載せ替える。`git rebase -X ignore-space-at-eol --onto <新しい本家のタグ> <古い本家のタグ> nd/patches`。`-X ignore-space-at-eol` は、本家が改行コード（CRLF／LF）を変えたファイルで衝突しないようにする（10.0.1 → 10.1.1 では `ExternalObject.cs` が CRLF から LF に変わっている）。実績: 10.0.1 → 10.1.1 は、このオプションを付けると衝突なしで載り、`ReqIFSharp.Tests` が 318/318 合格した。付けないと、`ExternalObject.cs` で衝突する。
3. 本家に同じ修正が取り込まれていたら、そのパッチのコミットは外す。
4. `ReqIFSharp.Tests` を実行する。
5. nupkg を作り、同名のタグ `<本家の版>-nd.<n>` を打つ。

## 改変の表示

Apache License 2.0 の 4(b) に従い、変更したファイルの先頭に、変更した旨と概要を書く。本家の版は書かない（載せ替えのたびに書き換えないため）。

```
// Modification notice (Apache License 2.0, section 4(b)):
//   This file was modified in the miles-team/reqifsharp fork of ReqIFSharp.
//   Change: <変更の概要>
```

## 解消の条件

本家に同等の修正が取り込まれ、公式版が出たら、NextDesign-ReqIF の参照を公式版に戻し、このフォークのパッチは使わなくなる。