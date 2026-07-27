import { beforeEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RelationshipsPage } from "@/features/identity/pages/RelationshipsPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { RelationshipStatus } from "@/services/api/dtos";

const PARENT_GUARDIAN_ID = "44444444-4444-4444-4444-444444444444";
const STUDENT_ID = "22222222-2222-2222-2222-222222222222";
const RELATIONSHIP_ID = "55555555-5555-5555-5555-555555555555";

describe("RelationshipsPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("invites a Relationship and shows the returned id", async () => {
    const inviteRelationship = vi.spyOn(identityService, "inviteRelationship").mockResolvedValue({
      relationshipId: RELATIONSHIP_ID,
      parentGuardianId: PARENT_GUARDIAN_ID,
      studentId: STUDENT_ID,
      status: RelationshipStatus.Invited,
    });

    renderWithProviders(<RelationshipsPage />);

    await userEvent.type(screen.getByLabelText("Parent/Guardian id"), PARENT_GUARDIAN_ID);
    await userEvent.type(screen.getByLabelText("Student id"), STUDENT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Send invitation" }));

    expect(inviteRelationship).toHaveBeenCalledWith(PARENT_GUARDIAN_ID, STUDENT_ID);
    expect(await screen.findByText(RELATIONSHIP_ID)).toBeInTheDocument();
  });

  it("shows a validation error for a malformed id instead of submitting", async () => {
    const inviteRelationship = vi.spyOn(identityService, "inviteRelationship");

    renderWithProviders(<RelationshipsPage />);

    await userEvent.type(screen.getByLabelText("Parent/Guardian id"), "not-a-guid");
    await userEvent.type(screen.getByLabelText("Student id"), STUDENT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Send invitation" }));

    expect(await screen.findByText(/enter a valid id/i)).toBeInTheDocument();
    expect(inviteRelationship).not.toHaveBeenCalled();
  });

  it("looks up Relationships for an account and confirms an invited one", async () => {
    vi.spyOn(identityService, "fetchRelationshipsForAccount").mockResolvedValue([
      {
        relationshipId: RELATIONSHIP_ID,
        parentGuardianId: PARENT_GUARDIAN_ID,
        studentId: STUDENT_ID,
        status: RelationshipStatus.Invited,
      },
    ]);
    const confirmRelationship = vi
      .spyOn(identityService, "confirmRelationship")
      .mockResolvedValue(undefined);

    renderWithProviders(<RelationshipsPage />);

    await userEvent.type(screen.getByLabelText("Account id"), PARENT_GUARDIAN_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText("Invited")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Confirm" }));

    expect(confirmRelationship).toHaveBeenCalledWith(RELATIONSHIP_ID);
    expect(await screen.findByText("Relationship confirmed.")).toBeInTheDocument();
  });

  it("shows an empty state when the account has no Relationships", async () => {
    vi.spyOn(identityService, "fetchRelationshipsForAccount").mockResolvedValue([]);

    renderWithProviders(<RelationshipsPage />);

    await userEvent.type(screen.getByLabelText("Account id"), STUDENT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText("No Relationships for this account")).toBeInTheDocument();
  });

  describe("as a Parent/Guardian", () => {
    beforeEach(() => {
      window.localStorage.setItem("tutorflow.devActorRole", "ParentGuardian");
    });

    it("shows a friendly My Children heading and identity prompt instead of the generic lookup", () => {
      renderWithProviders(<RelationshipsPage />);

      expect(screen.getByRole("heading", { name: "My Children" })).toBeInTheDocument();
      expect(screen.getByRole("heading", { name: "Let's find your children" })).toBeInTheDocument();
      expect(screen.queryByLabelText("Account id")).not.toBeInTheDocument();
    });

    it("shows each child as a card once the Parent's own id is known", async () => {
      window.localStorage.setItem("tutorflow.rememberedId.parentGuardian", PARENT_GUARDIAN_ID);
      vi.spyOn(identityService, "fetchRelationshipsForAccount").mockResolvedValue([
        {
          relationshipId: RELATIONSHIP_ID,
          parentGuardianId: PARENT_GUARDIAN_ID,
          studentId: STUDENT_ID,
          status: RelationshipStatus.Confirmed,
        },
      ]);
      vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([]);

      renderWithProviders(<RelationshipsPage />);

      expect(await screen.findByText(STUDENT_ID)).toBeInTheDocument();
      expect(screen.getByText("Confirmed")).toBeInTheDocument();
    });

    it("sends an invitation for the Student id alone — the Parent's own id is prefilled, not re-typed", async () => {
      window.localStorage.setItem("tutorflow.rememberedId.parentGuardian", PARENT_GUARDIAN_ID);
      vi.spyOn(identityService, "fetchRelationshipsForAccount").mockResolvedValue([]);
      const inviteRelationship = vi.spyOn(identityService, "inviteRelationship").mockResolvedValue({
        relationshipId: RELATIONSHIP_ID,
        parentGuardianId: PARENT_GUARDIAN_ID,
        studentId: STUDENT_ID,
        status: RelationshipStatus.Invited,
      });

      renderWithProviders(<RelationshipsPage />);
      await screen.findByText("You haven't added a child yet");

      expect(screen.queryByLabelText("Parent/Guardian id")).not.toBeInTheDocument();
      await userEvent.type(screen.getByLabelText("Student id"), STUDENT_ID);
      await userEvent.click(screen.getByRole("button", { name: "Send invitation" }));

      expect(inviteRelationship).toHaveBeenCalledWith(PARENT_GUARDIAN_ID, STUDENT_ID);
    });
  });
});
