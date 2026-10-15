class GumboParser < Formula
  desc "C99 library for parsing HTML5"
  homepage "https://codeberg.org/gumbo-parser/gumbo-parser"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gumbo-parser-0.14.1-1-any.pkg.tar.zst"
  sha256 "35259eb1f97a9bb8b302061759e59deecee6203096b5383b76be9389c14cf735"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://codeberg.org/gumbo-parser/gumbo-parser/archive/0.14.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"gumbo.h"
    assert_path_exists lib/"libgumbo.dll.a"
    assert_path_exists bin/"libgumbo-4.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gumbo.pc").read
  end
end
