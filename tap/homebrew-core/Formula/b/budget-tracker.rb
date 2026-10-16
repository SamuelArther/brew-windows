class BudgetTracker < Formula
  desc "Feature rich TUI budget tracker app"
  homepage "https://github.com/Feromond/budget-tracker-tui"
  url "https://github.com/Feromond/budget-tracker-tui/releases/download/v1.7.0/budget-tracker-1.7.0-x86_64-pc-windows-msvc.zip"
  sha256 "3a48e292efee95d5366f5fdbaf33e4bbf4f3ab350bde67b056fc6fe1b531a68b"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Feromond/budget-tracker-tui/archive/refs/tags/v1.7.0.tar.gz"
  end

  def install
    bin.install "budget-tracker.exe"
  end

  test do
    # budget-tracker is an interactive TUI with no non-interactive commands
    assert_match version.to_s, shell_output("#{bin}/budget-tracker --version")
  end
end
