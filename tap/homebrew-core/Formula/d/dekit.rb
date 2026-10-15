class Dekit < Formula
  desc "Process manager for dev and prod"
  homepage "https://dekit.run"
  url "https://github.com/pvolok/dekit/releases/download/v0.10.2/dekit-x86_64-pc-windows-msvc.zip"
  sha256 "60c60da3b2e0e273fde15ce31f2ab733989f83278bb71d70f219e0f75b6577d5"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pvolok/dekit/archive/refs/tags/v0.10.2.tar.gz"
  end

  def install
    bin.install "dekit.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dekit --version 2>&1")
  end
end
