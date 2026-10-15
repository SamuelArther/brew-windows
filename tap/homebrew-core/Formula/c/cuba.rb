class Cuba < Formula
  desc "Library for multidimensional numerical integration"
  homepage "https://feynarts.de/cuba/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cuba-2.1-2-any.pkg.tar.zst"
  sha256 "c53cad57644808188c99f6ccb50aada2a99a1e88852f512987bdc4ad15dbfd2e"
  license "LGPL-3.0-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(/href=.*?Cuba[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"cuba.h"
  end
end
