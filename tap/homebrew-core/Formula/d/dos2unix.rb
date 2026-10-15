class Dos2unix < Formula
  desc "Convert text between DOS, UNIX, and Mac formats"
  homepage "https://waterlander.net/dos2unix/"
  url "https://repo.msys2.org/msys/x86_64/dos2unix-7.5.7-1-x86_64.pkg.tar.zst"
  sha256 "320ae1d9de56fcd51e18c541effe710bb1237998cdf6d189cae857b463c86fb7"
  license "BSD-2-Clause"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url :homepage
    regex(/href=.*?dos2unix[._-]v?(\d+(?:\.\d+)+)\.t/i)
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
    msys2_bin "usr/bin/dos2unix.exe", "dos2unix"
    msys2_bin "usr/bin/mac2unix.exe", "mac2unix"
    msys2_bin "usr/bin/unix2dos.exe", "unix2dos"
    msys2_bin "usr/bin/unix2mac.exe", "unix2mac"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dos2unix --version 2>&1")
  end
end
