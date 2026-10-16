class Slacknimate < Formula
  desc "Text animation for Slack messages"
  homepage "https://github.com/mroth/slacknimate"
  url "https://github.com/mroth/slacknimate/releases/download/v1.1.0/slacknimate_1.1.0_windows_amd64.zip"
  sha256 "daac500337e5db764deaf664d6fe8b1578967459ae3fe8188c865edc7c20f327"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mroth/slacknimate/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    bin.install "slacknimate.exe"
  end

  test do
    system bin/"slacknimate", "--version"
    system bin/"slacknimate", "--help"
  end
end
