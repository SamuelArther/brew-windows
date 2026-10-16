class AgentManager < Formula
  desc "Run Claude Code, Codex, OpenCode and other AI coding agents in tmux"
  homepage "https://agent-manager.dev/"
  url "https://github.com/YoanWai/agent-manager/releases/download/v0.42.0/agent-manager_0.42.0_windows_amd64.zip"
  sha256 "65e6d17929f85c7c5aaba187bf66484649e3f0d3491f2df2baabe651cf40bf48"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/YoanWai/agent-manager/archive/refs/tags/v0.42.0.tar.gz"
  end

  def install
    bin.install "agent-manager.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/agent-manager --version 2>&1")
  end
end
