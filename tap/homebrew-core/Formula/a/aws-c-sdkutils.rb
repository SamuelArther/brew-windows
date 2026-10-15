class AwsCSdkutils < Formula
  desc "C99 library implementing AWS SDK specific utilities"
  homepage "https://github.com/awslabs/aws-c-sdkutils"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-sdkutils-1.0.0-1-any.pkg.tar.zst"
  sha256 "c5d0ab458a3891460d83ce1546511a965b44424c619779dd4e937c9bf9d08c3d"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-sdkutils/archive/refs/tags/v1.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/sdkutils/exports.h"
    assert_path_exists lib/"libaws-c-sdkutils.dll.a"
    assert_path_exists bin/"libaws-c-sdkutils.dll"
  end
end
