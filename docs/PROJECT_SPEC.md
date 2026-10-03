عنوان پروژه:
Quantower Unified Multi-Exchange Volume Intelligence Suite

نقش تو:
به‌عنوان مهندس ارشد C#/.NET، مهندس Market Data، توسعه‌دهنده Quantower Algo و متخصص Order Flow/Volume Trading عمل کن.

هدف، ساخت یک پروژه واقعی، تست‌پذیر، قابل نگهداری و قابل نصب برای Quantower است؛ نه صرفاً ارائه پیشنهاد، معماری نظری، شبه‌کد یا یک Plan.

این متن را مشخصات اصلی پروژه در نظر بگیر. در ابتدای کار، نسخه‌ای از آن را داخل repository در فایل docs/PROJECT_SPEC.md ذخیره کن تا مرجع ثابت پروژه باشد. همچنین فایل‌های زیر را برای حافظه پایدار پروژه ایجاد و در طول کار به‌روزرسانی کن:

- docs/IMPLEMENTATION_PLAN.md
- docs/PROJECT_STATUS.md
- docs/DECISION_LOG.md
- docs/KNOWN_LIMITATIONS.md

قواعد اجرای کار:
- ابتدا repository، فایل‌های AGENTS.md، ساختار فعلی، branch و تغییرات موجود را بررسی کن.
- اگر repository خالی است، Solution و ساختار مناسب پروژه را ایجاد کن.
- تغییرات متعلق به کاربر یا بخش‌های نامرتبط را دست نزن.
- کار را فقط با Plan، گزارش امکان‌سنجی یا پیشنهاد معماری تمام نکن.
- بعد از بررسی اولیه، وارد پیاده‌سازی شو و یک Vertical Slice واقعی، قابل build و قابل تست تحویل بده.
- کار را به Milestoneهای مستقل و قابل ارزیابی تقسیم کن.
- پس از هر Milestone، build و test را اجرا کن؛ اگر شکست خورد، قبل از ادامه علت را رفع کن.
- وضعیت واقعی هر Milestone، تصمیم‌ها، تست‌ها، محدودیت‌ها و مشکلات را در مستندات پروژه ثبت کن.
- در مواردی که می‌توانی با فرض منطقی پیش بروی، سؤال غیرضروری نپرس.
- هر فرض مهم را ثبت کن.
- اگر چیزی واقعاً ناممکن، نامستند یا نیازمند تصمیم اساسی کاربر است، blocker دقیق و راه بازکردن آن را توضیح بده.
- موفقیت کاذب گزارش نکن.
- Compilation به‌تنهایی کافی نیست؛ رفتار قابل مشاهده و تست‌شده لازم است.
- هیچ API Key، Secret، Seed Phrase یا Private Key از من درخواست نکن.
- هیچ Secret واقعی داخل repository، log، fixture، screenshot یا فایل نمونه قرار نده.
- اگر محیط Cloud به Quantower Windows GUI یا DLLهای محلی دسترسی ندارد، هسته مستقل را کاملاً build/test کن و مراحل دقیق build، نصب و Smoke Test روی Windows را تهیه کن.
- اگر قابلیت commit در محیط وجود دارد، تغییرات را در commitهای منطقی و کوچک نگه دار؛ در غیر این صورت تغییرات را مستقیم در workspace پیاده‌سازی کن.
- پاسخ نهایی باید دقیقاً مشخص کند چه چیزی واقعاً پیاده‌سازی و تست شده و چه چیزی فقط نیازمند تست محلی Quantower است.

وضعیت فعلی لایسنس:
در حال حاضر روی حساب من دوره آزمایشی ۷روزه Quantower All-in-One فعال است.

در این بازه باید حداکثر استفاده را از قابلیت‌های زیر ببریم:

- Multi-connect / Synchro Connections
- Volume Analysis
- Footprint/Cluster
- Volume Profiles
- Advanced Features
- دسترسی هم‌زمان به چند Exchange Connection
- قابلیت‌های کامل Quantower Algo و Custom Indicators

با این حال، محصول نهایی نباید فقط به Trial یا Multi-connect پولی وابسته باشد. معماری باید هم مسیر Native Multi-connect فعلی و هم مسیر مستقل بعد از پایان Trial را پشتیبانی کند.

هدف نهایی:
ساخت یک مجموعه حرفه‌ای از اندیکاتورهای Volume Trading، Order Flow، Liquidity و Derivatives در Quantower که بتوانند به انتخاب کاربر بر اساس یکی از این حالت‌ها کار کنند:

1. Current chart/native Quantower symbol
2. یک Exchange خاص، مانند Binance
3. Hyperliquid only
4. یک Connection انتخابی Quantower
5. چند Connection انتخابی Quantower
6. تمام Connectionهای فعال Quantower
7. چند Exchange انتخابی از Public API
8. تمام Exchangeهای فعال در External Collector
9. Hybrid Native + External
10. Spot only
11. Futures/Perpetual only
12. Spot + Futures/Perpetual با انتخاب صریح
13. Historical Replay از دیتای ذخیره‌شده محلی

