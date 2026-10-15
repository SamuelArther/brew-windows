class Lazygit < Formula
  desc "Simple terminal UI for git commands"
  homepage "https://github.com/jesseduffield/lazygit/"
  url "https://github.com/jesseduffield/lazygit/releases/download/v0.66.1/lazygit_0.66.1_windows_x86_64.zip"
  sha256 "1a23fb8aed2e757085ca0b1c63dd2a26a76618c5159e68cf7dd375154838cb2c"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/lazygit.exe", "lazygit"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lazygit -v")
  end
end
