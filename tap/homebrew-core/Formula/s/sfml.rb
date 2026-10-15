class Sfml < Formula
  desc "Multi-media library with bindings for multiple languages"
  homepage "https://www.sfml-dev.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sfml-3.0.2-3-any.pkg.tar.zst"
  sha256 "b1c518aca538205c836cf4cde91cfff9b0796e6d811c1148983b6695bce35593"
  license "Zlib"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/SFML/SFML/archive/refs/tags/3.0.2.tar.gz"
    regex(/v?(\d+(?:\.\d+)+)/i)
    strategy :github_releases
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"SFML/Main.hpp"
    assert_path_exists lib/"libsfml-audio.dll.a"
    assert_path_exists bin/"libsfml-audio-3.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/sfml-all.pc").read
  end
end
