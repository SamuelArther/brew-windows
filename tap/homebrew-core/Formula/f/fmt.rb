class Fmt < Formula
  desc "Open-source formatting library for C++"
  homepage "https://fmt.dev/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fmt-12.2.0-1-any.pkg.tar.zst"
  sha256 "7b368e76d231f44c968c6b2a0780ac122e48fac80b35aa9ec353a3bcb440b00f"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/fmtlib/fmt/releases/download/12.2.0/fmt-12.2.0.zip"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"fmt/os.h"
    assert_path_exists lib/"libfmt.dll.a"
    assert_path_exists bin/"libfmt-12.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/fmt.pc").read
  end
end
