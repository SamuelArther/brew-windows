class Speex < Formula
  desc "Audio codec designed for speech"
  homepage "https://speex.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-speex-1.2.1-1-any.pkg.tar.zst"
  sha256 "bed78c12555879a6463a6585b01fea72746143c0e98d7015ae99b8e9bb8daba4"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://ftp.osuosl.org/pub/xiph/releases/speex/?C=M&O=D"
    regex(%r{href=(?:["']?|.*?/)speex[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  resource "mingw-w64-ucrt-x86_64-libogg" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libogg-1.3.6-1-any.pkg.tar.zst"
    sha256 "110a34239e8f122da15e041b2601e2f3c6d4c909d738bdf978700601e0f64449"
  end

  resource "mingw-w64-ucrt-x86_64-speexdsp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-speexdsp-1.2.1-2-any.pkg.tar.zst"
    sha256 "397f3e3b7922cc7016c965ba42a67c22372ab8e0c4c144eddd26092a562acbf8"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/speexdec.exe", "speexdec"
    msys2_bin "ucrt64/bin/speexenc.exe", "speexenc"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/speexdec --version 2>&1")
  end
end
