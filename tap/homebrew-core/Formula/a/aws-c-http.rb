class AwsCHttp < Formula
  desc "C99 implementation of the HTTP/1.1 and HTTP/2 specifications"
  homepage "https://github.com/awslabs/aws-c-http"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-http-1.0.0-1-any.pkg.tar.zst"
  sha256 "51a61a36fb65283bf3b159d1cf49a47114955d0d8ee2f05d8f253579d5bf0fd4"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-http/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/http/http.h"
    assert_path_exists lib/"libaws-c-http.dll.a"
    assert_path_exists bin/"libaws-c-http.dll"
  end
end
