class Openlibm < Formula
  desc "High quality, portable, open source libm implementation"
  homepage "https://openlibm.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openlibm-0.8.7-1-any.pkg.tar.zst"
  sha256 "d8d073a35d0ef03670d617b595093bd50c118d37e06e14f843d24d383d971561"
  license all_of: ["MIT", "ISC", "BSD-2-Clause"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/JuliaMath/openlibm/archive/refs/tags/v0.8.8.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"openlibm/k_log.h"
    assert_path_exists lib/"libopenlibm.dll.a"
    assert_path_exists bin/"libopenlibm.dll"
  end
end
