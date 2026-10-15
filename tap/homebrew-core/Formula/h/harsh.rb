class Harsh < Formula
  desc "Habit tracking for geeks"
  homepage "https://github.com/wakatara/harsh"
  url "https://github.com/wakatara/harsh/releases/download/v0.14.5/harsh_Windows_x86_64.tar.gz"
  sha256 "97b24fb0f2c979752cb37afff7570e90a3836badd1e3bfe5072c298b13796f73"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wakatara/harsh/archive/refs/tags/v0.14.5.tar.gz"
  end

  def install
    bin.install "harsh.exe"
  end

  test do
    assert_match "Welcome to harsh!", shell_output("#{bin}/harsh todo")
    assert_match version.to_s, shell_output("#{bin}/harsh --version")
  end
end
