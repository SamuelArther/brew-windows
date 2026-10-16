class Favirecon < Formula
  desc "Uses favicon.ico to improve the target recon phase"
  homepage "https://edoardottt.com/"
  url "https://github.com/edoardottt/favirecon/releases/download/v1.0.4/favirecon_1.0.4_windows_amd64.zip"
  sha256 "63ddb74b04d7691142f9f2c844869a97c9aab30815c846c4ba9cb84c75a9d2e1"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/edoardottt/favirecon/archive/refs/tags/v1.0.4.tar.gz"
  end

  def install
    bin.install "favirecon.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/favirecon --help")

    output = shell_output("#{bin}/favirecon -u https://www.github.com -verbose 2>&1")
    assert_match "Checking favicon for https://www.github.com/favicon.ico", output
  end
end
