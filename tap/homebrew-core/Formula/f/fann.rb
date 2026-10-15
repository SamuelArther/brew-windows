class Fann < Formula
  desc "Fast artificial neural network library"
  homepage "https://sourceforge.net/projects/fann/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fann-2.2.0-3-any.pkg.tar.zst"
  sha256 "c40c32ff0382dc20e73b0ad1cbec2532a00b5a0ede35a19214cf18236b3d64d3"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/fann/fann/2.2.0/FANN-2.2.0-Source.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"fann.h"
    assert_path_exists lib/"libdoublefann.dll.a"
    assert_path_exists bin/"libdoublefann.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/fann.pc").read
  end
end
