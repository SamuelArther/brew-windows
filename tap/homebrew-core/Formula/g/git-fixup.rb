class GitFixup < Formula
  desc "Alias for git commit --fixup <ref>"
  homepage "https://github.com/keis/git-fixup"
  url "https://github.com/keis/git-fixup/archive/refs/tags/v1.6.1.tar.gz"
  sha256 "414d207687059094df9603c79d5e52343704e077b72777f25eda4d6ce291046e"
  license "ISC"
  head "https://github.com/keis/git-fixup.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-fixup-1.6.1"
    sha256 cellar: :any, x86_64_windows: "f645924efa404b98eb07378abfa22697dd8bbaf7915666d958320d5141e045f0"
  end

  def install
    system "make", "PREFIX=#{prefix}", "install"
    zsh_completion.install "completion.zsh" => "_git-fixup"
    fish_completion.install "completion.fish" => "git-fixup.fish"
  end

  test do
    system "git", "init"
    (testpath/"test").write "foo"
    system "git", "add", "test"
    system "git", "commit", "--message", "Initial commit"

    (testpath/"test").delete
    (testpath/"test").write "bar"
    system "git", "add", "test"
    system "git", "fixup"
  end
end
