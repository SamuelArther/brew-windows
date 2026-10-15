class Lexbor < Formula
  desc "Fast embeddable web browser engine written in C with no dependencies"
  homepage "https://lexbor.com/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lexbor-3.0.1-1-any.pkg.tar.zst"
  sha256 "659f1e25088709bf2a92594368172b1ab8f5e935642f0cc55fcfe60e2a5b20d0"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/lexbor/lexbor/archive/refs/tags/v3.0.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"lexbor/ns/ns.h"
    assert_path_exists lib/"liblexbor.dll.a"
    assert_path_exists bin/"liblexbor.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/lexbor.pc").read
  end
end
