class Rage < Formula
  desc "Simple, modern, secure file encryption"
  homepage "https://str4d.xyz/rage"
  url "https://github.com/str4d/rage/releases/download/v0.12.1/rage-v0.12.1-x86_64-windows.zip"
  sha256 "da5b8111c8f097c7822df505ad504696e4891ff8adec06a39171f8d717590b2c"
  license any_of: ["MIT", "Apache-2.0"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/str4d/rage/archive/refs/tags/v0.12.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "rage.exe"
    bin.install "rage-keygen.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rage --version 2>&1")
  end
end
