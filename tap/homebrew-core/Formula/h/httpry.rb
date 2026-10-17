class Httpry < Formula
  desc "Packet sniffer for displaying and logging HTTP traffic"
  homepage "https://github.com/jbittel/httpry"
  url "https://github.com/jbittel/httpry/archive/refs/tags/httpry-0.1.8.tar.gz"
  sha256 "b3bcbec3fc6b72342022e940de184729d9cdecb30aa754a2c994073447468cf0"
  license "GPL-2.0-only"
  head "https://github.com/jbittel/httpry.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/httpry-0.1.8"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b3547f3e910d1df50f143f80bc7034b9e2fb530517dc20a5054e7cef38094635"
  end

  uses_from_macos "libpcap"

  def install
    system "make"
    bin.install "httpry"
    man1.install "httpry.1"
    doc.install Dir["doc/*"]
  end

  test do
    system bin/"httpry", "-h"
  end
end
