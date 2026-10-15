class Liboqs < Formula
  desc "Library for quantum-safe cryptography"
  homepage "https://openquantumsafe.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-liboqs-0.16.0-1-any.pkg.tar.zst"
  sha256 "51c1f0455c0845188a72c0eb8f61df1ff1876ff5fb9c9ef049303a14258bd76b"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/open-quantum-safe/liboqs/archive/refs/tags/0.16.0.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"oqs/kem.h"
    assert_path_exists lib/"liboqs.dll.a"
    assert_path_exists bin/"liboqs-9.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/liboqs.pc").read
  end
end
