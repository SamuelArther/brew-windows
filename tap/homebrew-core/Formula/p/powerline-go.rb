class PowerlineGo < Formula
  desc "Beautiful and useful low-latency prompt for your shell"
  homepage "https://github.com/justjanne/powerline-go"
  url "https://github.com/justjanne/powerline-go/releases/download/v1.26/powerline-go-windows-amd64.exe"
  sha256 "c90aa3c99b1cd2f17058bd77e5414c4fe4b504954d3e9bbfd611dcfca14efa4e"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/justjanne/powerline-go/archive/refs/tags/v1.26.tar.gz"
  end

  def install
    bin.install "powerline-go-windows-amd64.exe" => "powerline-go.exe"
  end

  test do
    system bin/"powerline-go"
  end
end
