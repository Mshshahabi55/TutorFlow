import { useState, type ReactNode } from "react";
import {
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Divider,
  FormControlLabel,
  IconButton,
  Radio,
  RadioGroup,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import { ThemeProvider } from "@mui/material/styles";
import DownloadRoundedIcon from "@mui/icons-material/DownloadRounded";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  namedShadows,
  radiusPx,
  spacingUnitPx,
  typographyScale,
  theme,
  darkTheme,
} from "@/app/theme";
import { PageHeader } from "@/shared/components/PageHeader";
import { CopyableId } from "@/shared/components/CopyableId";
import { UnitText } from "@/shared/components/UnitText";
import { ThemeToggle } from "@/shared/components/ThemeToggle";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { FormSelect } from "@/shared/components/forms/FormSelect";
import { FormCheckbox } from "@/shared/components/forms/FormCheckbox";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { DataTable, type DataTableColumn } from "@/shared/components/table/DataTable";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { StatusPill, type StatusTone } from "@/shared/components/feedback/StatusPill";
import { LearningPlanCard } from "@/features/learningPlans/components/LearningPlanCard";
import { LearningPlanSkeleton } from "@/features/learningPlans/components/LearningPlanSkeleton";
import { PlanBadge } from "@/features/learningPlans/components/PlanBadge";
import { PlanFeatureList } from "@/features/learningPlans/components/PlanFeatureList";
import type { LearningPlanPreview } from "@/features/learningPlans/types";
import { ColorModeProvider } from "@/shared/context/ColorModeProvider";
import { useColorMode } from "@/shared/hooks/useColorMode";
import { useConfirmDialog } from "@/shared/hooks/useConfirmDialog";
import { useNotification } from "@/shared/hooks/useNotification";
import { formatToman } from "@/shared/money/rial";
import { toTehranDisplay } from "@/shared/time/tehranTime";

const NEUTRAL_STEPS = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900] as const;
const SEMANTIC_TONES: { label: string; paletteKey: "success" | "warning" | "error" | "info" }[] = [
  { label: "Success", paletteKey: "success" },
  { label: "Warning", paletteKey: "warning" },
  { label: "Error", paletteKey: "error" },
  { label: "Info", paletteKey: "info" },
];
const TYPE_SCALE_ORDER: (keyof typeof typographyScale)[] = [
  "h1",
  "h2",
  "h3",
  "h4",
  "h5",
  "h6",
  "subtitle1",
  "subtitle2",
  "body1",
  "body2",
  "caption",
  "overline",
];
const STATUS_TONES: StatusTone[] = ["neutral", "info", "success", "warning", "critical"];

/**
 * Phase D3: formalizes the named hierarchy the "Foundation UI & Design
 * System" brief asks for (Display/Page Title/Section Title/Card Title/
 * Subtitle/Body/Caption/Small Label) as a mapping onto the existing MUI
 * variant names — documentation only, no call site changes. h1–h3 stay
 * "reserved, unused today" (Phase D1); nothing here invents a parallel
 * scale, matching D1's own reasoning for keeping one.
 */
const TYPE_ROLE_MAP: { role: string; variant: keyof typeof typographyScale; note?: string }[] = [
  { role: "Display", variant: "h1", note: "reserved, unused today" },
  { role: "Page Title", variant: "h4", note: "PageHeader title" },
  { role: "Section Title", variant: "h5" },
  { role: "Card Title", variant: "subtitle1" },
  { role: "Subtitle", variant: "subtitle2" },
  { role: "Body", variant: "body1" },
  { role: "Caption", variant: "caption" },
  { role: "Small Label", variant: "overline" },
];

/**
 * Phase D3: the dark palette proven standalone, isolated in its own nested
 * ThemeProvider + ColorModeProvider — deliberately not the app-wide
 * ThemeProvider/ColorModeProvider (not composed into AppProviders.tsx yet;
 * see docs/phases/PHASE-D3-REPORT.md). Toggling here only ever affects this
 * boxed preview, never the rest of this style guide page.
 */
function DarkModePreviewPanel() {
  return (
    <ColorModeProvider>
      <DarkModePreviewContent />
    </ColorModeProvider>
  );
}

