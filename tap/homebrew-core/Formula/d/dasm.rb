class Dasm < Formula
  desc "Macro assembler with support for several 8-bit microprocessors"
  homepage "https://dasm-assembler.github.io/"
  url "https://github.com/dasm-assembler/dasm/releases/download/v2.20.17/dasm-v2.20.17-windows.zip"
  sha256 "7bb4b51816ca19a6062c2b54c3210fbb4edcb0a4bde42815341a802ea729049a"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dasm-assembler/dasm/archive/refs/tags/v2.20.17.tar.gz"
  end

  def install
    bin.install "dasm.exe"
  end

  test do
    assert_path_exists bin/"dasm.exe"
  end
end
