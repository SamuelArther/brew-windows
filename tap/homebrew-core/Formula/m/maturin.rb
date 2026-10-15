class Maturin < Formula
  desc "Build and publish Rust crates as Python packages"
  homepage "https://github.com/PyO3/maturin"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-maturin-1.15.0-1-any.pkg.tar.zst"
  sha256 "d5f2c3f70a6a3ef6060707a103e9e0022ea997f881490d7d5ddc781464e4dc75"
  license any_of: ["Apache-2.0", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/PyO3/maturin/archive/refs/tags/v1.15.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/maturin.exe", "maturin"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/maturin --version 2>&1")
  end
end
