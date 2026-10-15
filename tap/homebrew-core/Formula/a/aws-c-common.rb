class AwsCCommon < Formula
  desc "Core c99 package for AWS SDK for C"
  homepage "https://github.com/awslabs/aws-c-common"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-common-1.0.2-1-any.pkg.tar.zst"
  sha256 "d7312c06681a0a7762f92ea0dedfdbb16efe4a9ac20ea2e43d095deca7f2a0c5"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-common/archive/refs/tags/v1.0.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/common/uri.h"
    assert_path_exists lib/"libaws-c-common.dll.a"
    assert_path_exists bin/"libaws-c-common.dll"
  end
end
