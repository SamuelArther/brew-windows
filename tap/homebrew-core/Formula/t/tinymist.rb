class Tinymist < Formula
  desc "Services for Typst"
  homepage "https://myriad-dreamin.github.io/tinymist/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tinymist-0.15.8-1-any.pkg.tar.zst"
  sha256 "8bc766cdea37639a3fdb86cd9c5768ee55f0411849471b26a6d487bbf6e34267"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/Myriad-Dreamin/tinymist/archive/refs/tags/v0.15.8.tar.gz"
    strategy :github_latest
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/tinymist.exe", "tinymist"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tinymist -V 2>&1")
  end
end
