class Avce00 < Formula
  desc "Make Arc/Info (binary) Vector Coverages appear as E00"
  homepage "http://avce00.maptools.org/avce00/index.html"
  # Upstream is only available via HTTP, so we prefer Debian's HTTPS mirror
  url "https://deb.debian.org/debian/pool/main/a/avce00/avce00_2.0.0.orig.tar.gz"
  mirror "http://avce00.maptools.org/dl/avce00-2.0.0.tar.gz"
  sha256 "c0851f86b4cd414d6150a04820491024fb6248b52ca5c7bd1ca3d2a0f9946a40"
  license "MIT"

  # Last release on 2006-08-17
  deprecate! date: "2026-07-17", because: :unmaintained
  disable! date: "2027-01-17", because: :unmaintained

  conflicts_with "gdal", because: "both install a cpl_conv.h header"

  deny_network_access!

  bottle do
    sha256 cellar: :any, x86_64_windows: "667cf0abc1c32b88cb6e65f1958e6cdd4cbff047fd057108e671bac4015256d5"
  end

  def install
    system "make", "CC=#{ENV.cc}"
    bin.install "avcimport", "avcexport", "avcdelete", "avctest"
    lib.install "avc.a"
    include.install Dir["*.h"]
  end

  test do
    touch testpath/"test"
    system bin/"avctest", "-b", "test"
  end
end
