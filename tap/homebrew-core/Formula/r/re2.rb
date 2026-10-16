class Re2 < Formula
  desc "Alternative to backtracking PCRE-style regular expression engines"
  homepage "https://github.com/google/re2"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-re2-20251105-3-any.pkg.tar.zst"
  version "20251105"
  sha256 "42b02745d0725a58a0bd442ebc7bf82b28b42257e28c5459bd02ee80b2a2fa92"
  license "BSD-3-Clause"
  version_scheme 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/google/re2/releases/download/2025-11-05/re2-2025-11-05.tar.gz"
    regex(/^(\d{4}-\d{2}-\d{2})$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"re2/re2.h"
    assert_path_exists lib/"libre2.dll.a"
    assert_path_exists bin/"libre2-11.dll"
  end
end