باید بتوانیم برای مثال یک آزمایش را فقط بر اساس Binance، آزمایش دیگری را فقط بر اساس Hyperliquid و آزمایش سوم را براساس داده تجمیعی چند صرافی انجام دهیم.

تمام اندیکاتورها باید از یک Data/Aggregation Layer مشترک استفاده کنند. هر اندیکاتور نباید WebSocket یا اتصال مستقل خودش را ایجاد کند.

واقعیت‌هایی که باید با مستندات رسمی نسخه فعلی مجدداً بررسی شوند:
- Quantower Free معمولاً فقط یک Active Connection دارد.
- Multi-connect در All-in-One/Advanced/Crypto Package فعال است.
- Trial فعلی من All-in-One است و باید برای Technical Spike استفاده شود.
- Binance Spot و Binance Futures در Quantower دارای INFO mode برای دیتای عمومی بدون Trading API Key هستند.
- بعضی Exchangeهای دیگر مانند Bybit نیز ممکن است INFO mode داشته باشند.
- اتصال معاملاتی Hyperliquid در Quantower ممکن است Main Account Address و API Wallet Private Key بخواهد.
- برای صرفاً Market Data عمومی Hyperliquid نباید Private Key لازم باشد؛ در صورت نبود INFO mode بومی، Public API مستقل استفاده شود.
- Quantower به‌صورت خودکار Trade Tape چند صرافی را به یک Volume Analysis Feed واحد تبدیل نمی‌کند.
- فعال‌کردن Multi-connect فقط Connectionها را هم‌زمان فعال می‌کند؛ ادغام صحیح داده وظیفه موتور ماست.
- Core.Trades در Quantower مربوط به معاملات حساب کاربر است، نه Public Market Trade Tape.
- برای Public Trades باید از Symbol.NewLast، GetTickHistory یا API صحیح Symbol استفاده شود.
- برای L2 باید از Symbol.NewLevel2 و DepthOfMarket استفاده شود.
- Quantower از Custom Indicator، Strategy، Plugin و Connector در C# پشتیبانی می‌کند.
- Quantower یک Step Volume Profile نزدیک به VPSV دارد؛ ابتدا Gap Analysis انجام بده.
- تاریخچه Tick، L2، Aggressor Flag، Liquidations، OI و Funding بین Providerها یکسان و تضمین‌شده نیست.
- دیتای عمومی Exchangeها غالباً رایگان است، اما مشمول Rate Limit، محدودیت تاریخچه، Terms of Service و محدودیت منطقه‌ای است.
- هیچ محدودیت منطقه‌ای را دور نزن.

حالت‌های اصلی Data Source:

1. NativeQuantowerMultiConnect
2. UnifiedExternalFeed
3. Hybrid
4. HistoricalReplay

اولویت فعلی در دوره Trial:
- NativeQuantowerMultiConnect برای اثبات سریع امکان تجمیع
- Hybrid برای ترکیب Connectorهای Quantower با Public APIهایی مانند Hyperliquid
- UnifiedExternalFeed برای استقلال بلندمدت
- HistoricalReplay برای تست و Backtest قابل تکرار

NativeQuantowerMultiConnect:
در این حالت داده مستقیماً از Connectionهای فعال Quantower دریافت شود.

نمونه Connectionهای موردنظر:

- Binance Spot در INFO mode
- Binance Futures در INFO mode
- Bybit در INFO mode، اگر در نسخه فعلی قابل استفاده است
- OKX یا Exchangeهای عمومی قابل استفاده
- Hyperliquid فقط اگر دریافت Public Data بدون نیاز به Private Key واقعاً ممکن و مستند باشد، یا Connection از قبل توسط خود کاربر در Quantower تنظیم شده باشد

الزامات Native Multi-connect:
- تمام Connectionهای فعال را با API رسمی Quantower شناسایی کن.
- Connection ID، Connection Name و Symbol ID هر منبع را جداگانه حفظ کن.
- Symbolهای هم‌نام فقط با ticker شناسایی نشوند؛ Connection ID بخشی از هویت آن‌ها باشد.
- کاربر بتواند Symbol متناظر هر Exchange را انتخاب کند.
- Presetهای Symbol Mapping ایجاد شود.
- Public Trade Tape از Symbol.NewLast دریافت شود.
- تاریخچه از GetHistory/GetTickHistory دریافت شود.
- L2 از Symbol.NewLevel2 و DepthOfMarket دریافت شود.
- از Core.Trades برای Market Volume استفاده نشود.
- Subscriptionها به‌طور صحیح ساخته و Dispose/Unsubscribe شوند.
- قطع یک Connection کل سیستم را متوقف نکند.
- reconnect و تغییر Connection status مدیریت شود.
- وضعیت هر Source شامل Connected، Delayed، Reconnecting، Missing History، Gapped و Degraded نمایش داده شود.
- Tradeها قبل از Aggregation وارد Schema مشترک شوند.
- ابزارهای VPSV، CVD، Delta، Footprint و سایر اندیکاتورها مستقیماً به Connectionها وابسته نباشند.

