class GithubKeygen < Formula
  desc "Bootstrap GitHub SSH configuration"
  homepage "https://github.com/dolmen/github-keygen"
  url "https://github.com/dolmen/github-keygen/archive/refs/tags/v1.401.tar.gz"
  sha256 "0feb346de7927a3bcacadf2122b333041bb7b21b8262230265dc49a2d0f0b7ef"
  license "GPL-3.0-or-later"
  head "https://github.com/dolmen/github-keygen.git", branch: "release"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/github-keygen-1.401"
    sha256 cellar: :any, x86_64_windows: "9821857766adf01a0267d647f6a699df0f3c7abe416bf6ea1a70ed11663f3d43"
  end

  uses_from_macos "perl"

  def install
    bin.install "github-keygen"
  end

  test do
    system bin/"github-keygen"
  end
end
