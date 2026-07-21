# TutorFlow — Implementation Plan V3

## ۰. یادداشت نسخه: چرا V3 و چه تفاوتی با V2 دارد

`IMPLEMENTATION_PLAN_V2.md` و `FINAL_ARCHITECTURE_PLAN.md` هر دو با این فرض نوشته شده‌اند که فاز ۱ تا ۴ (Domain policies، Booking invariant، Session lifecycle، Admin RBAC، Audit) هنوز شروع نشده‌اند. این سند، V3، جایگزین کامل آن دو سند است و بر اساس یک ممیزی تازه و کامل از سورس‌کد فعلی (نه از حافظه یا مستندات قدیمی) نوشته شده است.

**یافته‌ی محوری:** آن‌چه در V2/FINAL_ARCHITECTURE_PLAN به‌عنوان «فاز ۱ تا ۴» توصیف شده، امروز به‌طور کامل، با کیفیت بالا، و با پوشش تست گسترده در Backend پیاده‌سازی شده است — و بخش عمده‌ی UI مربوط به «فاز ۵» هم در Frontend از قبل ساخته شده. نیاز واقعی پروژه دیگر «ساخت از صفر» نیست، بلکه **بستن چند شکاف حاکمیتی/مستندسازی مشخص + تکمیل ۶ endpoint باقی‌مانده + راستی‌آزمایی یکپارچگی Frontend↔Backend + سخت‌سازی نهایی** است.

روش تحلیل: خواندن مستقیم کد فعلی در `backend/src` (تمام لایه‌های Domain، Application، Infrastructure، Web)، تمام ۱۷ ADR در `docs/adr/`، `docs/api/AUTHORIZATION_MATRIX.md`، و فهرست کامل `frontend/src` (۱۶۰+ فایل). هیچ بخشی از این سند حدسی نیست؛ هرجا مدرکی در کد پیدا نشد، به‌صراحت «نامشخص» علامت‌گذاری شده.

---

## ۱. تحلیل فنی دقیق وضعیت فعلی

### ۱.۱ Backend — وضعیت واقعی

| حوزه | وضعیت | مدرک |
|---|---|---|
| معماری Clean/DDD (Domain-Application-Infrastructure-Web) | ✅ کامل و بدون نقض لایه | جهت وابستگی پروژه‌ها (`.csproj`) تأیید شد: Domain بدون وابستگی، Application←Domain، Infrastructure←Application، Web←Application+Infrastructure |
| Aggregate boundaries (`AvailabilitySlot`, `Session` جدا) | ✅ تثبیت‌شده و ratify‌شده | `ADR-015` (Accepted) |
| جلوگیری از Double-Booking (CONST-1) | ✅ دو لایه: Domain guard (`IsConsumed` check-then-set) + DB unique index | `AvailabilitySlot.Book()`, `SessionConfiguration.cs:38` (`HasIndex(...).IsUnique()`), `ADR-014` |
| Session State Machine (Scheduled→Completed/Cancelled/No-Show) | ✅ کامل، تمام transitionها guard شده‌اند | `Session.cs` — `Cancel/Complete/MarkNoShow/Reschedule` |
| Age/Guardian policy (IDR-5, IDR-6) | ✅ پیاده‌سازی‌شده در `BookSessionCommandHandler` با خواندن Relationship confirmed | — |
| Relationship Invite/Confirm (هر دو طرف) | ✅ کامل، مطابق تصحیح IDR-4 | `Relationship.cs`, `ADR-003` Second Addendum Correction 1 |
| Tutor Approval/Suspension | ✅ کامل | `ApproveTutorCommandHandler`, `SuspendTutorCommandHandler` |
| Admin RBAC (Coarse-grained) | ✅ کامل، ۱۸ endpoint با `RequirePermission` | `Permission.cs`, `RolePermissionCatalog.cs`, `AuthorizationMiddleware.cs` |
| Fine-grained ownership (Resource-instance) | ✅ کامل روی ۳۱ از ۳۷ endpoint | `OwnershipExtensions.VerifyOwnTutorId/VerifyIsParty/VerifyAuthenticated` |
| Audit Trail | ✅ کامل، same-transaction | `AuditEntry`, `AuditDomainEventHandler` (فقط ۶ رویداد مصوب طبق `ADR-016`) |
| احراز هویت (Login/Token/Lockout) | ✅ کامل و تست‌شده | `ADR-017` (Password + opaque token + sliding 30min) |
| تست‌ها | ✅ ۴۱۶+ تست پاس، صفر خطا | `Domain.Tests` (53)، `Application.Tests` (200)، `Infrastructure.Tests` (45)، `Web.Tests` (118) |
| **۶ endpoint حاکمیتی باز** | ⛔ عمداً بدون auth check، منتظر تصمیم Director | `AUTHORIZATION_MATRIX.md` §5 — فهرست دقیق در بخش ۳ |
| **۲ تصمیم امنیتی بدون ADR** | ⚠️ پیاده‌سازی شده ولی مستندسازی حاکمیتی ندارند | Account Lockout، Absolute Session Lifetime (جزئیات بخش ۳) |
| **۹ ADR با Status نامنسجم** | ⚠️ محتوا صحیح، ولی فوتر سند هنوز «Proposed» است | `ADR-001,002,004-010` |

