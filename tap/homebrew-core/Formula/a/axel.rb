class Axel < Formula
  desc "Light UNIX download accelerator"
  homepage "https://github.com/axel-download-accelerator/axel"
  url "https://repo.msys2.org/msys/x86_64/axel-2.17.14-1-x86_64.pkg.tar.zst"
  sha256 "5febccab31e4edd5bf2a87b0be61405cfe394842d266584dc4c27710811d071b"
  license "GPL-2.0-or-later" => { with: "cryptsetup-OpenSSL-exception" }
  revision 1

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/axel-download-accelerator/axel/releases/download/v2.17.14/axel-2.17.14.tar.xz"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "gettext" do
    url "https://repo.msys2.org/msys/x86_64/gettext-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "b31ab065c3538ecb1fb6ab48ab2b3049243d6d9e585331c882f38326eefc1831"
  end

  resource "libasprintf" do
    url "https://repo.msys2.org/msys/x86_64/libasprintf-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "fdd59ae72d355ae1c5615f12429493611782f3e38bf1f7ca9ca83b61fcfb6c26"
  end

  resource "libgettextpo" do
    url "https://repo.msys2.org/msys/x86_64/libgettextpo-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "a9f3ef6b3712e20f0dc6eecaf8c2ec6aff5f259b2dbe87422a0aaca7b27c98d5"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
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
    msys2_bin "usr/bin/axel.exe", "axel"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/axel --version 2>&1")
  end
end
