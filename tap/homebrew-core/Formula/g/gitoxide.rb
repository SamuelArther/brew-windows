class Gitoxide < Formula
  desc "Idiomatic, lean, fast & safe pure Rust implementation of Git"
  homepage "https://github.com/GitoxideLabs/gitoxide"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gitoxide-0.60.0-1-any.pkg.tar.zst"
  sha256 "9d4d0fec200d48804661545cf7a8b028c2a5f8e783b2adb2377907c7bd45b19f"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/GitoxideLabs/gitoxide/archive/refs/tags/v0.60.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/ein.exe", "ein"
    msys2_bin "ucrt64/bin/gix.exe", "gix"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ein --version 2>&1")
  end
end