UnifiedExternalFeed:
یک سرویس مستقل Market Data Collector/Normalizer ایجاد کن که WebSocket هر Exchange را فقط یک مرتبه باز کند، داده‌ها را نرمال و ذخیره کند و از طریق یک Quantower Custom Data-Feed Connector یا Local Adapter واحد در اختیار Quantower بگذارد.

هدف بلندمدت این است که Quantower فقط یک Connection تجمیعی مانند موارد زیر ببیند:

- BTC-SPOT.AGG
- BTC-PERP.AGG
- BTC.ALL.AGG
- ETH-SPOT.AGG
- ETH-PERP.AGG

در کنار Symbolهای تجمیعی، Single Venue Symbolها نیز موجود باشند:

- BTC-PERP.BINANCE
- BTC-PERP.HYPERLIQUID
- BTC-SPOT.BINANCE

این مسیر باید مستقل از dxFeed و Multi-connect پولی باشد، مشروط به مجازبودن آن در API و Terms of Quantower. هدف دورزدن لایسنس نیست؛ هدف ساخت Connector سفارشی مجاز با داده عمومی Exchangeهاست. وابستگی‌های لایسنس Quantower را دقیقاً مستند کن.

Hybrid Mode:
در این حالت بتوانیم برای مثال:

- Binance، Bybit و OKX را از Connectionهای Native Quantower بگیریم.
- Hyperliquid را از Public WebSocket مستقل دریافت کنیم.
- داده تاریخی را از Local Store تکمیل کنیم.
- همه منابع را بعد از ورود به Normalization Layer در یک Schema مشترک ادغام کنیم.

HistoricalReplay:
- داده خام و نرمال‌شده باید قابل ضبط باشد.
- Replay باید deterministic باشد.
- اجرای مجدد یک فایل Replay با تنظیمات یکسان باید خروجی یکسان تولید کند.
- سرعت Replay قابل تنظیم باشد.
- داده ناقص و gap باید همراه dataset ثبت شود.
- Indicatorها در Live و Replay باید از یک Calculation Engine استفاده کنند.

Interfaceهای معماری:
حداقل abstractionهای زیر یا معادل منطقی آن‌ها را تعریف کن:

- IMarketDataSource
- INativeQuantowerSource
- IExternalExchangeSource
- IHistoricalReplaySource
- IAggregatedMarketDataSource
- IMarketDataRecorder
- ISymbolMapper
- IVolumeNormalizer
- IExchangeCapabilityProvider
- ISourceHealthMonitor

Aggregation Engine و Indicatorها فقط به abstraction مشترک وابسته باشند و از پیاده‌سازی Connection مستقل باشند.

ساختار پیشنهادی پروژه:
نام دقیق پروژه‌ها را با معماری repository هماهنگ کن، ولی مسئولیت‌ها را جدا نگه دار:

- Core domain and calculations
- Exchange provider contracts
- Native Quantower source
- Binance Spot provider
- Binance USDⓈ-M Futures provider
- Hyperliquid provider
- Normalization
- Symbol mapping
- Aggregation engine
- Local event/tick storage
- Historical replay
- Quantower adapter/connector
- Quantower indicators
- CLI diagnostic/recorder tool
- Unit tests
- Provider contract tests
- Replay/golden tests
- Optional live integration tests

از C#/.NET استفاده کن مگر repository از قبل Stack تثبیت‌شده دیگری داشته باشد. Target Framework پروژه Quantower را از Quantower Template، نسخه نصب‌شده یا DLLهای رسمی تشخیص بده؛ حدس نزن.

اولویت Exchange Providerها:
P0:
- Binance Spot
- Binance USDⓈ-M Futures
- Hyperliquid Perpetuals

P1:
- Bybit
- OKX

P2:
- Coinbase
- Kraken
- Bitfinex
- BitMEX
- Deribit

P3:
- Exchangeهای دیگری که Public API پایدار و مجاز دارند

معماری Provider باید Plugin-like باشد تا اضافه‌کردن Exchange جدید نیازمند تغییر Aggregation Engine و Indicatorها نباشد.

Capability Matrix:
یک ماتریس واقعی برای هر Exchange/Connection ایجاد کن که موارد زیر را نشان دهد:

- Trades
- Individual trades یا aggregate trades
- Aggressor side
- Historical trades
- L1
- L2 snapshot
- L2 diff
- Historical L2
- Candles
- Open Interest
- Funding
- Actual liquidation events
- Account/API Key requirement
- Public authentication requirement
- Rate limits
- History retention
- Timestamp precision
- Trade ID quality
- Known limitations
- Native Quantower availability
- INFO mode availability

