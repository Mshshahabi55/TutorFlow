import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { SearchResultsHeader } from "@/features/discovery/components/SearchResultsHeader";

describe("SearchResultsHeader", () => {
  it("shows a searching message while pending", () => {
    render(<SearchResultsHeader isSearching resultCount={undefined} />);

    expect(screen.getByText("Searching…")).toBeInTheDocument();
  });

  it("uses singular wording for exactly one result", () => {
    render(<SearchResultsHeader isSearching={false} resultCount={1} />);

    expect(screen.getByText("1 tutor found")).toBeInTheDocument();
  });

  it("uses plural wording for any other count, including zero", () => {
    render(<SearchResultsHeader isSearching={false} resultCount={0} />);

    expect(screen.getByText("0 tutors found")).toBeInTheDocument();
  });
});
