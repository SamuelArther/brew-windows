class Fzf < Formula
  desc "Command-line fuzzy finder written in Go"
  homepage "https://junegunn.github.io/fzf/"
  version "0.74.5"
  url "https://github.com/junegunn/fzf/releases/download/v0.74.5/fzf-0.74.5-windows_amd64.zip"
  sha256 "01f854ec8b9b47c99a7d78023638f7d2056cfb0af56b72dabd112032eed3993e"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/fzf.exe", "fzf"
  end

  test do
    assert_equal "world", pipe_output("#{bin}/fzf -f wld", "hello\nworld\n").chomp
  end
end
