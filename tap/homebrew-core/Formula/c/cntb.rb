class Cntb < Formula
  desc "Contabo Command-Line Interface (CLI)"
  homepage "https://github.com/contabo/cntb"
  url "https://github.com/contabo/cntb/releases/download/v1.7/cntb_v1.7_windows_amd64.zip"
  sha256 "de34caeec1a632588c8ef16615923f7875580225fb7c60357196a19fa57ad612"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/contabo/cntb/archive/refs/tags/v1.7.tar.gz"
  end

  def install
    bin.install "cntb.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cntb version 2>&1")
  end
end
