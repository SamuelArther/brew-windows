class Asmfmt < Formula
  desc "Go Assembler Formatter"
  homepage "https://github.com/klauspost/asmfmt"
  url "https://github.com/klauspost/asmfmt/releases/download/v1.3.2/asmfmt-Windows_x86_64_1.3.2.zip"
  sha256 "79166cda936582c0841ef55c2105ec1b13ab591b42fbb5535fce074dbab4bf23"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/klauspost/asmfmt/archive/refs/tags/v1.3.2.tar.gz"
  end

  def install
    bin.install "asmfmt.exe"
  end

  test do
    input = "  TEXT ·subVV(SB), NOSPLIT, $0\n// func subVV(z, x, y []Word) (c Word)"
    expected = "TEXT ·subVV(SB), NOSPLIT, $0\n\t// func subVV(z, x, y []Word) (c Word)\n"
    assert_equal expected, pipe_output(bin/"asmfmt", input, 0)
  end
end
