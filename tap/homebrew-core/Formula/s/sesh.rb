class Sesh < Formula
  desc "Smart session manager for the terminal"
  homepage "https://github.com/joshmedeski/sesh"
  url "https://github.com/joshmedeski/sesh/releases/download/v2.33.0/sesh_Windows_x86_64.zip"
  sha256 "2660ade19af965d7d996cc61720924ed1e700f3056a208a80eb9a99d8260217e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/joshmedeski/sesh/archive/refs/tags/v2.33.0.tar.gz"
  end

  def install
    bin.install "sesh.exe"
  end

  test do
    output = shell_output("#{bin}/sesh root 2>&1", 1)
    assert_match "No root found for session", output

    assert_match version.to_s, shell_output("#{bin}/sesh --version")
  end
end
