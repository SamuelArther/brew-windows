class Hexyl < Formula
  desc "Command-line hex viewer"
  homepage "https://github.com/sharkdp/hexyl"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-hexyl-0.17.0-1-any.pkg.tar.zst"
  sha256 "abb6d6e72ad69cdd521cbbd2d7115809ce308b3ee52c9ab7556b4ec77800029a"
  license any_of: ["Apache-2.0", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/sharkdp/hexyl/archive/refs/tags/v0.17.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/hexyl.exe", "hexyl"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hexyl --version 2>&1")
  end
end
