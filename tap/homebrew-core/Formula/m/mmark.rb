class Mmark < Formula
  desc "Powerful markdown processor in Go geared towards the IETF"
  homepage "https://mmark.miek.nl/"
  url "https://github.com/mmarkdown/mmark/releases/download/v2.2.48/mmark_2.2.48_windows_amd64.tgz"
  sha256 "aa5bc8e45d24f9ca92a80b8da69bf898fc87e6aba5d44a9fd3fe57909eebdf31"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mmarkdown/mmark/archive/refs/tags/v2.2.48.tar.gz"
  end

  def install
    bin.install "mmark.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mmark --version 2>&1")
  end
end