function DarkModePreviewContent() {
  const { resolvedMode } = useColorMode();
  const previewTheme = resolvedMode === "dark" ? darkTheme : theme;

  return (
    <ThemeProvider theme={previewTheme}>
      <Box
        sx={{
          bgcolor: "background.default",
          color: "text.primary",
          p: 3,
          borderRadius: 2,
          border: "1px solid",
          borderColor: "divider",
        }}
      >
        <Stack spacing={2.5}>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="subtitle1">Preview — {resolvedMode} mode</Typography>
            <ThemeToggle />
          </Stack>

          <Stack direction="row" flexWrap="wrap" gap={2}>
            <Swatch label="background.default" hex={previewTheme.palette.background.default} textColor={previewTheme.palette.text.primary} />
            <Swatch label="background.paper" hex={previewTheme.palette.background.paper} textColor={previewTheme.palette.text.primary} />
            <Swatch label="primary.main" hex={previewTheme.palette.primary.main} textColor={previewTheme.palette.primary.contrastText} />
            {SEMANTIC_TONES.map(({ label, paletteKey }) => (
              <Swatch
                key={paletteKey}
                label={label}
                hex={previewTheme.palette[paletteKey].main}
                textColor={previewTheme.palette[paletteKey].contrastText}
              />
            ))}
          </Stack>

          <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
            <Button variant="contained">Primary</Button>
            <Button variant="outlined">Secondary</Button>
            <Button variant="text">Text</Button>
          </Stack>

          <Stack direction="row" spacing={1.5} flexWrap="wrap">
            {STATUS_TONES.map((tone) => (
              <StatusPill key={tone} label={tone} tone={tone} />
            ))}
          </Stack>

          <Card variant="outlined" sx={{ maxWidth: 420 }}>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom>
                Outlined card
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Same border-based depth language in both modes.
              </Typography>
            </CardContent>
          </Card>
        </Stack>
      </Box>
    </ThemeProvider>
  );
}

function Section({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <Stack spacing={2} component="section">
      <Box>
        <Typography variant="h5">{title}</Typography>
        {description ? (
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        ) : null}
      </Box>
      {children}
    </Stack>
  );
}

function Swatch({ label, hex, textColor = "#FFFFFF" }: { label: string; hex: string; textColor?: string }) {
  return (
    <Box
      sx={{
        width: 140,
        borderRadius: 1,
        overflow: "hidden",
        border: "1px solid",
        borderColor: "divider",
      }}
    >
      <Box sx={{ height: 56, backgroundColor: hex, display: "flex", alignItems: "flex-end", p: 1 }}>
        <Typography variant="caption" sx={{ color: textColor, fontWeight: 600 }}>
          {hex}
        </Typography>
      </Box>
      <Box sx={{ px: 1, py: 0.5 }}>
        <Typography variant="caption" color="text.secondary">
          {label}
        </Typography>
      </Box>
    </Box>
  );
}

interface DemoRow {
  id: string;
  subject: string;
  status: StatusTone;
}

const DEMO_ROWS: DemoRow[] = [
  { id: "1", subject: "Mathematics", status: "success" },
  { id: "2", subject: "Physics", status: "warning" },
  { id: "3", subject: "English", status: "critical" },
];

const demoColumns: DataTableColumn<DemoRow>[] = [
  { key: "subject", header: "Subject", render: (row) => row.subject },
  {
    key: "status",
    header: "Status",
    render: (row) => <StatusPill label={row.status} tone={row.status} />,
  },
];

/**
 * RC5.0: sample data only, for this dev-only page — no Learning Plan
 * backend exists yet (`docs/adr/ADR-021...`, Proposed, not Accepted). No
 * real page in the app constructs a `LearningPlanPreview` value; this is
 * the one place these components are shown with illustrative data, exactly
 * the same convention `DEMO_ROWS` already uses for the `DataTable` preview
 * below.
 */
const DEMO_LEARNING_PLAN: LearningPlanPreview = {
  learningPlanId: "demo-1",
  title: "IELTS Intensive",
  durationDays: 45,
  sessionsPerWeek: 4,
  totalSessions: 18,
  price: 12_000_000,
  description: "Focused preparation for the IELTS exam, four lessons a week.",
  status: "Active",
};

const demoFormSchema = z.object({
  name: z.string().min(1, "Required"),
  subject: z.string().min(1, "Required"),
  agree: z.boolean(),
});
type DemoFormValues = z.infer<typeof demoFormSchema>;

