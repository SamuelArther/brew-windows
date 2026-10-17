class Tcping < Formula
  desc "TCP connect to the given IP/port combo"
  homepage "https://github.com/mkirchner/tcping"
  url "https://github.com/mkirchner/tcping/archive/refs/tags/2.1.0.tar.gz"
  sha256 "b8aa427420fe00173b5a2c0013d78e52b010350f5438bf5903c1942cba7c39c9"
  license "MIT"
  head "https://github.com/mkirchner/tcping.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/tcping-2.1.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2bd33b5febe0164ffb011c00d9f76f65adef2969843e42c0911718b776d79ed9"
  end

  def install
    system "make"
    bin.install "tcping"
  end

  test do
    system bin/"tcping", "www.google.com", "80"
  end
end
