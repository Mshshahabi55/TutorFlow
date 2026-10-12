# Preply Component Inventory

Companion to `PREPLY_UI_UX_AUDIT.md`. Catalogues reusable UI components observed or reliably inferred from Preply's public structure, help-center descriptions, and third-party reviewer write-ups (see that document's §24 evidence log — no screenshot tool was available at the time). Conceptual names only; nothing here is an implementation. A later screenshot evidence pack (tutor-signup wizard only) is catalogued separately in `docs/product/preply-reverse-engineering/18-screenshot-evidence.md`; it does not replace or invalidate this file's component list.

Priority: **P0** = required for TutorFlow's core conversion path (search → profile → book → pay), **P1** = required soon after, **P2/P3** = later per `FRONTEND_IMPLEMENTATION_ROADMAP.md`.

---

### NavBar
- **Purpose:** primary site navigation, present on every public page.
- **Observed behavior:** logo, primary links (Find tutors / For business / Become a tutor / Proven Progress), language selector, currency selector, Help link, Log In button.
- **Variants:** logged-out (as above) vs. logged-in (**not reliably observable** — presumably swaps Log In for an account menu).
- **States:** default; **mobile collapsed state not reliably observable** (likely a hamburger menu — unconfirmed).
- **Responsive:** **not reliably observable.**
- **TutorFlow equivalent:** `AppNavBar`.
- **Priority:** P0.

### PrimaryButton / SecondaryButton
- **Purpose:** main and supporting calls to action ("Find your tutor", "Book trial lesson").
- **Observed behavior:** solid-fill primary button for the single most important action per screen; outline/text-style secondary elsewhere.
- **Variants:** size not measurable; likely one standard size per reviewer screenshots referenced in search results.
- **States:** default, hover/focus/disabled/loading — **not reliably observable**, assume standard web-button state set.
- **TutorFlow equivalent:** `Button` (primary/secondary variant) — likely already covered by whatever base UI kit TutorFlow's frontend adopts.
- **Priority:** P0.

### TutorCard
- **Purpose:** summary unit for a tutor in search results, homepage carousels, and "featured tutor" sections.
- **Observed behavior (from "How It Works" fetch):** photo, name, hourly rate range, review count/rating, specialties/tags, "Book trial lesson" CTA.
- **Variants:** carousel card (homepage) vs. list/grid card (search results, **list/grid exact layout not reliably observable**).
- **States:** default; hover (**not reliably observable**); unavailable/no-slots (**not reliably observable** — flag as "Needs product decision" for TutorFlow).
- **Responsive:** **not reliably observable.**
- **TutorFlow equivalent:** `TutorCard`.
- **Priority:** P0.

