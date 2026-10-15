class LadspaSdk < Formula
  desc "Linux Audio Developer's Simple Plugin"
  homepage "https://www.ladspa.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ladspa-sdk-1.17-2-any.pkg.tar.zst"
  sha256 "e59b0f7090531a0ed7f3b4536b610e207da8d4e587cc8647efd93dec3a309ca7"
  license "LGPL-2.1-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.ladspa.org/download/index.html"
    regex(/href=.*?ladspa[._-]sdk[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ladspa.h"
  end
end
