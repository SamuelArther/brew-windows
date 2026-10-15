class Nng < Formula
  desc "Nanomsg-next-generation -- light-weight brokerless messaging"
  homepage "https://nng.nanomsg.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nng-1.12.4-1-any.pkg.tar.zst"
  sha256 "569e4948b39e279d23f61fad88f538d1751344f01e090dd4460b1288d8139c3a"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/nanomsg/nng/archive/refs/tags/v1.12.4.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-wolfssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wolfssl-5.9.4-1-any.pkg.tar.zst"
    sha256 "0a655158c6aca7afa2f179df0020c365407569c6d82ff46608dc551b40e7243d"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/nngcat.exe", "nngcat"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nngcat --version 2>&1")
  end
end
