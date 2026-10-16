class Zola < Formula
  desc "Fast static site generator in a single binary with everything built-in"
  homepage "https://www.getzola.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zola-0.23.6-1-any.pkg.tar.zst"
  sha256 "f48c13649bbbba10da5ba842b77d861d45a5eca1dc0537efbb0fa019f1764b70"
  license "EUPL-1.2"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/getzola/zola/archive/refs/tags/v0.23.6.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/zola.exe", "zola"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/zola --version 2>&1")
  end
end
