class NativefiledialogExtended < Formula
  desc "Native file dialog library with C and C++ bindings"
  homepage "https://github.com/btzy/nativefiledialog-extended"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nativefiledialog-extended-1.3.0-1-any.pkg.tar.zst"
  sha256 "94e34072a723bc7c3efdea196f96d317b46ff8568cdfc0328c9c56af9b75a18b"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/btzy/nativefiledialog-extended/archive/refs/tags/v1.4.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"nfd.h"
    assert_path_exists lib/"libnfd.dll.a"
    assert_path_exists bin/"libnfd.dll"
  end
end
