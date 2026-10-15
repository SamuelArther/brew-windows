class AwsCAuth < Formula
  desc "C99 library implementation of AWS client-side authentication"
  homepage "https://github.com/awslabs/aws-c-auth"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-auth-1.0.0-1-any.pkg.tar.zst"
  sha256 "465d9e496d69b57b2a19e966fe60832f503a37fdff8ea89743fa09cccf2bc578"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-auth/archive/refs/tags/v1.0.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/auth/auth.h"
    assert_path_exists lib/"libaws-c-auth.dll.a"
    assert_path_exists bin/"libaws-c-auth.dll"
  end
end
