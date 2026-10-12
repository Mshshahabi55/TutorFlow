# Preply Screenshot Evidence

## Purpose

This document is the authoritative inventory of **three** screenshot evidence packs supplied directly to this project:

- **Part A** (`evidence/tutor.zip`, 13 images, SS01–SS13) — the live `preply.com/en/tutor-signup` tutor-application wizard.
- **Part B** (`evidence/BeforFindYourTutor.zip` + `evidence/FindYourTutor.zip`, 3 + 32 = 35 images, SS14–SS48) — the Preply homepage, a previously-undocumented "Find your tutor" matching quiz, real authenticated search results, a real tutor profile, and a real (uncompleted) checkout flow, captured in a single real browsing session.
- **Part C** (`evidence/PreplySentEmailForMe.zip`, 4 images, SS49–SS52) — two real transactional emails Preply sent to the account holder's real Gmail inbox: a tutor-message notification and an abandoned-checkout recovery email, directly following on from Part B's abandoned checkout session.

52 screenshots total. All are real screen captures, 1920×1080, desktop Chrome on Windows 11.

They supplement:
- direct public browser observations (`01-real-user-observations.md`)
- official Preply documentation (help.preply.com, cited throughout this directory)
- existing reverse-engineering notes (`07-wizard-inventory.md`, `03-tutor-journey.md`, `02-student-journey.md`)

They do **not** establish hidden backend behavior, validation rules, or business logic beyond what is visibly rendered on screen.

## PII Notice — read before using this document

**Part A** was captured by a real person partway through filling in Preply's real tutor-signup wizard, using their own real name, real email address, and a real uploaded face photo and webcam video frame. No tutor application was submitted ("Complete registration" was never clicked) — confirmed directly by the account holder.

**Part B contains stronger real-world-action evidence and is flagged explicitly here, not just in the final report:**
- The session shows a **real Google OAuth sign-in being completed** into Preply (a real Google account-chooser screen, SS35, followed immediately by an authenticated Preply header in SS36 onward — message/notification icons, an account-initial avatar, a personalized "Hello Mohammad" greeting on the Help Center in SS46).
- The authenticated account's **Messages panel (SS45) shows a pre-existing real conversation** with a tutor, dated almost ten weeks before this capture session — meaning this is evidence of an **already-existing real Preply student account**, not one newly created for this evidence pass.
- The session reached a **real checkout page** for a real trial lesson with a real tutor (`preply.com/en/checkout/...`, SS38–SS39), showing real pricing (€42.99 total, including a real "Processing fee" line item). **No payment was submitted**: the card-number, expiry, and CVC fields are empty placeholder text in SS39, and the "Book lesson and pay" button is rendered in a disabled/grey state, not the active pink state used elsewhere for enabled buttons. The next screenshot (SS40) shows navigation back to the tutor's profile page, consistent with the checkout being abandoned, not completed.
- The Google account-chooser screen (SS35) also displays a **second real person's full name and email address** (an account unrelated to the one used), in addition to the account holder's own name/email appearing twice in that same list.

**Part C adds one more concrete PII item**: one screenshot (SS50) shows the account holder's **real Gmail address in plain text**, in a Preply email-footer unsubscribe line ("Subscribed email address: ..."). It also confirms, via the real Gmail inbox chrome itself ("222" total inbox messages, a pre-existing "New message from Darcy" notification as message #2 and the newer abandoned-cart email as #1), that this is the account holder's **real, actively-used personal email inbox** — consistent with Part B's finding that this is a pre-existing real account, not a disposable one.

Per explicit instruction, **all personally identifying content (real names, real email addresses, the uploaded face photo/video) has been redacted from this document and from every other file in this directory**, across all three parts. Where a field's *value* would reveal PII, this document describes the field's *label, type, or presence* only. The source ZIP files (`evidence/tutor.zip`, `evidence/BeforFindYourTutor.zip`, `evidence/FindYourTutor.zip`, `evidence/PreplySentEmailForMe.zip`) still contain the original, unredacted images on disk — supplied by the user, not modified/moved/deleted by this task. If this repository is ever shared, pushed to a shared remote, or made public, those ZIPs (and nothing in this markdown file) are what would leak the PII, and the authenticated account shown is a **real, pre-existing personal account**, not a disposable test account.

No proprietary Preply source code, private API responses, or copyrighted assets are reproduced here — only observable UI structure, labels, and layout, consistent with `README.md` §3 and §20 of the originating tasks. One short, non-identifying public review quote (no reviewer name) is reproduced in SS39's description as an illustrative UI-content example, consistent with how marketing copy is already quoted elsewhere in this directory.

## Evidence Rules

Same verification labels as the rest of this directory (`README.md` §4), applied to screenshots specifically:

| Label | Meaning for a screenshot |
|---|---|
| **OBSERVED — screenshot evidence** | The element, step name, or layout is directly, unambiguously visible in the image (an on-page heading, an explicit breadcrumb label, a visible button/field). |
| **PARTIALLY OBSERVED — screenshot evidence** | The image shows part of a flow but required scrolling/continuation to see the rest, or the step identity is inferred from adjacent evidence rather than directly labeled in that specific frame. |
| **INFERRED** | Reasoned from context (e.g. chronological file order, field content matching a known section) but not directly visible as a label in that frame. |
| **NOT VERIFIED** | The screenshot set does not establish this. |

A screenshot proves only what is visibly rendered. It does not prove what happens on submit, what server-side validation exists, what happens after "Complete registration" is clicked (never observed — registration was not completed), or any authenticated behavior beyond this one wizard.

## Screenshot Inventory

All 13 images: desktop Chrome on Windows 11, full OS screenshot (browser chrome, tabs, and taskbar all visible), **1920×1080**, captured in a single real session on `preply.com/en/tutor-signup`. **No mobile/tablet screenshots were supplied — see §12 of the originating task / "Mobile" note below.**

Filenames are Windows screenshot timestamps only (no PII in filenames).

### SS01 — `Screenshot 2026-10-06 074156.png`

