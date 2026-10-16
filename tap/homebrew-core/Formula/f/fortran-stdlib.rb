class FortranStdlib < Formula
  desc "Fortran Standard Library"
  homepage "https://stdlib.fortran-lang.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fortran-stdlib-0.8.1-1-any.pkg.tar.zst"
  sha256 "6d23ea0d01864614d661317cc63c171f2d57de33da1986dabca319cd44b16e8c"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/fortran-lang/stdlib/archive/refs/tags/v0.8.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"libfortran_stdlib.dll.a"
    assert_path_exists bin/"libfortran_stdlib.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/fortran_stdlib.pc").read
  end
end
