class Lapack < Formula
  desc "Linear Algebra PACKage"
  homepage "https://www.netlib.org/lapack/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lapack-3.12.1-2-any.pkg.tar.zst"
  sha256 "bca8b281ca69101f8718ff4b87f9fc93e9913ac2e804e1a5289fccdaf4aae025"
  license all_of: ["BSD-3-Clause-Open-MPI", "BSD-3-Clause"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/Reference-LAPACK/lapack/archive/refs/tags/v3.12.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"liblapack.dll.a"
    assert_path_exists bin/"liblapack.dll"
  end
end
