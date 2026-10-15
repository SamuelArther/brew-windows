class Discount < Formula
  desc "C implementation of Markdown"
  homepage "https://www.pell.portland.or.us/~orc/Code/discount/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-discount-3.0.2.0-1-any.pkg.tar.zst"
  sha256 "32a38bdc51f89e4ee04fcdd4577f14ccefdcfc33da48c540a00cf6cc023b6223"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/href=.*?discount[._-]v?(\d+(?:\.\d+)+[a-z]?)\.t/i)
  end

  conflicts_with "multimarkdown", because: "both install `markdown` binaries"

  def install
    msys2_install
    msys2_bin "ucrt64/bin/makepage.exe", "makepage"
    msys2_bin "ucrt64/bin/markdown.exe", "markdown"
    msys2_bin "ucrt64/bin/mkd2html.exe", "mkd2html"
    msys2_bin "ucrt64/bin/theme.exe", "theme"
  end

  test do
    markdown = "[Homebrew](https://brew.sh/)"
    html = "<p><a href=\"https://brew.sh/\">Homebrew</a></p>"
    assert_equal html, pipe_output(bin/"markdown", markdown, 0).chomp
  end
end
