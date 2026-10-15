class AwsCS3 < Formula
  desc "C99 library implementation for communicating with the S3 service"
  homepage "https://github.com/awslabs/aws-c-s3"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-s3-1.3.0-1-any.pkg.tar.zst"
  sha256 "5039dae6f4f360fb87196b077a202118b10b6a70b45418a306ac141ad72541e7"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-s3/archive/refs/tags/v1.3.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/s3/s3.h"
    assert_path_exists lib/"libaws-c-s3.dll.a"
    assert_path_exists bin/"libaws-c-s3.dll"
  end
end
