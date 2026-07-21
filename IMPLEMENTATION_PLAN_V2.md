> **SUPERSEDED** — This plan is historical. See IMPLEMENTATION_PLAN_V3.md.

# TutorFlow - Implementation Plan V2

## ۱. خلاصه اجرایی

این برنامه یک نسخه‌ی عملیاتی و فازبندی‌شده برای پیاده‌سازی TutorFlow است که بر اساس معماری موجود و اولویت‌های هسته‌ی کسب‌وکار طراحی شده است. هدف اصلی این است که تیم توسعه بتواند از این مستند مستقیماً برای اجرای پروژه استفاده کند.

تمرکز اصلی در این نسخه روی موارد زیر است:
- تثبیت قوانین دامنه و سیاست‌های کسب‌وکار
- تضمین correctness و consistency در Booking
- تعریف دقیق چرخه‌ی زندگی Session
- نصب و اجرای کنترل‌های دسترسی و Admin workflows
- کاهش ریسک‌های اجرایی از طریق Decision Gates و معیارهای Done


## ۲. فازها با جزئیات کامل

### فاز ۱ — پایه‌گذاری قوانین دامنه و سیاست‌های کسب‌وکار

#### هدف
تثبیت قوانین اصلی در سطح Domain و آماده‌سازی زیرساخت برای فازهای بعدی.

#### تسک‌های مشخص
- تسک ۱.۱: ایجاد کلاس‌های Policy در Domain/Identity
  - AgePolicy.cs
  - GuardianConsentPolicy.cs
  - متدهای نمونه:
    - IsAdult(DateTime dateOfBirth)
    - RequiresConsent(AgeClassification)
- تسک ۱.۲: ایجاد Value Objects برای وضعیت‌ها
  - GuardianConsentStatus: Pending, Approved, Rejected, Expired
  - SessionStatus: Scheduled, Active, Completed, Cancelled, NoShow
- تسک ۱.۳: پیاده‌سازی State Machine برای Session
  - متد CanTransitionTo(SessionStatus newStatus)
  - تعریف قوانین انتقال مجاز
- تسک ۱.۴: نوشتن تست‌های واحد برای هر Policy
  - حداقل ۵ سناریو برای هر Policy
  - حداقل ۵ سناریو برای Session State Machine

#### خروجی‌های قابل اندازه‌گیری
- ۱۰۰٪ Policyها و Value Objects در کد موجود باشند
- ۱۰۰٪ تست‌های Policy پاس شوند
- لاگ یا مستند تصمیم‌گیری برای هر Policy ثبت شود

#### ریسک‌ها
- ambiguity در تصمیم‌گیری‌های کسب‌وکار
- تفاوت بین سیاست‌های حقوقی و نیازهای محصول

#### راهکارها
- تصمیمات در قالب ADR یا Decision Note ثبت شوند
- سیاست‌ها به‌صورت configurable و conservative طراحی شوند

---

### فاز ۲ — پیاده‌سازی Booking Flow و Consistency

#### هدف
تضمین این‌که فرآیند Booking ایمن، transactional و بدون Double-Booking باشد.

#### تسک‌های مشخص
- تسک ۲.۱: نهایی‌سازی مرز Aggregate بین AvailabilitySlot و Session
- تسک ۲.۲: پیاده‌سازی Booking در AvailabilitySlot
- تسک ۲.۳: اعمال invariant اصلی
  - AvailabilitySlot فقط یک بار مصرف شود
- تسک ۲.۴: ثبت Audit Entry برای هر Booking
- تسک ۲.۵: پیاده‌سازی validation برای Eligibility
  - Student/Parent eligibility
  - Tutor approval/suspension
  - Guardian requirement برای minors
- تسک ۲.۶: نوشتن Integration Tests برای Booking
  - حداقل ۳ سناریوی مختلف

#### خروجی‌های قابل اندازه‌گیری
- حداقل ۵ سناریوی Double-Booking در تست‌ها پوشش داده شود
- هیچ Bookingی بدون Audit Entry ثبت نشود
- ۱۰۰٪ متدهای عمومی Aggregate Booking دارای تست واحد باشند
- Integration Test برای End-to-End Booking نوشته شود

#### ریسک‌ها
- مشکل در concurrency
- سوءتفاهم در مرز Aggregate

#### راهکارها
- transactional invariant + DB constraint استفاده شود
- مرز Aggregate در این فاز بدون تغییر باقی بماند

---

### فاز ۳ — پیاده‌سازی چرخه‌ی جلسه و قوانین Parent/Guardian

#### هدف
امکان مدیریت امن و پیش‌بینی‌پذیر Session از طریق cancel، reschedule و تغییر وضعیت فراهم شود.

