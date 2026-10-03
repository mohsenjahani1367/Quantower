# تست محلی Windows و Quantower

مدل پیشنهادی برای اجرای دستورها و جمع‌آوری لاگ: **5.6‑Sol / Medium**. برای تحلیل اختلاف API، معماری و اصلاح پیچیده، نتیجه را به جلسهٔ **6.1‑Sol / High** برگردانید.

این مراحل دادهٔ عمومی می‌خوانند؛ هیچ سفارش یا تغییر حساب معاملاتی لازم نیست. Bridge فعلی را تغییر ندهید.

## ۱. دریافت و Build

ZIP تحویل را در یک پوشهٔ جدید، مثلاً `C:\Codex\VolumeIntelligence`، استخراج کنید. فایل‌های پروژهٔ قبلی را جایگزین نکنید. PowerShell را در ریشهٔ پروژه باز کنید:

```powershell
.\scripts\setup-windows.ps1 -InstallSdk
```

این دستور SDK رسمی پین‌شده را در `.local\dotnet` با کنترل SHA-512 نصب می‌کند؛ نیاز به نصب سراسری ندارد. سپس Solution، تست‌ها و دو پروژهٔ بومی را با DLL واقعی build می‌کند. مسیر پیش‌فرض از شواهد شما آمده است:

`C:\Quantower\TradingPlatform\v1.146.18\bin\TradingPlatform.BusinessLayer.dll`

اگر مسیر تغییر کرده، پارامتر `-BusinessLayerPath` بدهید. اسکریپت را بدون `-InstallSdk` برای استفادهٔ مجدد اجرا کنید. اگر build شکست خورد، **هیچ DLLی نصب نکنید** و فایل‌های `artifacts\windows-*.log` را برگردانید. پروژه‌های Quantower در Cloud کامپایل نشده‌اند؛ اختلاف API ممکن است نیاز به اصلاح داشته باشد.

## ۲. نصب Recorder فقط پس از Build موفق

وجود مسیر واقعی Scripts را بررسی و استراتژی‌های قبلی را حفظ کنید. نصب فقط DLLهای جدید پروژه را کپی و نسخهٔ قبلی همان فایل‌ها را backup می‌کند؛ DLL اصلی Quantower دست‌نخورده می‌ماند:

```powershell
.\scripts\install-quantower.ps1 -Destination 'C:\Quantower\Settings\Scripts\Strategies' -Component Strategy
```

در Strategies manager، `UPDATE ALL` و سپس **Volume Intelligence - Native Trade Recorder** را انتخاب کنید. نماد ۱ را از Connection واقعی Binance Spot انتخاب کنید. Base=`BTC`، Quote=`USDT`، Venue=`Binance`. ابتدا معنای Size و Spot بودن نماد را بررسی کنید، سپس گزینهٔ تأیید Spot/Base را روشن کنید. بدون این تأیید Recorder اجرا نمی‌شود.

نماد دوم فعلاً خالی بماند. اگر بعداً Spot منبع دوم واقعاً در دسترس بود، با Venue صحیح و همان Base/Quote انتخاب کنید؛ نماد Futures را به این Recorder معرفی نکنید.

Run را بزنید، ابتدا ۶۰ ثانیه ثبت کنید و Stop بزنید. در پوشهٔ پیش‌فرض:

