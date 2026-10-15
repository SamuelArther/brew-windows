class Just < Formula
  desc "Handy way to save and run project-specific commands"
  homepage "https://just.systems"
  version "1.59.0"
  url "https://github.com/casey/just/releases/download/1.59.0/just-1.59.0-x86_64-pc-windows-msvc.zip"
  sha256 "9c5463e2d0974e31bee71346aa5cbf19167de1585a4abc1bae7b3b04604ea9c0"
  license "CC0-1.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/just.exe", "just"
  end

  test do
    assert_match "just #{version}", shell_output("#{bin}/just --version")
  end
end
