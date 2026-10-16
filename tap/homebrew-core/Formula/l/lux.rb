class Lux < Formula
  desc "Fast and simple video downloader"
  homepage "https://github.com/iawia002/lux"
  url "https://github.com/iawia002/lux/releases/download/v0.24.1/lux_0.24.1_Windows_x86_64.zip"
  sha256 "bc487da29bac7c0d76607b8ebbe5ca43d6d94c927e551165344eeae713b02098"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/iawia002/lux/archive/refs/tags/v0.24.1.tar.gz"
  end

  def install
    bin.install "lux.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lux --version 2>&1")
  end
end
