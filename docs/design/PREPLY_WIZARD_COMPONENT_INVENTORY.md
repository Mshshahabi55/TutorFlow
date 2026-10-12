# Preply Wizard Component Inventory

Companion to `PREPLY_UI_UX_AUDIT.md` §12–17 (Wizard Forms section of the audit request). Covers conceptual, reusable components needed to reproduce Preply-level wizard UX — specifically for the tutor-onboarding flow and, more lightly, the trial-booking flow. **Conceptual names only; nothing here is implemented.** Evidence basis was, at the time this file was written, public fetches + help-center articles only, no screenshot/browser tool. See `PREPLY_UI_UX_AUDIT.md` §27 for the specific items marked "Not reliably observable" that bear directly on this document.

**Update from a later session:** the open question in the paragraph above — "whether the 8 application sections are true wizard steps or a single sectioned form" — is now resolved by direct screenshot evidence: it **is a true multi-step wizard** (breadcrumb step navigation with per-step completion checkmarks, explicit numbered steps, and a "Back"/"Save and continue" pair per step, not a single scrolling form). See `docs/product/preply-reverse-engineering/18-screenshot-evidence.md` for the full, OBSERVED 8-step breakdown.

---

### WizardShell
- **Purpose:** outer container/orchestrator for a multi-step flow — owns current-step state, step validity, and navigation.
- **Preply observation:** existence of a multi-section application flow is confirmed; whether it is literally one `WizardShell`-style stepper with distinct steps/URLs, or a single long form with anchor-linked sections, is **not reliably observable** this session.
- **Interaction behavior (recommended, not Preply-confirmed):** hold current step index, per-step completion state, and expose next/back/submit to child steps.
- **Variants:** linear-only wizard (no skipping) — matches the implied Preply pattern (no skip behavior was described anywhere in sourced material).
- **States:** in-progress, step-invalid (next disabled), submitting, submitted.
- **Accessibility:** should manage focus on step change, announce step changes to screen readers (**not a Preply-confirmed behavior — a recommendation**).
- **TutorFlow equivalent:** `WizardShell` — see `TUTORFLOW_WIZARD_ARCHITECTURE.md`.

### WizardHeader
- **Purpose:** shows wizard title and, optionally, exit/save-and-exit affordance.
- **Preply observation:** **not reliably observable.**
- **Recommendation:** TutorFlow should show an explicit "Save & exit" rather than a bare back-button, since the onboarding wizard is long and resumption matters (see architecture doc).

### WizardProgress
- **Purpose:** communicate how many steps remain / current position.
- **Preply observation:** a progress indicator's existence for the *tutor application* specifically is **not reliably observable** (distinct from the homepage's purely illustrative 3-step "How It Works" marketing graphic, which is not a functional wizard component).
- **Recommendation:** TutorFlow should include one regardless — a confirmed completion motivator in wizard-UX research generally, and consistent with the "perceived progress" benchmark criterion in the audit request.

### WizardStep
- **Purpose:** wraps a single step's form content, title, and instructions.
- **Preply observation:** each of the 8 application sections maps conceptually to one step's content, whether or not the chrome is a literal stepper.
- **TutorFlow equivalent:** `WizardStep`, one per onboarding section (photo, certifications, education, description, video, availability, pricing, + the one unconfirmed 8th section — do not invent its name).

### WizardFooter / StepNavigation
- **Purpose:** Back/Next/Submit actions, typically sticky at the bottom.
- **Preply observation:** **not reliably observable.** Sticky footer actions are a common pattern for long forms generally; not confirmed specifically for Preply's tutor application.
- **Recommendation:** disable "Next" until the current step's required fields are valid (standard progressive-disclosure practice — not Preply-specific, but consistent with the audit's own "error prevention" benchmark).

### FormSection / FormField
- **Purpose:** generic field-group and single-field wrapper (label, input, helper text, error text).
- **Preply observation:** existence of field-level guidance (e.g., profile description and video guidelines) is confirmed; the exact label/placeholder/error text patterns are **not reliably observable.**
- **TutorFlow equivalent:** `FormField`, shared across the wizard and ordinary forms — not a wizard-only component.