`Documents\Quantower\VolumeIntelligence\<run-id>\`

برای پایان drain منتظر `capture-status.json` بمانید. تعداد `captured` باید مثبت باشد. `failed`، `no-data`، `degraded` یا drop/reject را موفقیت محسوب نکنید. Timestamp، قیمت، Size، جهت Aggressor و tradeId را با Tape بومی مقایسه کنید. ID مصنوعی یا aggregation نامشخص به‌صورت flag ثبت می‌شوند؛ نبود Trade Count واقعی را صفر یا تخمین ننامید.

پس از تأیید اولین payload، حداقل ۳۰ دقیقه ضبط کنید تا از بازهٔ Trial استفاده شود. برای سنجش قطع/وصل، فقط در تست بعدی و با ثبت زمان قطع عمل کنید؛ دادهٔ دارای gap را جدا نگه دارید.

## ۳. Replay دادهٔ واقعی

در ریشهٔ پروژه، `$capture` را به پوشهٔ واقعی ضبط‌شده تنظیم کنید. تنظیمات دقیق منبع را از mapping همان اجرا بسازید:

```powershell
$capture = 'C:\Users\YOUR_USER\Documents\Quantower\VolumeIntelligence\YOUR_RUN'
$maps = @(Get-Content (Join-Path $capture 'mapping.json') -Raw | ConvertFrom-Json)
$settings = Get-Content 'config\appsettings.example.json' -Raw | ConvertFrom-Json
$settings.selection.sources = @($maps | ForEach-Object { $_.source })
$settings.selection.canonicalSymbol = $maps[0].canonicalSymbol
$settings.selection.baseAsset = $maps[0].baseAsset
$settings.selection.quoteAsset = $maps[0].quoteAsset
$settings.mappings = $maps
$settingsPath = Join-Path $capture 'replay-settings.json'
if (Test-Path $settingsPath) { throw 'Settings already exist; preserve them and use a new filename.' }
$settings | ConvertTo-Json -Depth 20 | Set-Content -Encoding utf8 $settingsPath
$dotnetExe = '.\.local\dotnet\dotnet.exe'
& $dotnetExe run --project src\VolumeIntelligence.Cli -c Release --no-build -- replay (Join-Path $capture 'trades.jsonl') --config $settingsPath --output (Join-Path $capture 'replay-1.json')
if ($LASTEXITCODE -ne 0) { throw 'Replay failed or late data was excluded; inspect output before continuing.' }
& $dotnetExe run --project src\VolumeIntelligence.Cli -c Release --no-build -- replay (Join-Path $capture 'trades.jsonl') --config $settingsPath --output (Join-Path $capture 'replay-2.json')
if ($LASTEXITCODE -ne 0) { throw 'Repeated replay failed.' }
Get-FileHash (Join-Path $capture 'replay-1.json'), (Join-Path $capture 'replay-2.json') -Algorithm SHA256
```

دو هش باید یکسان باشند. مقدار QuoteNotional باید با واحد واقعی منبع سازگار باشد. UnknownAggressor باعث مخفی‌کردن Total نمی‌شود؛ فقط Delta را محدود می‌کند. NativeIdentityUnverified به معنی اثبات‌نشدن حذف تکرار upstream است.

## ۴. آزمایش اندیکاتور Replay

پس از build موفق فایل indicator و تأیید مسیر واقعی Indicators:

```powershell
.\scripts\install-quantower.ps1 -Destination 'C:\Quantower\Settings\Scripts\Indicators' -Component Indicator
```

اندیکاتور **Volume Intelligence - VPSV Replay Levels** را روی همان نماد و Connection ثبت‌شده اضافه کنید؛ مسیر TradeFile و SettingsFile همان Capture باشد. این نسخه فقط POC/VAH/VAL **آخرین سشن کاملاً بسته‌شده** را نشان می‌دهد، نه histogram یا developing session. Capture یک‌دقیقه‌ایِ امروز هنوز سشن کامل بسته‌شده ندارد؛ نبود خط در همان سشن طبیعی است. برای این تست از ضبط دارای پایان سشن و کندل‌های بعد از آن استفاده کنید، یا زمان custom-session مشخصی را قبل از آزمایش تنظیم کنید.

همان داده و تنظیمات را روی چارت‌های ۱، ۵ و ۱۵ دقیقه مقایسه کنید. سطوح باید با خروجی JSON برابر باشند. از final profile داخل سشن خودش استفاده نمی‌شود؛ هیچ نتیجهٔ نهایی نباید به کندل‌های گذشتهٔ همان سشن منتقل شود. کش OHLCV قدیمی برای پرکردن حجم پروفایل استفاده نمی‌شود.

## ۵. فایل‌های برگشتی

- `artifacts\windows-core-build.log`، `windows-tests.log`، `windows-quantower-build.log`، `windows-indicator-build.log`
- `quantower-api-hash.txt`
- `capture-status.json`، `mapping.json` و نمونه‌ای از `trades.jsonl`
- دو گزارش Replay و هش آن‌ها
- تصویر Licence/Trial با پوشاندن اطلاعات شخصی؛ تصویر سطوح و نام Connection

هیچ credential، Bridge token، فایل account یا environment dump در بستهٔ برگشتی نگذارید. شروع GUI و compile محلی را قبل از مشاهدهٔ این خروجی‌ها موفق فرض نمی‌کنیم.
