class ClaudeSquad < Formula
  desc "Manage multiple AI agents like Claude Code, Aider and Codex in your terminal"
  homepage "https://smtg-ai.github.io/claude-squad/"
  url "https://github.com/smtg-ai/claude-squad/releases/download/v1.0.20/claude-squad_1.0.20_windows_amd64.zip"
  sha256 "83c4322c7a52ad249f154bdc960e61dec721582403fa621d09eb638fbd9aa669"
  license "AGPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/smtg-ai/claude-squad/archive/refs/tags/v1.0.20.tar.gz"
  end

  def install
    bin.install "claude-squad.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/claude-squad version 2>&1")
  end
end
