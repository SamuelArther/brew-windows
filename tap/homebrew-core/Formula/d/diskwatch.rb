class Diskwatch < Formula
  desc "Cross-platform disk diagnostics TUI"
  homepage "https://www.netwatchlabs.com/labs/diskwatch"
  url "https://github.com/matthart1983/diskwatch/releases/download/v0.6.0/diskwatch-windows-x86_64.tar.gz"
  sha256 "132f5373dc27c1a9e8a6cb8ea357bea17725578e59f77d59e2b49ab6ae0f4c5d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/matthart1983/diskwatch/archive/refs/tags/v0.6.0.tar.gz"
  end

  def install
    bin.install "diskwatch-windows-x86_64.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/diskwatch-windows-x86_64 --version 2>&1")
  end
end
