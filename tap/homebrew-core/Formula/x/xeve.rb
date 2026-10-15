class Xeve < Formula
  desc "Very fast Essential Video Encoder, MPEG-5 EVC (Essential Video Coding)"
  homepage "https://github.com/mpeg5/xeve"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xeve-0.5.1-2-any.pkg.tar.zst"
  sha256 "dd4211e7a4460a1b26d93cc51b270bc8b9011898fe433518d33caad1d1361201"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/mpeg5/xeve/archive/refs/tags/v0.7.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/xeve_app.exe", "xeve_app"
  end

  test do
    system bin/"xeve_app", "--help"
  end
end
