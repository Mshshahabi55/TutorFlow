import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { NavigationItem } from "@/layouts/NavigationItem";
import HomeRoundedIcon from "@mui/icons-material/HomeRounded";

function renderItem(props: Partial<React.ComponentProps<typeof NavigationItem>> = {}, initialEntry = "/") {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <NavigationItem to="/foo" label="Foo" icon={<HomeRoundedIcon />} {...props} />
    </MemoryRouter>,
  );
}

describe("NavigationItem", () => {
  it("renders a link with its label", () => {
    renderItem();

    expect(screen.getByRole("link", { name: "Foo" })).toHaveAttribute("href", "/foo");
  });

  it("marks itself selected and aria-current when the route matches", () => {
    renderItem({}, "/foo");

    expect(screen.getByRole("link", { name: "Foo" })).toHaveAttribute("aria-current", "page");
  });

  it("also matches a sub-route unless exact is set", () => {
    renderItem({}, "/foo/123");

    expect(screen.getByRole("link", { name: "Foo" })).toHaveAttribute("aria-current", "page");
  });

  it("with exact, does not match a sub-route", () => {
    renderItem({ exact: true }, "/foo/123");

    expect(screen.getByRole("link", { name: "Foo" })).not.toHaveAttribute("aria-current");
  });

  it("calls onNavigate when clicked", async () => {
    const onNavigate = vi.fn();
    const { default: userEvent } = await import("@testing-library/user-event");
    renderItem({ onNavigate });

    await userEvent.click(screen.getByRole("link", { name: "Foo" }));

    expect(onNavigate).toHaveBeenCalledTimes(1);
  });

  it("hides the label and wraps in a tooltip when collapsed", () => {
    renderItem({ collapsed: true });

    expect(screen.queryByText("Foo")).not.toBeInTheDocument();
    expect(screen.getByRole("link")).toBeInTheDocument();
  });
});
