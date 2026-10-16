class JwtUi < Formula
  desc "TUI for decoding and encoding JWT tokens"
  homepage "https://jwtui.cli.rs/"
  url "https://github.com/jwt-rs/jwt-ui/releases/download/v1.3.0/jwtui-windows.tar.gz"
  sha256 "1e1c953277ed34cb3e62d1e4e8fbb42ba0f08b3261284fd55b4ecf89e169a764"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jwt-rs/jwt-ui/archive/refs/tags/v1.3.0.tar.gz"
  end

  def install
    bin.install "jwtui.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jwtui --version 2>&1")
  end
end
