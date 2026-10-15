class Gengetopt < Formula
  desc "Generate C code to parse command-line arguments via getopt_long"
  homepage "https://www.gnu.org/software/gengetopt/"
  url "https://repo.msys2.org/msys/x86_64/gengetopt-2.23.1-1-x86_64.pkg.tar.zst"
  sha256 "a30b856331f8fa477239d3da62d396ada3f80114ceb2d613131bb97e0d04c6f2"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://ftpmirror.gnu.org/gengetopt/gengetopt-2.23.1.tar.xz"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/gengetopt.exe", "gengetopt"
  end

  test do
    assert_path_exists bin/"gengetopt.exe"
  end
end
