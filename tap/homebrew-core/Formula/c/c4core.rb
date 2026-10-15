class C4core < Formula
  desc "C++ utilities"
  homepage "https://github.com/biojppm/c4core"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-c4core-0.2.11-2-any.pkg.tar.zst"
  sha256 "0fe2269f3ef39cf8d5ace62e385e684c6a7fe0c69f186a6258a3b77e5456103c"
  license all_of: ["MIT", "BSL-1.0"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/biojppm/c4core/releases/download/v0.6.0/c4core-0.6.0-src.tgz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"c4/cpu.hpp"
    assert_path_exists lib/"libc4core.dll.a"
    assert_path_exists bin/"libc4core.dll"
  end
end
