class Asciidoctor < Formula
  desc "Text processor and publishing toolchain for AsciiDoc"
  homepage "https://asciidoctor.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-asciidoctor-2.0.26-2-any.pkg.tar.zst"
  sha256 "51d9c266cc3f9454e4dfe3d015faf08053fde500efdddb93ea8d1ef16294a1c3"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/asciidoctor/asciidoctor/archive/refs/tags/v2.0.26.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists bin/"asciidoctor"
  end
end
