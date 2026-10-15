class Cbindgen < Formula
  desc "Project for generating C bindings from Rust code"
  homepage "https://github.com/mozilla/cbindgen"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cbindgen-0.29.4-1-any.pkg.tar.zst"
  sha256 "64069c8e30f35dabc3eadf1643dcfced3779711d4d3d865fb39c4190a2e4a96d"
  license "MPL-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/mozilla/cbindgen/archive/refs/tags/v0.29.4.tar.gz"
    strategy :github_latest
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/cbindgen.exe", "cbindgen"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cbindgen --version 2>&1")
  end
end
