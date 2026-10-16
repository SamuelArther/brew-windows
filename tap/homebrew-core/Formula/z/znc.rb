class Znc < Formula
  desc "Advanced IRC bouncer"
  homepage "https://wiki.znc.in/ZNC"
  url "https://repo.msys2.org/msys/x86_64/znc-1.10.3-1-x86_64.pkg.tar.zst"
  sha256 "cf41ff39803fe1c19cf75bce171a7a19ec711f5624991ce8b728ce3862432db5"
  license "Apache-2.0"
  revision 1

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://znc.in/releases/"
    regex(/href=.*?znc[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "icu" do
    url "https://repo.msys2.org/msys/x86_64/icu-78.1-1-x86_64.pkg.tar.zst"
    sha256 "46efb2dc43d07af09c6e0d2aeb35bc81830137f482ff0b54be87650eb80dae20"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "openssl" do
    url "https://repo.msys2.org/msys/x86_64/openssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "4dc7c1199010b26d6382c59d8b16a9dbc857d314d76d9668f0f333bc5fecdd38"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/znc.exe", "znc"
  end

  test do
    mkdir ".znc"
    system bin/"znc", "--makepem"
    assert_path_exists testpath/".znc/znc.pem"
  end
end
