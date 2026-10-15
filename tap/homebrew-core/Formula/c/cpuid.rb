class Cpuid < Formula
  desc "CPU feature identification for Go"
  homepage "https://github.com/klauspost/cpuid"
  url "https://github.com/klauspost/cpuid/releases/download/v2.4.0/cpuid-windows_amd64.zip"
  sha256 "cf2c5b0236d38154f535658b4fc042bb97916436e42302ad0b822fb8481169b6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/klauspost/cpuid/archive/refs/tags/v2.4.0.tar.gz"
  end

  def install
    bin.install "cpuid.exe"
  end

  test do
    json = shell_output("#{bin}/cpuid -json")
    assert_match "BrandName", json
    assert_match "VendorID", json
    assert_match "VendorString", json
  end
end