#### تسک‌های مشخص
- تسک ۳.۱: پیاده‌سازی قوانین Cancel/Reschedule
- تسک ۳.۲: پیاده‌سازی State Transition برای Session
- تسک ۳.۳: اعمال منطق Parent/Guardian برای Minor Students
- تسک ۳.۴: ثبت Audit برای تغییر وضعیت Session
- تسک ۳.۵: نوشتن Unit/Integration Tests برای Session Lifecycle

#### خروجی‌های قابل اندازه‌گیری
- حداقل ۱ مسیر کامل Cancel و ۱ مسیر Reschedule پوشش داده شده باشد
- همه‌ی State Transitions در تست‌ها formal شده باشند
- هیچ اقدام غیرمجاز برای Parent/Guardian یا Student دیگر مجاز نباشد

#### ریسک‌ها
- ambiguity در business rules
- پیچیدگی بیشتر از حد در transition rules

#### راهکارها
- برای v1 یک policy conservative و شفاف انتخاب شود
- State Machine با محدودیت‌های روشن پیاده‌سازی شود

---

### فاز ۴ — پیاده‌سازی Admin workflows و کنترل‌های دسترسی

#### هدف
امکان مدیریت و پشتیبانی از سیستم توسط Admin با حداقل ریسک و حداکثر شفافیت.

#### تسک‌های مشخص
- تسک ۴.۱: تعریف نقش‌ها و Permissions برای Admin
- تسک ۴.۲: پیاده‌سازی Tutor Approval/Suspension
- تسک ۴.۳: پیاده‌سازی Account Management
- تسک ۴.۴: پیاده‌سازی Visibility بر روی Schedules
- تسک ۴.۵: ثبت Audit برای همه‌ی عملیات Admin

#### خروجی‌های قابل اندازه‌گیری
- حداقل ۱ workflow کامل Approval/Suspension پیاده‌سازی شود
- دسترسی‌های Admin از طریق tests تأیید شوند
- همه‌ی عملیات Admin در Audit ثبت شوند

#### ریسک‌ها
- دسترسی بیش از حد
- over-engineering در permission model

#### راهکارها
- برای v1 فقط مدل ساده‌ی RBAC و ownership-based validation استفاده شود

---

### فاز ۵ — Hardening، UX و آمادگی Release

#### هدف
تبدیل سیستم از نسخه‌ی فنی قابل اجرا به نسخه‌ی قابل ارائه و قابل استقرار.

#### تسک‌های مشخص
- تسک ۵.۱: تکمیل flowهای UI برای Registration، Booking، Session Management
- تسک ۵.۲: بهبود Error Handling و پیام‌های کاربرپسند
- تسک ۵.۳: اضافه‌کردن Observability و Health Checks
- تسک ۵.۴: نوشتن Smoke Tests برای مسیرهای بحرانی
- تسک ۵.۵: آماده‌سازی Deployment و Migration Strategy

#### خروجی‌های قابل اندازه‌گیری
- همه‌ی مسیرهای اصلی در E2E تست شوند
- Health Check و Logging برای Booking Failures موجود باشد
- نسخه‌ی قابل ارائه برای Demo آماده باشد

#### ریسک‌ها
- تزئین بیش از حد قبل از تثبیت هسته
- نبود monitoring مناسب

#### راهکارها
- تمرکز روی مسیرهای critical و بحرانی
- monitoring از همان ابتدای فاز ۵ در نظر گرفته شود


## ۳. جدول وابستگی‌ها

| فاز | وابسته به خروجی‌های فاز |
|-----|------------------------|
| فاز ۲ | فاز ۱ (Policyها و مدل‌های دامنه) |
| فاز ۳ | فاز ۲ (Booking Flow) + فاز ۱ (Session Status) |
| فاز ۴ | فاز ۱ (Roleها و Permissions) |
| فاز ۵ | فاز ۱-۴ (همه) |


## ۴. Decision Gates

### Decision Gate بعد از فاز ۱
#### معیارهای لازم
- ✅ ۱۰۰٪ تست‌های Policy پاس شده باشند
- ✅ همه Policyها در جلسه‌ای با تیم تأیید شده باشند
- ✅ مستندات تصمیم‌گیری (ADR/Decision Note) برای هر Policy ثبت شده باشند
- ✅ Tech Lead تأیید کرده باشد که مدل‌های دامنه با نیازهای Business همخوانی دارند

#### تأییدکننده‌ها
- Tech Lead
- Product Owner
- Architect (در صورت وجود)