### ۱.۲ Frontend — وضعیت واقعی

برخلاف فرض V2 (که فاز ۵ را «ساخت flowهای UI» توصیف می‌کرد)، Frontend از قبل شامل موارد زیر است — همه با تست co-located (`*.test.tsx`):

| ناحیه | صفحات/اجزای موجود |
|---|---|
| Auth | `LoginPage`, `AdminResetPasswordPage`, `AuthProvider`/`AuthContext` (token در حافظه، مطابق ADR-017)، `apiClient` با Bearer interceptor |
| Identity/Registration | `RegisterTutorPage`, `RegisterStudentPage`, `RegisterParentGuardianPage`, `TutorDirectoryPage`, `TutorDetailPage`, `TutorOfferingPage`, `StudentDetailPage`, `ParentGuardianDetailPage`, `RelationshipsPage`, `AdminPendingTutorsPage`, `TutorApprovalActions` |
| Scheduling | `DeclareAvailabilityPage`, `AvailabilitySlotDetailPage`, `BookSessionPage`, `SessionDetailPage`, `SessionActions`, `StudentSessionListPage`, `TutorSessionListPage` |
| Discovery | `TutorSearchPage` |
| Oversight | `AdminDashboardPage`, `GlobalSessionListPage` |
| زیرساخت مشترک | `DataTable`, `Form/FormTextField/FormSelect/FormCheckbox`, `LoadingState/ErrorState/EmptyState`, `NotificationProvider`, `ConfirmDialogProvider`, `ErrorBoundary`, `PageHeader`, `IdLookupForm`, routing کامل با lazy-loading (`router.tsx`) |

**یافته‌ی مهم Frontend:** فایل `frontend/src/layouts/AuthPlaceholderBanner.tsx` روی همه‌ی صفحات نمایش داده می‌شود و ادعا می‌کند: *"role-based permissions are not yet enforced — every signed-in account can currently reach every action regardless of role."* این ادعا دیگر **تا حد زیادی نادرست** است — Backend اکنون هم coarse-grained RBAC و هم fine-grained ownership را روی اکثر endpointها enforce می‌کند. این بنر باید بعد از بستن ۶ endpoint باز (بخش ۳) به‌روزرسانی یا حذف شود؛ نگه‌داشتنش به همین شکل، خودش یک اطلاع‌رسانی گمراه‌کننده به کاربر است.

