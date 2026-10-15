class Lemon < Formula
  desc "LALR(1) parser generator like yacc or bison"
  homepage "https://www.hwaci.com/sw/lemon/"
  url "https://repo.msys2.org/msys/x86_64/lemon-3.54.0-1-x86_64.pkg.tar.zst"
  sha256 "ee19d7f20249bad90d03ba3940493201076d871863860efb61149aa6d4d7b87b"
  license "blessing"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://www.sqlite.org/2026/sqlite-src-3540000.zip"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/lemon.exe", "lemon"
  end

  test do
    assert_path_exists bin/"lemon.exe"
  end
end
