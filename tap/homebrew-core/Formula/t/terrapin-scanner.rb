class TerrapinScanner < Formula
  desc "Vulnerability scanner for the Terrapin attack"
  homepage "https://terrapin-attack.com/"
  url "https://github.com/RUB-NDS/Terrapin-Scanner/releases/download/v1.1.3/Terrapin_Scanner_Windows_amd64.exe"
  sha256 "983ff810dcd5a08b35ba13c21884c0c0ce23c7fbd70839a26f821d20c3273ab3"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/RUB-NDS/Terrapin-Scanner/archive/refs/tags/v1.1.3.tar.gz"
  end

  def install
    bin.install "Terrapin_Scanner_Windows_amd64.exe" => "Terrapin-Scanner.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/Terrapin-Scanner --version 2>&1")
  end
end
