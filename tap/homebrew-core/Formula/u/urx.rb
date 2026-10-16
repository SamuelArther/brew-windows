class Urx < Formula
  desc "Extracts URLs from OSINT Archives for Security Insights"
  homepage "https://urx.hahwul.com"
  url "https://github.com/hahwul/urx/releases/download/0.11.0/urx-0.11.0-windows-x86_64.zip"
  sha256 "b46eca23db6dcebc37c0aaffd10310522128c15cc200237995c593b3d03f5140"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hahwul/urx/archive/refs/tags/0.11.0.tar.gz"
  end

  def install
    bin.install "urx.exe"
  end

  test do
    assert_match "urx #{version}", shell_output("#{bin}/urx --version")
    assert_match "https://brew.sh/", shell_output("#{bin}/urx brew.sh --providers=cc --include-sitemap")
  end
end
