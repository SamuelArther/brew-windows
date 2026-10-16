class Mufetch < Formula
  desc "Neofetch-style music cli"
  homepage "https://github.com/ashish0kumar/mufetch"
  url "https://github.com/ashish0kumar/mufetch/releases/download/v0.1.1/mufetch_windows_x86_64.zip"
  sha256 "7f54c321f7119d397b212f7ff13560d10cf6f92a7a609f8b60849be63429927a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ashish0kumar/mufetch/archive/refs/tags/v0.1.1.tar.gz"
  end

  def install
    bin.install "mufetch.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mufetch --version 2>&1")
  end
end
