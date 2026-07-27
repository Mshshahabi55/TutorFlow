import { beforeEach, describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { ProfilePage } from "@/routes/ProfilePage";

function renderProfile() {
  return render(
    <AuthProvider>
      <ActorProvider>
        <MemoryRouter initialEntries={["/profile"]}>
          <Routes>
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/identity/tutors/:tutorId" element={<div>Tutor profile route reached</div>} />
            <Route path="/identity/students/:studentId" element={<div>Student profile route reached</div>} />
          </Routes>
        </MemoryRouter>
      </ActorProvider>
    </AuthProvider>,
  );
}

describe("ProfilePage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("asks once for a Student id when nothing is remembered and no role is selected", () => {
    renderProfile();

    expect(screen.getByRole("heading", { name: "Let's find your profile" })).toBeInTheDocument();
    expect(screen.getByLabelText("Student id")).toBeInTheDocument();
  });

  it("redirects straight to the Tutor's existing profile page when a Tutor id is already remembered", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Tutor");
    window.localStorage.setItem(
      "tutorflow.rememberedId.tutor",
      "11111111-1111-1111-1111-111111111111",
    );

    renderProfile();

    expect(screen.getByText("Tutor profile route reached")).toBeInTheDocument();
  });
});
