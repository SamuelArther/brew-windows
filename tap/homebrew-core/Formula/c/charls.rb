class Charls < Formula
  desc "C++ JPEG-LS library implementation"
  homepage "https://github.com/team-charls/charls"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-charls-2.4.4-1-any.pkg.tar.zst"
  sha256 "82022511524c55b1caa9477c2cb296520425c8ffc0bb826f6899f8a2dd7b3077"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/team-charls/charls/archive/refs/tags/2.4.5.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"charls/charls.h"
    assert_path_exists lib/"libcharls.dll.a"
    assert_path_exists bin/"libcharls.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/charls.pc").read
  end
end
