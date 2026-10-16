class Litra < Formula
  desc "Control Logitech Litra lights from the command-line"
  homepage "https://github.com/timrogers/litra-rs"
  url "https://github.com/timrogers/litra-rs/releases/download/v3.3.0/litra_v3.3.0_windows-amd64.exe"
  sha256 "4024567efadd7fb54611a865be784012059e322221c0356150184dcb60283bdb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/timrogers/litra-rs/archive/refs/tags/v3.3.0.tar.gz"
  end

  def install
    bin.install "litra_v3.3.0_windows-amd64.exe" => "litra.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/litra --version")
    assert_match "No Logitech Litra devices found", shell_output("#{bin}/litra devices")
  end
end
