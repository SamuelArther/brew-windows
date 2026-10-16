class YtDlp < Formula
  desc "Feature-rich command-line audio/video downloader"
  homepage "https://github.com/yt-dlp/yt-dlp"
  url "https://github.com/yt-dlp/yt-dlp/releases/download/2026.08.19/yt-dlp.exe"
  version "2026.8.19"
  sha256 "66674953fe251b89f4d08c5f0e35e0728679bd67ab3d7d05c0562af101dd3e7a"
  license "Unlicense"
  revision 2

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "yt-dlp.exe"
  end

  test do
    system bin/"yt-dlp", "--version"
  end
end
