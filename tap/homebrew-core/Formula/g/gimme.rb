class Gimme < Formula
  desc "Shell script to install any Go version"
  homepage "https://github.com/travis-ci/gimme"
  url "https://github.com/travis-ci/gimme/archive/refs/tags/v1.5.6.tar.gz"
  sha256 "50c42ec01505bee0e5b60165a0f577fe1e08fe9278fe3fe4b073c521f781c61e"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/gimme-1.5.6"
    sha256 cellar: :any, x86_64_windows: "37f2a3a9f44318753e0eeb37571213dbd76c835715e0504073a8770d88dcd544"
  end

  def install
    bin.install "gimme"
  end

  test do
    system bin/"gimme", "-l"
  end
end
