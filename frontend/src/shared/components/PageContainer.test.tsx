import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { PageContainer } from "@/shared/components/PageContainer";

describe("PageContainer", () => {
  it("renders its children", () => {
    render(
      <PageContainer>
        <div>Page content</div>
      </PageContainer>,
    );

    expect(screen.getByText("Page content")).toBeInTheDocument();
  });
});