هیچ route guard مبتنی بر نقش در سمت Client وجود ندارد (خود بنر این را تأیید می‌کند: «Acting as (dev only) selector... grants no real access») — این طبق `ADR-010` **درست** است (اجرای واقعی مجوز همیشه باید سمت سرور باشد)، اما به‌عنوان تجربه‌ی کاربری، فیلتر کردن ناوبری بر اساس نقش واقعی کاربر (نه فقط یک انتخاب‌گر dev-only) هنوز باقی مانده است.

---

## ۲. فهرست فایل‌های Backend

### ۲.۱ مستقیماً قابل استفاده (بدون تغییر)
عملاً تمام `src/Domain/**`، تمام Handlerهای `src/Application/**` (۴۰+ فایل)، اکثر `src/Infrastructure/**`، و تمام `src/Web/Middleware/**` و `src/Web/Endpoints/**` (به‌جز موارد زیر). این حجم زیاد نتیجه‌ی مستقیم دو ممیزی معماری کامل است که امروز روی همین کد انجام شد و **هیچ نقض invariant، هیچ نقض لایه‌بندی، و هیچ IDOR باقی‌مانده‌ای پیدا نشد**.

### ۲.۲ نیاز به Refactoring جزئی (بدون تغییر رفتار کسب‌وکار)

| فایل | مشکل | اصلاح پیشنهادی |
|---|---|---|
| `src/Infrastructure/Identity/Repositories/TutorRepository.cs` (`SearchDiscoverableAsync`) | کل جدول Tutorهای discoverable را در حافظه لود می‌کند، سپس فیلتر Subject/Language/Location را در LINQ-to-Objects اعمال می‌کند | فیلتر را به سطح SQL منتقل کنید (EF Core 9 مقایسه‌ی `.Value` روی owned type را ساپورت می‌کند) |
| `src/Web/Endpoints/ResultMapping.cs` (`StatusCodeFor`) | خطای `EmailAlreadyRegistered` (Register×3) به‌جای ۴۰۹ به ۴۰۰ map می‌شود، برخلاف الگوی مشابه `SlotAlreadyBooked` | شرط `Contains("AlreadyBooked")` را به `Contains("AlreadyBooked") \|\| Contains("AlreadyRegistered")` تغییر دهید |
| `src/Infrastructure/Common/DomainEventDispatcher.cs` | کامنت کلاس ادعا می‌کند «صفر Handler ثبت شده» و «ADR-016 هنوز Proposed است» — هر دو نادرست‌اند | کامنت را به‌روزرسانی کنید (`AuditDomainEventHandler` فعال است؛ ADR-016 Accepted است) |
| `IStudentRepository`/`ITutorRepository`/... (`GetByIdAsync`) | یک متد always-tracked مشترک بین مسیرهای خواندنی و نویسشی — بار اضافی change-tracking روی هر GET | افزودن یک overload با `AsNoTracking` برای مسیرهای فقط-خواندنی (اولویت پایین) |

### ۲.۳ نیاز به تغییر برای فازهای بعدی (نیازمند تصمیم/کد جدید)

| فایل | نوع تغییر | پیش‌نیاز |
|---|---|---|
| `src/Application/Identity/Handlers/GetRelationshipByIdQueryHandler.cs` | افزودن fine-grained ownership check | تصمیم Director (بخش ۳، فاز ۱.۵) |
| `src/Application/Identity/Handlers/GetRelationshipsByAccountIdQueryHandler.cs` | افزودن fine-grained ownership check | همان |
| `src/Application/Identity/Handlers/GetPendingTutorsQueryHandler.cs` | تعیین Coarse-grained permission (`RequirePermission`) | همان |
| `src/Application/Scheduling/Handlers/GetStudentScheduleQueryHandler.cs` | افزودن fine-grained ownership check | همان |
| `src/Application/Scheduling/Handlers/GetTutorScheduleQueryHandler.cs` | افزودن fine-grained ownership check | همان |
| `src/Application/Scheduling/Handlers/GetTutorAvailabilitySlotsQueryHandler.cs` | افزودن fine-grained ownership check | همان |
| `src/Domain/Identity/Account.cs` (`MaxFailedLoginAttempts`, `LockoutDuration`) | تصویب رسمی از طریق ADR addendum، یا بدون تغییر کد | تصمیم Director (بخش ۳، فاز ۱.۲) |
| `src/Domain/Identity/AuthToken.cs` (`AbsoluteLifetime`) | تصویب رسمی، یا حذف برای تطابق با متن فعلی ADR-017 | همان |
| `docs/adr/ADR-001,002,004-010*.md` | اصلاح فیلد Status به Accepted | تصمیم مستندسازی، بدون نیاز به کد |
| `frontend/src/layouts/AuthPlaceholderBanner.tsx` | به‌روزرسانی متن پس از تکمیل فاز ۲ | تکمیل بخش ۲.۱/۲.۲ (بخش ۳) |

