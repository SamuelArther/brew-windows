class Doge < Formula
  desc "Command-line DNS client"
  homepage "https://dog.ramfield.net/"
  url "https://github.com/Dj-Codeman/dog_community/releases/download/v0.2.9/doge-v0.2.9-x86_64-pc-windows-msvc.exe.zip"
  sha256 "d3449f67d8ae793c5295c4bfcf60b20bd825a96a2d551835b593fbaaa735e1fc"
  license "EUPL-1.2"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Dj-Codeman/dog_community/archive/refs/tags/v0.2.9.tar.gz"
  end

  def install
    bin.install "doge-v0.2.9-x86_64-pc-windows-msvc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/doge-v0.2.9-x86_64-pc-windows-msvc --version 2>&1")
  end
end
