class Esbuild < Formula
  desc "Extremely fast JavaScript bundler and minifier"
  homepage "https://esbuild.github.io/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-esbuild-0.28.2-1-any.pkg.tar.zst"
  sha256 "18f9c9944e2c8ffb78dad576e8f4880c77f9ae96be24801618388ba9ba954a09"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/evanw/esbuild/archive/refs/tags/v0.28.2.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/esbuild.exe", "esbuild"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/esbuild --version 2>&1")
  end
end
