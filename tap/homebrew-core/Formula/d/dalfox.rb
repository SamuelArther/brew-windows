class Dalfox < Formula
  desc "XSS scanner and utility focused on automation"
  homepage "https://dalfox.hahwul.com"
  url "https://github.com/hahwul/dalfox/releases/download/v3.2.4/dalfox-v3.2.4-windows-x86_64.zip"
  sha256 "28100d9355a1c767facd571a180d08b87d4de61167b3db99d56e4d8a6601d69a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hahwul/dalfox/archive/refs/tags/v3.2.4.tar.gz"
  end

  def install
    bin.install "dalfox.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dalfox -V 2>&1")

    url = "https://pentest-ground.com:4280/vulnerabilities/xss_r/"
    output = shell_output("#{bin}/dalfox scan \"#{url}\" 2>&1", 1)
    assert_match "scan completed", output
  end
end
