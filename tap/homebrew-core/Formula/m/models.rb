class Models < Formula
  desc "Fast TUI and CLI for browsing AI models, benchmarks, and coding agents"
  homepage "https://reyamira.github.io/models/"
  url "https://github.com/reyamira/models/releases/download/v0.14.1/models-x86_64-pc-windows-msvc.zip"
  sha256 "c48129a2c8c59bc5e0468b4db830b556b8f8c15ae38b0db03a63e66f028b5862"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/reyamira/models/archive/refs/tags/v0.14.1.tar.gz"
  end

  def install
    bin.install "models.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/models --version")
    assert_match "claude-code", shell_output("#{bin}/models agents list-sources")
  end
end
