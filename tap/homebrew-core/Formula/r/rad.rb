class Rad < Formula
  desc "Modern CLI scripts made easy"
  homepage "https://amterp.dev/rad/"
  url "https://github.com/amterp/rad/releases/download/v0.12.1/rad_windows_amd64.zip"
  sha256 "68a1699efd6faa58ad385d448f449f906e4a78561eaa4183f36a7d5791ea261a"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/amterp/rad/archive/refs/tags/v0.12.1.tar.gz"
  end

  def install
    bin.install "rad.exe"
    bin.install "radls.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rad --version 2>&1")
  end
end
