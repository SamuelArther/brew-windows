class WgslAnalyzer < Formula
  desc "Language server implementation for WGSL and WESL"
  homepage "https://wgsl-analyzer.github.io"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wgsl-analyzer-20260426-1-any.pkg.tar.zst"
  version "20260426"
  sha256 "f802fc0156447bc74e7e755052e920757ebb7a9831e14e3d7058fd4ee05a33b9"
  license any_of: ["Apache-2.0", "MIT"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/wgsl-analyzer/wgsl-analyzer/archive/refs/tags/2026-09-30.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/wgsl-analyzer.exe", "wgsl-analyzer"
  end

  test do
    system bin/"wgsl-analyzer", "--version"
  end
end