#### مستندات موردنیاز
- ADR یا Decision Note برای هر Policy
- نمونه‌ی تست‌های Policy
- نسخه‌ی نهایی از مدل‌های دامنه

---

### Decision Gate بعد از فاز ۲
#### معیارهای لازم
- ✅ حداقل ۵ سناریوی Double-Booking در تست‌ها پوشش داده شده‌اند
- ✅ همه‌ی Bookingها دارای Audit Entry هستند
- ✅ End-to-End Booking flow با حداقل ۳ سناریوی مختلف اجرا شده است
- ✅ Code Review حداقل توسط ۲ نفر انجام شده است

#### تأییدکننده‌ها
- Tech Lead
- Backend Lead / Reviewer

#### مستندات موردنیاز
- گزارش تست Booking
- مستند API Endpoint
- گزارش Audit و Integrity

---

### Decision Gate بعد از فاز ۳
#### معیارهای لازم
- ✅ Session State Machine در تست‌ها تأیید شده است
- ✅ Cancel/Reschedule workflow با حداقل ۲ سناریوی موفق و ۲ سناریوی خطا پوشش داده شده است
- ✅ Parent/Guardian policy در تست‌ها تأیید شده است

#### تأییدکننده‌ها
- Tech Lead
- Product Owner

#### مستندات موردنیاز
- نسخه‌ی نهایی Session Lifecycle Rules
- گزارش تست‌های Permission و Guardian Flow

---

### Decision Gate بعد از فاز ۴
#### معیارهای لازم
- ✅ Admin workflows با حداقل ۲ سناریوی اصلی تست شده‌اند
- ✅ Permission model بازبینی و تأیید شده است
- ✅ Audit برای همه‌ی عملیات Admin ثبت می‌شود

#### تأییدکننده‌ها
- Tech Lead
- Product Owner

#### مستندات موردنیاز
- Permission Matrix
- Admin workflow documentation

---

### Decision Gate بعد از فاز ۵
#### معیارهای لازم
- ✅ همه‌ی مسیرهای اصلی در E2E تست شده‌اند
- ✅ Release Checklist تکمیل شده است
- ✅ Monitoring و Health Check فعال هستند

#### تأییدکننده‌ها
- Tech Lead
- Product Owner


## ۵. نقش‌ها و مسئولیت‌ها

| نقش | مسئولیت | فازهای مرتبط |
|-----|----------|--------------|
| Lead Developer | طراحی و اجرای تسک‌های اصلی Backend | همه فازها |
| Pair Developer | کمک به پیاده‌سازی و تست‌نویسی | همه فازها |
| Tech Lead | بازبینی معماری، تصمیم‌گیری‌های فنی، تأیید Decision Gates | همه فازها |
| Reviewer | بازبینی کد، QA، کمک به کیفیت | همه فازها |
| Product Owner | تأیید business rules، تصمیم‌گیری در مورد نیازهای تغییر یافته | فاز ۱، ۳، ۴، ۵ |

### پیشنهاد برای نقش‌ها در تیم
- Lead Developer: Backend Developer 1
- Pair Developer: Backend Developer 2
- Reviewer: Tech Lead / Senior Developer
- Product Owner: PM یا Product Lead


## ۶. زمان‌بندی دقیق

| فاز | ساعت توسعه‌دهنده | تعداد توسعه‌دهنده | روزهای تقویمی |
|-----|------------------|-------------------|---------------|
| فاز ۱ | ۴۰ ساعت | ۲ نفر | ۵ روز کاری |
| فاز ۲ | ۶۰ ساعت | ۲ نفر | ۷.۵ روز کاری |
| فاز ۳ | ۵۰ ساعت | ۲ نفر | ۶.۲۵ روز کاری |
| فاز ۴ | ۳۰ ساعت | ۱ نفر | ۷.۵ روز کاری |
| فاز ۵ | ۴۰ ساعت | ۲ نفر | ۵ روز کاری |

> ملاحظه: در صورت وجود تغییر در نیازهای Business یا پیچیدگی بیشتر از حد، فازهای ۲ و ۳ ممکن است نیاز به بازبینی زمان داشته باشند.


## ۷. معیارهای Done برای هر فاز

### فاز ۱ — Done Criteria
- [ ] تمام Policyهای موردنیاز در Domain ایجاد شده‌اند
- [ ] حداقل ۵ سناریوی Unit Test برای هر Policy نوشته شده‌اند
- [ ] Session State Machine با حداقل ۵ سناریوی تست پوشش داده شده است
- [ ] ADR/Decision Note برای هر Policy ثبت شده است
- [ ] Code Review توسط حداقل ۱ Reviewer انجام شده است

