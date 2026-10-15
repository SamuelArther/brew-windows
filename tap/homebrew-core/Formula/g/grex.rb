class Grex < Formula
  desc "Command-line tool for generating regular expressions"
  homepage "https://pemistahl.github.io/grex-js/"
  url "https://github.com/pemistahl/grex/releases/download/v1.4.6/grex-v1.4.6-x86_64-pc-windows-msvc.zip"
  sha256 "7691dbb8e46339a15d8362b79b2456f508266cf00a55fe685ed69a6259577485"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pemistahl/grex/archive/refs/tags/v1.4.6.tar.gz"
  end

  def install
    bin.install "grex.exe"
  end

  test do
    output = shell_output("#{bin}/grex a b c")
    assert_match "^[a-c]$\n", output
  end
end
