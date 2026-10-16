class QobineWeb < Formula
  desc "Server and web based player for Qobuz"
  homepage "https://github.com/SofusA/qobine"
  url "https://github.com/SofusA/qobine/releases/download/v2026-10-12/qobine-web-x86_64-pc-windows-msvc.zip"
  version "2026-10-12"
  sha256 "7fd1a5fa5a00b9cc37808481e7f0d2898eebee6c00e622e0067f402984443eb3"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sofusA/qobine/archive/refs/tags/v2026-10-12.tar.gz"
    regex(/^v?(\d+(?:[._-]\d+)+)$/i)
    strategy :github_latest
  end

  def install
    bin.install "qobine-web.exe"
  end

  test do
    # No version flag in this release; check the CLI loads and lists its commands.
    assert_match "Authenticate with Qobuz", shell_output("#{bin}/qobine-web --help")
  end
end
