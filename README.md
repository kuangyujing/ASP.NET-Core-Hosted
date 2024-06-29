# ASP.NET Core Hosted

## ソリューションとプロジェクトの作成

### SDKのバージョン

.NET 8以上を想定しています。バージョンがこれに満たない場合、[公式サイト](https://dotnet.microsoft.com/ja-jp/download/8.0)からダウンロードしてインストールしてください。

```sh
dotnet --version # => 8.0.300
```

なお、.NET 8.0のサポート期間は2026年11月10日までです。商用利用する場合はメンテナンスできるか検討してください。

### ソリューションの作成

```sh
mkdir BlazorApp
cd BlazorApp
dotnet new sln -n BlazorApp
```

次にASP.NET Core Hostedプロジェクトを作成しますが、.NET 8からはプロジェクトテンプレートとして付属しなくなりました。フレームワークとしてサポートされなくなったわけではないため、手動でホスティングされたBlazor Web Assemblyプロジェクトを作成します。

### Blazor WebAssemblyプロジェクトの作成

フロントエンドのBlazor WebAssemblyプロジェクトを作成します。

```sh
dotnet new blazorwasm -n BlazorApp.Client
```

### ASP.NET Core Web APIプロジェクトの作成

バックエンドのASP.NET Core Web APIプロジェクトを作成します。

```sh
dotnet new webapi -n BlazorApp.Server
```

### 共有プロジェクトの作成

フロントエンドとバックエンドの両方で使用する共有プロジェクトを作成します。

```sh
dotnet new classlib -n BlazorApp.Shared
```

### ソリューションにプロジェクトを追加

```sh
dotnet sln BlazorApp.sln add BlazorApp.Client/BlazorApp.Client.csproj
dotnet sln BlazorApp.sln add BlazorApp.Server/BlazorApp.Server.csproj
dotnet sln BlazorApp.sln add BlazorApp.Shared/BlazorApp.Shared.csproj
```

### プロジェクト間の参照

必要なプロジェクト間の参照を追加します。フロントエンドプロジェクトが共有プロジェクトを参照し、バックエンドープロジェクトも共有プロジェクトを参照します。

```sh
dotnet add MyBlazorApp.Client/MyBlazorApp.Client.csproj reference MyBlazorApp.Shared/MyBlazorApp.Shared.csproj
dotnet add MyBlazorApp.Server/MyBlazorApp.Server.csproj reference MyBlazorApp.Shared/MyBlazorApp.Shared.csproj
dotnet add MyBlazorApp.Server/MyBlazorApp.Server.csproj reference MyBlazorApp.Client/MyBlazorApp.Client.csproj
```

## プロジェクトのセットアップ

### バックエンド

#### 不足しているパッケージのインストール

フロントエンドのWebAssemblyをホスティングするためのパッケージをインスｔーオールします。

```sh
dotnet add BlazorApp.Server package Microsoft.AspNetCore.Components.WebAssembly.Server
```

#### Program.csの修正

主な変更点は以下の通りです。

Swagger UIの有効化して、デバッグ時に簡単にAPIのテストができるようにします。

```cs
app.UseSwagger();
app.UseSwaggerUI();
```

Blazor WebAssemblyのホスティングを有効化して、1つのApp Serviceのインスタンスだけでフロントエンドとバックエンドを共存できるように設定します。

```cs
using Microsoft.Extensions.Hosting;
builder.Services.AddServerSideBlazor();
```

トップレベルルーティングへ変更して、最新の.NETの基準に従い、ビルド時の警告を消します

```cs
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");
```

全体のソースコードは以下の通りです。

```cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI();
} else {
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();  // Make sure this line is correct
app.UseStaticFiles();

app.UseRouting();

// Replace UseEndpoints with top level route registrations
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

```

### フロントエンド

#### Program.cs

フロントエンドにバックエンドAPIのエンドポイントを設定します。

```cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorApp.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
```

### ビルド

ここまででASP.NET Core Hostedプロジェクトの準備は完了しました。次のコマンドでビルドができ、実行もできることを確認します。

```sh
dotnet build
```

ビルドが完了すると次のようなメッセージが表示されます。

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  BlazorApp.Shared -> BlazorApp/BlazorApp.Shared/bin/Debug/net8.0/BlazorApp.Shared.dll
  BlazorApp.Client -> BlazorApp/BlazorApp.Client/bin/Debug/net8.0/BlazorApp.Client.dll
  BlazorApp.Client (Blazor output) -> /Users/k/BlazorApp/BlazorApp.Client/bin/Debug/net8.0/wwwroot
  BlazorApp.Server -> BlazorApp/BlazorApp.Server/bin/Debug/net8.0/BlazorApp.Server.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.94
```

ビルドが完了すると実行できるようになるので、次のコマンドでバックエンドのプロジェクトを実行します。フロントエンドはバックエンドによってホスティングされるため、特に何もする必要はありません。

```sh
dotnet run --project BlazorApp.Server
```

ローカルサーバの立ち上げが成功すると次のようなメッセージが表示されます。

```
Building...
warn: Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[35]
      No XML encryptor configured. Key {af17ddb7-1457-404c-ac52-58e00bb71efb} may be persisted to storage in unencrypted form.
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5223
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /Users/k/AspNetCoreHosted/AspNetCoreHosted.Server
```

この時に出力されるローカルサーバのURL http://localhost:5223 にアクセスするとプロジェクトテンプレートで作成されるBlazorページがブラウザに表示されます。

## Azure App Serviceへのデプロイ

### リソースの作成

すでに作成済みの場合はスキップして構いません。

#### `az` コマンドのログイン

```sh
az login --use-device-code
```

#### Resource Group

```sh
az group create --name ***** --location "East US"
```

#### App Service Plan

```sh
az appservice plan create --name ***** --resource-group ***** --sku B1 --is-linux
```

#### App Service WebApp

```sh
az webapp create --resource-group ***** --plan ***** --name ***** --runtime "DOTNETCORE|8.0"
```

### リリースパッケージの作成

App Serviceへデプロイするためのパッケージを作成します。通常であればCI/CDを構築すべきですが、ここではZip Deployを利用します。

#### リリース用ファイルの作成

```sh
cd AspNetCoreHosted.Server
dotnet publish --configuration Release --output publish
```

#### デプロイ用パッケージの作成

```sh
cd publish
zip -r ../AspNetCoreHosted.zip .
cd ..
```

#### App Serviceへデプロイ

```sh
az webapp deploy --resource-group ***** --name ***** --src-path AspNetCoreHosted.zip --type zip
```

デプロイが完了するとこのようなメッセージが出力されるはずです。

```
Initiating deployment
Deploying from local path: AspNetCoreHosted.zip
Polling the status of sync deployment. Start Time: 2024-06-22 08:42:10.095172+00:00 UTC
Status: Build successful. Time: 0(s)
Status: Starting the site... Time: 16(s)
Status: Starting the site... Time: 32(s)
Status: Starting the site... Time: 50(s)
Status: Starting the site... Time: 66(s)
Status: Starting the site... Time: 82(s)
Status: Starting the site... Time: 100(s)
Status: Starting the site... Time: 116(s)
Status: Starting the site... Time: 132(s)
Status: Site started successfully. Time: 148(s)
Deployment has completed successfully
You can visit your app at: http://*****.azurewebsites.net
```

### GitHub Actionsを利用したデプロイ

```yaml
name: Build and deploy ASP.NET Core app to Azure Web App

on:
  push:
    branches:
      - master

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v2

    - name: Set up .NET Core
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.x'

    - name: Build with dotnet
      run: dotnet publish AspNetCoreHosted.Server/AspNetCoreHosted.Server.csproj --configuration Release --output ./publish

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: '*****'
        slot-name: 'production'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

