class Vivid < Formula
  desc "Generator for LS_COLORS with support for multiple color themes"
  homepage "https://github.com/sharkdp/vivid"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-vivid-0.11.1-1-any.pkg.tar.zst"
  sha256 "4dd107032f67fb99bc0d161684fca24cafe1859f537ea9edfca87db57e7be580"
  license any_of: ["MIT", "Apache-2.0"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/sharkdp/vivid/archive/refs/tags/v0.11.1.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/vivid.exe", "vivid"
  end

  test do
    assert_includes shell_output("#{bin}/vivid preview molokai"), "archives.images: \e[4;38;2;249;38;114m*.bin\e[0m\n"
  end
end
