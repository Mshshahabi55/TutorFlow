# RC3.0 — Identity Resolution
# Zero GUID UX
# Foundation Phase
# No Backend Changes
# No Questions

Read prompts/00_GLOBAL_RULES.md.

This phase is NOT about visuals.

This phase is about removing the biggest UX problem in the application.

Today almost every workspace begins like this:

Dashboard
↓

Enter Student ID

or

Enter Tutor ID

or

Enter Parent ID

↓

Fetch

↓

Show data

This is NOT acceptable for a marketplace.

The user already authenticated.

The application should identify who they are.

If backend identity resolution does not exist, frontend must create the best possible experience without inventing business logic.

==================================================

PRIMARY GOAL

After the first successful identification,
the application should remember the user's domain identity.

Student

Tutor

Parent

Admin

must never repeatedly enter GUIDs.

==================================================

DO NOT

Do not add endpoints.

Do not change backend.

Do not change authorization.

Do not invent identities.

Do not fake data.

==================================================

BUILD

A shared Identity Resolution layer.

This becomes the single place responsible for mapping

Authenticated Account

↓

Domain Identity

==================================================

REPLACE

Every existing

IdLookupForm

Enter Student ID

Enter Tutor ID

Enter Parent ID

Enter Admin ID

with one unified experience.

==================================================

FIRST RUN

If no remembered identity exists:

Show a friendly setup screen.

Examples

Who are you?

Select your profile

Student

Tutor

Parent

Administrator

Then

Enter your ID once.

After success

Remember this device.

==================================================

AFTER SUCCESS

Persist the resolved identity locally.

Every workspace automatically restores it.

No repeated prompts.

==================================================

IF THE REMEMBERED ID FAILS

Never show

Student not found

Tutor not found

Parent not found

Admin not found

Instead

"We couldn't verify your profile."

Choose another profile

Change ID

Retry

==================================================

SHARED COMPONENTS

Create reusable components.

IdentityResolver

IdentitySetup

IdentitySelector

IdentityError

IdentityLoading

RememberIdentityToggle

Do not duplicate implementations.

==================================================

SHARED HOOK

Create one shared hook.

Example

useResolvedIdentity()

Every workspace consumes this hook.

No page should implement its own lookup flow anymore.

==================================================

SUPPORTED WORKSPACES

Student Dashboard

Tutor Dashboard

Parent Dashboard

Admin Dashboard

Student Sessions

Tutor Sessions

Availability

Relationships

Every page currently asking for IDs.

==================================================

MIGRATION

Completely remove duplicated lookup logic.

Every page uses the shared solution.

==================================================

MICROCOPY

Friendly.

Examples

Welcome back.

We're restoring your workspace.

Who are you using TutorFlow as?

Remember this device.

Switch profile.

Change saved profile.

==================================================

ERROR EXPERIENCE

Professional.

Never expose raw lookup failures.

Guide users toward recovery.

==================================================

ACCESSIBILITY

Keyboard first.

Screen-reader friendly.

Visible focus.

==================================================

RESPONSIVE

Excellent mobile UX.

==================================================

PERFORMANCE

No duplicate requests.

Reuse existing React Query cache.

One identity lookup.

Many consumers.

==================================================

DELIVERABLE

Provide only:

Summary

Architecture decisions

Files changed

Components added

Hooks added

Pages migrated

Accessibility

Performance

Remaining backend limitations

Verification

tsc

eslint

vitest

build

STOP.