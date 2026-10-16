class Decompose < Formula
  desc "Reverse-engineering tool for docker environments"
  homepage "https://github.com/s0rg/decompose"
  url "https://github.com/s0rg/decompose/releases/download/v1.11.8/decompose_v1.11.8_windows_x86_64.zip"
  sha256 "7aa87b8dfaf767a4b5c32206b1e0cb579c3727c416e6cb1dec88cfb2745d718c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/s0rg/decompose/archive/refs/tags/v1.11.8.tar.gz"
  end

  def install
    bin.install "decompose.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/decompose -version")

    assert_match "Building graph", shell_output("#{bin}/decompose -local 2>&1", 1)
  end
end
