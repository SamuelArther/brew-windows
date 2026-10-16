class GitBug < Formula
  desc "Distributed, offline-first bug tracker embedded in git, with bridges"
  homepage "https://github.com/git-bug/git-bug"
  url "https://github.com/git-bug/git-bug/releases/download/v0.11.0/git-bug_0.11.0_windows_amd64.zip"
  sha256 "6160ac777134d579ad5f8097dc3e4e70f1aab02fd8f4cb3b21bdf90c7e52843a"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/git-bug/git-bug/archive/refs/tags/v0.11.0.tar.gz"
  end

  def install
    bin.install "git-bug.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-bug version 2>&1")
  end
end
