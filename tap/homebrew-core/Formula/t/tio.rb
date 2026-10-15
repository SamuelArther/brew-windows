class Tio < Formula
  desc "Simple TTY terminal I/O application"
  homepage "https://tio.github.io"
  url "https://repo.msys2.org/msys/x86_64/tio-3.9-2-x86_64.pkg.tar.zst"
  sha256 "3c35690560e51dd0d770c460349711b660c99a20f76bc42dd82150fdccc7df6c"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/tio/tio/releases/download/v3.9/tio-3.9.tar.xz"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "glib2" do
    url "https://repo.msys2.org/msys/x86_64/glib2-2.90.1-1-x86_64.pkg.tar.zst"
    sha256 "eb2280b5b4f921285b24c3267effecabbf3992bc37d77f2c3eac6028f35cab7a"
  end

  resource "libffi" do
    url "https://repo.msys2.org/msys/x86_64/libffi-3.8.0-1-x86_64.pkg.tar.zst"
    sha256 "e0a21ae5ee3f0ce1ff3f446ea2c95ad462803081014aecfa8fc1cd22bc4df187"
  end

  resource "libiconv" do
    url "https://repo.msys2.org/msys/x86_64/libiconv-1.19-1-x86_64.pkg.tar.zst"
    sha256 "0fa55ea2a6ccf97cf8c58b24b2615815e15e16e6e4e888091c263c2c83c5313d"
  end

  resource "libinih" do
    url "https://repo.msys2.org/msys/x86_64/libinih-62-1-x86_64.pkg.tar.zst"
    sha256 "aec0a662cd918bfbdfba94f4a8003b52eb4a38f89a0bf754e93526baa0c9395d"
  end

  resource "libintl" do
    url "https://repo.msys2.org/msys/x86_64/libintl-0.22.5-1-x86_64.pkg.tar.zst"
    sha256 "336d66b9d95cf9c1804958f8e260762a3e83bf158ed5981f783bc772a31073cf"
  end

  resource "libpcre2_8" do
    url "https://repo.msys2.org/msys/x86_64/libpcre2_8-10.49-1-x86_64.pkg.tar.zst"
    sha256 "2de42bf4656e3952a72809663e85899be76c7020e016687055ac0acd05145a3b"
  end

  resource "lua" do
    url "https://repo.msys2.org/msys/x86_64/lua-5.5.1-1-x86_64.pkg.tar.zst"
    sha256 "609af2673d7544c9263355802d2c96f7a33c7bc126e69371b52989879cedcdce"
  end

  resource "zlib" do
    url "https://repo.msys2.org/msys/x86_64/zlib-1.3.2-1-x86_64.pkg.tar.zst"
    sha256 "a04aa79996c57f0db936be66cf94326d7e67e9cd8dbffe4cf6e97693d0a1d9ef"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/tio.exe", "tio"
  end

  test do
    assert_match "Error: Not a tty device", shell_output("#{bin}/tio /dev/null 2>&1", 1)
  end
end
