class Kbt < Formula
  desc "Keyboard tester in terminal"
  homepage "https://github.com/bloznelis/kbt"
  url "https://github.com/bloznelis/kbt/releases/download/2.1.0/kbt-2.1.0-x86_64-pc-windows-gnu.zip"
  sha256 "429ae0b1caee25b70d6bafcb5ac506b76f3bb430677dec3f86ae26e970656ebb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bloznelis/kbt/archive/refs/tags/2.1.0.tar.gz"
  end

  def install
    bin.install "kbt.exe"
  end

  test do
    # kbt is a TUI application
    assert_match version.to_s, shell_output("#{bin}/kbt --version")
  end
end
