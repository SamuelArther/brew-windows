class Mp3cat < Formula
  desc "Reads and writes mp3 files"
  homepage "https://tomclegg.ca/mp3cat"
  url "https://github.com/tomclegg/mp3cat/archive/refs/tags/0.5.tar.gz"
  sha256 "b1ec915c09c7e1c0ff48f54844db273505bc0157163bed7b2940792dca8ff951"
  license "GPL-2.0-or-later"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/mp3cat-0.5"
    sha256 cellar: :any, x86_64_windows: "9f798d83e6bec5fc67a1a943b5c8ff88c9c2ccbf2843c283c196a29a6782e33b"
  end

  def install
    system "make"
    bin.install %w[mp3cat mp3log mp3log-conf mp3dirclean mp3http mp3stream-conf]
  end

  test do
    pipe_output("#{bin}/mp3cat -v --noclean - -", test_fixtures("test.mp3").to_s)
  end
end
