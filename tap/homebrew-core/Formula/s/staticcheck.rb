class Staticcheck < Formula
  desc "State of the art linter for the Go programming language"
  homepage "https://staticcheck.dev/"
  url "https://github.com/dominikh/go-tools/releases/download/2026.2.2/staticcheck_windows_amd64.tar.gz"
  sha256 "5f4f9491b41301e6a9d4de6f7542bd2efc1bfaf39e7cc52802d4db9e4a54e677"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dominikh/go-tools/archive/refs/tags/2026.2.2.tar.gz"
  end

  def install
    bin.install "staticcheck.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/staticcheck --version 2>&1")
  end
end
