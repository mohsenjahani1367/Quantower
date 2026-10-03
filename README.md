# Quantower Volume Intelligence

اولین بخش قابل اجرا از **Unified Multi-Exchange Volume Intelligence Suite**: هستهٔ مستقل C# برای انتخاب، نرمال‌سازی، ضبط و Replay معاملات عمومی و محاسبهٔ پروفایل حجم سشن. این نسخه سفارش معاملاتی ارسال نمی‌کند.

## وضعیت واقعی

- Solution مستقل .NET 10 قابل build است؛ هیچ بستهٔ NuGet خارجی لازم ندارد.
- تست‌های عددی، نرمال‌سازی، سشن/DST، قرارداد Binance و Replay در Linux اجرا شده‌اند. [نتایج](docs/VALIDATION.md).
- Provider عمومی Binance Spot پیاده شده؛ تست parser به معنی تأیید اتصال زنده نیست.
- سورس recorder بومی و اندیکاتور سطح‌های Replay آماده است؛ **build و اجرای آن‌ها با DLL واقعی روی Windows هنوز لازم است**.
- تمام Milestoneهای پرامپت یا تمام رابط گرافیکی VPSV تکمیل نشده‌اند. [محدودیت‌ها](docs/KNOWN_LIMITATIONS.md).

## اجرای مستقل

SDK پین‌شده: `10.0.401`. در Linux x64:

```bash
bash scripts/bootstrap-cloud.sh
bash scripts/verify.sh
```

با SDK نصب‌شده:

```bash
dotnet build VolumeIntelligence.sln -c Release
dotnet run --project tests/VolumeIntelligence.Tests -c Release --no-build
dotnet run --project src/VolumeIntelligence.Cli -c Release --no-build -- replay tests/fixtures/trades.synthetic.jsonl --config tests/fixtures/replay-settings.json
```

پاسخ نمونه: Total=10، Delta=2، POC=101، VAL=100، VAH=102 و یک Duplicate. داده مصنوعی است.

ضبط عمومی بدون کلید، فقط با اجرای صریح:

```bash
dotnet run --project src/VolumeIntelligence.Cli -c Release --no-build -- record recordings/binance-first --config config/appsettings.example.json --seconds 60 --max-reconnects 0
```

هر بار پوشهٔ جدید انتخاب کنید. `trades.jsonl`، `raw.jsonl`، `health.jsonl`، `settings.json` و `report.json` ایجاد می‌شوند. صفرمعامله یا اتصال ناقص موفقیت نیست.

## Windows و Quantower

[راهنمای مرحله‌به‌مرحله](docs/WINDOWS_SMOKE_TEST_FA.md) را دنبال کنید. `setup-windows.ps1` ابتدا هسته و تست‌ها، سپس دو پروژهٔ Quantower را با DLL واقعی build می‌کند؛ خودش چیزی در Quantower نصب نمی‌کند.

- [مشخصات اصلی](docs/PROJECT_SPEC.md)
- [وضعیت پروژه](docs/PROJECT_STATUS.md)
- [معماری](docs/ARCHITECTURE.md)
- [اتصال صرافی‌ها](docs/EXCHANGE_CONNECTIONS_FA.md)
- [ماتریس قابلیت‌ها](docs/CAPABILITY_MATRIX.md)
- [برنامهٔ ادامه](docs/ROADMAP.md)
