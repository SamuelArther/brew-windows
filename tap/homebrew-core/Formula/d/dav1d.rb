class Dav1d < Formula
  desc "AV1 decoder targeted to be small and fast"
  homepage "https://code.videolan.org/videolan/dav1d"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dav1d-1.5.4-1-any.pkg.tar.zst"
  sha256 "ccf26c6f29fd36f5692d57fd5e0fe3becec492e0b875fd5e04f47a9582eb8b37"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://code.videolan.org/videolan/dav1d/-/archive/1.5.4/dav1d-1.5.4.tar.bz2"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/dav1d.exe", "dav1d"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dav1d --version 2>&1")
  end
end
