class Tombi < Formula
  desc "TOML formatter, linter and language server"
  homepage "https://github.com/tombi-toml/tombi"
  url "https://github.com/tombi-toml/tombi/releases/download/v1.8.4/tombi-cli-1.8.4-x86_64-pc-windows-msvc.zip"
  sha256 "11809d18477d7ac952110dc3e8423533ff480915f780add1e589e1d27493b61e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tombi-toml/tombi/archive/refs/tags/v1.8.4.tar.gz"
    strategy :github_releases
  end

  def install
    install_tree
    windows_exe "libexec/tombi.exe", "tombi"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tombi --version 2>&1")
  end
end
