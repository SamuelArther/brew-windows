class Faac < Formula
  desc "ISO AAC audio encoder"
  homepage "https://freewareadvancedaudio.github.io"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-faac-2.1-1-any.pkg.tar.zst"
  sha256 "e80c28bce12df1fa08d64c33e2067d34c556ba8c8c6b772348e2f3fcab914401"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/FreewareAdvancedAudio/faac/archive/refs/tags/faac-2.2.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/faac.exe", "faac"
  end

  test do
    assert_path_exists bin/"faac.exe"
  end
end
