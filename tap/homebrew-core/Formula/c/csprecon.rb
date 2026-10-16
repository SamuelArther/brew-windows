class Csprecon < Formula
  desc "Discover new target domains using Content Security Policy"
  homepage "https://edoardottt.com/"
  url "https://github.com/edoardottt/csprecon/releases/download/v0.4.7/csprecon_0.4.7_windows_amd64.zip"
  sha256 "82f87e0264e7c09b0007729fa47b1dc4b33c1a8f8dfe1644c8ad6999cb36de92"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/edoardottt/csprecon/archive/refs/tags/v0.4.7.tar.gz"
  end

  def install
    bin.install "csprecon.exe"
  end

  test do
    output = shell_output("#{bin}/csprecon -u https://brew.sh")
    assert_match "avatars.githubusercontent.com", output
  end
end
