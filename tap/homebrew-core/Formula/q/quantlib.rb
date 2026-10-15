class Quantlib < Formula
  desc "Library for quantitative finance"
  homepage "https://www.quantlib.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-quantlib-1.39-1-any.pkg.tar.zst"
  sha256 "18a08b033d8b34ed683668dd52b7214bd87123ffffc6e9de3c41058057c98895"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/lballabio/QuantLib/releases/download/v1.44/QuantLib-1.44.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ql/any.hpp"
    assert_path_exists lib/"libQuantLib.dll.a"
    assert_path_exists bin/"libQuantLib-1.dll"
  end
end
