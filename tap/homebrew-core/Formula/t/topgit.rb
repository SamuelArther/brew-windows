class Topgit < Formula
  desc "Git patch queue manager"
  homepage "https://repo.or.cz/topgit/pro.git"
  url "https://github.com/mackyle/topgit/archive/refs/tags/topgit-0.19.14.tar.gz"
  sha256 "0556485ca8ddf0cf863de4da36b11351545aca74fbf71581ffe9f5a5ce0718cb"
  license "GPL-2.0-only"

  deny_network_access!

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/topgit-0.19.14"
    sha256 cellar: :any, x86_64_windows: "3116eda482373fa4f131133f38bb179a36f837e25bae9bae720ed30939fb4c96"
  end

  def install
    system "make", "install", "prefix=#{prefix}"
  end

  test do
    ENV["EDITOR"] = "true"
    system "git", "init", "--initial-branch=main"
    system "git", "config", "user.name", "BrewTestBot"
    system "git", "config", "user.email", "brew@test.bot"
    (testpath/"file").write "hello\n"
    system "git", "add", "file"
    system "git", "commit", "-m", "Initial commit"

    system bin/"tg", "--no-pager", "create", "-m", "t/feature: add feature", "t/feature", "main"
    assert_equal "main", (testpath/".topdeps").read.strip
    assert_match "Subject: [PATCH] t/feature", (testpath/".topmsg").read
  end
end
