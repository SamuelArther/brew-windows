class GitExtras < Formula
  desc "Small git utilities"
  homepage "https://github.com/tj/git-extras"
  url "https://github.com/tj/git-extras/archive/refs/tags/7.5.0.tar.gz"
  sha256 "bcfe0eabdccc806e53a10130fed6fb02373720ddeb670eecc5cc84d041d79880"
  license "MIT"
  head "https://github.com/tj/git-extras.git", branch: "main"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-extras-7.5.0"
    sha256 cellar: :any, x86_64_windows: "67a4ce51fc45cd7b90ad5958d5c90ac48280127e0a8989d3461f4c2f84c14489"
  end

  on_linux do
    depends_on "util-linux" # for `column`
  end

  conflicts_with "git-delete-merged-branches", because: "both install `git-delete-merged-branches` binaries"
  conflicts_with "git-ignore", because: "both install a `git-ignore` binary"
  conflicts_with "git-standup", because: "both install `git-standup` binaries"
  conflicts_with "git-sync", because: "both install a `git-sync` binary"
  conflicts_with "ugit", because: "both install `git-undo` binaries"

  def install
    system "make", "PREFIX=#{prefix}", "COMPL_DIR=#{bash_completion}", "INSTALL_VIA=brew", "install"
    fish_completion.install "etc/git-extras.fish"
    pkgshare.install "etc/git-extras-completion.zsh"
  end

  def caveats
    <<~EOS
      To load Zsh completions, add the following to your .zshrc:
        source #{opt_pkgshare}/git-extras-completion.zsh
    EOS
  end

  test do
    system "git", "init"
    assert_match(/#{testpath}/, shell_output("#{bin}/git-root"))
  end
end
