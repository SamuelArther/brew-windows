class Prek < Formula
  desc "Fast Git hook manager written in Rust, drop-in alternative to pre-commit"
  homepage "https://prek.j178.dev/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-prek-0.5.5-2-any.pkg.tar.zst"
  sha256 "c1a599dcb579438be64243088de1135fc6faf6dac9fa04a8d2290ef7a6037e1e"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/j178/prek/archive/refs/tags/v0.5.5.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/prek.exe", "prek"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/prek --version")

    output = shell_output("#{bin}/prek sample-config")
    assert_match "See https://prek.j178.dev for more information", output
  end
end
