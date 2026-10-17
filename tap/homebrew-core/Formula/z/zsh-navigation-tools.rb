class ZshNavigationTools < Formula
  desc "Zsh curses-based tools, e.g. multi-word history searcher"
  # Original homepage wiki.zshell.dev is down
  homepage "https://github.com/z-shell/zsh-navigation-tools"
  url "https://github.com/z-shell/zsh-navigation-tools/archive/refs/tags/v2.2.7.tar.gz"
  sha256 "ee832b81ce678a247b998675111c66aa1873d72aa33c2593a65626296ca685fc"
  license any_of: ["GPL-3.0-only", "MIT"]

  bottle do
    sha256 cellar: :any, x86_64_windows: "994cb92e5ea13dcd4ec5f962755bc965024cb00f8344f2acdab408121917585d"
  end

  uses_from_macos "zsh"

  deny_network_access!

  def install
    # Make the bottles uniform
    inreplace [".config/znt/n-cd.conf", "n-panelize"], "/usr/local", HOMEBREW_PREFIX

    system "make", "install", "PREFIX=#{prefix}"
  end

  def caveats
    <<~EOS
      To run zsh-navigation-tools, add the following at the end of your .zshrc:
        source #{HOMEBREW_PREFIX}/share/zsh-navigation-tools/zsh-navigation-tools.plugin.zsh

      You will also need to restart your terminal for this change to take effect.
    EOS
  end

  test do
    # This compiles package's main file
    # Zcompile is very capable of detecting syntax errors
    cp pkgshare/"n-list", testpath
    system "zsh", "-c", "zcompile n-list"
  end
end
