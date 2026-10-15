class Cpio < Formula
  desc "Copies files into or out of a cpio or tar archive"
  homepage "https://www.gnu.org/software/cpio/"
  url "https://repo.msys2.org/msys/x86_64/cpio-2.15-1-x86_64.pkg.tar.zst"
  sha256 "1937e62eb39c453781fc77667734f501df33e7431b7cf4bca7bda066878a8684"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://ftpmirror.gnu.org/cpio/cpio-2.15.tar.bz2"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/cpio.exe", "cpio"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cpio --version 2>&1")
  end
end