/**
 * Dev-only living reference for every Phase D1 token and every shared
 * component variant, on one page, so the owner can review the design
 * system before any real page is restyled (D2–D6). Not linked from
 * NavSidebar, and excluded from the production bundle — see
 * `router.tsx` (`import.meta.env.DEV` gate) and
 * docs/design/DESIGN-SYSTEM.md §"Excluding the style guide" for how.
 */
export function StyleGuidePage() {
  const { confirm } = useConfirmDialog();
  const { notify } = useNotification();
  const [simulateError, setSimulateError] = useState(false);
  const demoForm = useForm<DemoFormValues>({
    resolver: zodResolver(demoFormSchema),
    defaultValues: { name: "", subject: "", agree: false },
  });

  async function handleConfirmDemo(destructive: boolean) {
    const confirmed = await confirm({
      title: destructive ? "Delete this record?" : "Save these changes?",
      description: destructive ? "This cannot be undone." : undefined,
      confirmLabel: destructive ? "Delete" : "Save",
      destructive,
    });
    notify({
      message: confirmed ? "Confirmed." : "Cancelled.",
      severity: confirmed ? "success" : "info",
    });
  }

  return (
    <Stack spacing={5} maxWidth={960} pb={8}>
      <PageHeader
        title="Style Guide"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Every Phase D1 token and shared-component variant, in one place. Dev-only — not linked
            from navigation, not in the production build.
          </Typography>
        }
      />

      <Section title="Colour" description="Neutral grey scale, one accent, four semantic tones. Every combination below clears WCAG AA (4.5:1) — see docs/design/DESIGN-SYSTEM.md for the measured ratios.">
        <Typography variant="subtitle2">Neutral</Typography>
        <Stack direction="row" flexWrap="wrap" gap={2}>
          {NEUTRAL_STEPS.map((step) => (
            <Swatch
              key={step}
              label={`grey.${step}`}
              hex={theme.palette.grey[step]}
              textColor={step <= 300 ? "#14181F" : "#FFFFFF"}
            />
          ))}
        </Stack>
        <Typography variant="subtitle2">Accent (primary)</Typography>
        <Stack direction="row" flexWrap="wrap" gap={2}>
          <Swatch label="primary.light" hex={theme.palette.primary.light} textColor="#14181F" />
          <Swatch label="primary.main" hex={theme.palette.primary.main} />
          <Swatch label="primary.dark" hex={theme.palette.primary.dark} />
          <Swatch label="secondary.main (neutral, not a 2nd accent)" hex={theme.palette.secondary.main} />
        </Stack>
        <Typography variant="subtitle2">Semantic</Typography>
        <Stack direction="row" flexWrap="wrap" gap={2}>
          {SEMANTIC_TONES.map(({ label, paletteKey }) => (
            <Swatch key={paletteKey} label={label} hex={theme.palette[paletteKey].main} />
          ))}
        </Stack>
      </Section>

      <Divider />

      <Section title="Typography" description={`A modular scale on MUI's own semantic variant names — every call site already writes e.g. variant="h4", never a raw pixel size.`}>
        <Stack spacing={1.5}>
          {TYPE_SCALE_ORDER.map((variant) => (
            <Stack key={variant} direction="row" alignItems="baseline" spacing={2}>
              <Typography variant="caption" color="text.secondary" sx={{ width: 88, flexShrink: 0 }}>
                {variant} · {typographyScale[variant].fontSize}
              </Typography>
              <Typography variant={variant}>The quick brown fox jumps over the lazy dog</Typography>
            </Stack>
          ))}
        </Stack>
      </Section>

      <Divider />

      <Section
        title="Typography roles"
        description="Phase D3: the named hierarchy the design-system brief asks for, mapped onto the scale above — naming only, no new variants."
      >
        <Stack spacing={1}>
          {TYPE_ROLE_MAP.map(({ role, variant, note }) => (
            <Stack key={role} direction="row" alignItems="baseline" spacing={2}>
              <Typography variant="body2" sx={{ width: 120, flexShrink: 0 }}>
                {role}
              </Typography>
              <Typography variant="caption" color="text.secondary" sx={{ width: 88, flexShrink: 0 }}>
                {variant}
              </Typography>
              {note ? (
                <Typography variant="caption" color="text.secondary">
                  {note}
                </Typography>
              ) : null}
            </Stack>
          ))}
        </Stack>
      </Section>

      <Divider />

      <Section title="Spacing, radius, shadow" description={`Base spacing unit: ${spacingUnitPx}px. Depth comes from a 1px border + spacing, not shadow (used only where a floating surface has no bordered container of its own — Menu, Dialog, Snackbar).`}>
        <Stack direction="row" spacing={2} alignItems="flex-end">
          {[1, 2, 3, 4, 6, 8].map((multiple) => (
            <Box key={multiple} textAlign="center">
              <Box
                sx={{
                  width: multiple * spacingUnitPx,
                  height: 24,
                  backgroundColor: "primary.main",
                  borderRadius: 0.5,
                }}
              />
              <Typography variant="caption" color="text.secondary">
                {multiple} ({multiple * spacingUnitPx}px)
              </Typography>
            </Box>
          ))}
        </Stack>
        <Stack direction="row" spacing={3}>
          {Object.entries(radiusPx).map(([name, px]) => (
            <Box key={name} textAlign="center">
              <Box
                sx={{
                  width: 64,
                  height: 64,
                  borderRadius: `${px}px`,
                  border: "1px solid",
                  borderColor: "divider",
                  backgroundColor: "background.paper",
                }}
              />
              <Typography variant="caption" color="text.secondary">
                {name} ({px}px)
              </Typography>
            </Box>
          ))}
        </Stack>
        <Stack direction="row" spacing={3}>
          {Object.entries(namedShadows).map(([name, shadow]) => (
            <Box key={name} textAlign="center">
              <Box
                sx={{
                  width: 96,
                  height: 64,
                  borderRadius: 1,
                  backgroundColor: "background.paper",
                  boxShadow: shadow,
                }}
              />
              <Typography variant="caption" color="text.secondary">
                {name}
              </Typography>
            </Box>
          ))}
        </Stack>
      </Section>

      <Divider />

      <Section title="Unit-bearing values" description={`One typographic treatment for a Tehran time or a Toman amount (UnitText) — replacing six ad hoc renderings the Phase D1 audit found across pages.`}>
        <Stack direction="row" spacing={4}>
          <UnitText value={toTehranDisplay("2026-08-01T14:00:00Z")} unit="Tehran" />
          <UnitText value={formatToman(500_000)} unit="Toman" />
        </Stack>
      </Section>

      <Divider />

      <Section title="Buttons">
        <Stack spacing={2}>
          <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
            <Button variant="contained">Primary</Button>
            <Button variant="outlined">Secondary</Button>
            <Button variant="text">Text</Button>
            <Button variant="outlined" color="error">
              Destructive (propose)
            </Button>
            <Button variant="contained" color="error">
              Destructive (confirm)
            </Button>
          </Stack>
          <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
            <Button variant="contained" size="small">
              Small
            </Button>
            <Button variant="contained" size="medium">
              Medium
            </Button>
            <Button variant="contained" size="large">
              Large
            </Button>
          </Stack>
          <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
            <Button variant="contained" disabled>
              Disabled
            </Button>
            <Button variant="contained" disabled>
              Saving…
            </Button>
            <Button variant="outlined" startIcon={<DownloadRoundedIcon />}>
              Icon start
            </Button>
            <Tooltip title="Copy id">
              <IconButton aria-label="Copy id" size="small">
                <DownloadRoundedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>
      </Section>

      <Divider />

      <Section title="Status pills" description="One deliberate tone mapping, applied everywhere — a Cancelled Session and a Suspended Tutor use the same 'critical' tone, never an arbitrary one-off colour.">
        <Stack direction="row" spacing={1.5} flexWrap="wrap">
          {STATUS_TONES.map((tone) => (
            <StatusPill key={tone} label={tone} tone={tone} />
          ))}
        </Stack>
      </Section>

      <Divider />

      <Section
        title="Learning Plan components (RC5.0)"
        description="No Learning Plan backend exists yet (docs/adr/ADR-021..., Proposed, not Accepted) — sample data only, shown here so the reusable components can be reviewed. Every real page in the app renders these against an empty list and shows an honest 'coming soon' state instead."
      >
        <Stack direction="row" spacing={1.5} flexWrap="wrap">
          <PlanBadge label="Active" />
          <PlanBadge label="Draft" />
          <PlanBadge label="Archived" />
          <PlanBadge label={`${DEMO_LEARNING_PLAN.durationDays} Days`} tone="info" />
        </Stack>
        <Box maxWidth={320}>
          <PlanFeatureList
            features={[
              `${DEMO_LEARNING_PLAN.sessionsPerWeek} lessons / week`,
              `${DEMO_LEARNING_PLAN.totalSessions} total lessons`,
            ]}
          />
        </Box>
        <Stack direction="row" flexWrap="wrap" gap={2}>
          <LearningPlanCard plan={DEMO_LEARNING_PLAN} onEnroll={() => notify({ message: "Demo only — not a real enrollment.", severity: "info" })} />
          <LearningPlanSkeleton />
        </Stack>
      </Section>

      <Divider />

      <Section title="Form controls" description="Identical label / helper / error treatment everywhere, including the datetime-local input used for every Tehran-time entry point.">
        <Form form={demoForm} onSubmit={() => notify({ message: "Submitted.", severity: "success" })}>
          <Stack spacing={2} maxWidth={420}>
            <FormTextField name="name" label="Name" helperText="Helper text" />
            <FormTextField name="name" label="Name (error state)" />
            <FormSelect
              name="subject"
              label="Subject"
              options={[
                { value: "math", label: "Mathematics" },
                { value: "physics", label: "Physics" },
              ]}
            />
            <FormTextField
              name="name"
              label="Start time (Tehran)"
              type="datetime-local"
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <FormCheckbox name="agree" label="Checkbox" />
            <RadioGroup defaultValue="a" row>
              <FormControlLabel value="a" control={<Radio />} label="Radio A" />
              <FormControlLabel value="b" control={<Radio />} label="Radio B" />
            </RadioGroup>
            <Checkbox disabled checked />
            <Button type="submit" variant="contained" sx={{ alignSelf: "flex-start" }}>
              Submit
            </Button>
          </Stack>
        </Form>
        <Box maxWidth={420}>
          <IdLookupForm label="Record id" onSubmit={(id) => notify({ message: `Looked up ${id}`, severity: "info" })} />
        </Box>
      </Section>

      <Divider />

      <Section title="CopyableId">
        <CopyableId id="11111111-2222-3333-4444-555555555555" />
      </Section>

      <Divider />

      <Section title="Card & DataTable">
        <Card variant="outlined" sx={{ maxWidth: 480 }}>
          <CardContent>
            <Typography variant="subtitle1" gutterBottom>
              Outlined card
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Depth from a 1px border, not shadow.
            </Typography>
          </CardContent>
        </Card>
        <DataTable columns={demoColumns} rows={DEMO_ROWS} getRowKey={(row) => row.id} />
      </Section>

      <Divider />

      <Section title="Feedback states">
        <Stack spacing={1}>
          <Button
            size="small"
            variant="outlined"
            onClick={() => setSimulateError((prev) => !prev)}
            sx={{ alignSelf: "flex-start" }}
          >
            {simulateError ? "Show loading state" : "Show error state"}
          </Button>
          <LoadingState label="Loading…" minHeight={100} />
          {simulateError ? (
            <ErrorState error={new Error("Something went wrong")} onRetry={() => undefined} />
          ) : null}
          <EmptyState title="Nothing to show yet" description="A description can go here." />
        </Stack>
      </Section>

      <Divider />

      <Section title="Dialog & Notification">
        <Stack direction="row" spacing={2}>
          <Button variant="outlined" onClick={() => void handleConfirmDemo(false)}>
            Confirm dialog
          </Button>
          <Button variant="outlined" color="error" onClick={() => void handleConfirmDemo(true)}>
            Destructive confirm dialog
          </Button>
          <Button variant="outlined" onClick={() => notify({ message: "A notification.", severity: "info" })}>
            Notification
          </Button>
        </Stack>
      </Section>

      <Divider />

      <Section
        title="Dark mode (Phase D3)"
        description="The dark palette, proven here first — not yet wired into the live app (docs/phases/PHASE-D3-REPORT.md explains why). Toggle below to compare both against the same components."
      >
        <DarkModePreviewPanel />
      </Section>
    </Stack>
  );
}
