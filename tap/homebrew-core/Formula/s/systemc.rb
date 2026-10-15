class Systemc < Formula
  desc "Core SystemC language and examples"
  homepage "https://systemc.org/overview/systemc/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-systemc-3.0.2-1-any.pkg.tar.zst"
  sha256 "10fba992691edd753c22e66f42ed113ece7d4a2b7d4b8778107d5b3913571e52"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/accellera-official/systemc/archive/refs/tags/3.0.2.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"tlm.h"
    assert_path_exists bin/"libsystemc-3.0.2.dll"
  end
end
