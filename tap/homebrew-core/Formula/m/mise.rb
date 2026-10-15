class Mise < Formula
  desc "Polyglot runtime manager (asdf rust clone)"
  homepage "https://mise.jdx.dev/"
  url "https://github.com/jdx/mise/releases/download/v2026.10.11/mise-v2026.10.11-windows-x64.zip"
  sha256 "2337f9fdb13ba51d3bf5831cacec9c59e70fec9630e9ad30f65975eb6b83b162"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/mise.exe", "mise"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mise --version")
  end
end