Capability Matrix باید با آزمایش عملی و مستندات رسمی به‌روزرسانی شود، نه صرفاً با حدس.

مدل داده استاندارد Trade:
حداقل این فیلدها حفظ شوند:

- venue
- connectionId
- marketType
- canonicalSymbol
- venueSymbol
- tradeId
- exchangeTimestampUtc
- receiveTimestampUtc
- price
- baseQuantity
- quoteQuantity
- quoteNotional
- contractSize
- aggressorSide
- isAggregatedTrade
- aggregateTradeCount، اگر قابل محاسبه است
- sourceSequence
- rawPayloadReference
- dataQualityFlags

مدل داده Order Book:
حداقل این اطلاعات نگهداری شوند:

- venue
- canonicalSymbol
- venueSymbol
- marketType
- snapshot/diff type
- sequence range
- exchange timestamp
- receive timestamp
- side
- price
- size
- order count، اگر موجود است
- gap state
- rebuild state
- depth limit
- aggregation level

مدل داده مشتقات:
در صورت پشتیبانی Source:

- Open Interest
- Open Interest change
- Funding rate
- Mark price
- Index price
- Liquidation event
- Liquidation side
- Liquidation notional
- Contract type
- Contract multiplier

قواعد حیاتی نرمال‌سازی:
- حجم خام Spot، Linear Perpetual، Inverse Contract و Coin-M مستقیماً با هم جمع نشود.
- Base Quantity و Quote Notional جدا نگهداری شوند.
- واحد پیش‌فرض تجمیع بین Exchangeها Quote/USD Notional باشد.
- VolumeUnit شامل Base، QuoteNotional و USDNormalized باشد.
- USDT، USDC و USD کورکورانه برابر فرض نشوند.
- حالت StablecoinMode شامل Strict و Converted باشد.
- در حالت Converted منبع و timestamp نرخ تبدیل ثبت شود.
- contract multiplier و linear/inverse بودن لحاظ شود.
- Tick size و price step Exchangeها متفاوت است.
- Composite price bin باید روش گردکردن ثابت، قابل تنظیم و مستند داشته باشد.
- Spot و Perpetual به‌صورت پیش‌فرض جدا باشند.
- ترکیب Spot و Derivatives فقط با انتخاب صریح کاربر انجام شود.
- WeightingMode شامل RawNormalizedVolume، QuoteNotional، EqualVenue و CustomWeights باشد.
- ReferencePriceMode شامل ChartSymbol، SelectedVenue و WeightedMedian باشد.
- سهم هر Exchange در خروجی نهایی قابل مشاهده باشد.
- Provenance داده هر Indicator قابل بازرسی باشد.

Aggressor Side:
برای هر Provider mapping سمت معامله را براساس مستندات رسمی همان Exchange پیاده‌سازی و تست کن.

نمونه‌های مهم:
- در Binance، maker flag باید به‌درستی به buyer/seller aggressor تبدیل شود.
- در Hyperliquid، مقادیر Side باید طبق schema رسمی تبدیل شوند.
- تفاوت trade و aggTrade حفظ شود.
- AggTrade برای Volume/Delta ممکن است قابل استفاده باشد، ولی Trade Count تجمیعی بین Exchangeها الزاماً قابل مقایسه نیست.
- اگر Trade Count دقیق در دسترس نیست، مقدار را تخمینی معرفی نکن؛ کیفیت یا محدودیت آن را نشان بده.

پایداری و صحت داده:
- reconnect
- heartbeat
- exponential backoff
- resubscribe
- deduplication
- sequence validation
- order book gap detection
- snapshot rebuild
- out-of-order handling
- deterministic reorder buffer
- bounded queues
- backpressure
- cancellation
- graceful shutdown
- structured logging
- health metrics
- latency metrics

قواعد Timestamp:
- exchange timestamp مبنای ساخت bar/session باشد.
- receive timestamp برای latency نگهداری شود.
- همه داده‌ها در هسته به UTC تبدیل شوند.
- Time Zone و DST فقط در Session Boundary اعمال شوند.
- late event باید علامت‌گذاری شود.
- سیاست Include/Exclude/Correct برای late trades قابل تنظیم باشد.
- تغییر داده Session بسته‌شده فقط به‌صورت Late Data Correction و همراه flag/log انجام شود.

هیچ عملیات شبکه‌ای، storage سنگین یا محاسبه پرهزینه‌ای روی UI thread، OnPaintChart یا event handler حساس Quantower انجام نشود.

Local Recorder:
از ابتدای Technical Spike، داده خام و نرمال‌شده را ذخیره کن.

اهداف:
- حفظ دیتای جمع‌شده قبل از پایان Trial
- تست deterministic
- Backtest قابل تکرار
- تحلیل gap
- مقایسه Exchangeها
- ساخت fixture واقعی و anonymized
- امکان Replay بدون Connection زنده

