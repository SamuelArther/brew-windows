class GitFresh < Formula
  desc "Utility to keep git repos fresh"
  homepage "https://github.com/imsky/git-fresh"
  url "https://github.com/imsky/git-fresh/archive/refs/tags/v1.13.0.tar.gz"
  sha256 "7043aaf2bf66dade7d06ebcf96e5d368c4910c002b7b00962bd2bd24490ce2dc"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-fresh-1.13.0"
    sha256 cellar: :any, x86_64_windows: "092788938d97a9a8963245ea626c218eaa40bf7b98749c1ce7039b2f7d6c2ad6"
  end

  def install
    system "./install.sh", bin
    man1.install "git-fresh.1"
  end

  test do
    ENV["GIT_CONFIG_GLOBAL"] = "#{testpath}/.gitconfig"

    system "git", "config", "--global", "init.defaultBranch", "master"
    system bin/"git-fresh", "-T"
  end
end
