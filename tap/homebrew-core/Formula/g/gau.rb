class Gau < Formula
  desc "Open Threat Exchange, Wayback Machine, and Common Crawl URL fetcher"
  homepage "https://github.com/lc/gau"
  url "https://github.com/lc/gau/releases/download/v2.2.4/gau_2.2.4_windows_amd64.zip"
  sha256 "46eac6d96c9479368d3006191611a33084f177fe4025efed985f40f4b7e51e64"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/lc/gau/archive/refs/tags/v2.2.4.tar.gz"
  end

  def install
    bin.install "gau.exe"
  end

  test do
    output = shell_output("#{bin}/gau --providers urlscan brew.sh")
    assert_match %r{https?://brew\.sh(/|:)?.*}, output

    assert_match version.to_s, shell_output("#{bin}/gau --version")
  end
end
