# Real User Observations — Public Pages Only

Chronological log of the one real-browser public-pages investigation performed this project. Method: `WebFetch` against the live `preply.com`/`help.preply.com` pages (renders and reads actual page content — not a documentation summary, but also not full click-driven session automation; see `00-scope-and-methodology.md` for the exact capability boundary).

Persona throughout: **Anonymous visitor** (no account, no login).

## Session log

```text
[1] URL: https://preply.com/en/
    Persona: Anonymous
    Action: Opened homepage (simulated first visit)
    Verification: OBSERVED
    Result:
      Header nav: "Find tutors" / "For business" / "Become a tutor" / "Proven Progress"
      Language selector: "English" (dropdown, 20+ languages)
      Currency selector: "EUR" (also BRL, GBP, PLN, UAH, USD)
      "Log In" button with avatar icon — no forced signup wall
      Hero headline: "Learn faster with your best language tutor."
      Primary CTA: "Find your tutor"
      Trust signals: "100,000+" tutors · "300,000+" five-star reviews · "120+" subjects ·
        "180+" tutor nationalities · "4.8" App Store rating
      Subject list with live counts: English 33,602 · Spanish 10,053 · French 3,714 ·
        Chinese 5,253 · Arabic 3,655 · Japanese 2,902 (+12 more, expandable)
      "How Preply Works": Find tutor → Start learning → Make progress
      Footer: About Us (Who we are / How it works / Reviews / App / Careers / Status) ·
        For Students (Blog / Q&A / Discounts / Referral / Language tests) ·
        For Tutors (Become tutor / Teaching jobs by language) ·
        For Companies (Corporate/English/Spanish training) ·
        Support contact address: Brookline, MA · social links · app-store badges

[2] URL: https://preply.com/en/tutors/english
    Action: Attempted to follow a subject link
    Verification: — (404, wrong URL guess)
    Result: HTTP 404. Corrected via WebSearch to find the real URL.

[search] Query: "preply.com find english tutors search page url"
    Result: correct URL identified — preply.com/en/online/english-tutors

[3] URL: https://preply.com/en/online/english-tutors
    Persona: Anonymous
    Action: Navigated to the English-tutors search/discovery page
    Verification: OBSERVED
    Result:
      Filters (exact labels): "I want to learn" · "Price per lesson" (€2–€36+) ·
        "Country of birth" · "I'm available" · "Specialties" · "Also speaks" ·
        "Native speaker" (toggle) · "Tutor categories"
      Sort: "Sort tutors by Our top picks" dropdown
      Tutor card fields: photo, name (link), country flag, badge (Professional/Super Tutor),
        star rating, review count, price shown as "€32 50-min lesson" (duration-qualified,
        not a bare hourly figure), languages + proficiency, a descriptor tag (e.g.
        "Engaging and encouraging"), bio excerpt, active student count, total lessons taught,
        "Book trial lesson" button, "Send message" button, "Learn more" link
      Pagination: page-numbered "[1][2]…[4586]" — not infinite scroll
      No login wall on browsing itself
      URL query parameters for filters: not visible in static markup — filter interaction is
        JS-driven, not independently confirmed

[search] Query: "site:preply.com/en/tutor english"
    Result: real public tutor-profile URLs identified, e.g. preply.com/en/tutor/1166382

[4] URL: https://preply.com/en/tutor/1166382 (tutor "Enes V.")
    Persona: Anonymous
    Action: Opened a real public tutor profile
    Verification: OBSERVED
    Result:
      Badges: "Top 5% of English tutors" · "Professional Tutor" (verified teaching certificate)
        · "Super Tutor" (highly rated and experienced)
      Price: "€22 per 50-minute lesson" · 3,031 lessons taught · 4.9/5 (39 reviews)
      Languages: Native English; Turkish A2; Spanish/Italian/German A1
      Primary CTAs: "Book trial lesson" (×2) ·
        NEW FINDING: "Not a match? You still have 2 free tutor trials" — exact meaning not
        interpreted here; see 16-product-decisions-and-open-questions.md
      Subcategory review ratings VISIBLE ON THE PUBLIC PROFILE: Reassurance 4.9, Clarity 4.7,
        Progress 4.6, Preparation [value shown] — this is a public-facing display, not merely
        internal to the tutor (upgrades a prior NOT VERIFIED finding to OBSERVED)
      26 "anonymous student reviews" of 39 total; named + dated written reviews visible
      About section: teaching philosophy/experience/methodology narrative
      Specializations: Conversational English, Business English, English for beginners,
        American English, Intensive English, English for traveling, job interview prep,
        studying abroad, Writing, Business & Work
      Education: BA English, Penn State (2017–2021, diploma verified) + TEFL certification
      Availability widget: weekly view (a specific week shown), day selection visible; exact
        time-slot click behavior — NOT VERIFIED (static fetch cannot simulate a click)
      "You might also like": 3 similar tutors, €11–€17/lesson
      Breadcrumb: "Find Tutors › English Tutors Online › Enes V."
      Whether clicking a time slot or "Book trial lesson" requires login before anything else
        happens: NOT directly observed — INFERRED only (universal pattern; matches TutorFlow's
        own existing AuthGateModal design), never OBSERVED

[5] URL: https://help.preply.com/en/
    Persona: Anonymous
    Action: Opened the Help Center homepage
    Verification: OBSERVED
    Result:
      Collections: "Help for Students" (66 articles) · "Help for Tutors" (87 articles) ·
        "Preply Business" (18 articles)
      Search bar placeholder: "Search for articles..."
      Greeting: "Hello , how can we help you?"
      No live-chat widget visible in the static markup — does NOT disprove one exists via a
        JS-injected floating widget; absence here is inconclusive, not a negative finding

[6] URL: https://preply.com/en/teach
    Persona: Anonymous (prospective tutor)
    Action: Opened the "Become a tutor" landing page
    Verification: OBSERVED
    Result:
      Headline: "Make a living by teaching the largest community of learners worldwide"
      Process steps (exact text): "1. Sign up to create your tutor profile / 2. Get approved
        by our team in 3 business days / 3. Start earning by teaching students all over the
        world!"
      CTA: "Create a tutor profile now" (repeated)
      Rate guidance: "English tutors charge $15-25 per hour" on average
      Earnings claim: "Most popular tutors on Preply earn up to $550 a week"
      Testimonial: Krista A., English tutor — "Preply allowed me to make a living without
        leaving home!"
      FAQ section confirms exact commission structure: "trial lessons at 100%, subsequent
        lessons from 33-18%" — DOCUMENTED, directly from Preply's own official page
      CTA appears to launch registration directly, not further marketing content first
      CONFLICT NOTED: this page says tutor-application approval takes "3 business days";
        the separately-sourced Help Center article (see 17-evidence-and-sources.md, source
        for tutor registration) says "5 business days." Not resolved here — see
        16-product-decisions-and-open-questions.md.

[7] URL: https://preply.com/en/info/terms
    Action: Attempted to view the Terms of Service
    Verification: — (404, wrong URL guess)
    Result: HTTP 404. Not pursued further within this pass's budget — age-restriction and
      suspension/forfeiture ToS specifics therefore remain at whatever confidence they already
      had from search-engine-summarized (not directly fetched) sources — see
      04-platform-trust-and-safety.md.

--- BOUNDARY ---
Everything past this point requires login, account creation, or a real transaction — all
explicitly out of scope for this investigation. Marked BLOCKED throughout the rest of this
directory, not inferred or faked.
```

## Summary of what was and wasn't achieved

**Genuinely OBSERVED this pass:** homepage structure, search/filter UI labels, tutor card fields, one real public tutor profile in full detail (badges, price format, reviews including subcategory ratings, availability widget presence, related tutors), the Help Center's top-level structure, the Become-a-Tutor landing page including its own FAQ's commission figures.

**Not achieved, and correctly not claimed:** anything behind login, the exact click-triggered behavior of any CTA (no session/click capability available), mobile-viewport rendering (no viewport-emulation capability available), and the Terms of Service page (URL not found within this pass's budget).
