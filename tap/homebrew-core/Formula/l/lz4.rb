class Lz4 < Formula
  desc "Extremely Fast Compression algorithm"
  homepage "https://lz4.github.io/lz4/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lz4-1.10.0-1-any.pkg.tar.zst"
  sha256 "00a807df81a4b7e82445178b8625962ed592cf1f0a08a8e04c8b9338c4362638"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/lz4/lz4/archive/refs/tags/v1.10.0.tar.gz"
    strategy :github_latest
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/lz4.exe", "lz4"
    msys2_bin "ucrt64/bin/lz4c.exe", "lz4c"
    msys2_bin "ucrt64/bin/lz4cat.exe", "lz4cat"
    msys2_bin "ucrt64/bin/unlz4.exe", "unlz4"
  end

  test do
    input = "testing compression and decompression"
    compressed = pipe_output(bin/"lz4", input)
    refute_empty compressed
    decompressed = pipe_output("#{bin}/lz4 -d", compressed)
    assert_equal decompressed, input

    # Make sure lz4 executable is built multi-threaded
    assert_match "multithread", shell_output("#{bin}/lz4 -V")
  end
end
