class Miniaudio < Formula
  desc "Audio playback and capture library"
  homepage "https://miniaud.io"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-miniaudio-0.11.25-1-any.pkg.tar.zst"
  sha256 "81689ad2b8a6e3f818ebc6016f8fd2fd8b9a5835b522b8937ca06bc9f3d38f80"
  license any_of: [:public_domain, "MIT-0"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/mackron/miniaudio/archive/refs/tags/0.11.25.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"miniaudio/miniaudio.h"
    assert_match "Version: #{version}", (lib/"pkgconfig/miniaudio.pc").read
  end
end
