class Tfsort < Formula
  desc "CLI to sort Terraform variables and outputs"
  homepage "https://github.com/AlexNabokikh/tfsort"
  url "https://github.com/AlexNabokikh/tfsort/releases/download/v0.7.3/tfsort_0.7.3_windows_amd64.zip"
  sha256 "c2639cf1fb9f19be5b11c412faf9c1d3d0623e301b4fbef753fb2cacd2c61f75"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/AlexNabokikh/tfsort/archive/refs/tags/v0.7.3.tar.gz"
  end

  def install
    bin.install "tfsort.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tfsort --version 2>&1")
  end
end
