class Stuntman < Formula
  desc "Implementation of the STUN protocol"
  homepage "https://www.stunprotocol.org"
  url "https://www.stunprotocol.org/stunserver-1.2.16.tgz"
  sha256 "4479e1ae070651dfc4836a998267c7ac2fba4f011abcfdca3b8ccd7736d4fd26"
  license "Apache-2.0"
  head "https://github.com/jselbie/stunserver.git", branch: "master"

  livecheck do
    url :homepage
    regex(/href=.*?stunserver[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8153705e149932c1e81d6d87c4aee9cb6bb3841fab7640a682207292905173c9"
  end

  depends_on "boost" => :build

  # on macOS, stuntman uses CommonCrypt
  on_linux do
    depends_on "openssl@4"
  end

  def install
    ENV.cxx11

    system "make"
    bin.install "stunserver", "stunclient", "stuntestcode"
  end

  test do
    system bin/"stuntestcode"
  end
end