### MultiSelect / SearchableSelect / TagInput
- **Purpose:** choosing languages, subjects, specialties.
- **Preply observation:** existence of "languages," "teaching subject," and "specialties" as distinct application concerns is confirmed by the task's own step list and general Preply knowledge of multi-subject tutors; the exact control type for each (chip-based multi-select vs. searchable dropdown vs. free tagging) is **not reliably observable.**
- **Recommendation:** subjects/specialties are a bounded, platform-defined list (matches TutorFlow's own `Subjects` module being a controlled catalog) — a searchable multi-select is the safe default; free-text tagging is not appropriate since subjects must map to real `Subjects` module entities.

### FileUploader / ImageUploader / VideoUploader
- **Purpose:** profile photo, certificate/education documents, intro video.
- **Preply observation:** existence confirmed for all three; crop/trim UI, drag-and-drop support, progress indicators **not reliably observable.**
- **Recommendation:** `ImageUploader` should support basic crop (photo is a trust signal, Preply's own guidance stresses photo quality); `VideoUploader` needs an upload-progress and basic duration-check state given the sourced "under ~2 minutes" guidance — even though that guidance is content advice, not a confirmed hard validation rule.

### AvailabilityGrid
- **Purpose:** tutor sets recurring weekly availability + timezone.
- **Preply observation:** existence confirmed ("Calendar" section; dedicated "Setting up your availability" help article); exact grid interaction (click-drag vs. per-slot toggle) **not reliably observable.**
- **TutorFlow equivalent:** should render against TutorFlow's existing `Availability` module data model (weekly recurring rules) — this is the one onboarding step where TutorFlow already has a backend shape to design the UI against, unlike the others.

### RepeatingEntryEditor
- **Purpose:** add/remove multiple education or certification entries.
- **Preply observation:** existence of "Education" and "Certifications" as sections implies some tutors have more than one entry each, but an explicit add/remove repeating-entry UI is **not reliably observable** — it's a reasonable inference, not a confirmed pattern.
- **TutorFlow equivalent:** `RepeatingEntryEditor`, generic enough to serve both education and certification lists.

### RateInput
- **Purpose:** set hourly tutoring rate.
- **Preply observation:** existence and post-launch editability confirmed; no min/max or currency-formatting detail sourced.
- **TutorFlow equivalent:** currency-aware numeric input feeding `Tutors`' pricing, ultimately read by `ITutorPricingQuery`.

### ReviewStep
- **Purpose:** final summary before submission.
- **Preply observation:** **not reliably observable** as a distinct UI, though a final submit action is confirmed to exist.
- **Recommendation:** TutorFlow should include an explicit review/summary step — standard wizard best practice, not Preply-specific.

### CompletionState
- **Purpose:** post-submission "under review" status communication.
- **Preply observation:** confirmed that review takes ~5 business days and ends in an email notification; **in-app status-page UI is not reliably observable.**
- **TutorFlow equivalent:** `CompletionState`, showing `Under Review` / `Approved` / `Rejected` per the wizard architecture doc's state model.

---

## Wizard state model (observable vs. assumed)

```text
Not Started         — confirmed to exist (account created, application not started)
↓
In Progress         — confirmed (multi-section application being filled)
↓
Partially Completed — existence NOT reliably observable (no draft-save behavior was sourced)
↓
Completed           — implied (all required sections filled, ready to submit)
↓
Submitted           — confirmed (submission triggers review)
↓
Under Review        — confirmed ("reviews your application within 5 business days")
↓
Approved / Rejected — confirmed (email notification with outcome; rejection criteria partially listed in help-center: age, subject demand, location, profile quality)
```

**"Partially Completed" / draft persistence is the single biggest unconfirmed piece of this state model.** No sourced material described save-and-resume behavior one way or the other. TutorFlow's own architecture doc recommends building draft persistence regardless, as a best practice independent of whether Preply has it — this is a recommendation, not an observed-and-copied feature.

## Summary

**14 wizard-specific components catalogued.** Of these: 3 have confirmed existence with some behavioral detail (`AvailabilityGrid`, file/video uploaders, `RateInput`); 6 have confirmed existence but unconfirmed interaction detail; 5 are recommended-but-not-Preply-confirmed best-practice additions (`WizardProgress`, `WizardFooter` behavior, `ReviewStep`, draft persistence, focus management). This split is intentional — per the audit's own instruction not to simply reproduce step counts, but to build Preply-level wizard UX, some components here are "what good wizard UX requires" rather than "what Preply specifically does," and are labeled as such throughout.
