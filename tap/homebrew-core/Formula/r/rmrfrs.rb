class Rmrfrs < Formula
  desc "Filesystem cleaning tool"
  homepage "https://github.com/trinhminhtriet/rmrfrs"
  url "https://github.com/trinhminhtriet/rmrfrs/releases/download/v0.8.8/rmrfrs-windows-amd64.exe"
  sha256 "ab763381c98200e352c1f12fc62ca4bf6f31cf10155c152710275c5289194308"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/trinhminhtriet/rmrfrs/archive/refs/tags/v0.8.8.tar.gz"
  end

  def install
    bin.install "rmrfrs-windows-amd64.exe" => "rmrfrs.exe"
  end

  test do
    system bin/"rmrfrs", "--help"
  end
end
