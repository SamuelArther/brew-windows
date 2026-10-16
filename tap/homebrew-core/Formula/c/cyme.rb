class Cyme < Formula
  desc "List system USB buses and devices"
  homepage "https://github.com/tuna-f1sh/cyme"
  url "https://github.com/tuna-f1sh/cyme/releases/download/v3.0.2/cyme-v3.0.2-x86_64-pc-windows-gnu.zip"
  sha256 "e40d3d77bec7c42378ece627be832ce0d2fd6b1a571cbab5317a3fa76c5e7944"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tuna-f1sh/cyme/archive/refs/tags/v3.0.2.tar.gz"
  end

  def install
    bin.install "cyme.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cyme --version 2>&1")
  end
end
