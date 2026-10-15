class Brush < Formula
  desc "Bourne RUsty SHell (command interpreter)"
  homepage "https://brush.sh"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-brush-0.4.0-2-any.pkg.tar.zst"
  sha256 "3d328ea1ec5686b2d389238779d16d51cab3f625f5f295e1cb37201fb6b30a17"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/reubeno/brush/archive/refs/tags/brush-shell-v0.4.0.tar.gz"
    regex(/brush-shell[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/brush.exe", "brush"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/brush --version 2>&1")
  end
end
