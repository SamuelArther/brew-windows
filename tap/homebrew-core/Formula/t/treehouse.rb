class Treehouse < Formula
  desc "Manage worktrees without managing worktrees"
  homepage "https://github.com/kunchenguid/treehouse"
  url "https://github.com/kunchenguid/treehouse/releases/download/v3.1.4/treehouse-v3.1.4-windows-amd64.zip"
  sha256 "0a745ca15f0a39257d55d0d94b01cab43a6c5d894ca00d1d5ac6753d4257cc94"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kunchenguid/treehouse/archive/refs/tags/v3.1.4.tar.gz"
  end

  def install
    bin.install "treehouse.exe"
  end

  test do
    system "git", "init", "--quiet"
    system bin/"treehouse", "init"
    assert_path_exists testpath/"treehouse.toml"
    assert_match "max_trees", (testpath/"treehouse.toml").read
  end
end