---

## ۳. برنامه‌ی اجرایی فاز ۱ و فاز ۲ (بازتعریف‌شده)

### فاز ۱ — تطبیق حاکمیتی و بستن شکاف‌های شناخته‌شده (Governance Reconciliation)

هدف: هیچ تصمیم امنیتی/معماری بدون سند تصویب‌شده باقی نماند، و هیچ سند حاکمیتی با وضعیت واقعی کد در تناقض نباشد.

| تسک | هدف | فایل‌های مرتبط | اولویت | خروجی قابل اندازه‌گیری | ریسک | راهکار |
|---|---|---|---|---|---|---|
| ۱.۱ اصلاح Status ADRهای ۹گانه | همسو کردن مستندات حاکمیتی با واقعیت پیاده‌سازی‌شده | `docs/adr/ADR-001,002,004,005,006,007,008,009,010.md` | بالا | ۹ فایل Status="Accepted" با تاریخ | هیچ (فقط مستندسازی) | — |
| ۱.۲ تصمیم رسمی برای Account Lockout + Absolute Session Lifetime | یا تصویب رسمی این دو پارامتر امنیتی از طریق افزودنی به ADR-017، یا هم‌راستا کردن کد با متن فعلی ADR-017 (که صراحتاً می‌گوید «no absolute maximum») | `src/Domain/Identity/Account.cs`, `AuthToken.cs`, `docs/adr/ADR-017-*.md` | بالا (تناقض فعال با یک ADR مصوب) | یک ADR Addendum مصوب، یا commit حذف `AbsoluteLifetime` | تصمیم اشتباه درباره‌ی trade-off امنیتی | مشابه سابقه‌ی خود پروژه (ADR-011) عمل شود: تصمیم را صریح از Director بگیرید، در کد پیاده‌سازی نکنید |
| ۱.۳ اصلاح کامنت نادرست در DomainEventDispatcher | جلوگیری از گمراهی توسعه‌دهنده‌ی بعدی هنگام دیباگ Audit | `src/Infrastructure/Common/DomainEventDispatcher.cs` | متوسط | کامنت واقعیت را منعکس کند | هیچ | — |
| ۱.۴ اصلاح status code برای EmailAlreadyRegistered | یکنواختی HTTP contract برای خطاهای «قبلاً موجود است» | `src/Web/Endpoints/ResultMapping.cs` | متوسط | تست جدید: ثبت‌نام تکراری → ۴۰۹ | شکستن تست موجودی که ۴۰۰ انتظار داشت | grep برای تست‌های فعلی قبل از تغییر |
| ۱.۵ تصمیم Director برای ۶ ردیف Open در AUTHORIZATION_MATRIX | پیش‌نیاز مستقیم فاز ۲ | `docs/api/AUTHORIZATION_MATRIX.md` | بحرانی (بلاک‌کننده‌ی فاز ۲) | یک ADR Addendum که دقیقاً هر ۶ endpoint را تعیین‌تکلیف کند | تعریف قانون نادرست → دور دوم رفع نقص | از الگوی از قبل تأییدشده در همین پروژه (Decision 2/3/4/7 برای ۵ endpoint مشابه) پیروی شود |
| ۱.۶ (اختیاری) افزودن Concurrency Token به Account | رفع race condition جزئی در شمارش تلاش‌های ناموفق لاگین | `src/Domain/Identity/Account.cs`, `src/Infrastructure/Persistence/Configurations/*Configuration.cs` | پایین | تست concurrent-load برای lockout | نیاز به migration جدید | migration جداگانه و کوچک |