### FilterBar / FilterControl
- **Purpose:** narrow tutor search results.
- **Observed behavior:** **not reliably observable this session** (results-page fetch 404'd); existence and general categories (language/subject, price, availability, nationality) inferred only from the homepage directory and generic marketplace convention.
- **Variants/states:** unknown.
- **TutorFlow equivalent:** `FilterBar` with per-filter `Select`/`CheckboxGroup`/`RangeSlider` — **exact control types need product decision**, do not assume Preply's.
- **Priority:** P0 (the gap itself is high priority to resolve, even though the pattern isn't observed).

### StatCallout
- **Purpose:** reinforce trust/scale near CTAs ("100,000+ tutors", "300,000+ five-star reviews").
- **Observed behavior:** short numeric stat + label, repeated near hero and benefit sections.
- **TutorFlow equivalent:** `StatCallout` — useful once TutorFlow has real aggregate numbers; must not display invented statistics.
- **Priority:** P2 (marketing polish, not core flow).

### StepIndicator (3-step "How It Works")
- **Purpose:** communicate a simple 3-stage mental model to new visitors (Find tutor → Start learning → Keep progressing).
- **Observed behavior:** numbered steps with icon/image + short copy; this is **marketing content, not a functional wizard progress bar** — do not conflate with the tutor-application wizard's own (unconfirmed) progress indicator.
- **TutorFlow equivalent:** a simple `ProcessSteps` marketing component for the homepage/how-it-works page.
- **Priority:** P3.

### BenefitCard / FeatureCard
- **Purpose:** explain value props ("Why choose Preply", tool showcase: Classroom / Lesson Insights / Daily Exercises).
- **Observed behavior:** icon or image + heading + short body, laid out in a 3–4 column grid on desktop.
- **TutorFlow equivalent:** `FeatureCard`.
- **Priority:** P3.

### TestimonialCard
- **Purpose:** social proof via user reviews on marketing pages.
- **Observed behavior:** star rating + quote + attribution, displayed in a row/grid (6 observed on "How It Works").
- **TutorFlow equivalent:** `TestimonialCard` — content must be TutorFlow's own real reviews once available, never placeholder copy presented as real.
- **Priority:** P3.

### FaqAccordion
- **Purpose:** answer common pre-commitment questions without leaving the page.
- **Observed behavior:** 9 expandable question rows on "How It Works"; standard accordion — one open at a time **not confirmed**, could allow multiple open.
- **States:** collapsed, expanded.
- **TutorFlow equivalent:** `Accordion` (generic, reusable beyond FAQ).
- **Priority:** P2.

### Footer
- **Purpose:** secondary navigation, SEO link surface, legal/social links.
- **Observed behavior:** consistent multi-column layout (About Us / For Students / For Tutors / For Companies / Learn / location-and-subject SEO links), contact address, social icons, legal links.
- **TutorFlow equivalent:** `AppFooter` — TutorFlow should include only the column categories relevant to its own scope; the SEO-link-farm columns are a growth tactic, not a UX requirement.
- **Priority:** P1 (basic footer needed early; full SEO expansion is P3).

### RatingStars
- **Purpose:** display review scores on tutor cards and profiles.
- **Observed behavior:** star-based rating + numeric review count, referenced throughout (profile, cards, testimonials).
- **TutorFlow equivalent:** `RatingStars` — tied to the `Reviews` module once it has real data.
- **Priority:** P1.

### IntroVideoPlayer / VideoEmbed
- **Purpose:** tutor intro video on profile pages; embedded demo content on marketing pages.
- **Observed behavior:** help-center guidance caps tutor intro videos at under ~2 minutes; player chrome itself **not reliably observable**.
- **TutorFlow equivalent:** `VideoPlayer` wrapping a standard HTML5/embed player.
- **Priority:** P1 (profile), P2 (marketing use).

### BookingPanel
- **Purpose:** persistent price + "Book trial lesson" CTA on the tutor profile, reported by reviewers as sticky on desktop.
- **Observed behavior:** shows rate, likely duration options, primary CTA. Exact sticky/scroll behavior **not reliably observable** but plausible given it's the page's single conversion point.
- **TutorFlow equivalent:** `BookingPanel`, feeding directly into the `BookingWizard` (see `TUTORFLOW_WIZARD_ARCHITECTURE.md`).
- **Priority:** P0.

### DatePicker / TimeSlotPicker
- **Purpose:** let the student choose an available lesson start time from the tutor's open slots.
- **Observed behavior:** confirmed to exist and to filter to only the tutor's available slots (help-center); exact widget (calendar grid vs. scrollable day-list) **not reliably observable**.
- **States:** available slot, selected slot, unavailable/already-booked slot (**existence of a distinct unavailable visual state not confirmed but functionally necessary**).
- **TutorFlow equivalent:** `TimeSlotPicker`, backed by `ITutorAvailabilityQuery`/`IActiveBookingIntervalsQuery` (already-defined read-only contracts, `ADR-0014`).
- **Priority:** P0.

### PaymentMethodSelector
- **Purpose:** choose how to pay for a trial (new payment method vs. existing lesson-credit balance with that tutor).
- **Observed behavior:** confirmed to exist and to precede/follow duration selection depending on platform (web vs. app — confirmed order difference). Visual form **not reliably observable**.
- **TutorFlow equivalent:** `PaymentMethodSelector`, feeding the `CheckoutStep` that calls `Payments` per `ADR-0019`.
- **Priority:** P0.

### MultiSelect / SearchableSelect / TagInput
- **Purpose:** used in tutor-application fields such as subjects/specialties and, presumably, languages.
- **Observed behavior:** existence inferred from the application's "subjects" and "specialties" sections; exact control type (chips vs. multi-select dropdown vs. free-tag input) **not reliably observable.**
- **TutorFlow equivalent:** candidate components for the onboarding wizard — see `PREPLY_WIZARD_COMPONENT_INVENTORY.md` for the wizard-specific treatment.
- **Priority:** P0 (wizard-blocking).

### FileUploader / ImageUploader / VideoUploader
- **Purpose:** profile photo, certificate/education documents, intro video upload during tutor application.
- **Observed behavior:** confirmed to exist (photo, certifications, education, video are named application sections); crop/trim UI, file-size/type limits **not reliably observable.**
- **TutorFlow equivalent:** see wizard component inventory.
- **Priority:** P0 (wizard-blocking).

### AvailabilityGrid
- **Purpose:** tutor sets recurring weekly availability + timezone during application and ongoing management.
- **Observed behavior:** existence confirmed ("Calendar" section, "Setting up your availability" help article); exact grid/interaction **not reliably observable.**
- **TutorFlow equivalent:** `AvailabilityGrid`, backed by TutorFlow's existing `Availability` module (weekly recurring rules already in the domain model).
- **Priority:** P0 (wizard-blocking).

### ReviewStep / CompletionState
- **Purpose:** final review-and-submit screen of the tutor application; post-submission status display.
- **Observed behavior:** submission triggers a ~5-business-day human review with an email outcome notification (help-center confirmed); in-app "pending review" state UI **not reliably observable.**
- **TutorFlow equivalent:** see wizard architecture doc.
- **Priority:** P0 (wizard-blocking).

---

## Summary count

**20 components catalogued** (17 general + 3 explicitly wizard-feeding, cross-referenced in the dedicated wizard inventory). Of these, 6 are backed by direct fetched evidence, 9 by help-center/reviewer secondary sources, 5 by structural inference only (flagged accordingly above). None include measured visual tokens — see `PREPLY_UI_UX_AUDIT.md` §5–7 for why.
