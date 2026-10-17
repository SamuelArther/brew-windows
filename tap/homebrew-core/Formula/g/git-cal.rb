class GitCal < Formula
  desc "GitHub-like contributions calendar but on the command-line"
  homepage "https://github.com/k4rthik/git-cal"
  url "https://github.com/k4rthik/git-cal/archive/refs/tags/v0.9.1.tar.gz"
  sha256 "783fa73197b349a51d90670480a750b063c97e5779a5231fe046315af0a946cd"
  license "MIT"
  revision 1
  head "https://github.com/k4rthik/git-cal.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-cal-0.9.1_1"
    sha256 cellar: :any, x86_64_windows: "1f283a229ddb736b9202a89d33bad128c7bc426b7915d9a4cdb46051613cf219"
  end

  def install
    system "perl", "Makefile.PL", "INSTALL_BASE=#{prefix}", "INSTALLSITEMAN1DIR=#{man1}", "MAN1EXT=1"
    system "make"
    system "make", "install"

    # Build an `:all` bottle
    chmod 0755, [bin, share, share/"man", man1] # permissions match
  end

  test do
    system "git", "init"
    (testpath/"Hello").write "Hello World!"
    system "git", "add", "Hello"
    system "git", "commit", "-a", "-m", "Initial Commit"
    system bin/"git-cal"
  end
end
