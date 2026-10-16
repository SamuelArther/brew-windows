class Urlfinder < Formula
  desc "Extracting URLs and subdomains from JS files on a website"
  homepage "https://github.com/pingc0y/URLFinder"
  url "https://github.com/pingc0y/URLFinder/releases/download/2026.6.16/URLFinder_Windows_x86_64.zip"
  sha256 "cf796ca0164b2495ba4198211767cf077a4a0f1b0dda76b96ec8f3b4ba1421e4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pingc0y/URLFinder/archive/refs/tags/2026.6.16.tar.gz"
  end

  def install
    bin.install "URLFinder.exe" => "urlfinder.exe"
  end

  test do
    assert_match "Start 1 Spider...", shell_output("#{bin}/urlfinder -u https://example.com")
    assert_match version.to_s, shell_output("#{bin}/urlfinder version")
  end
end
