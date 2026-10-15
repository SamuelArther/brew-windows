class Corrosion < Formula
  desc "Easy Rust and C/C++ Integration"
  homepage "https://corrosion-rs.github.io/corrosion/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-corrosion-0.6.1-1-any.pkg.tar.zst"
  sha256 "8382d16cf83700c6c84267fc9f6611508f5884fcff4d64ed1c7f717b42a25cd9"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/corrosion-rs/corrosion/archive/refs/tags/v0.6.1.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"cmake/Corrosion/CorrosionConfig.cmake"
  end
end
