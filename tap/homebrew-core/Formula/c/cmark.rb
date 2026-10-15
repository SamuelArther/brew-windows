class Cmark < Formula
  desc "Strongly specified, highly compatible implementation of Markdown"
  homepage "https://commonmark.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cmark-0.31.2-1-any.pkg.tar.zst"
  sha256 "0399114e2a2c6aa850c713f00c1de522e2f59d7f2be9395b1ea7fb58edc5b9c3"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/commonmark/cmark/archive/refs/tags/0.31.2.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/cmark.exe", "cmark"
  end

  test do
    output = pipe_output(bin/"cmark", "*hello, world*")
    assert_equal "<p><em>hello, world</em></p>", output.chomp
  end
end
