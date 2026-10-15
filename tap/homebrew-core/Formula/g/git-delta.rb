class GitDelta < Formula
  desc "Syntax-highlighting pager for git and diff output"
  homepage "https://dandavison.github.io/delta/"
  version "0.20.1"
  url "https://github.com/dandavison/delta/releases/download/0.20.1/delta-0.20.1-x86_64-pc-windows-msvc.zip"
  sha256 "c9af7484b33f1dbc1312a892fc7d5753a6d3365140a4000d49e575a2de742c25"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/delta.exe", "delta"
  end

  test do
    assert_match "delta #{version}", shell_output("#{bin}/delta --version")
  end
end
