class Onednn < Formula
  desc "Basic building blocks for deep learning applications"
  homepage "https://www.oneapi.io/open-source/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-onednn-3.11.3-2-any.pkg.tar.zst"
  sha256 "496ccada00403457f3a68aea252fa18dfb66a135c3ca6f483b5a37c3fcb97b46"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/uxlfoundation/oneDNN/archive/refs/tags/v3.13.4.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"dnnl.h"
    assert_path_exists lib/"libdnnl.dll.a"
    assert_path_exists bin/"libdnnl.dll"
  end
end
