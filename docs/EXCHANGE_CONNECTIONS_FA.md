# دادهٔ عمومی و Trial

CLI این مرحله از endpoint رسمی market-data-only Binance Spot استفاده می‌کند و API Key ندارد. دستور record در README آمده است. محدودیت شبکه/منطقه و قوانین صرافی رعایت شوند؛ خطای دسترسی مجوز دورزدن محدودیت نیست.

در Quantower، Account/Licence را باز کنید و Trial و انقضا را ثبت کنید. سپس Connections Manager و وضعیت Binance Spot را ببینید. INFO mode و Multi-connect را فقط اگر واقعاً در نسخه/لایسنس فعلی ارائه می‌شوند انتخاب کنید؛ فعال‌بودن آن‌ها فرض نشده است.

برای هر Connection نماد دقیق، نوع بازار و واحد Size را بررسی کنید. Recorder فعلی فقط Spot است؛ Futures را به آن معرفی نکنید. Connected به‌تنهایی کافی نیست: Trade payload، بازه/تعداد واقعی History و L2 snapshot/diff باید جدا آزمایش شوند.

برای این مرحله حساب معاملاتی، کلید یا permission خصوصی لازم نیست. پس از Trial، Core/CLI/Replay مستقل می‌مانند؛ قابلیت‌های GUI/Connector/اتصال بومی به لایسنس فعلی وابسته‌اند و هنوز باید تأیید شوند.
