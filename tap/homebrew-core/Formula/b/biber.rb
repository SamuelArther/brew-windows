class Biber < Formula
  desc "Backend processor for BibLaTeX"
  homepage "https://sourceforge.net/projects/biblatex-biber/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-biber-2.21-2-any.pkg.tar.zst"
  sha256 "0a4319fec7373c99653c779c816f99c3a46f1b254bfb9bf8b092b6c19917e78a"
  license "Artistic-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/plk/biber/archive/refs/tags/v2.22.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists bin/"site_perl/biber"
  end
end