فرمت ذخیره‌سازی را براساس حجم، query و portability انتخاب و تصمیم را در DECISION_LOG ثبت کن. Schema باید versioned باشد.

Source Selector مشترک:
تمام Indicatorها باید از تنظیمات مشترک زیر استفاده کنند:

- DataSourceMode
- SelectedNativeConnections
- SelectedVenues
- SelectedSymbols
- MarketTypes
- CanonicalInstrument
- ReferencePriceMode
- VolumeUnit
- StablecoinMode
- WeightingMode
- CustomVenueWeights
- IncludeLateTrades
- MaximumAllowedLatency
- MinimumHealthySources
- Live/Replay mode
- DataQualityDisplay

Presetهای لازم:

- Current Chart Symbol
- Binance Spot Only
- Binance Futures Only
- Hyperliquid Only
- Selected Native Connections
- All Active Native Connections
- Binance + Hyperliquid Perps
- Major CEX Perps
- All Spot
- All Perps
- External Aggregated Feed
- Hybrid Native + External
- Custom Basket

در صورت محدودیت InputParameterهای Quantower، Settings Panel یا Plugin مناسب بساز و fallback ساده و مستند ارائه کن.

Gap Analysis:
قبل از بازسازی امکانات، Quantower و MMT را مقایسه کن.

به‌طور خاص:
- Quantower Step Volume Profile را با VPSV مقایسه کن.
- Quantower Footprint/Cluster را با MMT Footprint مقایسه کن.
- Volume Profile، VWAP، Time Statistics، DOM Surface و سایر ابزارهای موجود را بررسی کن.
- قابلیت موجود و سالم Quantower را بی‌دلیل دوباره نساز.
- تمرکز اصلی روی Multi-exchange data، Source Selection، دقت، Replay و قابلیت‌هایی باشد که واقعاً غایب‌اند.
- مشخص کن کدام قابلیت بعد از پایان All-in-One به لایسنس Quantower وابسته می‌ماند.
- محدودیت دو Indicator در نسخه Free یا هر محدودیت فعلی را از مستندات روز بررسی و ثبت کن.

MMT:
فهرست فعلی Indicatorهای رسمی و مهم MMT را از منابع عمومی بررسی کن، اما:

- کد اختصاصی MMT را کپی نکن.
- فرمول خصوصی را حدس نزن.
- UI، نام‌گذاری اختصاصی یا Branding را کپی نکن.
- سرویس MMT را scrape نکن.
- فقط مفاهیم شناخته‌شده بازار و رفتارهایی را که عمومی توضیح داده شده‌اند، با پیاده‌سازی مستقل بساز.
- اگر الگوریتم عمومی نیست، یک الگوریتم استاندارد، شفاف و تست‌شده ارائه بده.
- MMT API تنها می‌تواند Provider اختیاری یا Validation Source باشد، آن هم فقط در صورت مجوز و API Key جداگانه.
- پروژه نباید برای عملکرد اصلی به MMT API یا اشتراک MMT وابسته باشد.

اولویت صفر ـ VPSV کامل:
VPSV یعنی Volume Profile Session Volume.

VPSV باید براساس Trade-level data ساخته شود، نه تقریب ضعیف از OHLCV کندل‌ها. نتیجه آن برای یک مجموعه Trade یکسان نباید با تغییر timeframe چارت تغییر کند.

Sessionهای لازم:
- Daily
- Weekly
- Monthly
- London
- New York
- Asia
- Intraday
- Custom

تنظیمات Session:
- Time zone قابل انتخاب
- DST صحیح
- start/end سفارشی
- overnight session
- session template
- نمایش Session header
- current/developing session
- closed sessions

Render Mode:
- Total
- Buy/Sell
- Delta
- Combined

Row/Scaling:
- Number of Rows
- Ticks Per Row
- Global/shared row size
- Auto sizing
- Left/Right origin
- Width
- Opacity
- Color grading
- Volume labels
- Hide/show text
- Threshold filtering
- Threshold color
- Value Area row colors
- Outside Value Area colors

POC:
- POC
- Inline label
- Axis label
- Line style
- Thickness
- Developing POC
- Naked POC extension
- توقف extension در اولین touch معتبر

Value Area:
- VAH
- VAL
- درصد قابل تنظیم، پیش‌فرض 70%
- Developing VAH/VAL
- Naked VAH/VAL extension
- Inline/axis labels
- Line style/color
- deterministic expansion algorithm
- deterministic tie-breaking

Aggregate:
- Exchange selector
- Native Connection selector
- Spot/Futures/Perps selector
- Single Venue
- Selected Venues
- All Enabled Venues
- Quote-notional normalization
- Source contribution display
- Data quality status
- Missing/gapped source warning

