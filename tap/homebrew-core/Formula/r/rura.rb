class Rura < Formula
  desc "Interactive TUI scratchpad for building shell pipelines"
  homepage "https://github.com/tlipinski/rura"
  url "https://github.com/tlipinski/rura/releases/download/v1.13.0/rura-x86_64-pc-windows-msvc.zip"
  sha256 "d5812c6ab24db6c225954da6464481d1ac439c3eafceb1b803e2de70e9b84ace"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tlipinski/rura/archive/refs/tags/v1.13.0.tar.gz"
  end

  def install
    bin.install "rura.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rura --version 2>&1")
  end
end