**Decision Gate فاز ۱:** ADR-017 addendum (یا اصلاح کد) برای ۱.۲ تصویب شده باشد؛ تصمیم ۶ endpoint (۱.۵) به‌صورت مکتوب ثبت شده باشد؛ ۹ ADR وضعیت Accepted داشته باشند.

---

### فاز ۲ — بستن Endpointهای باز + راستی‌آزمایی یکپارچگی Frontend↔Backend

هدف: تمام سطح API طبق حاکمیت مصوب محافظت‌شده باشد، و Frontend موجود واقعاً با رفتار امنیتی فعلی Backend هم‌خوان باشد.

| تسک | هدف | فایل‌های مرتبط | اولویت | خروجی قابل اندازه‌گیری | ریسک | راهکار |
|---|---|---|---|---|---|---|
| ۲.۱ پیاده‌سازی fine-grained authorization برای ۵ endpoint | بستن `GET /relationships/{id}`, `GET /accounts/{id}/relationships`, `GET /students/{id}/schedule`, `GET /tutors/{id}/schedule`, `GET /tutors/{id}/availability-slots` طبق تصمیم فاز ۱.۵ | ۵ Handler فهرست‌شده در بخش ۲.۳ | بحرانی | ماتریس تست کامل (۴۰۱/۴۰۳/موفق/Admin) برای هر ۵ endpoint، دقیقاً به الگوی WP4 Priority 5 موجود | نقض دوباره‌ی حاکمیت با اختراع قانون | فقط قانون مصوب فاز ۱.۵ را پیاده‌سازی کنید؛ از `VerifyIsParty`/`VerifyOwnTutorId`/`VerifyAuthenticated` موجود دوباره استفاده کنید |
| ۲.۲ پیاده‌سازی permission برای `GET /tutors/pending` | بستن آخرین ردیف باز | `GetPendingTutorsQueryHandler.cs`, `IdentityEndpoints.cs` | بالا | تست ۴۰۱/۴۰۳/موفق Admin | همان | همان |
| ۲.۳ به‌روزرسانی AuthPlaceholderBanner | حذف اطلاع‌رسانی گمراه‌کننده به کاربر واقعی سیستم | `frontend/src/layouts/AuthPlaceholderBanner.tsx` | بالا | بنر یا حذف شود یا متنش با وضعیت واقعی هم‌خوان شود | — | فقط بعد از تکمیل ۲.۱/۲.۲ انجام شود |
| ۲.۴ بهینه‌سازی SearchDiscoverableAsync | رفع ریسک مقیاس‌پذیری شناسایی‌شده | `TutorRepository.cs` | متوسط | تست Infrastructure جدید که SQL تولیدشده را فیلتر شده تأیید کند | تغییر رفتار فیلتر (false negative/positive) | تست‌های موجود `SearchTutorsQueryHandlerTests` باید بدون تغییر پاس شوند |
| ۲.۵ تست یکپارچگی End-to-End Frontend↔Backand | تأیید این‌که UI موجود واقعاً روی auth واقعی کار می‌کند، نه فقط mock | مسیرهای بحرانی: login→search→book→cancel، register→invite→confirm، admin approve/suspend | بالا | حداقل ۳ سناریوی E2E دستی یا Playwright روی backend واقعی | کشف دیرهنگام ناسازگاری Contract | این تست باید *قبل* از فاز Hardening اجرا شود، نه بعد از آن |
| ۲.۶ (اختیاری) AsNoTracking read path | کاهش بار EF روی پرترافیک‌ترین GETها | Repository interfaceهای بخش ۲.۲ | پایین | Benchmark قبل/بعد | — | — |

