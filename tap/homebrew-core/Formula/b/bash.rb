class Bash < Formula
  desc "Bourne-Again SHell, a UNIX command interpreter"
  homepage "https://www.gnu.org/software/bash/"
  url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
  sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  license "GPL-3.0-or-later"

  # GNU bash via MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/bash.exe", "bash"
  end

  test do
    assert_equal "hello", shell_output("#{bin}/bash -c \"echo -n hello\"")
  end
end
