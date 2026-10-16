class AwsCCal < Formula
  desc "AWS Crypto Abstraction Layer"
  homepage "https://github.com/awslabs/aws-c-cal"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-cal-1.0.0-1-any.pkg.tar.zst"
  sha256 "07f61cf307c232ef7bef0a132fec05711c8d1facfecb161d6f570a243aeb91c9"
  license "Apache-2.0"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-cal/archive/refs/tags/v1.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/cal/cal.h"
    assert_path_exists lib/"libaws-c-cal.dll.a"
    assert_path_exists bin/"libaws-c-cal.dll"
  end
end
