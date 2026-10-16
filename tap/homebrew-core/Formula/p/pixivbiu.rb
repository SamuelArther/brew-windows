class Pixivbiu < Formula
  desc "Pixiv client. Easy to search, browse, and download artworks"
  homepage "https://github.com/txperl/PixivBiu"
  url "https://github.com/txperl/PixivBiu/releases/download/v3.1.5/PixivBiu_3.1.5_windows_amd64.zip"
  sha256 "1b64956a67b29dee01cee1026aa63a8bfbdafd08cf465244255186c840561d83"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/txperl/PixivBiu/archive/refs/tags/v3.1.5.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "pixivbiu.exe"
  end

  test do
    system bin/"pixivbiu", "--help"
  end
end
