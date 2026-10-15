class JsonC < Formula
  desc "JSON parser for C"
  homepage "https://github.com/json-c/json-c/wiki"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-json-c-0.19-2-any.pkg.tar.zst"
  sha256 "e209dfd2e0955a9ab22bf394b13d477b2d24ae2660327e3981651efd464be0d5"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :head
    regex(/^json-c[._-](\d+(?:\.\d+)+)(?:[._-]\d{6,8})?$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"json-c/json.h"
    assert_path_exists lib/"libjson-c.dll.a"
    assert_path_exists bin/"libjson-c-5.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/json-c.pc").read
  end
end
