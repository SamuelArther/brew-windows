class Gsl < Formula
  desc "Numerical library for C and C++"
  homepage "https://www.gnu.org/software/gsl/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gsl-2.8-1-any.pkg.tar.zst"
  sha256 "841bb5f98a3a008bac2561483e230f7ecb44326c41161ce53634d2ce7a0ab3f5"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftpmirror.gnu.org/gsl/gsl-2.8.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/gsl-histogram.exe", "gsl-histogram"
    msys2_bin "ucrt64/bin/gsl-randist.exe", "gsl-randist"
  end

  test do
    system bin/"gsl-randist", "0", "20", "cauchy", "30"
  end
end
