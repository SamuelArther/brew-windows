class Addlicense < Formula
  desc "Scan directories recursively to ensure source files have license headers"
  homepage "https://github.com/google/addlicense"
  url "https://github.com/google/addlicense/releases/download/v1.2.0/addlicense_v1.2.0_Windows_x86_64.zip"
  sha256 "fe4f4a54daa4a750ffa7d1b0da471d077ac24f09d04d7c1f307e7b7950969d25"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/google/addlicense/archive/refs/tags/v1.2.0.tar.gz"
  end

  def install
    bin.install "addlicense.exe"
  end

  test do
    system bin/"addlicense", "--help"
  end
end