رفتار:
- Profile جدا برای هر Session
- current session در Real Time به‌روزرسانی شود.
- Session بسته‌شده repaint نشود، مگر Late Data Correction ثبت‌شده.
- تغییر chart timeframe نتیجه Trade-based profile را تغییر ندهد.
- Price binning، Value Area، tie-breaking، session boundary و touch detection کاملاً deterministic و مستند باشند.
- POC/VAH/VAL باید با Golden Tests قابل اثبات باشند.
- اگر تطابق دقیق با رفتار MMT به علت خصوصی‌بودن الگوریتم ممکن نیست، ادعای تطابق کامل نکن.

Indicatorهای بعدی:

P1 ـ Core Volume/Order Flow:
- VPVR / Visible Range Volume Profile
- Fixed Range Volume Profile
- Aggregated Volume با Buy/Sell split
- Delta Volume
- CVD
- Session-reset CVD
- Rolling CVD
- Trade-size filtered CVD
- Footprint/Cluster
- Bid × Ask
- Delta cells
- Per-bar POC
- Per-bar Value Area
- Diagonal Imbalance
- Stacked Imbalance
- Unfinished Auction
- Session VWAP
- Anchored VWAP
- Standard-deviation bands
- Bar Statistics
- Trade Counter
- Trade Pulse
- Large Trade/Volume Bubbles

P2 ـ Liquidity/Derivatives:
- Order Book Depth Profile
- Bid/Ask Depth Imbalance
- Historical Order Book Heatmap
- DOM
- Tape/Time & Sales
- Actual Liquidations
- Liquidation Bubbles
- Open Interest
- OI Change
- Funding Rate
- OI/Price/Delta divergence

P3 ـ Advanced:
- Estimated Liquidation Heatmap با برچسب واضح Estimated/Modelled
- TPO/Market Profile
- Net Positioning فقط با روش قابل دفاع
- Absorption/Exhaustion tools
- Hyperliquid real liquidation/TP/SL maps فقط در صورت دسترسی واقعی و قابل اثبات به داده
- Composite dashboards
- Confluence tools
- Alerts

Actual و Estimated:
- Actual event با modelled estimate مخلوط نشود.
- نام Indicator، legend و مستندات باید نشان دهند داده Actual، Derived یا Estimated است.
- Liquidation Heatmap تخمینی نباید به‌عنوان موقعیت واقعی معامله‌گران معرفی شود.
- اگر Hyperliquid داده واقعی ارائه می‌دهد، منبع و محدودیت آن ثبت شود.

Bar Statistics:
در صورت موجودبودن داده، حداقل این موارد قابل نمایش باشند:

- Total Volume
- Buy Volume
- Sell Volume
- Delta
- Delta %
- Trade Count
- Buy/Sell Trade Count
- Average Trade Size
- Maximum Trade Size
- Large Trade Volume
- Liquidations
- OI
- OI Change
- Funding
- Source count
- Data quality
- Latency

راهنمای اتصال رایگان:
فایل فارسی docs/EXCHANGE_CONNECTIONS_FA.md ایجاد کن و دو موضوع را کاملاً جدا توضیح بده.

A) Public Market Data:
- برای Trade، Volume، Price و Order Book عمومی معمولاً حساب یا API Key لازم نیست.
- روش ورود به Quantower ID دارای All-in-One Trial
- روش بررسی فعال‌بودن Trial و تاریخ انقضا
- مسیر Connections Manager
- روش اتصال Binance Spot در INFO mode
- روش اتصال Binance Futures در INFO mode
- روش اتصال Bybit/سایر Exchangeها در INFO mode، اگر موجود است
- روش اتصال هم‌زمان آن‌ها با Multi-connect
- نحوه تشخیص Connection status سبز
- نحوه انتخاب Symbol متناظر از هر Connection
- نحوه بررسی Trade، History و L2
- روش اجرای External Collector عمومی Binance و Hyperliquid بدون کلید
- محدودیت تاریخچه رایگان
- Rate limits
- تفاوت Public Market Data با Account Data
- وضعیت پروژه بعد از پایان Trial

B) Private Account/Trading Data:
- فقط در صورت نیاز واقعی به Balance، Orders یا Trading
- حداقل permission
- Withdrawal همیشه Disabled
- Trading در حالت تحلیلی Disabled
- IP whitelist در صورت امکان
- Secretها خارج از repository
- استفاده از Windows Credential Manager، DPAPI یا secret store
- Hyperliquid API Wallet Private Key اختیار معامله دارد
- Private Key برای Market Data عمومی هرگز درخواست یا استفاده نشود
- Codex Cloud نباید Secret واقعی دریافت کند

فایل appsettings.example.json یا معادل آن فقط placeholder داشته باشد.

Technical Spike دوره Trial:
پیش از پایان Trial یک آزمایش عملی تعریف و مستند کن:

1. فعال‌بودن All-in-One را تأیید کن.
2. Binance Spot و Binance Futures را هم‌زمان در INFO mode متصل کن.
3. در صورت امکان Bybit یا OKX را نیز متصل کن.
4. برای BTC یک Symbol متناظر از هر Connection مشخص کن.
5. حداقل ۳۰ دقیقه Trade زنده را هم‌زمان ثبت کن.
6. venue، connectionId، marketType، timestamps، price، size، notional و aggressor side را ذخیره کن.
7. خروجی Single Venue و Aggregated را مقایسه کن.
8. ثابت کن Binance-only فقط Binance را مصرف می‌کند.
9. ثابت کن Custom Basket دقیقاً Sourceهای انتخابی را مصرف می‌کند.
10. ثابت کن Aggregated mode Trade را دوبار نمی‌شمارد.
11. VPSV را روی Single Venue و Aggregate اجرا کن.
12. POC، VAH، VAL و Total Volume را مقایسه کن.
13. latency، gap، reconnect و تاریخچه هر Source را ثبت کن.
14. یک Replay fixture از داده ثبت‌شده ایجاد کن.
15. Capability Matrix را با نتایج واقعی به‌روزرسانی کن.

چون Codex Cloud به Quantower محلی من دسترسی ندارد:
- ابزار یا Indicator تشخیصی لازم را بساز.
- مراحل دقیق تست دستی را بنویس.
- خروجی و log مورد انتظار را مشخص کن.
- نتیجه تست محلی را از قبل موفق فرض نکن.
- بخش‌هایی که فقط توسط من قابل اجرا هستند واضح علامت بزن.

تست‌ها و معیار پذیرش:
- Golden Test برای POC، VAH، VAL و Delta با جواب دستی مشخص
- یکسان‌بودن VPSV در چند chart timeframe برای یک Trade dataset
- Binance-only
- Hyperliquid-only
- Custom Basket
- All Enabled Venues
- عدم ورود Source غیرفعال
- عدم duplicate
- Spot normalization
- Linear Perp normalization
- Inverse Contract normalization
- Strict USDT/USDC
- Converted USDT/USDC
- Tick-size differences
- Price-bin rounding
- Value Area tie-breaking
- UTC session boundary
- DST
- overnight session
- duplicate events
- out-of-order events
- late events
- reconnect
- order book gap/rebuild
- degraded source
- deterministic replay
- graceful shutdown
- performance/memory benchmark
- no network operation on UI thread
- Provider contract tests با fixture
- Live tests به‌صورت opt-in
- هیچ Test وابسته به Secret واقعی نباشد

ارزیابی تأثیر Indicatorها:
هدف «بهترین عملکرد ممکن برای Strategy» است، اما هیچ Indicator را سودآور فرض نکن.

- امکان export Featureها و Indicator values را فراهم کن.
- Walk-forward testing را در Roadmap قرار بده.
- Out-of-sample validation
- Ablation tests
- Baseline comparison
- Fees
- Slippage
- Latency
- Missing data
- Survivorship/data-selection bias
- No lookahead
- No repaint
- Overfitting control

Indicatorها را براساس این معیارها اولویت‌بندی کن:

- Data quality
- Independence/non-redundancy
- Actionability
- Stability
- Latency
- Computational cost
- Backtestability
- Explainability

Indicatorهای بسیار correlated را فقط برای زیادکردن تعداد پروژه نساز.

Milestoneها:

Milestone 0 ـ Repository Audit و Feasibility:
- بررسی repository
- بررسی Quantower API/version
- بررسی Trial capabilities
- بررسی Native Connectorها
- Capability Matrix اولیه
- Quantower/MMT Gap Analysis
- ساخت Plan اجرایی
- ساخت Diagnostic checklist
- سپس بدون توقف غیرضروری وارد Milestone 1 شو

Milestone 1 ـ Native Multi-connect Vertical Slice:
- abstractionهای Data Source
- شناسایی چند Connection
- Symbol mapping
- دریافت Public Trades
- Normalization
- Source Selection
- Deduplication
- Recorder
- Aggregation Engine
- محاسبات VPSV
- Golden Tests
- Quantower indicator/adapter
- راهنمای Smoke Test محلی

Milestone 2 ـ External Independence:
- Binance public provider
- Hyperliquid public provider
- UnifiedExternalFeed
- Local service
- Custom Quantower connector/adapter
- Live/replay parity
- عدم وابستگی محاسبات به Trial

Milestone 3 ـ P1 Indicators:
- VPVR
- CVD/Delta
- Footprint
- VWAP
- Bar Statistics
- Trade Counter
- Volume Bubbles

Milestone 4 ـ P2/P3:
- Order Book tools
- Heatmap
- Liquidations
- OI
- Funding
- TPO
- advanced/confluence tools

هر Milestone باید:
- هدف قابل مشاهده داشته باشد.
- فایل‌های تغییرکرده را ثبت کند.
- commandهای build/test داشته باشد.
- acceptance criteria داشته باشد.
- نتیجه واقعی را در PROJECT_STATUS ثبت کند.
- failureها را قبل از ادامه رفع کند.

تعریف Done برای اولین اجرای این درخواست:
حداقل یک Vertical Slice واقعی تحویل بده که شامل موارد زیر باشد:

- Solution قابل build
- Core data model
- Data Source abstractions
- Native Multi-connect adapter تا بیشترین حد ممکن
- حداقل یک Public Exchange Provider قابل تست در محیط
- Source Selection
- Symbol mapping
- Volume normalization
- Deduplication
- Event ordering
- Recorder یا Replay حداقلی
- محاسبات کامل و مستقل VPSV
- Golden Tests
- Quantower VPSV indicator/adapter
- مستند اتصال رایگان و Multi-connect
- مستند امنیت
- Capability Matrix
- Gap Analysis
- اجرای تست‌ها و گزارش نتیجه

اگر Quantower DLL یا Connector API در Cloud موجود نبود:
- Quantower-specific code را پشت interface جدا کن.
- از test double استفاده کن.
- Core و VPSV را کامل تست کن.
- build script ویندوز تهیه کن.
- مسیر اضافه‌کردن Reference به TradingPlatform.BusinessLayer.dll را مستند کن.
- مسیر نصب DLL در Quantower را براساس نسخه واقعی بررسی و مستند کن.
- blocker دقیق را ثبت کن.
- باقی Vertical Slice را متوقف نکن.

خروجی‌های الزامی:
- Source code
- Solution/project files
- README
- docs/PROJECT_SPEC.md
- docs/IMPLEMENTATION_PLAN.md
- docs/PROJECT_STATUS.md
- docs/DECISION_LOG.md
- docs/ARCHITECTURE.md
- docs/CAPABILITY_MATRIX.md
- docs/QUANTOWER_MMT_GAP_ANALYSIS.md
- docs/DATA_DICTIONARY.md
- docs/EXCHANGE_CONNECTIONS_FA.md
- docs/SECURITY.md
- docs/VALIDATION.md
- docs/KNOWN_LIMITATIONS.md
- config نمونه بدون Secret
- automated tests
- build/install scripts
- Quantower installation guide
- troubleshooting guide
- Trial test checklist
- post-Trial migration guide
- roadmap

منابع اولیه:
این منابع را با نسخه فعلی مستندات رسمی بررسی کن و اطلاعات قدیمی را کورکورانه تکرار نکن:

Quantower:
https://www.quantower.com/faq
https://www.quantower.com/pricing
https://help.quantower.com/quantower/connections/connections-manager
https://help.quantower.com/quantower/connections/connection-to-binance-futures
https://help.quantower.com/quantower/connections/connection-to-hyperliquid
https://help.quantower.com/quantower/quantower-algo/introduction
https://help.quantower.com/quantower/quantower-algo/downloading-history
https://help.quantower.com/quantower/quantower-algo/level2-data
https://help.quantower.com/quantower/quantower-algo/access-volume-analysis-data-from-indicators
https://help.quantower.com/quantower/analytics-panels/chart/volume-analysis-tools/volume-profiles
https://api.quantower.com/docs/TradingPlatform.BusinessLayer.Symbol.html
https://github.com/Quantower/Examples

Binance:
https://developers.binance.com/en/docs/products/spot/rest-api
https://developers.binance.com/en/docs/products/spot/web-socket-streams
https://github.com/binance/binance-spot-api-docs/blob/master/faqs/market_data_only.md
https://github.com/binance/binance-public-data

Hyperliquid:
https://hyperliquid.gitbook.io/hyperliquid-docs/for-developers/api
https://hyperliquid.gitbook.io/hyperliquid-docs/for-developers/api/websocket
https://hyperliquid.gitbook.io/hyperliquid-docs/for-developers/api/websocket/subscriptions
https://hyperliquid.gitbook.io/hyperliquid-docs/for-developers/api/info-endpoint
https://hyperliquid.gitbook.io/hyperliquid-docs/for-developers/api/rate-limits-and-user-limits
https://hyperliquid.gitbook.io/hyperliquid-docs/historical-data

MMT:
https://mmt.gg/features/
https://mmt.gg/features/order-flow/
https://mmt.gg/features/volume-profile/
https://mmt.gg/features/derivatives/
https://mmt.gg/indicators/

گزارش نهایی:
در پایان هر اجرا:

- ابتدا نتیجه واقعی را بگو.
- مشخص کن چه Milestoneهایی کامل شدند.
- مشخص کن چه کدی پیاده‌سازی شد.
- فایل‌های مهم را معرفی کن.
- commandهای build/test اجراشده و نتیجه را گزارش کن.
- تست‌های ردشده را پنهان نکن.
- بخش‌های نیازمند Quantower Windows را جدا کن.
- محدودیت‌های داده و لایسنس را بنویس.
- مشخص کن در Trial چه چیزی باید فوراً روی سیستم من تست یا ضبط شود.
- مرحله بعد را براساس بیشترین کاهش ریسک و بیشترین ارزش داده انتخاب کن.
- کار را فقط با Plan یا TODO بدون کد تمام نکن.