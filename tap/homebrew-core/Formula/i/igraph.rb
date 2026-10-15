class Igraph < Formula
  desc "Network analysis package"
  homepage "https://igraph.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-igraph-1.0.1-1-any.pkg.tar.zst"
  sha256 "98abc3483ac52900c6ade08a23fb220fe6ae7f9f00a14e40220949eedca2f6d9"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/igraph/igraph/releases/download/1.0.1/igraph-1.0.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"igraph/igraph.h"
    assert_path_exists lib/"libigraph.dll.a"
    assert_path_exists bin/"libigraph.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/igraph.pc").read
  end
end
