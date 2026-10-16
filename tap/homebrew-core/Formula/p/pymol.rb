class Pymol < Formula
  desc "Molecular visualization system"
  homepage "https://pymol.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pymol-3.1.0-5-any.pkg.tar.zst"
  sha256 "8a0450e783ca8df4b05ffaa46305078cd5de97e0304bbe685d89d64914a98dcf"
  license :cannot_represent
  revision 3

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/schrodinger/pymol-open-source/archive/refs/tags/v3.1.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists bin/"pymol.bat"
  end
end
