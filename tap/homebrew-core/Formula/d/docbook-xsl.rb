class DocbookXsl < Formula
  desc "XML vocabulary to create presentation-neutral documents"
  homepage "https://github.com/docbook/xslt10-stylesheets"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-docbook-xsl-1.79.2-9-any.pkg.tar.zst"
  sha256 "c36d57ad1e29aff5ec18b21a773fcca9acc7fa72b6ff0f6fb73519fc1a38de75"
  license "MIT"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :homepage
    regex(%r{^(?:release/)?(\d+(?:\.\d+)+)$}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    assert_path_exists share/"licenses/mingw-w64-ucrt-x86_64-docbook-xsl/COPYING"
  end
end
