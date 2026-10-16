class RvR < Formula
  desc "Declarative R package manager"
  homepage "https://a2-ai.github.io/rv-docs/"
  url "https://github.com/A2-ai/rv/releases/download/v0.23.2/rv-v0.23.2-x86_64-pc-windows-msvc.zip"
  sha256 "a552f20a6d3bc56603a7b6deb66f944cd95ce20148b53211ea80edf75d589cbd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/A2-ai/rv/archive/refs/tags/v0.23.2.tar.gz"
  end

  def install
    bin.install "rv.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rv --version 2>&1")
  end
end
