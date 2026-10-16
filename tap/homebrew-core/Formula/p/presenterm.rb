class Presenterm < Formula
  desc "Terminal slideshow tool"
  homepage "https://mfontanini.github.io/presenterm/"
  url "https://github.com/mfontanini/presenterm/releases/download/v0.16.1/presenterm-0.16.1-x86_64-pc-windows-msvc.zip"
  sha256 "301bc9111db499cb79fb305048b1aad56d3fe91b2c713396fed60ec0c1d2c90c"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mfontanini/presenterm/archive/refs/tags/v0.16.1.tar.gz"
  end

  def install
    bin.install "presenterm.exe"
  end

  test do
    # presenterm is a TUI application
    assert_match version.to_s, shell_output("#{bin}/presenterm --version")
  end
end
