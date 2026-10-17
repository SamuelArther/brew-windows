class ZshAutosuggestions < Formula
  desc "Fish-like fast/unobtrusive autosuggestions for zsh"
  homepage "https://github.com/zsh-users/zsh-autosuggestions"
  url "https://github.com/zsh-users/zsh-autosuggestions/archive/refs/tags/v0.7.1.tar.gz"
  sha256 "0df7affff21cd87ed298e6a3970ed08a1dd66a6efa676454ee5b091ad503badf"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "276d305cfd02f8da90d23a767b342a172fcd464bd418ecdef747436b2a76fec5"
  end

  uses_from_macos "zsh" => :test

  deny_network_access!

  def install
    pkgshare.install "zsh-autosuggestions.zsh"
  end

  def caveats
    <<~EOS
      To activate the autosuggestions, add the following at the end of your .zshrc:

        source #{HOMEBREW_PREFIX}/share/zsh-autosuggestions/zsh-autosuggestions.zsh

      You will also need to restart your terminal for this change to take effect.
    EOS
  end

  test do
    assert_match "history",
      shell_output("zsh -c '. #{pkgshare}/zsh-autosuggestions.zsh && echo $ZSH_AUTOSUGGEST_STRATEGY'")
  end
end
