class Zxc < Formula
  desc "High-performance asymmetric lossless compression library"
  homepage "https://github.com/hellobertrand/zxc"
  url "https://github.com/hellobertrand/zxc/releases/download/v0.15.0/zxc-0.15.0-windows-x86_64.zip"
  sha256 "41b1e3ea854a4b54a8957725bbaf4bc8111e09a7914d283fa422281da17d8407"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hellobertrand/zxc/archive/refs/tags/v0.15.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/zxc.exe", "zxc"
  end

  test do
    input = "Hello world"
    compressed = pipe_output(bin/"zxc", input)
    refute_empty compressed
    decompressed = pipe_output("#{bin}/zxc -d", compressed)
    assert_equal input, decompressed
  end
end
