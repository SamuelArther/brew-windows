class Check < Formula
  desc "C unit testing framework"
  homepage "https://libcheck.github.io/check/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-check-0.15.2-4-any.pkg.tar.zst"
  sha256 "e047d4e3c1eb384be9eaa4bc4d39fb09c5d9a740c36957a7e70856aa0f5ef134"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libcheck/check/releases/download/0.15.2/check-0.15.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"check.h"
    assert_path_exists lib/"libcheck.dll.a"
    assert_path_exists bin/"libcheck-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/check.pc").read
  end
end
