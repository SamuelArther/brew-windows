class Md4c < Formula
  desc "C Markdown parser. Fast. SAX-like interface"
  homepage "https://github.com/mity/md4c"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-md4c-0.6.0-1-any.pkg.tar.zst"
  sha256 "a8e5aa7b84ba26047dad56301e821fb4efbed4ced9fce83cd86377a175172476"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/mity/md4c/archive/refs/tags/v0.6.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/md2html.exe", "md2html"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/md2html --version 2>&1")
  end
end
