class JsonFortran < Formula
  desc "Fortran 2008 JSON API"
  homepage "https://jacobwilliams.github.io/json-fortran/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-json-fortran-9.3.1-1-any.pkg.tar.zst"
  sha256 "3a207fbf80e59561e5ed08fe4758e1c11fb014e75c9cc64452008ec59a90314f"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/jacobwilliams/json-fortran/archive/refs/tags/9.3.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"libjsonfortran.dll.a"
    assert_path_exists bin/"libjsonfortran-9.3.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/json-fortran.pc").read
  end
end
