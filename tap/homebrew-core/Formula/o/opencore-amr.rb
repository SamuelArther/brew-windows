class OpencoreAmr < Formula
  desc "Audio codecs extracted from Android open source project"
  homepage "https://opencore-amr.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opencore-amr-0.1.6-1-any.pkg.tar.zst"
  sha256 "65c6b5a2795ff12fd1839fa52b59785210ca1c2154fcbb73d7ea39767ea11fc4"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/opencore-amr/opencore-amr/opencore-amr-0.1.6.tar.gz"
    regex(%r{url=.*?/opencore-amr[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"opencore-amrwb/dec_if.h"
    assert_path_exists lib/"libopencore-amrnb.dll.a"
    assert_path_exists bin/"libopencore-amrnb-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/opencore-amrnb.pc").read
  end
end
