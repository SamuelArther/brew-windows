class Stk < Formula
  desc "Sound Synthesis Toolkit"
  homepage "https://ccrma.stanford.edu/software/stk/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-stk-5.0.1-2-any.pkg.tar.zst"
  sha256 "93a49882d2319f5baf81ccd51056a9baff9c6a7f914c382b707deb95290abf96"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ccrma.stanford.edu/software/stk/download.html"
    regex(/href=.*?stk[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"stk/FM.h"
    assert_path_exists lib/"libstk.dll.a"
    assert_path_exists bin/"libstk.dll"
  end
end
