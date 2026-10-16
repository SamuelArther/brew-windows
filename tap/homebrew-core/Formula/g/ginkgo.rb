class Ginkgo < Formula
  desc "High-performance numerical linear algebra software package"
  homepage "https://ginkgo-project.github.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ginkgo-1.9.0-3-any.pkg.tar.zst"
  sha256 "2e997b1c94c1213d9d4662973886edac769207841ddb5473ef509353cb1ce29c"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/ginkgo-project/ginkgo/archive/refs/tags/v1.11.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ginkgo/config.hpp"
    assert_path_exists lib/"libginkgo.dll.a"
    assert_path_exists bin/"libginkgo.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/ginkgo.pc").read
  end
end
