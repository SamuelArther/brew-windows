class Xxhash < Formula
  desc "Extremely fast non-cryptographic hash algorithm"
  homepage "https://xxhash.com"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xxhash-0.8.4-1-any.pkg.tar.zst"
  sha256 "a22d39e85edd6f0b4c7fe34733d28f17d49715f6c8dea67c142161c02bfbbde2"
  license all_of: [
    "BSD-2-Clause", # library
    "GPL-2.0-or-later", # `xxhsum` command line utility
  ]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/Cyan4973/xxHash/archive/refs/tags/v0.8.4.tar.gz"
    strategy :github_latest
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/xxhsum.exe", "xxhsum"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/xxhsum --version 2>&1")
  end
end
