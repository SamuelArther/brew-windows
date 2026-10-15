class Kondo < Formula
  desc "Save disk space by cleaning non-essential files from software projects"
  homepage "https://github.com/tbillington/kondo"
  url "https://github.com/tbillington/kondo/releases/download/v0.9/kondo-x86_64-pc-windows-msvc.zip"
  sha256 "4c70e7de85bfaf1b66de931654e9943681af42b093cd591530fc0dd4cc4a0f88"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tbillington/kondo/archive/refs/tags/v0.9.tar.gz"
  end

  def install
    bin.install "kondo.exe"
  end

  test do
    system bin/"kondo", "--help"
  end
end