**Decision Gate فاز ۲:** هر ۶ endpoint در `AUTHORIZATION_MATRIX.md` از حالت Open خارج شده و WP3 Status="Protected" باشد؛ `dotnet test` سبز؛ AuthPlaceholderBanner به‌روزرسانی‌شده باشد.

---

## ۴. Frontend — تسک‌های ریز (به‌جای «فاز ۵ بساز از صفر»، اکنون «تکمیل و اتصال نهایی»)

از آن‌جا که اکثر صفحات از قبل ساخته و تست‌شده‌اند، تمرکز این بخش روی **رفتار در برابر Backend واقعی** است، نه ساخت کامپوننت جدید.

### ۴.۱ Booking UX
- بررسی `BookSessionPage.tsx`/`AvailabilitySlotDetailPage.tsx`/`SessionActions.tsx` در برابر خطاهای Domain واقعی Backend: `BookSessionCommand.SlotAlreadyBooked` (اکنون ۴۰۹)، `IDR-6` (minor بدون Relationship تأییدشده) — پیام‌های خطای اختصاصی هر کد، نه فقط generic "خطا رخ داد".
- تست: بوکینگ هم‌زمان دو کاربر روی یک Slot → یکی موفق، دیگری باید پیام واضح "این زمان دیگر در دسترس نیست" ببیند (نه یک خطای عمومی شبکه).

### ۴.۲ Session Management UI
- `SessionActions.tsx`: دکمه‌های Cancel/Reschedule/Complete/No-Show باید بر اساس نقش کاربر (Tutor/Student/ParentGuardian/Admin) *پیش از* فراخوانی API فیلتر شوند (UX)، ولی تصمیم نهایی همیشه از پاسخ ۴۰۳ سرور گرفته شود — هرگز فرض نکنید فیلتر UI کافی است.
- بررسی این‌که وضعیت‌های نمایشی (`sessionStatus.ts`) دقیقاً با ۴ حالت واقعی Backend (`SessionStatus` enum) یک‌به‌یک مطابقت دارند.

### ۴.۳ Registration & Identity UX
- سه صفحه‌ی ثبت‌نام از قبل کامل‌اند؛ باقی‌مانده: نگاشت پیام خطای `RegisterXCommand.EmailAlreadyRegistered` (بعد از فاز ۱.۴ که کد ۴۰۹ می‌شود) به یک پیام کاربرپسند فارسی/انگلیسی مشخص، نه پیام عمومی Conflict.
- `RelationshipsPage.tsx`: تأیید این‌که پیام "Only the other party may confirm" (نتیجه‌ی Correction 1 در ADR-003) به‌وضوح به کاربر نمایش داده می‌شود.

### ۴.۴ Admin UX
- `AdminDashboardPage.tsx`, `AdminPendingTutorsPage.tsx`, `GlobalSessionListPage.tsx`, `TutorApprovalActions.tsx`, `AdminResetPasswordPage.tsx` از قبل موجودند.
- کار باقی‌مانده: پس از تکمیل فاز ۲، ناوبری (`NavSidebar.tsx`) باید بر اساس نقش واقعی کاربر لاگین‌شده (نه فقط "Acting as (dev only)") آیتم‌های Admin را نشان/پنهان کند — این صرفاً UX است، اجرای واقعی مجوز همچنان سمت سرور می‌ماند (مطابق ADR-010).

### ۴.۵ Error / Notification / Loading
- `LoadingState`/`ErrorState`/`EmptyState`/`NotificationProvider`/`ConfirmDialogProvider`/`ErrorBoundary` از قبل پیاده‌سازی و تست‌شده‌اند — نیازی به ساخت مجدد نیست.
- کار باقی‌مانده: یک نگاشت مرکزی از `ApiRequestError.code` (مثل `LoginCommand.AccountLocked`, `GetStudentByIdQuery.Unauthenticated`) به پیام فارسی/انگلیسی مشخص برای هر کد، به‌جای نمایش پیام خام Backend.

