class Soci < Formula
  desc "Database access library for C++"
  homepage "https://soci.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-soci-4.1.2-1-any.pkg.tar.zst"
  sha256 "95c91806d93638b4a7599fae294ec66122b7a95df1d60cb3bf24f787a6fffc0a"
  license "BSL-1.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/soci/soci/soci-4.1.4/soci-4.1.4.zip"
    regex(%r{url=.*?/soci[._-]v?(\d+(?:\.\d+)+)\.zip}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"soci/row.h"
    assert_path_exists lib/"libsoci_core_4_1.dll.a"
    assert_path_exists bin/"libsoci_core_4_1.dll"
  end
end
