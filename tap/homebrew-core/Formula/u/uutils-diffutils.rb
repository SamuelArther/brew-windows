class UutilsDiffutils < Formula
  desc "Cross-platform Rust rewrite of the GNU diffutils"
  homepage "https://uutils.github.io/diffutils/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-uutils-diffutils-0.5.0-2-any.pkg.tar.zst"
  sha256 "d91a7e5c8edd41c046fac2426c2e482fa8f51128d8c9e66cb230bc4e7b593424"
  license any_of: ["Apache-2.0", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/uutils/diffutils/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/uu-cmp.exe", "uu-cmp"
    msys2_bin "ucrt64/bin/uu-diff.exe", "uu-diff"
  end

  test do
    system bin/"uu-cmp", "--help"
  end
end
