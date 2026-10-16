class Bison < Formula
  desc "Parser generator"
  homepage "https://www.gnu.org/software/bison/"
  url "https://repo.msys2.org/msys/x86_64/bison-3.8.2-5-x86_64.pkg.tar.zst"
  sha256 "7439ca784a9909e0730a513d3aff15852c3b292ac85053fa2dd7a2a19c1ce04c"
  license "GPL-3.0-or-later"
  version_scheme 1

  # GNU bison via MSYS2.

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
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

  resource "m4" do
    url "https://repo.msys2.org/msys/x86_64/m4-1.4.21-1-x86_64.pkg.tar.zst"
    sha256 "b561e8871c7599502d9fae7196ee965b3fed3d7af09154eb8d2f5e4d9e5e5106"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/bison.exe", "bison"
    msys2_bin "usr/bin/yacc", "yacc"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bison --version")
  end
end
