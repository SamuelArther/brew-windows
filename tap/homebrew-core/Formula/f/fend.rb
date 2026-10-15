class Fend < Formula
  desc "Arbitrary-precision unit-aware calculator"
  homepage "https://printfn.github.io/fend"
  url "https://github.com/printfn/fend/releases/download/v1.5.8/fend-1.5.8-windows-x64-exe.zip"
  sha256 "b32a4ac9d8d10e998ad934f0a0b83e4a46b45225312dcd8ae510a067fa91789c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/printfn/fend/archive/refs/tags/v1.5.8.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "fend.exe"
  end

  test do
    assert_equal "1000 m", shell_output("#{bin}/fend 1 km to m").strip
  end
end
