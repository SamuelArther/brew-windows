class QobineTui < Formula
  desc "Tui player for Qobuz"
  homepage "https://github.com/SofusA/qobine"
  url "https://github.com/SofusA/qobine/releases/download/v2026-10-12/qobine-tui-x86_64-pc-windows-msvc.zip"
  version "2026-10-12"
  sha256 "51f62a4eac2d6809b76fe36557da39a0550d735cef59536128ae385de3cf1d19"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sofusA/qobine/archive/refs/tags/v2026-10-12.tar.gz"
    regex(/^v?(\d+(?:[._-]\d+)+)$/i)
    strategy :github_latest
  end

  def install
    bin.install "qobine-tui.exe"
  end

  test do
    system bin/"qobine-tui", "--help"
  end
end