---

## ۵. استراتژی Rollback هر فاز

| فاز | ریسک اصلی Rollback | استراتژی |
|---|---|---|
| فاز ۱ (Governance) | تغییر رفتار امنیتی (حذف Absolute Lifetime) کاربران فعال را قطع می‌کند | تغییرات کد این فاز حداقلی‌اند (یک خط `TimeSpan`)؛ در صورت مشکل، revert تک-commit بدون نیاز به migration |
| فاز ۲ (بستن ۶ endpoint) | افزودن authorization جدید ممکن است دسترسی مشروع موجود در Frontend را قطع کند | هر endpoint در یک commit مجزا با تست کامل قبل/بعد؛ در صورت شکست، revert تک-endpoint بدون تأثیر بر ۵ تای دیگر (دقیقاً همان الگوی اجراشده برای ۵ endpoint قبلی) |
| فاز ۲.۴ (بهینه‌سازی Search) | تغییر منطق فیلتر ممکن است نتایج جست‌وجو را عوض کند | تست‌های Regression موجود (`SearchTutorsQueryHandlerTests`) باید بدون تغییر خروجی پاس شوند؛ در غیر این صورت commit را جدا از بقیه‌ی فاز ۲ نگه دارید تا مستقل revert شود |
| فاز Frontend (بخش ۴) | تغییر پیام خطا/ناوبری باعث رگرسیون بصری شود | هر تغییر با تست `*.test.tsx` موجود همراه شود؛ چون کامپوننت‌ها از قبل تست‌محور ساخته شده‌اند، رگرسیون بصری زود تشخیص داده می‌شود |
| کلی | Migration دیتابیس (فاز ۱.۶) قابل rollback نبودن | هر migration باید `Down()` معتبر داشته باشد و روی یک کپی از دیتابیس staging قبل از production تست شود |

---

## ۶. تست‌های لازم پیش از شروع هر فاز

| فاز | تست‌های پیش‌نیاز (باید سبز باشند قبل از شروع) |
|---|---|
| فاز ۱ | کل مجموعه‌ی فعلی: `dotnet test` → ۴۱۶/۴۱۶ (baseline) |
| فاز ۲.۱/۲.۲ | برای هر یک از ۶ endpoint: تست Application-layer (unit) برای ۴ حالت (unauthenticated→401، unauthorized→403، owner/party→success، Admin→success) — دقیقاً همان الگوی `GetSessionByIdQueryHandlerTests.cs` که برای ۵ endpoint قبلی نوشته شد |
| فاز ۲.۴ | یک تست Infrastructure جدید که تأیید کند SQL تولیدی شامل `WHERE` روی Subject/Language/Location است (نه فیلتر پس از لود کامل) |
| فاز ۲.۵ | حداقل ۳ سناریوی E2E دستی/Playwright: (۱) ثبت‌نام Tutor → تأیید Admin → جست‌وجو → بوکینگ توسط Student، (۲) دعوت Relationship توسط Parent → تأیید توسط Student → مشاهده‌ی پروفایل متقابل، (۳) لاگین اشتباه ۵ بار → قفل حساب → پیام مناسب در UI |
| فاز Frontend (بخش ۴) | تست‌های موجود `*.test.tsx` (۴۰+ فایل) باید بدون تغییر سبز بمانند؛ برای هر پیام خطای جدید، یک تست جدید در `ErrorState.test.tsx` یا معادل صفحه‌ی مربوطه اضافه شود |

---

## ۷. پیشنهاد Database و Infrastructure

انتخاب فناوری از قبل نهایی و مصوب است (`ADR-013`: PostgreSQL، `ADR-014`: Unique Constraint برای Concurrency، `ADR-016`: Same-transaction Audit) — این بخش صرفاً پیشنهادهای سخت‌سازی (Hardening) روی همین انتخاب‌هاست، نه تغییر فناوری:

