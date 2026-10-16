class GitCinnabar < Formula
  desc "Git remote helper to interact with mercurial repositories"
  homepage "https://github.com/glandium/git-cinnabar"
  url "https://github.com/glandium/git-cinnabar/releases/download/0.7.5/git-cinnabar.windows.x86_64.zip"
  sha256 "8b47c6709e62ab65254c76129436cfeb25eae468bdaea9f429b1ae420fcf000a"
  license all_of: ["MPL-2.0", "GPL-2.0-only"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://static.crates.io/crates/git-cinnabar/git-cinnabar-0.7.5.crate"
  end

  def install
    bin.install "git-cinnabar.exe"
    bin.install "git-remote-hg.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-cinnabar --version 2>&1")
  end
end
