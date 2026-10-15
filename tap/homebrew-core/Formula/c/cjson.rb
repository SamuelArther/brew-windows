class Cjson < Formula
  desc "Ultralightweight JSON parser in ANSI C"
  homepage "https://github.com/DaveGamble/cJSON"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cjson-1.7.19-1-any.pkg.tar.zst"
  sha256 "47dc4b9873657a4dfa35f0efe5746b172ee4ae622a12b93be00981a9c17911cd"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/DaveGamble/cJSON/archive/refs/tags/v1.7.19.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"cjson/cJSON.h"
    assert_path_exists lib/"libcjson.dll.a"
    assert_path_exists bin/"libcjson-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libcjson.pc").read
  end
end
