class GitStandup < Formula
  desc "Git extension to generate reports for standup meetings"
  homepage "https://github.com/nilbuild/git-standup"
  url "https://github.com/nilbuild/git-standup/archive/refs/tags/2.3.2.tar.gz"
  sha256 "48d5aaa3c585037c950fa99dd5be8a7e9af959aacacde9fe94143e4e0bfcd6ba"
  license "MIT"
  head "https://github.com/nilbuild/git-standup.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-standup-2.3.2"
    sha256 cellar: :any, x86_64_windows: "8b433d0890e0675db153fcda0a037cc20c356e3f79cc9c11b3dbc7b7138c2e7e"
  end

  conflicts_with "git-extras", because: "both install `git-standup` binaries"

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    system "git", "init"
    (testpath/"test").write "test"
    system "git", "add", testpath/"test"
    system "git", "commit", "--message", "test"
    system "git", "standup"
  end
end
