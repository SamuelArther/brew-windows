class Gum < Formula
  desc "Tool for glamorous shell scripts"
  homepage "https://github.com/charmbracelet/gum"
  version "2.0.2"
  url "https://github.com/charmbracelet/gum/releases/download/v2.0.2/gum_2.0.2_Windows_x86_64.zip"
  sha256 "0397091dec9b4e8f00e02b90fd3eb07bf45acabbdb61d67437950f18e03a8b79"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/gum.exe", "gum"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gum --version")
  end
end
