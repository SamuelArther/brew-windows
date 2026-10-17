class Semtag < Formula
  desc "Semantic tagging script for git"
  homepage "https://github.com/nico2sh/semtag"
  url "https://github.com/nico2sh/semtag/archive/refs/tags/v0.2.0.tar.gz"
  sha256 "37890fc5bbdeba923b51ff615c4e99b002c95d922925ed1efefd117ad7934548"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "30074886759baa9fd83fa65ce3318d6f52bf71323ef85fa965d629bb28bef78c"
  end

  on_macos do
    depends_on "bash" # Needs Bash 4.x>
  end

  def install
    bin.install "semtag"
  end

  test do
    touch "file.txt"
    system "git", "init"
    system "git", "add", "file.txt"
    system "git", "commit", "-m'test'"
    system bin/"semtag", "final"
    output = shell_output("git tag --list")
    assert_match "v0.0.1", output
  end
end
