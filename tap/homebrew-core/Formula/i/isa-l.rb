class IsaL < Formula
  desc "Intelligent Storage Acceleration Library"
  homepage "https://github.com/intel/isa-l"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-isa-l-2.31.1-1-any.pkg.tar.zst"
  sha256 "f57db6a91c606c4551fd0f96d00580b4897025e53b166ee749501e4173bd7e5e"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/intel/isa-l/archive/refs/tags/v2.32.1.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/igzip.exe", "igzip"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/igzip --version 2>&1")
  end
end
