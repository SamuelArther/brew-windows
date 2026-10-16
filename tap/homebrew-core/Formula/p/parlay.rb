class Parlay < Formula
  desc "Enrich SBOMs with data from third party services"
  homepage "https://github.com/snyk/parlay"
  url "https://github.com/snyk/parlay/releases/download/v0.11.0/parlay_Windows_x86_64.zip"
  sha256 "51e5fee301181cffb8dd1fad8757bdc9328990ad7c73f4d700b06591b7757599"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/snyk/parlay/archive/refs/tags/v0.11.0.tar.gz"
  end

  def install
    bin.install "parlay.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/parlay --version 2>&1")
  end
end
