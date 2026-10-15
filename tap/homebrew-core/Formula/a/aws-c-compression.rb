class AwsCCompression < Formula
  desc "C99 implementation of huffman encoding/decoding"
  homepage "https://github.com/awslabs/aws-c-compression"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-compression-1.0.0-1-any.pkg.tar.zst"
  sha256 "2516f6ab54d19c25e698a715e74f677a0c60379d6d31e7ab3e44132ddb6f7d57"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-compression/archive/refs/tags/v1.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/compression/exports.h"
    assert_path_exists lib/"libaws-c-compression.dll.a"
    assert_path_exists bin/"libaws-c-compression.dll"
  end
end
