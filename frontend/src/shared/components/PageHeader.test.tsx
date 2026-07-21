import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { Typography } from "@mui/material";
import { PageHeader } from "@/shared/components/PageHeader";

describe("PageHeader", () => {
  it("renders the title", () => {
    render(<PageHeader title="Tutor directory" />);

    expect(screen.getByRole("heading", { name: "Tutor directory" })).toBeInTheDocument();
  });

  it("renders a subtitle node when provided", () => {
    render(
      <PageHeader
        title="Tutor detail"
        subtitle={<Typography color="text.secondary">11111111-1111-1111-1111-111111111111</Typography>}
      />,
    );

    expect(screen.getByText("11111111-1111-1111-1111-111111111111")).toBeInTheDocument();
  });

  it("renders an action when provided", () => {
    render(<PageHeader title="Tutor directory" action={<button>Register</button>} />);

    expect(screen.getByRole("button", { name: "Register" })).toBeInTheDocument();
  });
});
