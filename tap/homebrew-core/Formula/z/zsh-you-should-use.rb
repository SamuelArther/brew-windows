class ZshYouShouldUse < Formula
  desc "ZSH plugin that reminds you to use existing aliases for commands you just typed"
  homepage "https://github.com/MichaelAquilina/zsh-you-should-use"
  url "https://github.com/MichaelAquilina/zsh-you-should-use/archive/refs/tags/1.11.1.tar.gz"
  sha256 "4d884aed12f1301f4620888e322c406c215b719e49385aa3088dc84c8ed383ed"
  license "GPL-3.0-only"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2377f9753a860f9dbb638af9116a40b386d4a0a331279d1dd8b0304c695a7261"
  end

  uses_from_macos "zsh"

  deny_network_access!

  def install
    pkgshare.install "you-should-use.plugin.zsh"
  end

  def caveats
    <<~EOS
      To activate You Should Use, add the following to your .zshrc:

        source #{HOMEBREW_PREFIX}/share/zsh-you-should-use/you-should-use.plugin.zsh

      You will also need to restart your terminal for this change to take effect.
    EOS
  end

  test do
    assert_match version.to_s,
      shell_output("zsh -c '. #{pkgshare}/you-should-use.plugin.zsh && " \
                   "echo $YSU_VERSION'")
  end
end
