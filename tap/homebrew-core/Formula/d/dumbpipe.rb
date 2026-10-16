class Dumbpipe < Formula
  desc "Unix pipes between devices"
  homepage "https://dumbpipe.dev"
  url "https://github.com/n0-computer/dumbpipe/releases/download/v0.39.0/dumbpipe-v0.39.0-windows-x86_64.zip"
  sha256 "a58f4e1f58281b6f5407baa6502c58cc824c41d8e0c418bbb6eea51bfd674856"
  license any_of: ["MIT", "Apache-2.0"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/n0-computer/dumbpipe/archive/refs/tags/v0.39.0.tar.gz"
  end

  def install
    bin.install "dumbpipe.exe"
  end

  test do
    system bin/"dumbpipe", "--help"
  end
end
