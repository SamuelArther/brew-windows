class AwsCIo < Formula
  desc "Event driven framework for implementing application protocols"
  homepage "https://github.com/awslabs/aws-c-io"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-io-1.0.0-1-any.pkg.tar.zst"
  sha256 "f44695d9b9a4592e570d362f65d82f516f92a5e375788afd068e02cf80e7f082"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-io/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/io/io.h"
    assert_path_exists lib/"libaws-c-io.dll.a"
    assert_path_exists bin/"libaws-c-io.dll"
  end
end