1. **ایندکس‌گذاری تکمیلی:** جدول `Relationships` فاقد ایندکس روی `ParentGuardianId`/`StudentId` است (بررسی مستقیم `RelationshipConfiguration.cs` این را تأیید کرد)، در حالی‌که `GetByAccountIdAsync` روی همین دو ستون فیلتر می‌کند و اکنون علاوه‌بر مسیر Booking، روی دو Query Handler دیگر هم استفاده می‌شود. پیشنهاد: افزودن یک composite/covering index در یک migration مستقل و کوچک.
2. **Concurrency Token روی `Account`:** برای رفع race condition شناسایی‌شده در شمارش تلاش‌های ناموفق لاگین (بخش ۱.۶).
3. **Migration جدید صرفاً برای موارد بالا:** هیچ migration بزرگ یا تغییر schema گسترده لازم نیست؛ دو migration کوچک و مستقل کافی است.
4. **Health Check:** endpoint `GET /health` از قبل Public و مصوب است (`ADR-003` Addendum Decision 5) — برای Observability واقعی (فاز ۲.۵/Hardening)، بررسی شود که این health check واقعاً اتصال دیتابیس را هم چک می‌کند یا فقط zSaaS-liveness است؛ در کد فعلی این جزئیات تأیید نشده و باید بررسی شود.
5. **بدون معرفی زیرساخت جدید:** طبق تصمیم صریح پروژه (`ADR-016`: "no new infrastructure")، هیچ صف پیام، Outbox، یا سرویس جانبی در این نسخه معرفی نشود.

---

## ۸. خلاصه‌ی اجرایی برای تیم توسعه

**وضعیت واقعی:** بک‌اند TutorFlow یک سیستم بالغ، تست‌شده (۴۱۶+ تست)، و به‌طور مکرر ممیزی‌شده است — نه یک پروژه در نقطه‌ی صفر. فرانت‌اند نیز بیش از ۹۰٪ از مسیرهای اصلی کاربری را از قبل پوشش می‌دهد.

**آن‌چه واقعاً باقی مانده:**
1. **بستن ۶ endpoint حاکمیتی باز** (نیازمند یک تصمیم صریح از مدیر پروژه قبل از هر کدنویسی — دقیقاً به همان روشی که برای ۵ endpoint مشابه قبلاً با موفقیت انجام شد).
2. **رسمی‌سازی دو تصمیم امنیتی بدون سند** (Account Lockout، Absolute Session Lifetime) — یکی از آن‌ها فعلاً با یک ADR مصوب در تناقض است.
3. **همسو کردن مستندات با واقعیت** (Status نه ADR، بنر Frontend گمراه‌کننده، یک کامنت نادرست در کد).
4. **دو اصلاح کوچک کیفیت کد** (status code ۴۰۹ برای ایمیل تکراری، بهینه‌سازی جست‌وجوی Tutor).
5. **راستی‌آزمایی یکپارچگی**: اطمینان از این‌که UI موجود واقعاً با رفتار امنیتی فعلی Backend سازگار است.

**زمان‌بندی تخمینی:** با توجه به این‌که حجم کار واقعی به‌مراتب کمتر از V2 است، فاز ۱ (تطبیق حاکمیتی) ۲-۳ روز کاری و فاز ۲ (بستن endpointها + یکپارچگی) ۴-۵ روز کاری تخمین زده می‌شود — نه ۵ روز و ۷.۵ روز که V2 برای «ساخت از صفر» پیش‌بینی کرده بود.

**پیام اصلی به تیم:** خطر واقعی این پروژه دیگر «نبود قوانین دامنه» نیست؛ خطر واقعی، **انجام دوباره‌ی کاری است که قبلاً درست انجام شده**، و **نادیده گرفتن شکاف‌های واقعی و کوچکی که این ممیزی مشخص کرد**. اولویت تیم باید دقیقاً همان ۶ آیتم بالا باشد.