- Path: `docs/product/preply-reverse-engineering/evidence/tutor.zip` (extracted, not committed as a loose file)
- Resolution: 1920×1080
- Likely page/flow: Tutor signup — first step ("About"), mid-scroll
- User role: Tutor applicant
- Viewport/device: Desktop, Chrome
- Evidence classification: **PARTIALLY OBSERVED — screenshot evidence** (step name not visible in this specific crop — no breadcrumb/H1 in frame — inferred from content and from SS06's later breadcrumb, which confirms "About" is step 1)

Visible UI:
- Intro copy: "...automatically saved as you complete each section. You can return at any time to finish your registration."
- Form fields: **First name** (text input, populated — PII redacted), **Last name** (text input, populated — PII redacted), **Email** (text input, populated — PII redacted), **Country of birth** (dropdown, populated with a real country), **Subject you teach** (dropdown, single-select, set to a real subject), **Languages you speak** (repeating row: language dropdown + proficiency-level dropdown + delete/trash icon, two rows populated), **Add another language** (text link)
- No footer/navigation buttons visible in this crop

Observable interaction clues: dropdown chevrons on Country/Subject/Language/Level fields; a trash-can icon per language row (removable rows); "Add another language" as a repeating-group affordance.

What this screenshot proves: Preply's real tutor-signup first step collects **name, email, country of birth, a single primary teaching subject, and a repeatable list of spoken languages with per-language proficiency level** — all on one page/step, auto-saved per section.

What this screenshot does NOT prove: the exact step label shown in the breadcrumb for this page (inferred, not directly observed here); whether "Subject you teach" supports multiple subjects elsewhere on this same step (only one dropdown is visible in this crop); any server-side validation.

Related Preply journey: `03-tutor-journey.md` — Tutor Application Wizard.
Related TutorFlow feature: `BasicsStep.tsx` (displayName/headline/about) + `TeachingStep.tsx` (subjects/languages/country of birth) — **TutorFlow splits this single Preply step's content across two separate steps.**
TutorFlow parity status: **PARTIALLY MATCHED** — all underlying data fields exist somewhere in TutorFlow's wizard, but TutorFlow does not collect name/email in the wizard at all (collected at account registration instead) and splits subject/language/country-of-birth into a later step rather than the first one.
Notes: This is the single most structurally significant screenshot for the step-count conflict in `07-wizard-inventory.md` — see that document's update.

### SS02 — `Screenshot 2026-10-06 074216.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Photo" step
- User role: Tutor applicant
- Evidence classification: **OBSERVED — screenshot evidence** (explicit on-page H1: "Profile photo")

Visible UI:
- H1 "Profile photo", subtext "Choose a photo that will help learners get to know you."
- A live profile-preview card: avatar thumbnail (a real uploaded photo — redacted from description), name line with a small country-flag icon, "Teaches English lessons" (graduation-cap icon), "Speaks [Language] (Native), [Language] (B2)" (quote-mark icon) — i.e., a **live-updating preview card driven by the previous step's data**, rendered inline on the Photo step itself
- "Upload new photo" button (outlined style)
- "What your photo needs" section: a 4-photo example grid (reference/good-example photos, generic stock-style headshots, not the user's own), followed by a checklist: "You should be facing forward", "Frame your head and shoulders", "You should be centered and upright" (each with a checkmark icon)

Observable interaction clues: "Upload new photo" is a button, not a drag-and-drop zone, in this view; the example grid is static reference imagery, not interactive.

What this screenshot proves: the Photo step shows a **live summary/preview card of prior-step data** (name, subject, languages) alongside the photo upload control — i.e., earlier steps' answers are visibly carried forward and rendered as a mini-profile preview on later steps, not just saved silently.

What this screenshot does NOT prove: file-size/format limits for the photo (not shown in this crop); what happens on a rejected/invalid photo upload (no error state observed).

Related TutorFlow feature: `PhotoStep.tsx`.
TutorFlow parity status: **NOT VERIFIED** — whether TutorFlow's own Photo step renders a similar live cross-step preview card was not re-checked as part of this screenshot pass (out of scope; would require a dedicated code read).

### SS03 — `Screenshot 2026-10-06 074255.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Certification" step
- Evidence classification: **OBSERVED — screenshot evidence** (explicit on-page H1: "Teaching certification")

Visible UI:
- H1 "Teaching certification", subtext "Do you have teaching certificates? If so, describe them to enhance your profile credibility and get more students."
- Checkbox: "I don't have a teaching certificate"
- **Subject** dropdown (pre-set to the subject chosen earlier), with a trash/delete icon (repeating-entry row)
- **Certification** dropdown: placeholder "Select verified certificate", helper text "Write the name exactly as it appears on your certificate"
- Checkbox: "My certificate is not on the list"
- **Years of study**: two "Select" dropdowns (from–to) side by side
- "Upload your certificate" panel begins at the bottom of the crop: "Our team will manually review your submission", "Upload" button

What this screenshot proves: Certification is optional (explicit "I don't have..." opt-out checkbox), supports a **controlled/verified-certificate list** distinct from free-text, a fallback "not on the list" path, a years-of-study range, and a manual-review file upload with explicit "Our team will manually review your submission" copy.

What this screenshot does NOT prove: what happens after upload (no confirmation/pending state observed); whether multiple certificates can be added here (no "Add another certificate" link visible in this specific crop, unlike the Education step which explicitly has one — see SS05).

Related TutorFlow feature: `CredentialsStep.tsx` (combines Certification + Education into one TutorFlow step — see Education entries below).
TutorFlow parity status: **PARTIALLY MATCHED** — TutorFlow collects certificate name/org/year/file, but Preply's real flow treats Certification as its own distinct step (step 3 of 8) with a controlled-list-plus-manual-review pattern, while TutorFlow combines it with Education into a single step. TutorFlow's own code comment already self-identifies this as an intentional deviation ("Preply sections 2+3", `CredentialsStep.tsx`); this screenshot **upgrades that deviation's evidence from DOCUMENTED (help article) to OBSERVED (screenshot)**.

### SS04 — `Screenshot 2026-10-06 074514.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Education" step
- Evidence classification: **OBSERVED — screenshot evidence** (explicit on-page H1: "Education")

Visible UI:
- H1 "Education", subtext "Tell students more about the higher education that you've completed or are working on"
- Checkbox: "I don't have a higher education degree"
- Fields: **University** (text, placeholder "E.g. Mount Royal University"), **Degree** (text, placeholder "E.g. Bachelor's degree in the English Language"), **Degree type** (dropdown, placeholder "Choose degree type..."), **Specialization** (text, placeholder "E.g. Teaching English as a Foreign Language"), **Years of study** (two "Select" dropdowns, from–to)

What this screenshot proves: Education is a **separate step from Certification** (contradicting any assumption that they were combined in Preply's real flow), is optional (explicit opt-out checkbox), and asks for university/degree/degree-type/specialization/years as distinct fields, with realistic example placeholder text for each.

What this screenshot does NOT prove: anything about the Diploma-verified badge or multi-entry support — see SS05 (continuation of this same step).

Related TutorFlow feature: `CredentialsStep.tsx`.
TutorFlow parity status: **PARTIALLY MATCHED** (same note as SS03 — TutorFlow merges this into Credentials).

### SS05 — `Screenshot 2026-10-06 074530.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Education" step, continued (scrolled further)
- Evidence classification: **PARTIALLY OBSERVED — screenshot evidence** (continuation of SS04; no new H1/breadcrumb in this specific crop, inferred from visible field continuity)

Visible UI:
- Bottom portion of the same Degree type / Specialization / Years-of-study fields as SS04
- **"Get a 'Diploma verified' badge"** panel: "Upload your diploma to boost your credibility! Our team will review it and add the badge to your profile. Once reviewed, your files will be deleted." + explicit file constraints: **"JPG or PNG format; maximum size of 20MB."** + "Upload" button
- **"Add another education"** text link (repeating-entry group)
- Footer: "Back" (outlined) and "Save and continue" (filled, pink/brand color) buttons

What this screenshot proves: a distinct "Diploma verified" **badge-granting mechanism** tied to document upload with an explicit file-deletion-after-review privacy statement, and explicit file constraints (JPG/PNG, 20MB max); Education supports multiple entries via "Add another education".

What this screenshot does NOT prove: what the badge actually looks like on a public profile (not captured in this set).

TutorFlow parity status: **MISSING** — no "verified badge" concept tied to document review exists in TutorFlow's `CredentialsStep.tsx` today; this is a genuinely new, concrete finding (see Visual Parity Gaps, P2).

### SS06 — `Screenshot 2026-10-06 074649.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Description" step (step 5 of 8)
- Evidence classification: **OBSERVED — screenshot evidence** (full breadcrumb visible, plus explicit H1 "Profile description")

Visible UI:
- **Full page header**: Preply logo (top-left), a help/question-mark icon and a small round account-avatar (top-right) — the first screenshot in the set showing the actual site chrome/header, not just scrolled content
- **Full 8-step breadcrumb, visible and legible for the first time in this set**: `☑ About › ☑ Photo › ☑ Certification › ☑ Education › 5 Description › 6 Video › 7 Availability › 8 Pricing` — steps 1–4 show a checkmark-in-box icon (completed); step 5 ("Description") is the current step, bold/highlighted; steps 6–8 show a plain number and greyed/inactive styling
- H1 "Profile description", subtext: "This info will go on your public profile. Write it in the language you'll be teaching and make sure to follow our **guidelines to get approved**" (linked text)
- A **numbered writing-guide checklist**, each line with a checkmark: "1. Introduce yourself", "2. Teaching experience", "3. Motivate potential students", "4. Write a catchy headline"
- A **character counter**, "651 / 400", rendered in green text, next to "Back" / "Save and continue" buttons

What this screenshot proves definitively: **Preply's real tutor-signup wizard has exactly 8 named steps, in this exact order: About, Photo, Certification, Education, Description, Video, Availability, Pricing.** This directly confirms (not merely documents) the step sequence, resolves the "is the 8th section identifiable" open question from `07-wizard-inventory.md` (the 8th section is **"About"**, i.e. the very first step — it is not a hidden/unknown later step as previously assumed, it is simply the step the cited help article's own text undercounted), and shows the free-text profile description is guided by **four explicit sub-prompts**, not a blank textarea, with a soft max-length counter that is visually non-blocking even past the limit (shown in green at 651/400, not red/error-styled).

What this screenshot does NOT prove: whether "Save and continue" is actually disabled past 400 characters (the button appears enabled in this frame; color alone does not prove a hard limit either way).

Related TutorFlow feature: `BasicsStep.tsx` ("About you" textarea, 4000-char max, no sub-prompt guidance, no visible counter in the reviewed source).
TutorFlow parity status: **PARTIALLY MATCHED** — TutorFlow has an equivalent free-text field but without the four-part guided-writing structure or an inline length counter.

### SS07 — `Screenshot 2026-10-06 074808.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Video" step (step 6 of 8), live camera capture
- Evidence classification: **OBSERVED — screenshot evidence** (breadcrumb visible and legible: `☑ About › ☑ Photo › ☑ Certification › ☑ Education › ☑ Description › 6 Video › 7 Availability › 8 Pricing`)

Visible UI:
- Large centered video/camera preview frame (a real live webcam capture — face description redacted) filling most of the viewport
- "Back" (outlined) and "Next" (filled, pink) buttons beneath the preview

What this screenshot proves: the Video step opens directly into a **live camera preview**, large and centered, as the primary call to action — consistent with an in-browser recording capability, not merely a file-upload form.

What this screenshot does NOT prove: recording controls, timer, or start/stop state (not visible in this exact frame — see SS08/SS09 for the fuller Video-step UI, which was reached by scrolling or a different view state).

TutorFlow parity status: **NOT VERIFIED** — in-browser webcam recording was not independently re-checked against `VideoStep.tsx` in this pass.

### SS08 — `Screenshot 2026-10-06 074836.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Video" step, requirements/guidance view
- Evidence classification: **OBSERVED — screenshot evidence** (breadcrumb visible, H1 "Video introduction")

Visible UI:
- Two-column layout. Left: H2 "Add a horizontal video of up to 2 minutes", instructions ("Introduce yourself to students in the same language as your written description. If you teach a different language, include a short sample." + "Learn more about video guidelines" link), an empty video placeholder box reading **"Your video will appear here"**, and a **"Writing help" section with a pink "New" badge** ("Get an auto generated script based on your information in just a few seconds!")
- Right: "Video requirements" panel, "Make sure your video meets the requirements to get approved", a green-checkmarked **"Do"** list: "Your video should be between 30 seconds and 2 minutes long", "Record in horizontal mode and at eye level", "Use good lighting and a neutral background", "Use a stable surface so that your video does not appear shaky", "Make sure your face and eyes are fully visible (except for religious reasons)", "Highlight your teaching experience and any relevant teaching certification(s)" (partially visible, continues in SS09)

What this screenshot proves: explicit, itemized video **Do-list requirements** including an explicit religious-exception carve-out for face visibility; an AI-assisted script-writing feature flagged "New" (recently launched relative to Preply's own product history).

TutorFlow parity status: **MISSING** — no equivalent "Do/Don't" requirements panel or AI script-writing assist was found in `VideoStep.tsx` by filename inspection; full content not re-verified in this pass.

### SS09 — `Screenshot 2026-10-06 074853.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Video" step, continued (scrolled further)
- Evidence classification: **PARTIALLY OBSERVED — screenshot evidence** (continuation of SS08; no breadcrumb in this specific crop)

Visible UI:
- "Create script" button (with a small sparkle/AI icon) and a **"Start recording"** button (video-camera icon, pink)
- "Or paste a link to your video" with "Learn how to upload videos to **Youtube** or **Vimeo**" (both linked) and a URL input with placeholder `www.youtube.com/watch?v=...`
- "Add a thumbnail (optional)" with "Don't worry if you don't have a thumbnail ready, we'll use the preview above." + "Learn more about thumbnail guidelines" link, and a "Show more" disclosure toggle
- Right column, **"Don't"** list (red X icon): "Include your surname or any contact details", "Include logos or links", "Use slideshows or presentations", "Have any other people visible in your video"
- Footer: "Back" / "Save and continue"

What this screenshot proves: the Video step offers **three distinct submission paths** — in-browser recording, external YouTube/Vimeo link, or (implicitly, via "Show more") possibly direct file upload; an explicit anti-doxxing rule ("Don't include your surname or any contact details") and anti-slideshow / no-other-people-visible rules.

TutorFlow parity status: **MISSING** — no "paste a video link" alternative path or explicit Do/Don't rule-set is present in TutorFlow's Video step per the filenames/structure reviewed.

### SS10 — `Screenshot 2026-10-06 075052.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Availability" step (step 7 of 8), timezone confirmation
- Evidence classification: **OBSERVED — screenshot evidence** (full header + breadcrumb visible: `☑ About › ☑ Photo › ☑ Certification › ☑ Education › ☑ Description › ☑ Video › 7 Availability › 8 Pricing`; H1 "Availability")

Visible UI:
- H1 "Availability", H2/question "Is this your current timezone?"
- A boxed, centered timezone statement: **"08:20 (GMT+3:30) - Asia, Tehran"** (auto-detected, real value — not PII, this is a timezone, not identifying — retained)
- Two buttons: **"No"** (outlined) and **"Yes"** (filled, pink)

What this screenshot proves: Preply **auto-detects the browser/system timezone and asks for explicit Yes/No confirmation** before proceeding, rather than presenting a blank dropdown to fill in from scratch.

Related TutorFlow feature: `AvailabilityStep.tsx` — confirmed by direct code read to use a plain **required dropdown** ("Select your timezone", `FormField label="Timezone" required`), not an auto-detect-and-confirm pattern.
TutorFlow parity status: **PARTIALLY MATCHED** — both capture timezone and both treat it as required, but the interaction pattern differs (manual select-from-list vs. detect-then-confirm-or-correct). Listed as a P2/P3 visual-parity gap, not a functional gap.

### SS11 — `Screenshot 2026-10-06 075137.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Availability" step, weekly schedule editor (Monday–Thursday visible)
- Evidence classification: **PARTIALLY OBSERVED — screenshot evidence** (continuation of SS10; no breadcrumb in this specific crop)

Visible UI:
- Repeating per-day blocks, each: a checked checkbox + day name ("Monday", "Tuesday", "Wednesday", "Thursday" partially visible), **"From"** / **"To"** dropdown pair (each defaulting to **09:00** / **17:00**), and an **"Add another timeslot"** text link per day

What this screenshot proves: the default weekly template pre-populates **every visible day as enabled, 09:00–17:00**, with per-day multiple-timeslot support via "Add another timeslot".

TutorFlow parity status: **NOT VERIFIED** — TutorFlow's own default weekly-availability pre-population behavior was not re-checked in this pass.

### SS12 — `Screenshot 2026-10-06 075147.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Availability" step, weekly schedule editor (Saturday/Sunday + step footer)
- Evidence classification: **PARTIALLY OBSERVED — screenshot evidence** (continuation of SS11; confirms Friday–Sunday follow the same pattern, Friday itself not in frame but Saturday/Sunday both shown)

Visible UI:
- Saturday and Sunday blocks, same checked/From-To/Add-another-timeslot pattern as SS11 (also defaulting to 09:00–17:00)
- Footer: "Back" / **"Save and continue"**

What this screenshot proves: all 7 days of the week follow the identical default-enabled, 09:00–17:00, multi-timeslot-capable pattern — i.e., Preply's default proposed schedule is a full 7-day, 9-to-5 template, not a blank slate the tutor must build from scratch.

TutorFlow parity status: **NOT VERIFIED** (same caveat as SS11).

### SS13 — `Screenshot 2026-10-06 075211.png`

- Resolution: 1920×1080
- Likely page/flow: Tutor signup — "Pricing" step (step 8 of 8, final step)
- Evidence classification: **OBSERVED — screenshot evidence** (full breadcrumb visible: `☑ About › ☑ Photo › ☑ Certification › ☑ Education › ☑ Description › ☑ Video › ☑ Availability › 8 Pricing`; H1 "Set your 50 minute lesson price")

Visible UI:
- H1 "Set your 50 minute lesson price", a single price input with a "$" icon, helper text **"Price in USD only"**
- Collapsible section header "Preply commission" (chevron icon, collapsed — contents not visible in this frame)
- **"Final details"** info box: "By completing registration, you also agree to the terms and conditions of the **Preply Corporate Tutor Program**, which gives you access to teach corporate students. You can choose not to be matched with these students at any time. **Learn more here.**" (both linked)
- Footer: "Back" (outlined) and **"Complete registration"** (filled, pink) — the final submit action, **not clicked** in this screenshot

What this screenshot proves: pricing is entered as a **single 50-minute-lesson price in USD only** (not an hourly rate, and not multi-currency, at least in this UI); a collapsible, not-expanded-by-default "Preply commission" disclosure exists at this exact step; tutors are enrolled by default (opt-out, not opt-in) into a "Corporate Tutor Program" unless they later choose otherwise; **there is no distinct "Review your application" summary step anywhere in this 8-step sequence** — Pricing is the literal last step before submission.

What this screenshot does NOT prove: the actual commission percentage shown when expanded (not visible, collapsed in this frame — commission figures elsewhere in this directory, e.g. 18–33%, come from the separate Become-a-Tutor marketing page, source S05/S27, not from this screenshot); what the post-submit confirmation screen looks like (registration was not completed).

Related TutorFlow feature: `RateStep.tsx` + `ReviewStep.tsx`.
TutorFlow parity status: **PARTIALLY MATCHED / INTENTIONAL-DEVIATION CANDIDATE** — TutorFlow has a dedicated final "Review" step (summarizing all prior answers before submit) that **Preply's real flow does not appear to have** — Preply goes straight from Pricing to "Complete registration" with no visible review/summary screen in this evidence set. This is a genuine, newly-discovered product-shape difference, not previously documented anywhere in this directory.

## Cross-Screenshot Findings

1. **The real step count is 8, and the order is now fully confirmed**: About → Photo → Certification → Education → Description → Video → Availability → Pricing. This is a strictly stronger (OBSERVED, not DOCUMENTED) version of the finding in `07-wizard-inventory.md` Source B, and it corrects that document's framing of an "unidentifiable 8th section" — the 8th section is simply the first one, "About," which the cited help-center article's prose undercounted.
2. **Certification and Education are confirmed as two genuinely separate steps** (SS03 vs. SS04/SS05), not a single combined section — upgrading TutorFlow's own existing self-aware code comment ("Preply sections 2+3") from an inference to directly observed fact.
3. **No "Review/Summary" step exists in Preply's real flow** — Pricing (step 8) leads directly to "Complete registration." TutorFlow has a dedicated Review step Preply does not.
4. **Every multi-section step (Education, Video) explicitly supports multiple entries** via an "Add another ___" link — a consistent, repeated UI pattern across the wizard, not a one-off.
5. **Two distinct "verification/trust badge" mechanics** were observed: a controlled-list "verified certificate" selector (SS03) and a "Diploma verified" badge tied to manual document review (SS05) — both are manual-review-gated and both explicitly state uploaded files are deleted after review (privacy-relevant copy, Education step only; Certification step's upload panel does not show the same deletion promise in the visible crop).
6. **The account header (logo, help icon, avatar) only appears once a user has scrolled to the top of a step** (SS06 onward) — earlier screenshots (SS01–SS05) were captured mid-scroll, past the fixed header, which is why their breadcrumb isn't visible — this is a capture artifact of the screenshots themselves, not a Preply UI absence.

## Newly Verified UI Patterns

- Auto-save-per-section confirmation copy, stated explicitly in the UI itself (SS01).
- Live cross-step preview card (name/subject/languages shown on the Photo step) (SS02).
- Timezone auto-detect-and-confirm (Yes/No), not a blank dropdown (SS10).
- Default-populated weekly availability template (all 7 days, 09:00–17:00) rather than a blank schedule (SS11, SS12).
- A soft, non-blocking character counter on free-text profile description, shown even past the stated limit without switching to an error style (SS06).
- AI-assisted "Create script" video-writing aid, labeled "New" (SS08, SS09).
- Explicit anti-doxxing and anti-slideshow rules for the intro video (SS09).
- Single-step, no-separate-review submission model — Pricing step doubles as the final review/submit screen (SS13).

## Remaining Gaps

This screenshot set covers only the **tutor-signup wizard, desktop, steps 1–8, up to but not including the post-submit confirmation screen**. It does not provide any evidence for: the student journey, tutor dashboard/earnings, Classroom, Messaging, Reviews, Notifications, Subscriptions, mobile/tablet rendering of any page, the tutor-search/discovery experience, any tutor public profile page, or what happens after "Complete registration" is actually clicked (application-submitted confirmation state, review-pending state, etc. — all still **NOT VERIFIED**, same as before this evidence pack).

## Evidence Limitations

- All 13 screenshots are **desktop only** (1920×1080). **Mobile visual evidence: NOT PROVIDED by this screenshot set.**
- Several step identities were inferred from scroll-continuation and chronological file order rather than directly observed in-frame (flagged per-screenshot above as PARTIALLY OBSERVED) — never upgraded to OBSERVED where the breadcrumb/H1 itself isn't in the frame.
- No screenshot shows a validation-error state, an empty/loading state, or any state reached by an invalid input.
- No screenshot shows the actual expanded "Preply commission" percentage figure (SS13's commission section is collapsed).
- No screenshot shows what happens after submission (the application was deliberately not submitted).
- This evidence does not confirm or change anything about authenticated areas outside this one wizard (student dashboard, messaging, Classroom, etc. all remain exactly as previously documented elsewhere in this directory).

## Screenshot → Feature Matrix

| Screenshot | Page/Flow | Role | Evidence | TutorFlow Area | Parity |
|---|---|---|---|---|---|
| SS01 | Tutor signup — About (step 1) | Tutor applicant | PARTIALLY OBSERVED | `BasicsStep.tsx` + `TeachingStep.tsx` | PARTIALLY MATCHED |
| SS02 | Tutor signup — Photo (step 2) | Tutor applicant | OBSERVED | `PhotoStep.tsx` | NOT VERIFIED |
| SS03 | Tutor signup — Certification (step 3) | Tutor applicant | OBSERVED | `CredentialsStep.tsx` | PARTIALLY MATCHED |
| SS04 | Tutor signup — Education (step 4) | Tutor applicant | OBSERVED | `CredentialsStep.tsx` | PARTIALLY MATCHED |
| SS05 | Tutor signup — Education (step 4, cont'd) | Tutor applicant | PARTIALLY OBSERVED | `CredentialsStep.tsx` | MISSING (Diploma-verified badge) |
| SS06 | Tutor signup — Description (step 5) | Tutor applicant | OBSERVED | `BasicsStep.tsx` | PARTIALLY MATCHED |
| SS07 | Tutor signup — Video (step 6) | Tutor applicant | OBSERVED | `VideoStep.tsx` | NOT VERIFIED |
| SS08 | Tutor signup — Video (step 6, requirements) | Tutor applicant | OBSERVED | `VideoStep.tsx` | MISSING (Do/Don't panel, AI script) |
| SS09 | Tutor signup — Video (step 6, cont'd) | Tutor applicant | PARTIALLY OBSERVED | `VideoStep.tsx` | MISSING (link-paste path, rules) |
| SS10 | Tutor signup — Availability (step 7, timezone) | Tutor applicant | OBSERVED | `AvailabilityStep.tsx` | PARTIALLY MATCHED |
| SS11 | Tutor signup — Availability (step 7, Mon–Thu) | Tutor applicant | PARTIALLY OBSERVED | `AvailabilityStep.tsx` | NOT VERIFIED |
| SS12 | Tutor signup — Availability (step 7, Sat–Sun) | Tutor applicant | PARTIALLY OBSERVED | `AvailabilityStep.tsx` | NOT VERIFIED |
| SS13 | Tutor signup — Pricing (step 8, final) | Tutor applicant | OBSERVED | `RateStep.tsx` + `ReviewStep.tsx` | PARTIALLY MATCHED / deviation candidate |

## Visual Parity Gaps

### P0 — core booking/payment/lifecycle blockers

None. This evidence pack covers only the tutor-application wizard, not booking/payment/lesson lifecycle — no P0-tier finding applies.

### P1 — major navigation/dashboard/messaging/notification/classroom gaps

- **No distinct Review/Summary step in Preply's real flow, but TutorFlow has one** (SS13). Low implementation cost either way, but it is a genuine product-shape question for whoever owns the wizard-structure decision in `16-product-decisions-and-open-questions.md` — should TutorFlow drop its Review step to match, or is a review step a deliberate, justified TutorFlow improvement? Not decided here.
- **Certification and Education are confirmed-separate Preply steps**; TutorFlow combines them. Now evidenced at OBSERVED strength rather than DOCUMENTED — raises the weight of item 1 in `16-product-decisions-and-open-questions.md` (wizard structure) without resolving it.

### P2 — secondary UX/detail gaps

- "Diploma verified" badge mechanic tied to document upload + review (SS05) — missing from TutorFlow entirely.
- Video step's explicit Do/Don't requirements panel and anti-doxxing rule text (SS08, SS09) — missing from TutorFlow's Video step.
- AI-assisted "Create script" video-writing aid (SS08, SS09) — missing from TutorFlow.
- Timezone auto-detect-and-confirm pattern vs. TutorFlow's plain required dropdown (SS10) — UX-pattern gap, not a data gap.
- Four-part guided-writing sub-prompts + soft character counter on the profile-description field (SS06) — missing from TutorFlow's equivalent "About you" textarea.
- "Paste a YouTube/Vimeo link" as an alternative to uploading/recording video (SS09) — not confirmed present in TutorFlow's Video step.

### P3 — polish/minor differences

- Default weekly-availability template (all 7 days pre-enabled, 09:00–17:00) vs. TutorFlow's own default (not re-verified) — cosmetic/onboarding-friction difference only.
- "USD only" pricing-entry copy at this step (SS13) vs. TutorFlow/TutorFlow's EUR-only V1 deviation — already a known, already-decided `INTENTIONAL DEVIATION` (`ADR-0004`), not a new gap.

## Architecture Mapping (documentation only — no code written)

```text
Feature: "Diploma verified" badge (document-upload + manual review + badge grant)
Preply evidence: SS05
TutorFlow current state: CredentialsStep.tsx collects education entries; no badge/review-state concept
Gap: No reviewer workflow, no badge field on TutorApplication, no file-retention-then-delete policy
Potential TutorFlow module: Tutors (additive field on TutorApplication, mirrors the existing
  Photo/Rate submission-readiness pattern) — requires an Admin/reviewer actor, which is already a
  known blocker (see 16-product-decisions-and-open-questions.md item 9, Admin role granularity)
Existing dotnet-claude-kit pattern: none new — additive entity field + existing review-queue shape
Dependencies: Admin role/reviewer workflow (not yet built at all, per 13-tutorflow-current-state-and-parity.md)
Priority: P2
```

```text
Feature: Video step Do/Don't requirements panel + AI "Create script" assist
Preply evidence: SS08, SS09
TutorFlow current state: VideoStep.tsx exists; exact current content not re-verified in this pass
Gap: Unknown extent until VideoStep.tsx is read directly — flagged, not assumed
Potential TutorFlow module: Tutors (frontend-only content/copy addition for the Do/Don't panel;
  the AI script-writing assist would be a new integration, out of scope to estimate here)
Existing dotnet-claude-kit pattern: n/a — presentation-layer content for the panel; a new external
  AI-provider dependency for the script-writing feature specifically, not evaluated here
Dependencies: none blocking for the Do/Don't panel; an LLM-provider decision for script-writing
Priority: P2 (panel), P3 (AI script assist — purely a "nice to have" convenience feature)
```

```text
Feature: No separate Review/Summary step in Preply's real flow
Preply evidence: SS13 (Pricing step leads directly to "Complete registration")
TutorFlow current state: ReviewStep.tsx exists as an explicit 8th step
Gap: Not a missing-feature gap — the reverse case: TutorFlow has something Preply's real flow
  does not appear to have
Potential TutorFlow module: Tutors (if a Product Owner decides to remove it)
Existing dotnet-claude-kit pattern: n/a
Dependencies: none
Priority: Documentation gap / Product Owner review (per 16-product-decisions-and-open-questions.md
  item 1), not an engineering priority tier
```

## Mobile / Responsive (Part A)

Mobile visual evidence: **NOT PROVIDED** by the Part A screenshot set. All 13 images are 1920×1080 desktop Chrome captures. No mobile/tablet inference is made from them.

---

# Part B — Student Discovery, Matching Quiz & Checkout Evidence Pack (SS14–SS48)

## Evidence Rules (Part B)

Same labels and discipline as Part A (see above). Additionally for this pack: a page reached via `Continue`/`Skip` from the previous screen is evidence the transition **occurred**, but not evidence of every intermediate validation step — classified `OBSERVED` for the destination state itself, `INFERRED` for anything about the transition mechanism not directly visible (e.g. whether client-side or server-side validation ran).

## Screenshot Inventory (Part B)

All 35 images, 1920×1080, desktop Chrome, single real session, chronological.

### BeforFindYourTutor.zip (3 images)

| ID | Filename | Page | Classification |
|---|---|---|---|
| SS14 | `Screenshot 2026-10-06 075848.png` | Homepage (`preply.com/?logged_out=y`), hero | OBSERVED |
| SS15 | `Screenshot 2026-10-06 075902.png` | Homepage, scrolled — stats + popular-languages grid | OBSERVED |
| SS16 | `Screenshot 2026-10-06 075916.png` | Homepage, scrolled — tutor-card marketing visual + "Lessons you'll love. Guaranteed." | OBSERVED |

### FindYourTutor.zip (32 images)

| ID | Filename | Page | Classification |
|---|---|---|---|
| SS17 | `081442.png` | `/get-started` — subject capture hero | OBSERVED |
| SS18 | `081455.png` | Get-started quiz — "What's your goal?" | OBSERVED |
| SS19 | `081516.png` | Get-started quiz — "Which topics would you like to focus on?" | OBSERVED |
| SS20 | `081527.png` | Get-started quiz — "Which career-related skills..." | OBSERVED |
| SS21 | `081541.png` | Get-started quiz — "Which industry do you work in?" | OBSERVED |
| SS22 | `081603.png` | Get-started quiz — "What's your job title or role?" (skippable) | OBSERVED |
| SS23 | `081610.png` | Get-started quiz — "By when do you want to achieve this goal?" | OBSERVED |
| SS24 | `081625.png` | Get-started quiz — "What's your English level?" | OBSERVED |
| SS25 | `081639.png` | Get-started quiz — "Do you have any special educational needs?" | OBSERVED |
| SS26 | `081654.png` | Get-started quiz — "What kind of teaching style works best for you?" (select up to 3) | OBSERVED |
| SS27 | `081712.png` | Get-started quiz — "What country would you like your tutor to be from?" | OBSERVED |
| SS28 | `081723.png` | Get-started quiz — "Are there any other languages..." | OBSERVED |
| SS29 | `081747.png` | Get-started quiz — "When can you take lessons?" (days + times) | OBSERVED |
| SS30 | `081819.png` | Get-started quiz — "What's your budget?" (range slider) | OBSERVED |
| SS31 | `081836.png` | Get-started quiz — "Are you interested in learning another subject?" (skippable) | OBSERVED |
| SS32 | `081847.png` | Get-started quiz — final free-text step + running answer-summary tray | OBSERVED |
| SS33 | `081901.png` | Branded loading screen — "Finding tutors who will inspire you." | OBSERVED |
| SS34 | `081911.png` | Search results (blurred) behind a mandatory sign-up modal | OBSERVED |
| SS35 | `081917.png` | Real Google OAuth account-chooser screen | OBSERVED |
| SS36 | `081926.png` | Search results, now authenticated — first tutor card in full | OBSERVED |
| SS37 | `081958.png` | Tutor profile — booking calendar/schedule | OBSERVED |
| SS38 | `082103.png` | Checkout — "You chose Tuesday at 13:30" confirmation modal | OBSERVED |
| SS39 | `082116.png` | Checkout — full payment form + price breakdown + review carousel | OBSERVED |
| SS40 | `082222.png` | Tutor profile (back-navigated) — more schedule slots + Resume/Certifications tab | OBSERVED |
| SS41 | `082233.png` | Tutor profile — more certifications + "My specialties" accordion begins | OBSERVED |
| SS42 | `082238.png` | Tutor profile — specialties accordion continued, "Show more specialties" | OBSERVED |
| SS43 | `082251.png` | Tutor profile (scrolled to top) — intro video + AI teaching-style match explainer | OBSERVED |
| SS44 | `082316.png` | Authenticated account dropdown menu | OBSERVED |
| SS45 | `082345.png` | Messages slide-over panel, with a pre-existing real conversation | OBSERVED |
| SS46 | `082358.png` | Help Center, authenticated/personalized home | OBSERVED |
| SS47 | `082419.png` | Saved Tutors (Favorites) — empty state | OBSERVED |
| SS48 | `082427.png` | Notifications slide-over panel — empty state | OBSERVED |

## Student / Find Your Tutor Evidence

### The homepage (SS14–SS16)

A pink hero section: top promo banner ("Preply stands with Ukraine and its people — Join us", dismissible), header nav (`Find tutors`, `For business`, `Become a tutor`, `Proven Progress`), a language/currency selector ("English, USD" while logged out), a help icon, and an outlined "Log In" button. Hero headline "Learn faster with your best language tutor.", a "Find your tutor" CTA, and a hero image collage with an inline mini tutor-card ("Ryan C.", "Super Tutor" badge, "200 lessons", "★4.9 84 reviews"). Scrolling reveals exact stat labels: **100,000+ Experienced tutors, 300,000+ 5-star tutor reviews, 120+ Subjects taught, 180+ Tutor nationalities, 4.8★ on the App Store** — then a 3×3 "popular languages" grid (English 33,602 teachers, Spanish 10,052, French 3,712, German 1,508, Italian 2,541, Chinese 5,252, Arabic 3,650, Japanese 2,902, Portuguese 1,635 — each linking to `preply.com/en/online/{language}-tutors`) with a "Show more" expander. Further down, a 3-column explainer with a stacked tutor-card marketing visual, then a full-bleed pink section: **"Lessons you'll love. Guaranteed." / "Try another tutor for free if you're not satisfied."** — this is the literal, on-page source of Preply's Satisfaction Guarantee marketing copy, and (per SS37–SS39 below) is now confirmed to be the real mechanism behind the previously-ambiguous "2 free tutor trials" finding.

**TutorFlow parity: MISSING** — TutorFlow's homepage/landing content was not itself re-audited in this pass, but no equivalent Satisfaction-Guarantee marketing section, stat strip, or popular-languages grid is known to exist.

### The "Find your tutor" matching quiz (SS17–SS32) — entirely new, previously undocumented

This is the single largest new discovery in this evidence pack: a **15-question-ish, branching, personalized matching quiz** that sits between the homepage and the real search-results page, at `preply.com/en/get-started`, invoked from the homepage's "Find your tutor" CTA. **No prior document in this directory describes this flow at all** — all earlier research found only the direct `/online/{subject}-tutors` search page.

Structure, confirmed by reading the URL's own `start_after=` query parameter at each step (the flow is literally state-encoded in the URL, one answer per param):

1. **Subject** (pre-filled from the CTA clicked, e.g. `subject=english`) — hero step, has its own `What do you want to learn?` dropdown + `Find your tutor` button + a `Show N tutors` secondary link.
2. **Goal** (`goal`) — single-select: Career and business / Lessons for kids / Exams and course work / Culture, travel and hobby.
3. **Topic focus** (`use_case`) — chip multi-look single-select, content branches on the goal answer (e.g. career → "Workplace communication", "Job interview preparation", "Career development"...).
4. **Skills to improve** (`skill`) — further branches on the topic answer, with a "Show all skills" expander.
5. **Industry** (`career_industry`) — radio list + "Show all industries" expander (shown only for the career/business branch).
6. **Job title/role** (`career_role`) — free-text textarea, **explicitly skippable** ("Skip" button, not "Continue").
7. **Timeline** (`goal_achievement_period`) — 1-4 weeks / 1-3 months / 3-6 months / As long as it takes / I just need one lesson.
8. **Self-assessed level** (`level`) — I'm just starting / I know the basics / I'm conversational / I'm fluent (plain language, not CEFR codes).
9. **Special educational needs** (`special_needs`) — No / English for ADHD students / English for autistic students / English for dyslexic students / Yes, other. **Previously entirely undocumented** — a genuine accessibility/inclusion-aware matching dimension.
10. **Teaching-style traits** (`tutor_style`) — "Select up to 3 of your favorite traits": Adaptable, Approachable, Encouraging, Engaging, Goal-Focused, Patient, Structured, No preference. (Confirmed later, SS36/SS43, that tutor cards and profiles visibly carry these exact trait labels as pills, and the profile explicitly says when a trait matches the student's selection.)
11. **Tutor's country** (`country_of_birth`) — an explicit "Only English native speakers" toggle (off by default) + country chips (the first chips shown were geographically relevant to the browsing session, e.g. the detected country, then major English-speaking countries) + "Show all".
12. **Other languages the tutor should speak** (`also_speaks`) — chip multi-select + "Show all".
13. **Weekly availability** (`availability`) — two independent chip groups: **Days** (Mon–Sun) and **Times** (Morning/Afternoon/Evening/Night, each with its own icon) — a coarse day/time-of-day preference, distinct from a tutor's own fine-grained weekly calendar.
14. **Budget** (`price`) — a dual-handle range slider, "$X – $Y+ per 50-min lesson" (confirms 50-minute-lesson pricing framing again, consistent with Part A's Pricing step).
15. **Cross-sell: another subject** (`multi_subject_intent`) — "Are you interested in learning another subject in the future?" chip multi-select (Spanish, French, German, Japanese, Italian, Korean, Arabic, Chinese (Mandarin), + Show all) + explicit **"Skip for now"**.
16. **Final free-text + running summary tray** (`step_answer_free_text`) — "Is there anything else that could help us find your perfect tutor? We'll use this to make matches even better", textarea with example placeholder, **"Skip and finish"**. Critically, this final step's left panel renders a **running tray of every prior answer as icon+label chips** (subject, goal, timeline, level, tutor-country, days, time, budget) — the only review/summary mechanism observed anywhere across both evidence packs (contrast with Part A's tutor-signup wizard, which has no equivalent summary step at all).
17. A full-screen branded loading transition — **"Finding tutors who will inspire you."** with the Preply logo, before landing on results.

Every forced-choice step disables `Continue` until a selection is made; every optional step uses a distinctly-labeled `Skip`/`Skip for now`/`Skip and finish` button instead, never a disabled `Continue`. Every step's illustration changes to match its topic. The left/right split layout (pink illustration panel + white question panel, back-arrow top-left, Preply logo bottom-left) is fully consistent across all 15 question steps.

**TutorFlow parity: MISSING** — TutorFlow has no equivalent pre-search matching/preference quiz at all; search starts directly from filters. This is a substantial, previously-unknown product surface.

### Real, authenticated search results (SS34, SS36)

Completing the quiz lands on the real search-results page at `preply.com/en/online/english-tutors`, with **every quiz answer translated into live search filter query parameters** — confirmed directly by URL inspection: `priceRange=14-40`, `time=afternoon,late-afternoon`, `taxonomyAttributes=job_interview_preparation,interview_skills`, `CoB=GB` (country of birth), plus more (truncated in the address bar). The page's own H1 is dynamically personalized: **"English tutors that help you develop professionally"** (because `goal=career`). The filter bar itself is now a row of **removable, editable chips** mirroring the quiz answers exactly: `I want to learn: English`, `Goal: Career and business`, `Focus areas: Job interview preparation, Interview skills` (×), `Price per lesson: €12–€35` (×), `Also speaks`, `Country of birth` (×), `Tutor categories`, an availability chip, and a `Sort tutors by: Our top picks` dropdown.

**Before the results can be freely browsed, a mandatory sign-up modal appears** (SS34): **"All set! Sign up to meet the best English tutors for you."** with `Continue with Google`, `Continue with Facebook`, an `or sign up with email` divider + email field + `Continue`, consent text linking Preply's Terms of Use/Subscription Terms/Privacy Policy, and `Already have a Preply account? Log in`. The search results themselves are visible (not hard-blocked), only blurred/dimmed and non-interactive behind the modal.

**This is now direct, OBSERVED evidence for a finding this directory previously only carried as an INFERENCE** (`I01` in `17-evidence-and-sources.md`: "Guest-booking login gate... never directly observed clicking through"). **Important scope caveat, not over-claimed**: this gate was observed specifically immediately after completing the get-started matching quiz. Whether navigating directly to a plain `/online/{subject}-tutors` search URL (bypassing the quiz) also triggers this same modal is **NOT VERIFIED** by this evidence — an earlier real-browser pass (`01-real-user-observations.md`, source S02) reached a search-results page directly without encountering any such gate, so this may be specific to the quiz-completion funnel, not a universal guest-browsing block. `16-product-decisions-and-open-questions.md` item I01 should be updated to reflect this nuance, not simply marked OBSERVED across the board.

### Tutor cards, in full (SS36)

Once past the gate (post-authentication), the first result card shows, in exact reading order: tutor photo, name ("Darcy Q."), a verified checkmark, a country flag, `€71` price, `50-min lesson` duration label, a heart/favorite icon, `Super Tutor` badge + `Professional` badge (icon-prefixed), subject (`English`), `Speaks English (Native), Spanish (Intermediate) +3`, a green **teaching-style trait pill** (`Goal-Focused` — directly reflecting the quiz's trait-selection step), a bio snippet, a `Learn more` link, a `Book trial lesson` button (primary, pink) and `Send message` button (secondary, outlined), and a **distinct social-proof micro-copy line with a trend icon: "Popular. Booked 15 times recently."** The stats block shows **three separate counters, precisely labeled**: `5★ 42 reviews` (rating+review-count combined), `59 students`, `1414 lessons` — resolving prior ambiguity about whether "students" and "lessons taught" are one stat or two (they are two, both real, both shown). A companion panel beside the card shows a (grey, unloaded-thumbnail) video-preview box with a play button, plus `View full schedule` and `See {name}'s profile` buttons.

**TutorFlow parity: PARTIALLY MATCHED** — TutorFlow's tutor cards do not currently carry a "Booked N times recently" popularity line, do not separately break out students-vs-lessons-taught counters (parity matrix already lists this as MISSING, now with exact real labels to match against), and do not show teaching-style-trait pills at all (a new concept this evidence pack introduces).

### Tutor profile, in full (SS37, SS40–SS43)

- **Schedule**: a duration toggle (`25 mins` / `50 mins`), a date-range navigator (`< >`, "Oct 6 – 12, 2026"), an explicit timezone dropdown reflecting the browser's real detected timezone, and a 7-day grid of clickable time-slot links per day (several slots per day, e.g. Tue: 11:00, 11:30, 12:00, 12:30, 13:00...). An info banner reads: "Choose the time for your first lesson. The timings are displayed in your local timezone."
- **Sidebar (persistent across the whole profile)**: price + duration, `★ rating` + `reviews`/`lessons` stats, a `Book trial lesson` button (with a lightning-bolt icon), three icon-only buttons (message, favorite, share), a green callout: **"Not a match? You still have 2 free tutor trials."**, and a `Popular — N lesson bookings in the last 2 days` line.
- **Resume / Certifications tab**: a repeated entry pattern — year range, certificate/qualification name (shown twice, bold then regular — likely a "name" + "issuing body" pair), and a green **"✓ Certificate verified"** badge per entry. This directly matches (and validates) the "verified certificate" + manual-review concept found in Part A's tutor-signup Certification/Education steps (SS03/SS05) — now confirmed as it actually renders on a real published profile.
- **"My specialties"**: a vertical accordion list (chevron-down per row, collapsed by default) — Conversational English, Business English, IELTS, Intensive English, British English, English for traveling, English job interview prep, Cambridge ESOL: FCE/CAE/CPE, For studying abroad, BEC — with a **"Show more specialties"** button for further items.
- **Intro video**: a real embedded **Vimeo** player (branded watermark, standard Vimeo control bar — play/pause, scrubber, volume, settings, speed, picture-in-picture, fullscreen), muted-by-default with an "Unmute" prompt overlay — directly confirming Part A's signup-wizard finding that Preply supports "paste a YouTube or Vimeo link" as a video-submission path (SS09): this profile's video is in fact Vimeo-hosted.
- **"{Name}'s teaching style" — AI match explainer**: directly beneath the video, a small sparkle/AI icon, the line **"Great match! {Name}'s teaching style matches 1 of your preferences"**, and the tutor's own style-trait pills, where the trait(s) matching the student's quiz answer are visually distinguished (checkmark + highlighted color) from non-matching traits (plain/grey). **This is a genuinely new, previously entirely undocumented recommendation-explainability feature** — the product explicitly tells the student *why* a given tutor was matched.

**TutorFlow parity: MISSING** (AI match-explainer, verified-certificate badge, specialties accordion, popularity-bookings line) / **PARTIALLY MATCHED** (schedule/calendar — TutorFlow has its own slot-picker but without the explicit "times shown in your local timezone" banner or the 25-min/50-min duration toggle on the same screen) / **NOT VERIFIED** (whether TutorFlow's video playback uses an equivalent embed pattern).

### Checkout (SS38–SS39) — reached, not completed

Clicking a time slot opens a confirmation modal: **"You chose Tuesday at 13:30 with {Name}"** / "Here's how your trial stays flexible" with two checked bullets: **"Cancel or reschedule for free until 12 hours before your lesson."** and **"Try up to two other tutors for free."** — then `Continue to checkout`.

This is the single most decisive piece of evidence in this entire evidence pack for two previously-unresolved items:
1. **The cancellation window is 12 hours.** This was already `DOCUMENTED` (help-center article S16) but conflicted with TutorFlow's own locked ≥24h spec. It is now additionally **OBSERVED, directly, inside the live product's own checkout flow** — the strongest possible evidence tier this directory uses, removing any remaining doubt that 12h is Preply's real, current, live policy.
2. **"2 free tutor trials" is now fully resolved.** This phrase was previously recorded as `NOT VERIFIED (meaning unclear)` after being seen once, out of context, on a public tutor profile. It now appears **four separate times** across this evidence pack in increasingly explicit contexts — a generic public-profile teaser (prior research), the authenticated profile sidebar (SS37, SS40–SS43: "You still have 2 free tutor trials"), the checkout confirmation modal (SS38: "Try up to two other tutors for free"), and the checkout page itself (SS39: "Free tutor replacement — If this tutor isn't a match, try 2 more for free"). It is the Satisfaction Guarantee mechanism, literally: every student account gets up to **two free tutor-replacement trials** if their first (or second) pick isn't a match — directly tied to the homepage's "Lessons you'll love. Guaranteed." marketing copy (SS16). **This item should be removed from "Behavior observed but not interpreted" in `16-product-decisions-and-open-questions.md` and moved to confirmed findings.**

The full checkout page (SS39) shows: a `Your tutor` summary card (name, rating+reviews, students/lessons/years-experience stat icons, a personalized relevance line — "Perfect for business topics — Highly rated by learners like you"), a `Trial lesson details` card (date badge, exact time range, "Time is based on your location", a green "Cancel or reschedule for free until {exact computed timestamp}" banner), a `Checkout info` price breakdown — **`25-min lesson €42.72` + `Processing fee (?) €0.27` = `Total €42.99`** (a real, exact, line-itemized **"Processing fee"**, directly and concretely informing the open `16-product-decisions-and-open-questions.md` item about Taxes & Fees — it is a small flat/percentage payment-processing surcharge, not a tax, at least in this instance) — a `Have a promo code?` link (a previously undocumented promo-code affordance), and the same "Free tutor replacement" guarantee callout. The payment form shows card-number/expiry/CVC fields (all empty, placeholder-only), `Apple Pay` and `PayPal` buttons, a pre-checked `Save this card for future payments` checkbox (opt-out, not opt-in — worth noting as a dark-pattern-adjacent default), legal text linking a combined **"Preply's Refund and Payment Policy"** (one document, not two separate ones), and `It's safe to pay on Preply. All transactions are protected by SSL encryption.` A right-hand panel reinforces trust with a review carousel: `"{Name} is a great choice"`, `★5 · 42 reviews`, left/right arrows, and a real (non-identifying) review excerpt.

**The `Book lesson and pay · €42.99` button is rendered disabled (grey, not the active pink used elsewhere)** because the card fields are empty — consistent with no real payment being attempted. The following screenshot shows navigation back to the tutor's profile page, confirming the checkout was abandoned.

**TutorFlow parity**: the 12-hour cancellation window is now `MISMATCHED, confirmed by the strongest evidence tier in this directory` against TutorFlow's locked ≥24h spec (see `16-product-decisions-and-open-questions.md` item 2, now upgraded in confidence). The itemized "Processing fee" is `MISSING` from TutorFlow's checkout (flat total only). The "Have a promo code?" affordance is `MISSING`. The pre-checked "save card" default is a UX-pattern TutorFlow should deliberately decide on, not copy by default (dark-pattern-adjacent).

## Navigation Evidence

### Authenticated header (SS36 onward)

Once signed in, the header changes from the logged-out version (logo, nav links, language/currency selector, help icon, outlined `Log In` button) to: logo, `Find tutors` / `Corporate training` nav links, a `Refer a friend` button, `English, EUR` selector (currency switched from USD to EUR post-authentication — a real, observed locale/currency adjustment tied to the account), a **messages icon with an unread-count badge**, a help icon, a **favorites/heart icon**, a **notifications bell icon**, and a **circular account-initial avatar** (no photo loaded — just an initial letter in a colored circle).

### Account dropdown menu (SS44) — previously entirely undocumented

Clicking the avatar opens a menu: the account name at top (next to the avatar), then **`Home`, `Messages`, `My lessons`, `Saved tutors`, `Refer a friend`, `Settings`, `Help`**, a divider, then **`Log out`**. This is the first direct evidence anywhere in this project's research of Preply's real authenticated student navigation structure, and it maps almost one-to-one onto TutorFlow's own already-planned-but-not-yet-built P0 gap (the "My Lessons" page) — confirming **"My lessons"** is the correct real label to match, not an invented one.

### Messages panel (SS45)

A slide-over panel (not a full page navigation) titled `Messages`, with three tabs: `All`, `Unread (1)`, `Archived`. Each conversation row shows: tutor photo, name, date, a one-line message preview, and an unread-count badge where applicable. One real conversation (dated ~10 weeks before this capture) and one just-created empty conversation stub (from viewing a tutor's profile — "No messages yet") are both shown, confirming that **viewing/favoriting a tutor's profile can create an empty conversation entry before any message is actually sent.**

### Help Center, authenticated (SS46)

`help.preply.com/en/`, now personalized: `Hello {FirstName}, how can we help you?`, a search bar, and three category cards with **exact article counts**: `Help for Students — 68 articles`, `Help for Tutors — 95 articles`, `Preply Business — 20 articles`. No live-chat widget is visible here either, consistent with the earlier anonymous-session non-finding (source S04) — still not a confirmed absence, just not visible in either session.

### Saved Tutors / Favorites — empty state (SS47)

Reached via the account menu's `Saved tutors` item, which routes to the **same search-results URL** with a `favTutors=true` query flag (`?source=header&favTutors=true`), not a separate dedicated page. Empty state: `Save tutors using the ♡ icon`, `Browse and save tutors on the "Find tutors" page. View your saved tutors here anytime.`, and a `Browse tutors` button.

### Notifications panel — empty state (SS48)

A slide-over panel titled `Notifications`: a bell illustration, `Don't miss out on updates!`, and `Keep an eye on this feed for updates about your learning journey on Preply`. No notification types or examples are visible (the panel is empty) — the exact notification types/channels remain `NOT VERIFIED`, as before; this only newly confirms the empty-state copy and panel mechanics.

## Visual Design Patterns (Part B)

- **Split-screen quiz layout**: a fixed pink illustration panel (left) + white question panel (right), back-arrow top-left, Preply logo bottom-left of the pink panel, consistent across all 15+ quiz steps, each with its own topic-matched illustration.
- **Disabled-until-valid primary actions**: every forced-choice quiz step and the checkout payment button render their primary CTA in a visually distinct disabled/grey state until the required input is present, versus the active pink state.
- **Distinctly-labeled optional steps**: `Skip`, `Skip for now`, `Skip and finish` are each used precisely once, for exactly the three genuinely optional quiz steps, never interchangeably.
- **Running answer-summary chips**: icon+label pill pairs, reused as both a "review tray" (quiz's final step) and as "editable active filters" (search results' filter bar) — the same visual component serving two different functional roles.
- **Slide-over panels** (not full navigations) for Messages and Notifications, both titled, both with an `✕` close control top-right, both reachable from persistent header icons.
- **Trait-pill matching**: a visual-matched-vs-unmatched state (checkmark+color vs. plain) reused identically between the quiz's trait-selection step and a tutor's own profile trait display.
- **Disabled payment CTA tied to empty required fields**, not a separate validation-error state — the button simply doesn't activate until card fields are populated.

## Cross-Screenshot Journey Evidence

The full, confirmed, real (if ultimately abandoned-before-payment) journey captured in this pack:

```text
Homepage (logged out)
  → "Find your tutor" CTA
  → /get-started quiz (16 steps: subject → goal → use_case → skill → industry →
     job_role[skip] → timeline → level → special_needs → tutor_style → tutor_country →
     also_speaks → availability → budget → other_subjects[skip] → free_text[skip])
  → branded loading screen
  → search results (filters pre-populated from quiz answers)
  → MANDATORY sign-up modal (Google / Facebook / email)
  → real Google OAuth sign-in (account chooser → authenticated)
  → search results, now authenticated, filters + results fully interactive
  → tutor profile (schedule, resume/certifications, specialties, video, AI match explainer)
  → time-slot selected → "You chose Tuesday at 13:30" confirmation modal
  → "Continue to checkout"
  → real checkout page (price breakdown, payment form)
  → [ABANDONED — no payment fields filled, pay button left disabled, navigated back]
  → back to tutor profile
  → account dropdown menu explored (Home/Messages/My lessons/Saved tutors/Settings/Help/Log out)
  → Messages panel opened (pre-existing real conversation visible)
  → Help Center (authenticated)
  → Saved Tutors (empty state)
  → Notifications panel (empty state)
```

Every arrow above is `OBSERVED` (a literal screenshot exists for the destination state). No arrow's *interaction mechanism* (exact click target, hover state, keyboard path) is claimed beyond what's visible — consistent with this directory's standing rule not to claim an interaction was literally clicked unless shown.

## TutorFlow Visual Parity (Part B)

| Preply evidence | TutorFlow current state | Status | Gap | Priority |
|---|---|---|---|---|
| "Find your tutor" matching quiz (SS17–SS32) | No equivalent; search starts from filters directly | MISSING | A substantial new pre-search personalization surface | P2 — valuable but not launch-blocking; TutorFlow's filter-first search is a reasonable, simpler V1 |
| Quiz answers pre-populate search filters as removable chips (SS36) | TutorFlow's own filter UI not re-audited here | NOT VERIFIED | — | — |
| Mandatory sign-up gate after quiz completion (SS34) | TutorFlow has `AuthGateModal` (pre-existing, `I01`) | PARTIALLY MATCHED | Gate trigger condition differs (quiz-completion vs. booking-action) | P3 |
| Teaching-style trait pills + "Select up to 3" matching (SS26, SS36, SS43) | No equivalent concept anywhere in TutorFlow | MISSING | New Tutors-module field + Search-module matching logic | P2 |
| AI match-explainer ("matches 1 of your preferences") (SS43) | No equivalent | MISSING | Recommendation-explainability UI; depends on the trait concept above existing first | P2 |
| "Popular. Booked N times recently" card micro-copy (SS36) | No equivalent | MISSING | Small, Bookings-derived computed stat | P3 |
| Students / lessons-taught as two separate card counters (SS36) | Already tracked as MISSING in `13-...md` | MISSING (confirmed, now with exact labels) | Depends on Lessons completion-tracking | P2 |
| "Verified certificate" badge on public profile (SS40) | `CredentialsStep.tsx` collects data, no badge/review-state concept | MISSING | Needs Admin/reviewer workflow (same blocker as Part A's Diploma-badge finding) | P2 |
| Specialties accordion with "Show more" (SS41–SS42) | Not independently verified in this pass | NOT VERIFIED | — | — |
| 12-hour cancellation window, now OBSERVED at checkout (SS38) | Spec locked at ≥24h | **CONFIRMED MISMATCH (strongest evidence tier)** | Spec correction, zero code risk — refund mechanism itself still unbuilt | **P1** — resolve before any refund logic is written |
| "2 free tutor trials" fully resolved as the Satisfaction Guarantee mechanism (SS16, SS37–SS39) | No tutor-replacement-credit concept exists | MISSING | A real, concrete, small feature: N free tutor-replacement trials per student, tied to the trial-booking flow | P2 — now a well-specified candidate, not a vague "someday" item |
| Itemized "Processing fee" line at checkout (SS39) | Flat total only | MISSING | Directly informs the open Taxes & Fees product decision | Blocked on Product Owner decision (per `16-...md`) |
| "Have a promo code?" checkout affordance (SS39) | Absent | MISSING | New, previously undiscovered | P3 |
| Authenticated account menu: Home/Messages/My lessons/Saved tutors/Refer a friend/Settings/Help/Log out (SS44) | No unified account menu exists yet (Tutor Dashboard/Student Dashboard both MISSING per `13-...md`) | MISSING | Directly validates the exact nav label ("My lessons") for TutorFlow's own already-proposed P0 slice | Confirms, does not change, the existing P0 roadmap item |
| Messages panel creates an empty conversation stub on profile view, before any message sent (SS45) | NOT VERIFIED against TutorFlow (Messaging module is a pure stub) | BLOCKED | Messaging module doesn't exist yet | P1 (pre-existing) |
| Vimeo-hosted intro video embed (SS43) | Not independently verified | NOT VERIFIED | — | — |
| "Refer a friend" header CTA (SS36, SS44) | Absent | MISSING | Growth feature, not core journey | P3 |

## Newly Discovered Gaps (Part B) — priority rollup

**P1:**
- Cancellation-window spec correction (24h → 12h) — now confirmed at the highest evidence tier this directory uses; should land before any refund-building work, exactly as `15-dependency-graph-and-roadmap.md` already recommends, now with stronger justification.

**P2:**
- "Find your tutor" matching quiz (entire new surface, optional for V1 given TutorFlow's filter-first search already works).
- Teaching-style trait pills + "Select up to 3" + AI match-explainer (a connected pair of features).
- "2 free tutor trials" / free tutor-replacement guarantee — now well-specified enough to actually scope.
- Students/lessons-taught as two distinct card counters (reinforces existing P2 item).
- "Verified certificate" badge (reinforces Part A's P2 finding from a second, independent angle).

**P3:**
- "Popular. Booked N times recently" card micro-copy.
- "Have a promo code?" checkout affordance.
- "Refer a friend" growth CTA.
- Pre-checked "save card for future payments" default — a product/UX decision (opt-in vs. opt-out), not an engineering gap.

**Blocked on existing Product Owner decisions:**
- Itemized "Processing fee" — directly feeds the already-open Taxes & Fees decision in `16-product-decisions-and-open-questions.md`; still not invented or decided here.

## Evidence Limitations (Part B)

- This pack does not show the tutor-side of anything (dashboard, earnings, accept/decline) — Part A (tutor-signup) remains the only tutor-side evidence.
- No payment was completed, so the actual post-payment confirmation screen, webhook-driven status update, or "My lessons" page populated with a real booking is **NOT VERIFIED** by this evidence.
- No mobile/tablet screenshots — all 35 images are 1920×1080 desktop.
- The exact mechanism and universality of the sign-up gate (quiz-only vs. all search access) is **NOT VERIFIED** — flagged explicitly above, not assumed either way.
- The "Processing fee" amount (€0.27 on €42.72, ≈0.6%) is a single data point for one trial lesson in one currency; whether it's a flat fee, a percentage, or varies by payment method/region is **NOT VERIFIED**.
- The notification panel's real notification types remain entirely unknown — only its empty state was observed.
- Whether "My lessons" (seen only as a menu label, never clicked into) shows upcoming lessons, past lessons, or both, and in what layout, is **NOT VERIFIED** — the menu item was not clicked through in this evidence pack.

---

# Part C — Real Transactional Email Evidence (SS49–SS52)

## Evidence Rules (Part C)

Same labels and discipline as Parts A/B. These screenshots show **real emails Preply actually sent and delivered** to a real Gmail inbox — this is a stronger evidence tier than anything else in this directory for the Notifications area specifically, because it is not a UI mockup or a help-article description but the literal rendered product of Preply's real notification/email system.

## Screenshot Inventory (Part C)

| ID | Filename | Content | Classification |
|---|---|---|---|
| SS49 | `Screenshot 2026-10-06 085701.png` | Gmail inbox — "New message from Darcy" email, top portion (header + message-preview card) | OBSERVED |
| SS50 | `Screenshot 2026-10-06 085712.png` | Same email, scrolled — trial-lesson duration explainer + footer (subscribed email, unsubscribe links) | OBSERVED |
| SS51 | `Screenshot 2026-10-06 085754.png` | Gmail inbox — "Darcy is ready for your lesson" abandoned-checkout recovery email, top portion | OBSERVED |
| SS52 | `Screenshot 2026-10-06 085759.png` | Same email, scrolled — CTA button + tracking-link domain visible on hover | OBSERVED |

## Email Evidence

### Email 1 — "New message from Darcy" (SS49–SS50)

A real Gmail message (inbox position "2 of 222", confirming a long-lived, actively-used real inbox). Rendered content: Preply logo, headline **"You have a new message"**, subtext "Darcy sent you a message.", a card with the tutor's photo, name, an **"↗ Highly rated by learners like you"** tag, a truncated message-preview snippet ("Thank you for visiting my profile. I work with professionals who need their English to match their expertise, whether that's..."), and a **"View message"** CTA button (pink). Scrolling reveals the same email continues with trial-lesson education content under a (partially cropped) "...a trial lesson" heading: **"A trial lesson is a casual, commitment-free way to try Preply."**, followed by two side-by-side option cards — **"25 minutes — Enough time to introduce yourself and chat about your English goals."** and **"50 minutes — Everything in the 25-minute lesson, plus some extra time to create a learning plan together."** — each with its own icon (green rising-chart vs. blue ascending-stairs). The footer shows social icons (Instagram, LinkedIn, TikTok, YouTube), a **"Subscribed email address: [REAL EMAIL ADDRESS — REDACTED]"** line, and **"Manage notifications | Contact us"** links.

What this proves: Preply combines a **tutor-initiated-message notification** with **unsolicited trial-lesson educational/conversion content** in a single email, not two separate sends. It also gives the first concrete, *differentiated* value proposition for the two trial durations found throughout this directory (25min = introductions/goals; 50min = introductions/goals **plus** a collaborative learning-plan step) — previously this directory only recorded that 25/50-minute options exist, never *why* a student might pick one over the other.

What this does not prove: the exact trigger condition (is this sent on every new message, or only the first message from a given tutor? not established), send-timing/delay, or whether push/SMS equivalents exist alongside email.

### Email 2 — "Darcy is ready for your lesson" — real abandoned-checkout recovery email (SS51–SS52)

A second, newer real Gmail message (inbox position "1 of 222" — the newest message, received after Email 1). Tab title: "Try a English lesson with D[arcy]". Rendered content: Preply logo, headline **"Darcy is ready for your lesson"**, a card showing the tutor's photo and, critically, a label reading **"Abandoned slot"** followed by the **exact date and time of the real, uncompleted checkout session from Part B** ("Tuesday, October 6, 13:30" — "Time is based on your location"). Below that: the same **"Perfect for business topics — Highly rated by learners like you"** personalization line already seen at checkout in Part B (confirming this relevance copy is reused system-wide, not generated fresh per surface). Body text: "See what it's like to learn English 1-on-1 with Darcy. Get to know them, chat about your goals, and start learning right away." and a pink **"Give it a try"** CTA button. Hovering the button reveals a tracked link on the `ablink.tips.preply.com` subdomain (an email click-tracking/link-wrapping domain, consistent with standard marketing-automation infrastructure — the long opaque tracking token itself is not reproduced here, it carries no product-structure information).

**This is the single most significant finding in Part C**: it is direct, real, dated proof that Preply runs **automated abandoned-checkout/cart-recovery email marketing** for trial-lesson bookings specifically — triggered by a real user leaving a real checkout page without completing payment (exactly what happened in Part B, SS38–SS40), and sent with the exact slot details still attached, explicitly labeled "Abandoned slot." No prior document in this directory recorded that this automation exists at all.

What this does not prove: the exact delay between abandonment and send (both emails were only noticed together in this inbox check, so the precise timing window is **NOT VERIFIED**), whether multiple reminder emails are sent over time, whether the "Abandoned slot" offer expires, or whether an equivalent push/in-app notification also fires.

## TutorFlow Visual Parity (Part C)

| Preply evidence | TutorFlow current state | Status | Gap | Priority |
|---|---|---|---|---|
| Tutor-message notification email (combined with trial-lesson education content) | `Notifications` module is a pure stub; zero email infrastructure exists anywhere (confirmed by direct code inspection, prior audit) | MISSING | Needs real email infrastructure + a Messaging-event-driven trigger (depends on Messaging module, also a stub) | P1 (pre-existing gap, now with a concrete real template to design against) |
| Abandoned-checkout/cart-recovery email, triggered by a real abandoned Bookings/Payments checkout, with the exact slot re-attached | **IMPLEMENTED (ADR-0029)**: real `IEmailSender`/Postmark infrastructure, a new `BookingHoldExpired` event, a time-driven `BookingHoldExpirySweepService` (reuses the existing hold-expiry guard logic, not the literal per-tutor lazy sweep itself — that one cannot serve as a reliable notification trigger, see ADR-0029 §3), idempotent recovery email send | MATCHED (external behavior: a real recovery email now fires on a real abandoned checkout) / NOT VERIFIED (exact trigger timing, retry policy — never claimed as Preply parity) | None remaining for this slice; tutor-message notification (above) remains separate | P1 — **done**, this slice |
| Differentiated 25-min vs. 50-min trial value proposition copy | TutorFlow shows duration as a plain option, no differentiated messaging confirmed | PARTIALLY MATCHED / NOT VERIFIED | Copy-only enhancement, no architecture impact | P3 |
| Email click-tracking via a dedicated link-wrapping subdomain | Not applicable — no email infrastructure exists yet | N/A | Standard infrastructure once email is built, not a product decision | — |

## Newly Discovered Gaps (Part C)

**P1:**
- **Abandoned-checkout recovery email — ✅ IMPLEMENTED (ADR-0029).** Real email infrastructure (Postmark, provider-neutral `IEmailSender` abstraction), a real `BookingHoldExpired` trigger, and an idempotent recovery-email handler now exist. See ADR-0029 for the full design and the exact evidence-vs-engineering-decision boundary (trigger timing is TutorFlow's own choice, not a Preply-verified number).
- Tutor-message notification email — reinforces the pre-existing Messaging+Notifications P1 gap with a concrete real template.

**P3:**
- Differentiated trial-duration marketing copy.

## Evidence Limitations (Part C)

- Only 2 of Preply's presumably-many transactional email types were captured (a message notification and an abandoned-checkout recovery email). No booking-confirmation, payment-receipt, lesson-reminder, or review-request email is evidenced here — all remain **NOT VERIFIED**, same as before.
- The exact trigger delay/timing for the abandoned-checkout email is unknown — only that it arrived at some point after the Part B session, and was the newer of the two emails in the inbox.
- Whether this pack's two emails represent Preply's *only* notification channel (email) or whether push/SMS/in-app equivalents also exist alongside them remains **NOT VERIFIED** — consistent with the pre-existing, unresolved finding in `12-classroom-messaging-reviews-notifications.md`.
- No settings/preferences screen was captured in this pack (only a footer "Manage notifications" link, not clicked through).
