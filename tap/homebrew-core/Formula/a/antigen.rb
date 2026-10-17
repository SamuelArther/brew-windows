class Antigen < Formula
  desc "Plugin manager for zsh, inspired by oh-my-zsh and vundle"
  homepage "https://github.com/zsh-users/antigen"
  url "https://github.com/zsh-users/antigen/releases/download/v2.2.3/v2.2.3.tar.gz"
  sha256 "bd3f1077050d52f459bc30fa3f025c44c528d625b4924a2f487fd2bacb89d61e"
  license "MIT"
  head "https://github.com/zsh-users/antigen.git", branch: "develop"

  # project has no commits and release since 2019
  # and some crash reports, https://github.com/zsh-users/antigen/issues/712
  # https://github.com/zsh-users/antigen/issues/753
  deprecate! date: "2025-11-22", because: :unmaintained
  disable! date: "2026-11-22", because: :unmaintained

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c910a650b62e93c124814fa45dd48319a2f44e84fa26dc896887e2f9e6fc6331"
  end

  uses_from_macos "zsh"

  def install
    pkgshare.install "bin/antigen.zsh"
  end

  def caveats
    <<~EOS
      To activate antigen, add the following to your ~/.zshrc:
        source #{HOMEBREW_PREFIX}/share/antigen/antigen.zsh
    EOS
  end

  test do
    (testpath/".zshrc").write "source #{HOMEBREW_PREFIX}/share/antigen/antigen.zsh\n"
    system "zsh", "--login", "-i", "-c", "antigen help"
  end
end
