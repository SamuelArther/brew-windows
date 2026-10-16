class VulsioGost < Formula
  desc "Local CVE tracker & notification system"
  homepage "https://github.com/vulsio/gost"
  url "https://github.com/vulsio/gost/releases/download/v0.7.4/gost_0.7.4_windows_amd64.tar.gz"
  sha256 "929af2a75b1ae13d5e34364dea58cc97f09dd80515a0461dec55e78148b280d0"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vulsio/gost/archive/refs/tags/v0.7.4.tar.gz"
  end

  def install
    bin.install "gost.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gost version 2>&1")
  end
end