### فاز ۲ — Done Criteria
- [ ] حداقل ۵ سناریوی Double-Booking در تست‌ها پوشش داده شده و همگی پاس شده‌اند
- [ ] هیچ Bookingی بدون Audit Entry ثبت نشده است
- [ ] ۱۰۰٪ متدهای عمومی در Aggregate Booking دارای تست واحد هستند
- [ ] Integration Test برای End-to-End Booking با حداقل ۳ سناریوی مختلف نوشته شده است
- [ ] Code Review توسط حداقل ۲ نفر از تیم انجام شده است
- [ ] مستندات API برای Booking Endpoint به‌روز شده است

### فاز ۳ — Done Criteria
- [ ] حداقل ۱ مسیر کامل Cancel و ۱ مسیر Reschedule پوشش داده شده‌اند
- [ ] همه‌ی State Transitions برای Session تست شده‌اند
- [ ] Parent/Guardian policy برای Minor Students با حداقل ۲ سناریوی موفق و ۲ سناریوی خطا پوشش داده شده است
- [ ] Audit برای تغییر وضعیت Session ثبت می‌شود
- [ ] Code Review توسط حداقل ۱ Reviewer انجام شده است

### فاز ۴ — Done Criteria
- [ ] Admin workflows برای Approval/Suspension و Account Management پیاده‌سازی شده‌اند
- [ ] Permission model با حداقل ۲ سناریوی موفق و ۲ سناریوی خطا تست شده است
- [ ] همه‌ی عملیات Admin در Audit ثبت می‌شوند
- [ ] Code Review توسط حداقل ۱ Reviewer انجام شده است

### فاز ۵ — Done Criteria
- [ ] همه‌ی مسیرهای اصلی در E2E تست شده‌اند
- [ ] Health Check و Logging برای مسیرهای بحرانی فعال هستند
- [ ] Release Checklist تکمیل شده است
- [ ] Demo یا Review برای Product Owner انجام شده است


## ۸. ریسک‌ها و راهکارها

### ریسک ۱: بیماری یا مرخصی یکی از توسعه‌دهندگان
- راهکار: حداقل ۲ نفر در هر فاز مشارکت داشته باشند
- راهکار: مستندسازی تسک‌ها و انتقال اطلاعات در پایان هر فاز

### ریسک ۲: تغییر در نیازهای Business در میانه‌ی فاز
- راهکار: در هر Decision Gate نیازها بازبینی شوند
- راهکار: تغییرات مهم در قالب یک short-change request ثبت شوند

### ریسک ۳: پیچیدگی بیشتر از حد تخمین
- راهکار: بعد از ۳ روز از هر فاز، یک Checkpoint برگزار شود
- راهکار: در صورت لزوم، کارهای غیرضروری به فازهای بعدی منتقل شوند

### ریسک ۴: ambiguity در Policyها
- راهکار: قبل از پیاده‌سازی، یک جلسه‌ی تصمیم‌گیری با Tech Lead و Product Owner برگزار شود

### ریسک ۵: وابستگی‌های فنی بین فازها
- راهکار: از Dependency Table و Decision Gates استفاده شود


## ۹. ضمائم (Checklistها، قالب‌های گزارش)

### Checklist فاز ۱
- [ ] Policyها ایجاد شده‌اند
- [ ] Value Objects ایجاد شده‌اند
- [ ] State Machine پیاده‌سازی شده است
- [ ] Unit Tests نوشته شده‌اند
- [ ] ADR ثبت شده است

### Checklist فاز ۲
- [ ] Booking Flow پیاده‌سازی شده است
- [ ] Double-Booking tests اضافه شده‌اند
- [ ] Audit Entry ثبت می‌شود
- [ ] Integration Tests اضافه شده‌اند
- [ ] API Documentation به‌روز شده است

### Checklist فاز ۳
- [ ] Cancel/Reschedule پیاده‌سازی شده است
- [ ] Session Lifecycle تست شده است
- [ ] Parent/Guardian Policy اعمال شده است
- [ ] Audit ثبت می‌شود

### Checklist فاز ۴
- [ ] Admin workflows پیاده‌سازی شده‌اند
- [ ] Permissions تست شده‌اند
- [ ] Audit ثبت می‌شود

### Checklist فاز ۵
- [ ] UI flowها تکمیل شده‌اند
- [ ] E2E tests اجرا شده‌اند
- [ ] Health Checks فعال‌اند
- [ ] Release Checklist تکمیل شده است

### قالب گزارش پیشرفت فاز
- فاز: 
- وضعیت: 
- تسک‌های تکمیل‌شده: 
- مانع‌ها: 
- تصمیم‌های نیازمند تأیید: 
- زمان باقی‌مانده: 
