class Caire < Formula
  desc "Content aware image resize tool"
  homepage "https://github.com/esimov/caire"
  url "https://github.com/esimov/caire/releases/download/v1.5.0/caire-1.5.0-windows-amd64.zip"
  sha256 "5160b3a94d8b59228e069a57ba254aa00fbd00ccda4c474423ecbcf8cf557c8c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/esimov/caire/archive/refs/tags/v1.5.0.tar.gz"
  end

  def install
    bin.install "caire.exe"
  end

  test do
    system bin/"caire", "--help"
  end
end
