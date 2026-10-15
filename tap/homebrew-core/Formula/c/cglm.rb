class Cglm < Formula
  desc "Optimized OpenGL/Graphics Math (glm) for C"
  homepage "https://github.com/recp/cglm"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cglm-0.9.6-1-any.pkg.tar.zst"
  sha256 "03317bac4f723e65b6841164adc61c3132088f61c2906f156faafc6ccb31402b"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/recp/cglm/archive/refs/tags/v0.9.6.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"cglm/io.h"
    assert_path_exists lib/"libcglm.dll.a"
    assert_path_exists bin/"libcglm-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/cglm.pc").read
  end
end
