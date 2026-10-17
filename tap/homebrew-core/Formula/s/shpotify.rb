class Shpotify < Formula
  desc "Command-line interface for Spotify on a Mac"
  homepage "https://harishnarayanan.org/projects/shpotify/"
  url "https://github.com/hnarayanan/shpotify/archive/refs/tags/2.1.tar.gz"
  sha256 "b41d8798687be250d0306ac0c5a79420fa46619c5954286711a5d63c86a6c071"
  license "MIT"
  head "https://github.com/hnarayanan/shpotify.git", branch: "main"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/shpotify-2.1"
    sha256 cellar: :any, x86_64_windows: "0353ed446ba32f9943376b8520a8688b22b9be708c08ac2fdbf1a2d84304a058"
  end

  def install
    bin.install "spotify"
  end

  test do
    system bin/"spotify"
  end
end
