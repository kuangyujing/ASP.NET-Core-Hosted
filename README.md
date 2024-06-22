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
mkdir AspNetCoreHosted
cd AspNetCoreHosted
dotnet new sln -n AspNetCoreHosted
```

次にASP.NET Core Hostedプロジェクトを作成しますが、.NET 8からはプロジェクトテンプレートとして付属しなくなりました。フレームワークとしてサポートされなくなったわけではないため、手動でホスティングされたBlazor Web Assemblyプロジェクトを作成します。

### Blazor WebAssemblyプロジェクトの作成

フロントエンドのBlazor WebAssemblyプロジェクトを作成します。

```sh
dotnet new blazorwasm -n AspNetCoreHosted.Client
```

### ASP.NET Core Web APIプロジェクトの作成

バックエンドのASP.NET Core Web APIプロジェクトを作成します。

```sh
dotnet new webapi -n AspNetCoreHosted.Server
```

### 共有プロジェクトの作成

フロントエンドとバックエンドの両方で使用する共有プロジェクトを作成します。

```sh
dotnet new classlib -n AspNetCoreHosted.Shared
```

### ソリューションにプロジェクトを追加

```sh
dotnet sln AspNetCoreHosted.sln add AspNetCoreHosted.Client/AspNetCoreHosted.Client.csproj
dotnet sln AspNetCoreHosted.sln add AspNetCoreHosted.Server/AspNetCoreHosted.Server.csproj
dotnet sln AspNetCoreHosted.sln add AspNetCoreHosted.Shared/AspNetCoreHosted.Shared.csproj
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
dotnet add AspNetCoreHosted.Server package Microsoft.AspNetCore.Components.WebAssembly.Server
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
using AspNetCoreHosted.Client;

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
  AspNetCoreHosted.Shared -> AspNetCoreHosted/AspNetCoreHosted.Shared/bin/Debug/net8.0/AspNetCoreHosted.Shared.dll
  AspNetCoreHosted.Client -> AspNetCoreHosted/AspNetCoreHosted.Client/bin/Debug/net8.0/AspNetCoreHosted.Client.dll
  AspNetCoreHosted.Client (Blazor output) -> /Users/k/AspNetCoreHosted/AspNetCoreHosted.Client/bin/Debug/net8.0/wwwroot
  AspNetCoreHosted.Server -> AspNetCoreHosted/AspNetCoreHosted.Server/bin/Debug/net8.0/AspNetCoreHosted.Server.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.94
```

ビルドが完了すると実行できるようになるので、次のコマンドでバックエンドのプロジェクトを実行します。フロントエンドはバックエンドによってホスティングされるため、特に何もする必要はありません。

```sh
dotnet run --project AspNetCoreHosted.Server
```

