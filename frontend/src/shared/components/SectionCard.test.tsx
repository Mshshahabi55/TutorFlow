import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { SectionCard } from "@/shared/components/SectionCard";

describe("SectionCard", () => {
  it("renders the title as a heading and the children", () => {
    render(
      <SectionCard title="Upcoming Sessions">
        <p>content</p>
      </SectionCard>,
    );

    expect(screen.getByRole("heading", { name: "Upcoming Sessions" })).toBeInTheDocument();
    expect(screen.getByText("content")).toBeInTheDocument();
  });

  it("labels the section region with its own heading for assistive tech", () => {
    render(
      <SectionCard title="Recent Activity">
        <p>content</p>
      </SectionCard>,
    );

    expect(screen.getByRole("region", { name: "Recent Activity" })).toBeInTheDocument();
  });

  it("renders an optional action alongside the title", () => {
    render(
      <SectionCard title="Recommended Tutors" action={<button>Browse all tutors</button>}>
        <p>content</p>
      </SectionCard>,
    );

    expect(screen.getByRole("button", { name: "Browse all tutors" })).toBeInTheDocument();
  });

  it("defaults the heading to <h5>, correctly nested under this app's usual <h4> PageHeader", () => {
    render(
      <SectionCard title="Teaching Information">
        <p>content</p>
      </SectionCard>,
    );

    const heading = screen.getByRole("heading", { name: "Teaching Information" });
    expect(heading.tagName).toBe("H5");
  });

  it("renders as <h2> when headingComponent is overridden, for pages whose own title is a real <h1>", () => {
    render(
      <SectionCard title="Reviews" headingComponent="h2">
        <p>content</p>
      </SectionCard>,
    );

    const heading = screen.getByRole("heading", { name: "Reviews" });
    expect(heading.tagName).toBe("H2");
  });
});
