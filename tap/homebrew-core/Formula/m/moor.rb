class Moor < Formula
  desc "Nice to use pager for humans"
  homepage "https://github.com/walles/moor"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-moor-2.19.4-1-any.pkg.tar.zst"
  sha256 "463de6383ac451333cb7e11d71e69ab2d96bb5f44d01cd530dcf4853723f1599"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/walles/moor/archive/refs/tags/v2.19.4.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/moor.exe", "moor"
  end

  test do
    # Test piping text through moor
    (testpath/"test.txt").write <<~EOS
      tyre kicking
    EOS
    assert_equal "tyre kicking", shell_output("#{bin}/moor test.txt").strip
  end
end
