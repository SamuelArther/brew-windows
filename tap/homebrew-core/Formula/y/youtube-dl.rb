class YoutubeDl < Formula
  desc "Download videos from supported websites"
  homepage "https://ytdl-org.github.io/youtube-dl/"
  url "https://github.com/ytdl-org/youtube-dl/releases/download/2021.12.17/youtube-dl.exe"
  sha256 "26e5c00c35c5c3edc86dfc0a720aed109a13b1b7c67ac654a0ce8ff82a1f2c16"
  license "Unlicense"

  # Actual youtube-dl, not yt-dlp. Upstream latest stable is old; modern site compatibility is not guaranteed.

  def install
    install_tree
    windows_exe "libexec/youtube-dl.exe", "youtube-dl"
  end
end
