class Gotz < Formula
  desc "Displays timezones in your terminal"
  homepage "https://github.com/merschformann/gotz"
  url "https://github.com/merschformann/gotz/releases/download/v0.1.15/gotz_0.1.15_windows_amd64.tar.gz"
  sha256 "5cf9911e09335ee28f324aa6bb557d6db3ec2829c0b205e8f4019ed13eb19b2c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/merschformann/gotz/archive/refs/tags/v0.1.15.tar.gz"
  end

  def install
    bin.install "gotz.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gotz -version")
    assert_match "Local", shell_output("#{bin}/gotz -timezones America/New_York")
  end
end
