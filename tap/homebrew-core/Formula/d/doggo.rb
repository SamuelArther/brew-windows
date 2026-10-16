class Doggo < Formula
  desc "Command-line DNS Client for Humans"
  homepage "https://doggo.mrkaran.dev/"
  url "https://github.com/mr-karan/doggo/releases/download/v1.4.1/doggo-windows-x86_64.zip"
  sha256 "93472211f07ef69e1a57349f29d9ebc988ded191b1f1df191a2d23e081d8c2ac"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mr-karan/doggo/archive/refs/tags/v1.4.1.tar.gz"
  end

  def install
    bin.install "doggo.exe"
  end

  test do
    answer = shell_output("#{bin}/doggo --short example.com NS @1.1.1.1")
    assert_equal "hera.ns.cloudflare.com.\nelliott.ns.cloudflare.com.\n", answer

    assert_match version.to_s, shell_output("#{bin}/doggo --version")
  end
end
